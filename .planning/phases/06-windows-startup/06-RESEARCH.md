# Phase 6: Windows Startup - Research

**Researched:** 2026-02-09
**Domain:** Windows Registry Startup Configuration (HKCU Run Key)
**Confidence:** HIGH

## Summary

Windows startup registration for desktop applications is a well-established pattern using the `HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run` registry key. This phase adds a simple checkbox in the existing SettingsForm to toggle startup behavior by writing/removing the application's executable path to/from the registry.

The implementation follows the existing ApplicationContext + static services architecture. A new `StartupService` (static class, similar to `ConfigurationService` and `CredentialService`) encapsulates registry operations. The SettingsForm gains a single checkbox that reads current state on load and immediately writes to the registry on toggle. AppSettings stores the user's preference to support checkbox synchronization when the settings window re-opens.

**Primary recommendation:** Use `Microsoft.Win32.Registry` (built into .NET) to access `HKEY_CURRENT_USER\...\Run` with quoted executable path. No elevation required, no additional dependencies, works reliably across Windows versions. Implement as static `StartupService` class with `IsEnabled()`, `Enable()`, `Disable()` methods matching the existing service pattern.

## Standard Stack

### Core

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| **Microsoft.Win32.Registry** | Built-in (.NET 8) | Registry read/write operations | Official .NET API for Windows Registry, no external dependencies |
| **System.Windows.Forms** | Built-in (.NET 8) | CheckBox control for UI | Already in use for SettingsForm |

### Supporting

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| N/A | - | - | No additional libraries needed |

### Alternatives Considered

| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Registry HKCU Run key | Task Scheduler API | Task Scheduler offers advanced triggers (delays, conditions, elevation) but adds significant complexity. Only justified for enterprise scenarios requiring centralized management or advanced scheduling. For simple "start with Windows" functionality, registry is the standard choice. |
| Registry HKCU Run key | Startup folder shortcut | Startup folder (`%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup`) is user-visible and can be manually deleted, while registry is less discoverable. Registry approach aligns better with programmatic control from settings UI. |
| Application.ExecutablePath | Environment.ProcessPath | `Application.ExecutablePath` is WinForms-specific and well-tested. `Environment.ProcessPath` is .NET 5+ but may return null in some hosting scenarios. Stick with `Application.ExecutablePath` for WinForms apps. |

**Installation:**
```bash
# No installation needed - Microsoft.Win32.Registry is part of .NET runtime
```

## Architecture Patterns

### Recommended Project Structure
```
Coxixo/
├── Services/
│   ├── StartupService.cs        # NEW - Static service for registry operations
│   ├── ConfigurationService.cs  # EXISTING - Pattern to follow
│   └── CredentialService.cs     # EXISTING - Pattern to follow
├── Models/
│   └── AppSettings.cs           # MODIFIED - Add StartWithWindows property
└── Forms/
    ├── SettingsForm.cs          # MODIFIED - Add checkbox control
    └── SettingsForm.Designer.cs # MODIFIED - Add checkbox to layout
```

### Pattern 1: Static Service for Registry Operations

**What:** Static class with `IsEnabled()`, `Enable()`, `Disable()` methods encapsulating all registry logic. Matches existing `ConfigurationService` and `CredentialService` patterns.

**When to use:** Always for stateless utility services that don't require dependency injection or instance lifetime management.

**Example:**
```csharp
// Source: Existing architecture pattern from ConfigurationService
using Microsoft.Win32;

namespace Coxixo.Services;

/// <summary>
/// Manages Windows startup registration via HKCU Run registry key.
/// </summary>
public static class StartupService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "Coxixo";

    /// <summary>
    /// Checks if the application is registered to start with Windows.
    /// </summary>
    public static bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        if (key == null) return false;

        var value = key.GetValue(AppName) as string;
        if (value == null) return false;

        // Verify the registry value matches current executable path
        string currentPath = $"\"{Application.ExecutablePath}\"";
        return value.Equals(currentPath, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Registers the application to start with Windows.
    /// </summary>
    public static void Enable()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
        if (key == null)
            throw new InvalidOperationException("Cannot access Run registry key");

        // Quote the path to handle spaces in executable location
        string exePath = $"\"{Application.ExecutablePath}\"";
        key.SetValue(AppName, exePath, RegistryValueKind.String);
    }

    /// <summary>
    /// Unregisters the application from Windows startup.
    /// </summary>
    public static void Disable()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
        if (key == null) return; // Key doesn't exist, nothing to do

        if (key.GetValue(AppName) != null)
            key.DeleteValue(AppName, throwOnMissingValue: false);
    }
}
```

