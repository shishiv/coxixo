# Phase 4: Polish - Research

**Researched:** 2026-01-18
**Domain:** WinForms UI theming, tray icon customization, settings form
**Confidence:** HIGH

## Summary

Phase 4 focuses on applying the Coxixo brand identity to the application and building a settings UI. The existing codebase already has embedded icons (`icon-idle.ico`, `icon-recording.ico`) and a placeholder for the Settings click handler. This phase will replace placeholder icons with brand-compliant designs, implement tray icon animation for recording state, build a dark-themed Settings form with hotkey picker and API configuration, and add API connection status display.

The approach is straightforward WinForms development without third-party UI libraries. For dark theming, manual BackColor/ForeColor setting is recommended over .NET 9 experimental APIs since the project targets .NET 8. Icon animation uses a standard Timer pattern to cycle between icon frames.

**Primary recommendation:** Use manual dark theme styling (BackColor/ForeColor) for .NET 8 compatibility. Create multi-state icons as embedded resources. Implement hotkey picker using TextBox with KeyDown event capture.

## Standard Stack

The established libraries/tools for this domain:

### Core (Already in Project)
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| System.Windows.Forms | .NET 8 | UI framework | Native WinForms for tray apps |
| System.Drawing | .NET 8 | Icon/graphics | Built-in icon manipulation |

### Supporting (No New Dependencies Needed)
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| System.Windows.Forms.Timer | Built-in | Icon animation | Pulsing recording indicator |
| System.Drawing.Bitmap | Built-in | Generate icons | Programmatic icon creation if needed |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Manual dark theme | .NET 9 `Application.SetColorMode()` | Requires upgrading to .NET 9, API is experimental |
| Manual dark theme | DarkNet NuGet | Only themes title bar, not controls |
| Manual dark theme | Dark-Mode-Forms library | Adds dependency, may conflict with manual styling |
| Embedded .ico files | Programmatic icon generation | More complex, harder to design |

**Note:** No new package installations needed. Everything required is already available.

## Architecture Patterns

### Recommended Project Structure
```
Coxixo/
├── Forms/
│   └── SettingsForm.cs           # New settings window
│   └── SettingsForm.Designer.cs  # Designer partial
├── Resources/
│   ├── icon-idle.ico             # Existing (replace with brand design)
│   ├── icon-recording.ico        # Existing (replace with brand design)
│   ├── icon-recording-1.ico      # New (animation frame 1)
│   ├── icon-recording-2.ico      # New (animation frame 2) - optional
│   ├── icon-processing.ico       # New (transcribing state)
│   └── icon-error.ico            # New (error/unconfigured state)
├── Services/
│   └── (existing services)
└── TrayApplicationContext.cs     # Modify for animation + settings
```

### Pattern 1: Manual Dark Theme
**What:** Set BackColor/ForeColor explicitly on all controls
**When to use:** .NET 8 applications needing dark UI
**Example:**
```csharp
// Dark theme colors from brand guide
private static class DarkTheme
{
    public static readonly Color Background = Color.FromArgb(0x1E, 0x1E, 0x1E);  // #1E1E1E
    public static readonly Color Surface = Color.FromArgb(0x25, 0x25, 0x26);     // #252526
    public static readonly Color Text = Color.White;
    public static readonly Color TextMuted = Color.FromArgb(0x80, 0x80, 0x80);
    public static readonly Color Border = Color.FromArgb(0x3C, 0x3C, 0x3C);
    public static readonly Color Primary = Color.FromArgb(0x00, 0x78, 0xD4);     // #0078D4 Azure Blue
    public static readonly Color Success = Color.FromArgb(0x00, 0xCC, 0x6A);     // #00CC6A
    public static readonly Color Error = Color.FromArgb(0xE8, 0x11, 0x23);       // Red
}

private void ApplyDarkTheme(Control control)
{
    control.BackColor = DarkTheme.Background;
    control.ForeColor = DarkTheme.Text;

    foreach (Control child in control.Controls)
    {
        ApplyDarkTheme(child);
    }
}
```

