# Phase 1: Foundation - Research

**Researched:** 2026-01-17
**Domain:** .NET 8 WinForms system tray application, global hotkey detection, secure credential storage
**Confidence:** HIGH

## Summary

Phase 1 establishes the application shell with system tray presence and push-to-talk hotkey detection. The research covers six key areas: ApplicationContext-based tray apps, NotifyIcon best practices, global hotkey detection for push-to-talk, DPAPI credential encryption, configuration persistence, and memory optimization.

The critical finding for this phase is that **RegisterHotKey API cannot detect key release**, making a low-level keyboard hook (WH_KEYBOARD_LL) mandatory for push-to-talk functionality. The hook must capture both WM_KEYDOWN and WM_KEYUP messages to implement hold-to-record behavior.

**Primary recommendation:** Use ApplicationContext pattern for formless tray app, implement custom low-level keyboard hook for push-to-talk, use DPAPI with entropy for API key storage, and store non-sensitive settings in JSON file in AppData.

## Standard Stack

The established libraries/tools for this phase:

### Core

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| .NET 8 | 8.0+ | Runtime & language | LTS, DATAS GC, Native AOT ready |
| Windows Forms | Built-in | System tray / NotifyIcon | Lightest weight, NotifyIcon native |
| System.Security.Cryptography | Built-in | DPAPI ProtectedData | Windows credential encryption |
| System.Text.Json | Built-in | Configuration serialization | Built-in, no dependencies |

### Supporting

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| Microsoft.Extensions.Configuration.Json | 8.0+ | Read appsettings.json | If using IConfiguration pattern |
| SharpHook | Latest | Cross-platform keyboard hook | Alternative to manual P/Invoke (adds ~500KB) |

### Alternatives Considered

| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Manual P/Invoke hook | NonInvasiveKeyboardHook | Simpler API but may not support key-up events |
| Manual P/Invoke hook | SharpHook | Cross-platform, clean API, but adds dependency (~500KB) |
| DPAPI | ASP.NET Core Data Protection | Overkill for desktop app, designed for web |

**Installation:**

```bash
# Core - no additional packages needed for Phase 1
dotnet new winforms -n Coxixo -f net8.0-windows

# Optional: If using IConfiguration
dotnet add package Microsoft.Extensions.Configuration.Json
```

## Architecture Patterns

### Recommended Project Structure

```
Coxixo/
├── Program.cs                      # Entry point, Application.Run(TrayApplicationContext)
├── TrayApplicationContext.cs       # ApplicationContext subclass, owns NotifyIcon
├── Services/
│   ├── KeyboardHookService.cs      # Low-level keyboard hook for push-to-talk
│   └── ConfigurationService.cs     # Settings load/save
├── Models/
│   └── AppSettings.cs              # Strongly-typed settings model
├── Resources/
│   ├── icon-idle.ico               # 16x16 and 32x32 combined
│   └── icon-recording.ico          # Recording state icon
└── appsettings.json                # Non-sensitive defaults (optional)
```

### Pattern 1: ApplicationContext for Formless Tray App

**What:** Run a Windows Forms application without a visible form, using only system tray presence.

**When to use:** Tray-only applications that don't need a main window.

**Example:**

```csharp
// Program.cs
[STAThread]
static void Main()
{
    Application.EnableVisualStyles();
    Application.SetCompatibleTextRenderingDefault(false);
    Application.Run(new TrayApplicationContext());
}

// TrayApplicationContext.cs
public class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _trayIcon;
    private readonly KeyboardHookService _hotkeyService;

    public TrayApplicationContext()
    {
        // Initialize tray icon FIRST (gives visual feedback)
        _trayIcon = new NotifyIcon
        {
            Icon = Resources.IconIdle,
            Text = "Coxixo - Push to Talk",
            Visible = true,
            ContextMenuStrip = CreateContextMenu()
        };

        // Initialize hotkey service
        _hotkeyService = new KeyboardHookService();
        _hotkeyService.HotkeyPressed += OnHotkeyPressed;
        _hotkeyService.HotkeyReleased += OnHotkeyReleased;
        _hotkeyService.Start();

        // Handle application exit
        Application.ApplicationExit += OnApplicationExit;
    }

    private void OnApplicationExit(object? sender, EventArgs e)
    {
        CleanupTrayIcon();
    }

    private void CleanupTrayIcon()
    {
        _trayIcon.Visible = false;
        _trayIcon.Icon = null;  // Required on Windows 7+
        _trayIcon.Dispose();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            CleanupTrayIcon();
            _hotkeyService?.Dispose();
        }
        base.Dispose(disposing);
    }
}
```