### Pattern 2: AppSettings Property for Persistence

**What:** Add `bool StartWithWindows` property to `AppSettings` model to persist user's preference. This enables checkbox state synchronization when settings window re-opens.

**When to use:** Always store UI state in settings model to support "reload and verify current state" pattern.

**Example:**
```csharp
// Source: Existing AppSettings.cs pattern
public class AppSettings
{
    // ... existing properties ...

    /// <summary>
    /// Whether to launch Coxixo automatically when Windows starts.
    /// </summary>
    public bool StartWithWindows { get; set; } = false;
}
```

### Pattern 3: SettingsForm Integration

**What:** Add CheckBox control that reads registry state on load, toggles immediately on click, and saves preference to AppSettings on Save.

**When to use:** For settings that affect system state outside the application (registry, file associations, etc.) - apply immediately rather than only on Save button.

**Example:**
```csharp
// SettingsForm.cs
private CheckBox chkStartWithWindows;

private void LoadSettings()
{
    _settings = ConfigurationService.Load();

    // ... existing settings loading ...

    // Synchronize checkbox with actual registry state (source of truth)
    chkStartWithWindows.Checked = StartupService.IsEnabled();
}

private void ChkStartWithWindows_CheckedChanged(object? sender, EventArgs e)
{
    try
    {
        if (chkStartWithWindows.Checked)
            StartupService.Enable();
        else
            StartupService.Disable();
    }
    catch (UnauthorizedAccessException)
    {
        MessageBox.Show(
            "Cannot modify startup settings. Registry write permission denied.",
            "Startup Configuration",
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning
        );

        // Revert checkbox to actual state
        chkStartWithWindows.Checked = StartupService.IsEnabled();
    }
}

private void BtnSave_Click(object? sender, EventArgs e)
{
    // ... existing validation ...

    _settings.StartWithWindows = chkStartWithWindows.Checked;
    ConfigurationService.Save(_settings);

    // ... rest of save logic ...
}
```

### Pattern 4: Dark Theme CheckBox Styling

**What:** Apply dark theme colors to checkbox using existing `ApplyDarkTheme()` recursive pattern. Standard CheckBox supports `BackColor` and `ForeColor` for basic theming.

**When to use:** For all new controls added to SettingsForm to maintain visual consistency.

**Example:**
```csharp
// SettingsForm.cs ApplyDarkTheme method (existing pattern)
private void ApplyDarkTheme(Control control)
{
    control.BackColor = DarkTheme.Background;
    control.ForeColor = DarkTheme.Text;

    foreach (Control child in control.Controls)
    {
        // ... existing control type checks ...

        if (child is CheckBox cb)
        {
            cb.ForeColor = DarkTheme.Text;
            // Note: CheckBox background is transparent, inherits from parent
        }

        ApplyDarkTheme(child);
    }
}
```

### Anti-Patterns to Avoid

- **Unquoted executable paths in registry:** Paths with spaces (e.g., `C:\Program Files\Coxixo\Coxixo.exe`) will fail without quotes. Always use `$"\"{Application.ExecutablePath}\""`.

- **Writing to HKEY_LOCAL_MACHINE:** HKLM requires elevation and affects all users. Use HKEY_CURRENT_USER for per-user startup (no admin privileges needed).

- **Not disposing RegistryKey objects:** Always use `using` statement to ensure registry handles are released. Leaked handles can cause registry access failures.

- **Storing startup preference only in registry:** Registry state can be modified externally (Task Manager, third-party tools). Store preference in AppSettings too, so checkbox reflects user's last choice even if registry was modified outside the app.