### Pattern 2: Timer-Based Icon Animation
**What:** Use System.Windows.Forms.Timer to cycle through icon frames
**When to use:** Recording state pulsing indicator
**Example:**
```csharp
private System.Windows.Forms.Timer? _animationTimer;
private Icon[] _recordingFrames = null!;
private int _currentFrame = 0;

private void StartRecordingAnimation()
{
    _animationTimer = new System.Windows.Forms.Timer();
    _animationTimer.Interval = 500; // 500ms between frames
    _animationTimer.Tick += (s, e) =>
    {
        _currentFrame = (_currentFrame + 1) % _recordingFrames.Length;
        _trayIcon.Icon = _recordingFrames[_currentFrame];
    };
    _animationTimer.Start();
}

private void StopRecordingAnimation()
{
    _animationTimer?.Stop();
    _animationTimer?.Dispose();
    _animationTimer = null;
    _trayIcon.Icon = _idleIcon;
    _currentFrame = 0;
}
```

### Pattern 3: Hotkey Picker with KeyDown Capture
**What:** TextBox that captures a single keypress and displays it
**When to use:** Allowing user to configure the push-to-talk key
**Example:**
```csharp
private void HotkeyTextBox_KeyDown(object sender, KeyEventArgs e)
{
    e.Handled = true;
    e.SuppressKeyPress = true;

    // Ignore modifier-only presses
    if (e.KeyCode == Keys.ControlKey || e.KeyCode == Keys.ShiftKey ||
        e.KeyCode == Keys.Menu || e.KeyCode == Keys.LWin || e.KeyCode == Keys.RWin)
        return;

    // Store the selected key
    _selectedKey = e.KeyCode;
    hotkeyTextBox.Text = _selectedKey.ToString();
}
```

### Pattern 4: API Health Check with Latency
**What:** Simple HTTP request to measure connection latency
**When to use:** Settings window status indicator
**Example:**
```csharp
private async Task<(bool connected, int latencyMs)> TestConnectionAsync(string endpoint, string apiKey)
{
    if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey))
        return (false, 0);

    var stopwatch = Stopwatch.StartNew();
    try
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("api-key", apiKey);
        client.Timeout = TimeSpan.FromSeconds(5);

        // HEAD request to endpoint (lightweight check)
        var response = await client.GetAsync($"{endpoint.TrimEnd('/')}/openai/deployments?api-version=2024-02-01");
        stopwatch.Stop();

        return (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NotFound,
                (int)stopwatch.ElapsedMilliseconds);
    }
    catch
    {
        return (false, 0);
    }
}
```

### Anti-Patterns to Avoid
- **Using .NET 9 experimental dark mode APIs in .NET 8 project:** Will not compile
- **Creating icons at runtime from Bitmap without proper cleanup:** Memory leaks from GDI handles
- **Blocking UI thread during API connection test:** Use async/await properly
- **Using Form.ShowDialog() from tray context:** Can cause focus issues; use Show() with manual activation

## Don't Hand-Roll

Problems that look simple but have existing solutions:

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Icon files | Generate at runtime | Pre-designed .ico files | Design tools create better icons, simpler code |
| Key name display | Custom switch statement | `Keys.ToString()` | Built-in enum formatting |
| Timer for animation | Thread.Sleep loops | `System.Windows.Forms.Timer` | Thread-safe, integrates with message loop |
| Color constants | Magic numbers | Named color class | Maintainable, matches brand guide |

**Key insight:** Icon design should be done in a design tool (Figma, GIMP, etc.) and embedded as resources. Runtime icon generation is complex and yields inferior results.

## Common Pitfalls