**Source:** [Creating Tray Applications in .NET - Simple Talk](https://www.red-gate.com/simple-talk/development/dotnet-development/creating-tray-applications-in-net-a-practical-guide/)

### Pattern 2: Low-Level Keyboard Hook for Push-to-Talk

**What:** Use WH_KEYBOARD_LL hook to detect both key press and key release for hold-to-record.

**When to use:** Push-to-talk requires detecting when user releases the key, not just when they press it.

**Example:**

```csharp
// KeyboardHookService.cs
public sealed class KeyboardHookService : IDisposable
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int WM_SYSKEYUP = 0x0105;

    public event EventHandler? HotkeyPressed;
    public event EventHandler? HotkeyReleased;

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
    private readonly LowLevelKeyboardProc _proc;
    private IntPtr _hookId = IntPtr.Zero;
    private Keys _targetKey = Keys.F8;  // Configurable
    private bool _isKeyDown = false;

    public KeyboardHookService()
    {
        _proc = HookCallback;
    }

    public void Start()
    {
        _hookId = SetHook(_proc);
        if (_hookId == IntPtr.Zero)
            throw new InvalidOperationException("Failed to install keyboard hook");
    }

    private IntPtr SetHook(LowLevelKeyboardProc proc)
    {
        using var curProcess = Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule!;
        return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
            GetModuleHandle(curModule.ModuleName), 0);
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int vkCode = Marshal.ReadInt32(lParam);
            var key = (Keys)vkCode;

            if (key == _targetKey)
            {
                var msg = wParam.ToInt32();

                // Key pressed (not already down - prevents repeat)
                if ((msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN) && !_isKeyDown)
                {
                    _isKeyDown = true;
                    HotkeyPressed?.Invoke(this, EventArgs.Empty);
                }
                // Key released
                else if ((msg == WM_KEYUP || msg == WM_SYSKEYUP) && _isKeyDown)
                {
                    _isKeyDown = false;
                    HotkeyReleased?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        if (_hookId != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
        }
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook,
        LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
        IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);
}
```

**Source:** [Low-Level Keyboard Hook in C# - Microsoft Learn](https://learn.microsoft.com/en-us/archive/blogs/toub/low-level-keyboard-hook-in-c)

### Pattern 3: DPAPI Credential Storage

**What:** Use Windows DPAPI to encrypt sensitive data (API keys) tied to the current user account.

**When to use:** Storing API credentials that must persist but should be encrypted.

**Example:**

```csharp
// CredentialService.cs
using System.Security.Cryptography;
using System.Text;

public static class CredentialService
{
    private static readonly string CredentialsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Coxixo",
        "credentials.dat"
    );

    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("Coxixo.v1.Entropy");

    public static void SaveApiKey(string apiKey)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(CredentialsPath)!);

        byte[] plaintext = Encoding.UTF8.GetBytes(apiKey);
        byte[] encrypted = ProtectedData.Protect(
            plaintext,
            Entropy,
            DataProtectionScope.CurrentUser
        );

        File.WriteAllBytes(CredentialsPath, encrypted);
    }

    public static string? LoadApiKey()
    {
        if (!File.Exists(CredentialsPath))
            return null;

        try
        {
            byte[] encrypted = File.ReadAllBytes(CredentialsPath);
            byte[] decrypted = ProtectedData.Unprotect(
                encrypted,
                Entropy,
                DataProtectionScope.CurrentUser
            );
            return Encoding.UTF8.GetString(decrypted);
        }
        catch (CryptographicException)
        {
            // Credentials corrupted or from different user
            return null;
        }
    }
}
```

**Source:** [How to: Use Data Protection - Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/standard/security/how-to-use-data-protection)

### Pattern 4: JSON Settings Persistence

**What:** Store non-sensitive user settings in a JSON file in AppData.

**When to use:** User-configurable settings like hotkey choice, endpoint URL.

**Example:**

```csharp
// AppSettings.cs
public class AppSettings
{
    public Keys HotkeyKey { get; set; } = Keys.F8;
    public string AzureEndpoint { get; set; } = "";
    public string WhisperDeployment { get; set; } = "whisper";
}

// ConfigurationService.cs
public static class ConfigurationService
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Coxixo",
        "settings.json"
    );

    public static AppSettings Load()
    {
        if (!File.Exists(SettingsPath))
            return new AppSettings();

        try
        {
            var json = File.ReadAllText(SettingsPath);
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public static void Save(AppSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);

        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(settings, options);
        File.WriteAllText(SettingsPath, json);
    }
}
```

### Anti-Patterns to Avoid

- **Using RegisterHotKey for push-to-talk:** RegisterHotKey only fires on key press, not release. Cannot implement hold-to-record.
- **Creating a hidden Form for tray app:** Unnecessary overhead. ApplicationContext is lighter.
- **Storing API keys in appsettings.json:** JSON files are human-readable. Use DPAPI for sensitive data.
- **Hardcoding AppData paths:** Use `Environment.GetFolderPath()` for cross-system compatibility.
- **Not disposing NotifyIcon:** Causes ghost icons in system tray.

## Don't Hand-Roll

Problems that look simple but have existing solutions:

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Low-level keyboard hook | Custom P/Invoke without proper cleanup | SharpHook library OR well-tested P/Invoke pattern | Hook leaks cause system-wide keyboard lag |
| Credential encryption | Custom AES encryption | DPAPI ProtectedData | Key management is hard; DPAPI handles it |
| JSON serialization | String concatenation | System.Text.Json | Edge cases (escaping, null handling) |
| Icon with multiple resolutions | Single size icon | Multi-resolution ICO file (16x16 + 32x32) | Windows scales badly from wrong size |

**Key insight:** Keyboard hooks and credential storage have subtle failure modes that are hard to debug. Use battle-tested patterns.

## Common Pitfalls

### Pitfall 1: NotifyIcon Ghost Icons

**What goes wrong:** System tray icon remains visible after app closes or crashes.

**Why it happens:** Windows doesn't auto-clean tray icons. Must explicitly set Visible=false and Dispose.

**How to avoid:**
```csharp
// In EVERY exit path including crash handlers
_trayIcon.Visible = false;
_trayIcon.Icon = null;  // Required for Windows 7+
_trayIcon.Dispose();
```

**Warning signs:** Multiple app icons accumulating during development.

**Source:** [dotnet/winforms Issue #6996](https://github.com/dotnet/winforms/issues/6996)

### Pitfall 2: Keyboard Hook Without Message Pump

**What goes wrong:** Hook registration succeeds but callbacks never fire.

**Why it happens:** Low-level hooks communicate via Windows messages. Need `Application.Run()` pumping messages.

**How to avoid:**
- Always register hooks on UI thread
- Ensure `Application.Run()` is called (ApplicationContext handles this)
- Test hotkey while another app (Notepad) has focus

**Warning signs:** Hotkey only works when your window is focused.

**Source:** [Global Hotkeys - CodeProject](https://www.codeproject.com/Articles/1273010/Global-Hotkeys-within-Desktop-Applications)

### Pitfall 3: Keyboard Auto-Repeat Flood

**What goes wrong:** HotkeyPressed fires repeatedly while user holds key (100+ times).

**Why it happens:** Windows sends repeated WM_KEYDOWN messages for held keys.

**How to avoid:**
```csharp
// Track key state, only fire on state change
if ((msg == WM_KEYDOWN) && !_isKeyDown)
{
    _isKeyDown = true;
    HotkeyPressed?.Invoke(this, EventArgs.Empty);
}
```

**Warning signs:** Recording starts multiple times while holding key.

### Pitfall 4: DPAPI Fails Under Impersonation

**What goes wrong:** ProtectedData.Unprotect throws CryptographicException.

**Why it happens:** DPAPI uses current user's profile which may not be loaded during impersonation.

**How to avoid:**
- Only use DPAPI from the application's normal execution context
- Don't access credentials from background services running as different user

**Warning signs:** Works in dev, fails in production with "Key not valid for use in specified state".

**Source:** [How to: Use Data Protection - Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/standard/security/how-to-use-data-protection)

### Pitfall 5: Multiple App Instances

**What goes wrong:** User launches app twice. Second instance fails to register hotkey (silently), both show tray icons.

**Why it happens:** No single-instance enforcement.

**How to avoid:**
```csharp
// In Program.cs
static Mutex? _mutex;

[STAThread]
static void Main()
{
    const string mutexName = "Global\\CoxixoSingleInstance";
    _mutex = new Mutex(true, mutexName, out bool createdNew);

    if (!createdNew)
    {
        // Another instance running - could signal it here
        MessageBox.Show("Coxixo is already running.", "Coxixo",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
        return;
    }

    Application.EnableVisualStyles();
    Application.SetCompatibleTextRenderingDefault(false);
    Application.Run(new TrayApplicationContext());
}
```

**Source:** [Single Instance App with Mutex](https://www.autoitconsulting.com/site/development/single-instance-winform-app-csharp-mutex-named-pipes/)

## Code Examples

Verified patterns from official sources:

### NotifyIcon with Context Menu

```csharp
// Source: Microsoft Learn
private ContextMenuStrip CreateContextMenu()
{
    var menu = new ContextMenuStrip();
    menu.Items.Add("Settings...", null, OnSettingsClick);
    menu.Items.Add(new ToolStripSeparator());
    menu.Items.Add("Exit", null, OnExitClick);
    return menu;
}

private void OnExitClick(object? sender, EventArgs e)
{
    Application.Exit();
}
```

### Icon State Change for Recording Indicator

```csharp
// Source: Project research - ARCHITECTURE.md
private void OnHotkeyPressed(object? sender, EventArgs e)
{
    // Immediately update icon BEFORE starting recording
    _trayIcon.Icon = Resources.IconRecording;
    _trayIcon.Text = "Coxixo - Recording...";
}

private void OnHotkeyReleased(object? sender, EventArgs e)
{
    _trayIcon.Icon = Resources.IconIdle;
    _trayIcon.Text = "Coxixo - Push to Talk";
}
```

### Extracting Icon from Executable (Avoid Duplicate Resources)

```csharp
// Source: Icon Usage by Windows and Windows Forms Applications
// If application icon is already embedded via project properties,
// extract it at runtime instead of duplicating in Resources
var appIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
_trayIcon.Icon = new Icon(appIcon, 16, 16);  // Request small size
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Form with hidden visibility | ApplicationContext | Always available | Lighter memory footprint |
| RegisterHotKey for toggle | Low-level hook for push-to-talk | N/A (different use case) | Enables hold-to-record |
| .NET Framework DPAPI | .NET 8 ProtectedData (same API) | .NET Core 1.0+ | Cross-runtime compatible |
| app.config XML | appsettings.json | .NET Core 1.0+ | Modern JSON format |
| Server GC always | DATAS (Dynamic GC) | .NET 8 | 8x memory reduction possible |

**Deprecated/outdated:**
- ContextMenu class: Use ContextMenuStrip instead
- IniFile for settings: Use JSON
- CryptoServiceProvider: Use built-in RNG for entropy

## Open Questions

Things that couldn't be fully resolved:

1. **NonInvasiveKeyboardHook key-up support**
   - What we know: Library uses low-level hooks
   - What's unclear: Whether it exposes separate KeyPressed/KeyReleased events
   - Recommendation: Use manual P/Invoke or SharpHook for guaranteed key-up detection

2. **Icon scaling on high-DPI displays**
   - What we know: NotifyIcon uses 16x16 or 32x32 from ICO file
   - What's unclear: Behavior at 150%+ DPI scaling
   - Recommendation: Include both 16x16 and 32x32 in ICO file, test on high-DPI

3. **Hook timeout on Windows 10/11**
   - What we know: Windows removes hooks that take >1000ms in callback
   - What's unclear: Exact enforcement behavior
   - Recommendation: Keep callback minimal, offload work to separate thread

## Sources

### Primary (HIGH confidence)

- [Microsoft Learn: NotifyIcon Class](https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.notifyicon?view=windowsdesktop-10.0)
- [Microsoft Learn: Low-Level Keyboard Hook in C#](https://learn.microsoft.com/en-us/archive/blogs/toub/low-level-keyboard-hook-in-c)
- [Microsoft Learn: How to Use Data Protection (DPAPI)](https://learn.microsoft.com/en-us/dotnet/standard/security/how-to-use-data-protection)
- [Microsoft Learn: LowLevelKeyboardProc](https://learn.microsoft.com/en-us/windows/win32/winmsg/lowlevelkeyboardproc)
- [dotnet/winforms Issue #6996 - Ghost Icons](https://github.com/dotnet/winforms/issues/6996)

### Secondary (MEDIUM confidence)

- [Creating Tray Applications in .NET - Simple Talk](https://www.red-gate.com/simple-talk/development/dotnet-development/creating-tray-applications-in-net-a-practical-guide/)
- [Formless System Tray Application - CodeProject](https://www.codeproject.com/Articles/290013/Formless-System-Tray-Application)
- [Global Hotkeys within Desktop Applications - CodeProject](https://www.codeproject.com/Articles/1273010/Global-Hotkeys-within-Desktop-Applications)
- [SharpHook GitHub](https://github.com/TolikPylypchuk/SharpHook)
- [Single Instance App with Mutex - AutoIt Consulting](https://www.autoitconsulting.com/site/development/single-instance-winform-app-csharp-mutex-named-pipes/)

### Tertiary (LOW confidence)

- [NonInvasiveKeyboardHook GitHub](https://github.com/kfirprods/NonInvasiveKeyboardHook) - Key-up support unclear
- [Icon Usage by Windows and WinForms](https://www.hhhh.org/cloister/csharp/icons/) - Older article but relevant patterns

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - All built-in .NET 8 components
- Architecture (ApplicationContext): HIGH - Microsoft documentation + multiple authoritative guides
- Architecture (keyboard hook): HIGH - Microsoft official blog post with complete code
- DPAPI: HIGH - Microsoft Learn documentation with verified examples
- NotifyIcon disposal: HIGH - Confirmed via GitHub issue with Microsoft response
- Pitfalls: HIGH - Cross-referenced from project PITFALLS.md research

**Research date:** 2026-01-17
**Valid until:** 2026-02-17 (30 days - stable .NET 8 ecosystem)