- **Ignoring registry write failures:** Enterprise environments may lock down registry writes. Always catch `UnauthorizedAccessException` and inform the user gracefully.

- **Using RunOnce key:** `RunOnce` deletes the entry after first execution, inappropriate for persistent startup. Use `Run` key instead.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Registry path string manipulation | Custom string builders for registry paths | Const string literals | Registry paths are fixed by Windows API contract. Hardcode `@"Software\Microsoft\Windows\CurrentVersion\Run"` - no dynamic construction needed. |
| Registry permission checking | Pre-flight permission validation logic | Try-catch on actual operation | No reliable way to check write permissions without attempting the write. Let `OpenSubKey(..., writable: true)` throw if access denied. |
| CheckBox custom painting | Owner-draw CheckBox for dark theme | Standard CheckBox with `ForeColor` | WinForms CheckBox supports basic theming via `ForeColor`. Custom painting adds complexity without benefit for simple dark theme. Only needed for heavily custom styles. |
| Task Scheduler integration | Task Scheduler COM API for startup | Registry Run key | Task Scheduler is overkill for simple "start with Windows" feature. Only justified if you need advanced triggers (delay, conditions, elevation without UAC). |

**Key insight:** Windows startup registration is a solved problem with a 30-year-old API (registry Run keys). The API is stable, requires no dependencies, and works across all Windows versions since Windows 95. Don't add complexity with Task Scheduler, startup folder shortcuts, or custom permission checking unless specific requirements demand it.

## Common Pitfalls

### Pitfall 1: Path Quoting and Spaces

**What goes wrong:** Executable path written to registry without quotes fails when path contains spaces (e.g., `C:\Program Files\Coxixo\Coxixo.exe`). Windows parses unquoted paths incorrectly, trying to execute `C:\Program.exe` instead.

**Why it happens:** `Application.ExecutablePath` returns unquoted path. Developers often write it directly to registry without considering space handling.

**How to avoid:** Always wrap executable path in double quotes when writing to registry:
```csharp
string exePath = $"\"{Application.ExecutablePath}\"";
key.SetValue(AppName, exePath);
```