### Pitfall 1: GDI Handle Leaks with Icons
**What goes wrong:** Creating Icon from Bitmap.GetHicon() without DestroyIcon causes handle leak
**Why it happens:** GetHicon() returns unmanaged Windows handle that must be freed
**How to avoid:** If generating icons at runtime, call DestroyIcon via P/Invoke after creating Icon
**Warning signs:** Memory/handle count growing over time, eventual GDI resource exhaustion

### Pitfall 2: Settings Form Shown Behind Other Windows
**What goes wrong:** Form opens but appears behind current foreground window
**Why it happens:** Windows activation rules for background processes
**How to avoid:** Use `form.Show()` then `form.Activate()` and `form.BringToFront()`
**Warning signs:** User clicks Settings but nothing seems to happen

### Pitfall 3: KeyDown Not Capturing All Keys
**What goes wrong:** Some keys (Tab, Enter, arrows) don't trigger KeyDown
**Why it happens:** WinForms processes these as navigation keys
**How to avoid:** Override `IsInputKey()` to return true for desired keys, or use `PreviewKeyDown`
**Warning signs:** Hotkey picker ignores certain keys user wants to use

### Pitfall 4: Timer Tick After Form Disposed
**What goes wrong:** ObjectDisposedException when animation timer fires after form closes
**Why it happens:** Timer continues running after form disposal
**How to avoid:** Stop and dispose timer in Form.FormClosing event
**Warning signs:** Random exceptions, especially when rapidly opening/closing settings

### Pitfall 5: Credentials Not Masked in TextBox
**What goes wrong:** API key visible as plaintext
**Why it happens:** Forgot to set `UseSystemPasswordChar = true`
**How to avoid:** Set `UseSystemPasswordChar = true` or `PasswordChar = '*'`
**Warning signs:** Security review finds credential exposure

### Pitfall 6: Saving Settings While Hotkey Hook Active
**What goes wrong:** Changing hotkey setting doesn't take effect
**Why it happens:** KeyboardHookService still using old key
**How to avoid:** Update `_hotkeyService.TargetKey` after saving settings, or restart hook
**Warning signs:** User changes hotkey but old key still works

## Code Examples

Verified patterns from official sources and existing codebase:

### Loading Embedded Icon (Current Pattern)
```csharp
// Source: TrayApplicationContext.cs lines 63-74
private Icon LoadEmbeddedIcon(string resourceName)
{
    var assembly = Assembly.GetExecutingAssembly();
    using var stream = assembly.GetManifestResourceStream(resourceName);
    if (stream == null)
    {
        // Fallback to application icon
        return Icon.ExtractAssociatedIcon(Application.ExecutablePath)
               ?? SystemIcons.Application;
    }
    return new Icon(stream);
}
```

### Settings Form Template
```csharp
// Based on brand mockup in coxixo-brand-guides.html
public partial class SettingsForm : Form
{
    public SettingsForm()
    {
        InitializeComponent();

        // Form setup
        this.FormBorderStyle = FormBorderStyle.FixedSingle;
        this.MaximizeBox = false;
        this.MinimizeBox = true;
        this.StartPosition = FormStartPosition.CenterScreen;
        this.Text = "Coxixo Settings";
        this.Size = new Size(320, 400);

        ApplyDarkTheme(this);
    }
}
```

### Context Menu Update (Current Pattern to Extend)
```csharp
// Source: TrayApplicationContext.cs lines 76-83
private ContextMenuStrip CreateContextMenu()
{
    var menu = new ContextMenuStrip();
    menu.Items.Add("Settings...", null, OnSettingsClick);
    menu.Items.Add(new ToolStripSeparator());
    menu.Items.Add("Exit", null, OnExitClick);
    return menu;
}
```