**Warning signs:**
- Application doesn't start on Windows login when installed in `Program Files` or other directory with spaces
- Works fine when installed in `C:\Coxixo\` (no spaces) but fails in default installation directory
- Event Viewer shows "file not found" errors during Windows startup

**Source:** [How to fix the Windows unquoted service path vulnerability](https://isgovern.com/blog/how-to-fix-the-windows-unquoted-service-path-vulnerability/)

---

### Pitfall 2: Registry State vs. Settings State Mismatch

**What goes wrong:** User modifies startup via Windows Task Manager or third-party tool. Checkbox in SettingsForm shows incorrect state because it only reads from AppSettings, not actual registry.

**Why it happens:** Two sources of truth: registry (actual Windows behavior) and AppSettings (user's last choice in app). External tools modify registry without updating AppSettings.

**How to avoid:** Always read from registry on SettingsForm load, not from AppSettings:
```csharp
private void LoadSettings()
{
    _settings = ConfigurationService.Load();

    // CORRECT: Read from registry (source of truth)
    chkStartWithWindows.Checked = StartupService.IsEnabled();

    // WRONG: Read from settings (may be stale)
    // chkStartWithWindows.Checked = _settings.StartWithWindows;
}
```

**Warning signs:**
- Checkbox shows "enabled" but application doesn't start on Windows login
- User reports "checkbox doesn't match Task Manager's Startup tab"
- Checkbox state changes after external modification (e.g., disabling via Task Manager) and re-opening settings

**Recommendation:** Registry is the source of truth for current state. AppSettings stores user's last choice for auditing/diagnostics but is NOT used for checkbox initialization.

---

### Pitfall 3: Registry Write Permission Failures in Enterprise Environments

**What goes wrong:** Application crashes or fails silently when trying to write to registry in locked-down enterprise environment. Some corporate policies restrict registry writes even to HKCU.

**Why it happens:** `OpenSubKey(..., writable: true)` or `SetValue()` throws `UnauthorizedAccessException` when Group Policy restricts registry modifications.

**How to avoid:** Wrap all registry writes in try-catch with user-friendly error handling:
```csharp
private void ChkStartWithWindows_CheckedChanged(object? sender, EventArgs e)
{
    try
    {
        if (chkStartWithWindows.Checked)
            StartupService.Enable();
        else
            StartupService.Disable();
    }
    catch (UnauthorizedAccessException)
    {
        MessageBox.Show(
            "Cannot modify startup settings. Your system administrator may have restricted this feature.",
            "Permission Denied",
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning
        );

        // Revert checkbox to actual registry state
        chkStartWithWindows.Checked = StartupService.IsEnabled();
    }
    catch (Exception ex)
    {
        MessageBox.Show(
            $"Failed to update startup settings: {ex.Message}",
            "Error",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error
        );

        chkStartWithWindows.Checked = StartupService.IsEnabled();
    }
}
```

**Warning signs:**
- Works on developer machine but fails for some users
- Users in corporate environments report "startup option doesn't work"
- Event logs show registry access denied errors

**Recommendation:** Always catch exceptions during registry writes and revert UI to actual state. Don't disable checkbox preemptively (too complex, rare scenario) - fail gracefully on actual operation.

---

### Pitfall 4: OpenSubKey Returns Null (Missing Key Handling)

**What goes wrong:** Code assumes `OpenSubKey()` always succeeds and directly calls methods on the returned `RegistryKey`, causing `NullReferenceException` when key doesn't exist.

**Why it happens:** `OpenSubKey()` returns `null` instead of throwing when key doesn't exist. New Windows installations or rare edge cases may have missing registry structure.

**How to avoid:** Always null-check before using returned RegistryKey:
```csharp
public static bool IsEnabled()
{
    using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
    if (key == null) return false; // Key doesn't exist - app not registered

    var value = key.GetValue(AppName) as string;
    return value != null;
}

public static void Enable()
{
    using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
    if (key == null)
    {
        // Run key should always exist in Windows, but handle edge case
        throw new InvalidOperationException(
            "Cannot access Windows startup registry key. This may indicate system corruption."
        );
    }

    key.SetValue(AppName, $"\"{Application.ExecutablePath}\"");
}
```

**Warning signs:**
- Crashes with `NullReferenceException` when calling `key.SetValue()` or `key.GetValue()`
- Happens on fresh Windows installations or after registry corruption
- Error message: "Object reference not set to an instance of an object"

**Source:** [RegistryKey.OpenSubKey does not ALWAYS return an existing value - Microsoft Q&A](https://learn.microsoft.com/en-us/answers/questions/504632/registrykey-opensubkey-does-not-always-return-an-e)

**Recommendation:** For `IsEnabled()`, return `false` on null (safe default). For `Enable()`/`Disable()`, throw descriptive exception (Run key missing indicates serious system issue).

---

### Pitfall 5: Checkbox Applies on Save Instead of Immediately

**What goes wrong:** User toggles "Start with Windows" checkbox, then clicks Cancel. Expects registry to remain unchanged, but some implementations write to registry only on Save button, causing confusion when checkbox state doesn't persist.

**Why it happens:** Inconsistent UX pattern - some settings apply on Save (hotkey, credentials), but startup registration is system-level configuration that users expect to apply immediately (like Windows Settings app behavior).

**How to avoid:** Apply registry changes immediately on checkbox toggle, not on Save button:
```csharp
// Designer.cs - Wire event handler
chkStartWithWindows.CheckedChanged += ChkStartWithWindows_CheckedChanged;

// SettingsForm.cs - Apply immediately
private void ChkStartWithWindows_CheckedChanged(object? sender, EventArgs e)
{
    try
    {
        if (chkStartWithWindows.Checked)
            StartupService.Enable();
        else
            StartupService.Disable();
    }
    catch (Exception ex)
    {
        // Handle exception and revert UI...
    }
}

// Still save preference to AppSettings on Save button
private void BtnSave_Click(object? sender, EventArgs e)
{
    _settings.StartWithWindows = chkStartWithWindows.Checked;
    ConfigurationService.Save(_settings);
    // ...
}
```

**Warning signs:**
- User reports "checkbox doesn't do anything when I click it"
- User confusion when clicking Cancel after toggling checkbox (expects registry to revert but it already changed)
- UX mismatch with Windows Settings app (which applies startup changes immediately)

**Recommendation:** Follow Windows Settings app pattern - apply registry changes immediately on toggle. Save preference to AppSettings on Save button for audit trail, but registry is modified on CheckedChanged event.

## Code Examples

Verified patterns from official sources and existing codebase architecture:

### Complete StartupService Implementation

```csharp
// Source: Coxixo architecture pattern (ConfigurationService/CredentialService)
// Registry API: https://learn.microsoft.com/en-us/dotnet/api/microsoft.win32.registry
using Microsoft.Win32;

namespace Coxixo.Services;

/// <summary>
/// Manages Windows startup registration via HKCU Run registry key.
/// Follows the static service pattern established by ConfigurationService and CredentialService.
/// </summary>
public static class StartupService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "Coxixo";

    /// <summary>
    /// Checks if the application is currently registered to start with Windows.
    /// Reads from HKEY_CURRENT_USER registry (source of truth).
    /// </summary>
    /// <returns>True if registered and path matches current executable, false otherwise.</returns>
    public static bool IsEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
            if (key == null) return false;

            var value = key.GetValue(AppName) as string;
            if (value == null) return false;

            // Verify registry value matches current executable path (with quotes)
            string currentPath = $"\"{Application.ExecutablePath}\"";
            return value.Equals(currentPath, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception)
        {
            // Registry read failure - assume not enabled
            return false;
        }
    }

    /// <summary>
    /// Registers the application to start automatically when Windows starts.
    /// Writes to HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run.
    /// </summary>
    /// <exception cref="UnauthorizedAccessException">Thrown when registry write permission is denied.</exception>
    /// <exception cref="InvalidOperationException">Thrown when Run registry key doesn't exist.</exception>
    public static void Enable()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
        if (key == null)
        {
            throw new InvalidOperationException(
                "Cannot access Windows startup registry key. This may indicate system corruption."
            );
        }

        // Quote path to handle spaces in executable location
        string exePath = $"\"{Application.ExecutablePath}\"";
        key.SetValue(AppName, exePath, RegistryValueKind.String);
    }

    /// <summary>
    /// Unregisters the application from Windows startup.
    /// Removes entry from HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run.
    /// Safe to call even if not currently registered.
    /// </summary>
    public static void Disable()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
            if (key == null) return; // Run key doesn't exist, nothing to do

            if (key.GetValue(AppName) != null)
            {
                key.DeleteValue(AppName, throwOnMissingValue: false);
            }
        }
        catch (UnauthorizedAccessException)
        {
            // Swallow exception on disable - user intent is "not started",
            // permission failure achieves same outcome
        }
    }
}
```

### AppSettings Model Extension

```csharp
// Source: Existing Coxixo\Models\AppSettings.cs
namespace Coxixo.Models;

public class AppSettings
{
    // ... existing properties ...

    /// <summary>
    /// Whether the user enabled "Start with Windows" in settings.
    /// Note: Actual startup state is in registry - this stores user's last choice.
    /// </summary>
    public bool StartWithWindows { get; set; } = false;
}
```

### SettingsForm Integration (Designer)

```csharp
// SettingsForm.Designer.cs - Add to existing control declarations
partial class SettingsForm
{
    // ... existing controls ...

    private CheckBox chkStartWithWindows;