### Tray Icon Tooltip States
```csharp
// Recommended tooltip patterns for different states
_trayIcon.Text = $"Coxixo - Press {_settings.HotkeyKey} to talk";  // Idle
_trayIcon.Text = "Coxixo - Recording...";                          // Recording
_trayIcon.Text = "Coxixo - Transcribing...";                       // Processing
_trayIcon.Text = "Coxixo - Configure API in Settings";             // Unconfigured
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Manual recursive dark theming | .NET 9 `Application.SetColorMode()` | .NET 9 (Nov 2024) | Simpler, but experimental and requires .NET 9 |
| `RegisterHotKey` Win32 API | WH_KEYBOARD_LL hook | N/A | Already using modern hook approach |
| Custom icon encoder | Pre-designed embedded .ico | N/A | Design-time icons are standard practice |

**Note:** This project uses .NET 8, so manual dark theming is the correct approach. The .NET 9 dark mode API is experimental and would require framework upgrade.

**.NET 9 Dark Mode (for reference, NOT for use):**
```csharp
// Only available in .NET 9+, experimental
Application.SetColorMode(SystemColorMode.Dark);  // WFO5001 warning
```

## Open Questions

Things that couldn't be fully resolved:

1. **Icon Design Specifics**
   - What we know: Brand guide specifies 3 bars forming "C" shape with green dot
   - What's unclear: Exact pixel-level design for 16x16 tray icons
   - Recommendation: Use design tool to create .ico files, test at actual tray size

2. **Animation Frame Count**
   - What we know: Pulsing effect needed for recording state
   - What's unclear: Whether 2 frames (on/off) or more gradual animation
   - Recommendation: Start with 2 frames (icon + icon-with-red-dot), can add more later

3. **API Health Check Endpoint**
   - What we know: Need to verify credentials and measure latency
   - What's unclear: Best Azure endpoint to ping that's lightweight
   - Recommendation: Use `/openai/deployments` endpoint (returns 200 or 404 based on deployment)

## Sources

### Primary (HIGH confidence)
- Current codebase: `TrayApplicationContext.cs`, `ConfigurationService.cs`, `KeyboardHookService.cs`
- Brand guide: `coxixo-brand-guides.html`
- [Microsoft Learn: NotifyIcon.Icon Property](https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.notifyicon.icon)
- [Microsoft Learn: Control.KeyDown Event](https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.control.keydown)
- [Microsoft Learn: Check Modifier Keys](https://learn.microsoft.com/en-us/dotnet/desktop/winforms/input-keyboard/how-to-check-modifier-key)

### Secondary (MEDIUM confidence)
- [Microsoft Learn: .NET 9 WinForms Dark Mode](https://learn.microsoft.com/en-us/dotnet/desktop/winforms/whats-new/net90) - Confirmed experimental status
- [Microsoft Learn: Bitmap.GetHicon](https://learn.microsoft.com/en-us/dotnet/api/system.drawing.bitmap.gethicon) - For runtime icon generation if needed
- [C# Helper: Multi-image Icons](https://www.csharphelper.com/howtos/howto_make_icon.html) - Icon file format reference

### Tertiary (LOW confidence)
- WebSearch findings on dark mode libraries (DarkNet, Dark-Mode-Forms) - Not recommended due to complexity
- Community patterns for timer-based animation - Standard but implementation varies

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - No new dependencies, using built-in .NET 8 APIs
- Architecture: HIGH - Standard WinForms patterns, existing codebase patterns established
- Pitfalls: HIGH - Well-documented issues with GDI handles, timer disposal
- Dark theming: MEDIUM - Manual approach is reliable but verbose; .NET 9 API exists but not applicable

**Research date:** 2026-01-18
**Valid until:** 60 days (stable APIs, no expected breaking changes)

---

## Recommendations for Planning

Based on this research, the planner should structure tasks as follows:

1. **Icon Creation (Design Task)** - Create brand-compliant .ico files outside of code
2. **Settings Form UI** - Build form with dark theme, layout matching mockup
3. **Hotkey Picker** - Implement TextBox-based key capture
4. **API Connection Test** - Add async latency check to Settings
5. **Tray Icon Animation** - Timer-based recording state animation
6. **Integration** - Wire Settings form to save/load, update tray icon states

Each task is relatively independent and can be verified in isolation before integration.