    private void InitializeComponent()
    {
        // ... existing initialization ...

        // chkStartWithWindows
        chkStartWithWindows = new CheckBox();
        chkStartWithWindows.Location = new Point(12, 370); // After deployment textbox
        chkStartWithWindows.Size = new Size(280, 20);
        chkStartWithWindows.Name = "chkStartWithWindows";
        chkStartWithWindows.Text = "Start with Windows";
        chkStartWithWindows.CheckedChanged += ChkStartWithWindows_CheckedChanged;

        // Add to form controls
        this.Controls.Add(chkStartWithWindows);

        // Adjust form height to accommodate new control
        this.ClientSize = new Size(304, 440); // Was 405, now 440 (+35px)

        // Move buttons down
        btnCancel.Location = new Point(127, 400);  // Was 365
        btnSave.Location = new Point(212, 400);     // Was 365
        btnTestConnection.Location = new Point(12, 335); // Unchanged
    }
}
```

### SettingsForm Integration (Logic)

```csharp
// SettingsForm.cs
using Coxixo.Services;

private void LoadSettings()
{
    _settings = ConfigurationService.Load();

    // ... existing settings loading (hotkey, endpoint, etc.) ...

    // Read from registry (source of truth), not from AppSettings
    chkStartWithWindows.Checked = StartupService.IsEnabled();
}

private void ChkStartWithWindows_CheckedChanged(object? sender, EventArgs e)
{
    try
    {
        if (chkStartWithWindows.Checked)
            StartupService.Enable();
        else
            StartupService.Disable();
    }
    catch (UnauthorizedAccessException)
    {
        MessageBox.Show(
            "Cannot modify startup settings. Your system administrator may have restricted this feature.",
            "Permission Denied",
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning
        );

        // Revert checkbox to actual registry state
        chkStartWithWindows.Checked = StartupService.IsEnabled();
    }
    catch (Exception ex)
    {
        MessageBox.Show(
            $"Failed to update startup settings: {ex.Message}",
            "Error",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error
        );

        chkStartWithWindows.Checked = StartupService.IsEnabled();
    }
}

private void BtnSave_Click(object? sender, EventArgs e)
{
    // ... existing validation (hotkey, credentials, etc.) ...

    // Update settings model
    _settings.Hotkey = hotkeyPicker.SelectedCombo ?? HotkeyCombo.Default();
    _settings.AzureEndpoint = txtEndpoint.Text.Trim();
    _settings.WhisperDeployment = txtDeployment.Text.Trim();
    _settings.StartWithWindows = chkStartWithWindows.Checked; // NEW - save preference

    // Save settings and credentials
    ConfigurationService.Save(_settings);
    CredentialService.SaveApiKey(txtApiKey.Text.Trim());

    this.DialogResult = DialogResult.OK;
    this.Close();
}
```

### Dark Theme Integration

```csharp
// SettingsForm.cs - Add to existing ApplyDarkTheme method
private void ApplyDarkTheme(Control control)
{
    control.BackColor = DarkTheme.Background;
    control.ForeColor = DarkTheme.Text;

    foreach (Control child in control.Controls)
    {
        // ... existing control type checks (HotkeyPickerControl, TextBox, Button, Panel) ...

        else if (child is CheckBox cb)
        {
            cb.ForeColor = DarkTheme.Text;
            // CheckBox background is transparent, inherits from parent
        }

        ApplyDarkTheme(child);
    }
}
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Startup folder shortcuts | Registry Run key | Windows 95+ | Both still work, but registry preferred for programmatic control (less user-visible, survives folder cleanup) |
| HKEY_LOCAL_MACHINE | HKEY_CURRENT_USER | Best practice evolution | HKLM requires elevation and affects all users. HKCU is per-user, no admin needed, safer for desktop apps |
| Unquoted paths | Quoted paths in registry | Security hardening (CVE advisories 2010s) | Unquoted paths with spaces are security vulnerability (path hijacking). Always quote executable paths |
| Registry.SetValue static method | RegistryKey.OpenSubKey with using statement | .NET Framework 2.0+ | Static `Registry.SetValue` is convenient but doesn't allow error handling or null checks. Explicit `OpenSubKey` provides better control |

**Deprecated/outdated:**
- **RunOnce key for persistent startup:** RunOnce deletes entry after first execution. Only use for one-time initialization tasks, never for "start with Windows" feature. Use Run key instead.
- **Writing directly to registry without quotes:** Security vulnerability (CVE-2013-1609 and related). Always quote executable paths to prevent path hijacking attacks.
- **Assuming HKCU write always succeeds:** Enterprise Group Policy can restrict registry writes. Always catch `UnauthorizedAccessException` and handle gracefully.

## Open Questions

1. **Should checkbox state persist if registry write fails?**
   - What we know: AppSettings stores user's last choice, registry holds actual state. On permission failure, registry doesn't change but AppSettings could still store "user wanted it enabled".
   - What's unclear: Should we save `_settings.StartWithWindows = true` even if `Enable()` threw exception, or only save on successful registry write?
   - Recommendation: Only save to AppSettings on successful registry operation OR on Save button click (regardless of registry state). This keeps AppSettings as "last successful preference" rather than "intended preference that might have failed".

2. **Should we disable checkbox if registry is read-only?**
   - What we know: Some enterprise environments restrict registry writes via Group Policy. We can detect this on first write attempt.
   - What's unclear: Should we proactively check write permission on form load and disable checkbox, or wait for user to attempt toggle and show error?
   - Recommendation: Wait for user attempt. Pre-flight permission checking is complex (need to attempt write to detect, can't just query permission) and rare scenario. Better UX is optimistic enablement with graceful failure handling.

3. **Should we verify registry on every settings open or cache state?**
   - What we know: External tools (Task Manager, third-party apps) can modify registry without our knowledge. Reading registry on every form open ensures accurate state.
   - What's unclear: Is registry read performance concern? Should we cache state between opens?
   - Recommendation: Always read from registry on form open. Registry read is fast (<1ms) and ensures checkbox shows accurate state. No caching needed for settings dialog opened infrequently.

## Sources

### Primary (HIGH confidence)

- [Run and RunOnce Registry Keys - Win32 apps | Microsoft Learn](https://learn.microsoft.com/en-us/windows/win32/setupapi/run-and-runonce-registry-keys) - Official Windows API documentation for startup registry keys
- [Microsoft.Win32.Registry Class | Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/api/microsoft.win32.registry) - Official .NET API documentation
- [RegistryKey Class | Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/api/microsoft.win32.registrykey) - Official RegistryKey API with examples and best practices
- Coxixo existing codebase (ConfigurationService.cs, CredentialService.cs, SettingsForm.cs) - Established architecture patterns

### Secondary (MEDIUM confidence)

- [Add application to Windows start-up registry for Current user - Microsoft Q&A](https://learn.microsoft.com/en-us/answers/questions/1363124/add-application-to-windows-start-up-registry-for-c) - Verified by Microsoft support, practical examples
- [RegistryKey.OpenSubKey does not ALWAYS return an existing value - Microsoft Q&A](https://learn.microsoft.com/en-us/answers/questions/504632/registrykey-opensubkey-does-not-always-return-an-e) - Null handling guidance from Microsoft community
- [How to fix the Windows unquoted service path vulnerability](https://isgovern.com/blog/how-to-fix-the-windows-unquoted-service-path-vulnerability/) - Security best practices for registry paths
- [Configure Startup Applications in Windows - Microsoft Support](https://support.microsoft.com/en-us/windows/configure-startup-applications-in-windows-115a420a-0bff-4a6f-90e0-1934c844e473) - User-facing Windows Settings behavior (UX pattern reference)

### Tertiary (LOW confidence)

- [How to make an Application auto run on Windows startup in C#](https://foxlearn.com/windows-forms/how-to-make-an-application-auto-run-on-windows-startup-in-csharp-279.html) - Tutorial with code examples (not official source)
- Stack Overflow and community forums - Various implementation examples (cross-verified with official docs)

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - Microsoft.Win32.Registry is official .NET API, well-documented, stable across versions
- Architecture: HIGH - Matches existing Coxixo service patterns (static services, AppSettings persistence, SettingsForm integration)
- Pitfalls: HIGH - Common issues are well-documented (path quoting, null handling, permission failures) with official sources

**Research date:** 2026-02-09
**Valid until:** 60 days (registry API is stable, unlikely to change in Windows ecosystem)
