# Architecture Integration: v1.1 Features

**Project:** Coxixo v1.1 Milestone
**Researched:** 2026-02-09
**Confidence:** HIGH

## Executive Summary

The v1.1 features (hotkey modifiers, microphone selection, language selection, Windows startup) integrate cleanly into the existing ApplicationContext + static services architecture with minimal disruption. All four features follow the established pattern: settings storage in AppSettings model, persistence via ConfigurationService, UI in SettingsForm, and runtime behavior managed by existing services with minor extensions.

**Key integration pattern:** Extend existing services rather than create new ones. KeyboardHookService gains modifier support, AudioCaptureService gains device selection, TranscriptionService gains language configuration, and a new static StartupService handles Windows registry integration.

## System Overview

### Current Architecture (v1.0)

```
Program.cs (entry point, single instance mutex)
    └── TrayApplicationContext : ApplicationContext
            ├── NotifyIcon (tray icon, context menu)
            ├── KeyboardHookService (WH_KEYBOARD_LL global hook)
            ├── AudioCaptureService (NAudio WaveInEvent)
            ├── AudioFeedbackService (beep sounds)
            ├── TranscriptionService (Azure OpenAI Whisper)
            └── SettingsForm (WinForms dialog)
                    ├── ConfigurationService (JSON + DPAPI)
                    └── CredentialService (DPAPI encryption)

AppSettings Model:
    - HotkeyKey: Keys enum
    - AzureEndpoint: string
    - WhisperDeployment: string
    - ApiVersion: string
    - AudioFeedbackEnabled: bool
```

### v1.1 Integration Points

```
[NEW] AppSettings fields:
    - HotkeyModifiers: Keys enum (Ctrl, Alt, Shift flags)
    - AudioInputDeviceNumber: int
    - TranscriptionLanguage: string (ISO-639-1)
    - StartWithWindows: bool

[MODIFIED] KeyboardHookService:
    - Add ModifierKeys property
    - Modify HookCallback to check modifiers
    - Track modifier state during hook processing

[MODIFIED] AudioCaptureService:
    - Add DeviceNumber property
    - Pass DeviceNumber to WaveInEvent constructor
    - Add GetAvailableDevices() static method

[MODIFIED] TranscriptionService:
    - Accept language parameter in constructor
    - Pass to AudioTranscriptionOptions.Language

[NEW] StartupService (static):
    - IsEnabled(): bool
    - Enable()
    - Disable()
    - Registry: HKCU\Software\Microsoft\Windows\CurrentVersion\Run

[MODIFIED] SettingsForm:
    - Add modifier checkboxes (Ctrl, Alt, Shift)
    - Add microphone ComboBox
    - Add language ComboBox
    - Add startup CheckBox
```

## Component Responsibilities

### Modified Components

#### 1. AppSettings Model
**Current:** 5 properties (hotkey, Azure config, audio feedback)
**New:** +4 properties (modifiers, device, language, startup)

```csharp
public class AppSettings
{
    // Existing
    public Keys HotkeyKey { get; set; } = Keys.F8;
    public string AzureEndpoint { get; set; } = "";
    public string WhisperDeployment { get; set; } = "whisper";
    public string ApiVersion { get; set; } = "2024-02-01";
    public bool AudioFeedbackEnabled { get; set; } = true;

    // NEW for v1.1
    public Keys HotkeyModifiers { get; set; } = Keys.None;
    public int AudioInputDeviceNumber { get; set; } = 0; // Default device
    public string TranscriptionLanguage { get; set; } = "pt"; // Portuguese
    public bool StartWithWindows { get; set; } = false;
}
```

**Integration impact:** ConfigurationService automatically serializes/deserializes new fields. Backwards compatible (missing fields default to sensible values).

---

#### 2. KeyboardHookService
**Current:** Monitors single key (Keys enum), fires HotkeyPressed/Released events
**Modification:** Add modifier tracking and validation

```csharp
public sealed class KeyboardHookService : IDisposable
{
    private Keys _targetKey = Keys.F8;
    private Keys _requiredModifiers = Keys.None; // NEW
    private bool _isKeyDown = false;
    private Keys _currentModifiers = Keys.None; // NEW - track current state

    public Keys TargetKey { get; set; }
    public Keys RequiredModifiers { get; set; } // NEW

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            // Track modifier state
            UpdateModifierState(); // NEW - check GetAsyncKeyState

            int vkCode = Marshal.ReadInt32(lParam);
            var key = (Keys)vkCode;

            if (key == _targetKey)
            {
                var msg = wParam.ToInt32();
                bool modifiersMatch = _currentModifiers == _requiredModifiers; // NEW

                if ((msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN)
                    && !_isKeyDown && modifiersMatch) // MODIFIED
                {
                    _isKeyDown = true;
                    HotkeyPressed?.Invoke(this, EventArgs.Empty);
                }
                else if ((msg == WM_KEYUP || msg == WM_SYSKEYUP) && _isKeyDown)
                {
                    _isKeyDown = false;
                    HotkeyReleased?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    private void UpdateModifierState() // NEW
    {
        Keys mods = Keys.None;
        if (GetAsyncKeyState((int)Keys.ControlKey) < 0) mods |= Keys.Control;
        if (GetAsyncKeyState((int)Keys.ShiftKey) < 0) mods |= Keys.Shift;
        if (GetAsyncKeyState((int)Keys.Menu) < 0) mods |= Keys.Alt;
        _currentModifiers = mods;
    }

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey); // NEW P/Invoke
}
```

**Integration impact:**
- TrayApplicationContext sets `_hotkeyService.RequiredModifiers = _settings.HotkeyModifiers` after loading settings
- No breaking changes to existing event handlers
- Modifier state checked on every hook callback (minimal performance impact)

**Confidence:** HIGH - GetAsyncKeyState is standard Windows API for modifier tracking in keyboard hooks

---

#### 3. AudioCaptureService
**Current:** Uses default microphone (WaveInEvent with no DeviceNumber specified)
**Modification:** Add device selection support

```csharp
public sealed class AudioCaptureService : IDisposable
{
    private int _deviceNumber = 0; // NEW - default device

    public int DeviceNumber // NEW
    {
        get => _deviceNumber;
        set => _deviceNumber = value;
    }

    public void StartCapture()
    {
        if (_isRecording) return;

        try
        {
            _buffer = new MemoryStream();
            var waveFormat = new WaveFormat(SampleRate, BitsPerSample, Channels);
            _writer = new WaveFileWriter(_buffer, waveFormat);

            _waveIn = new WaveInEvent
            {
                DeviceNumber = _deviceNumber, // MODIFIED - was implicit 0
                WaveFormat = waveFormat,
                BufferMilliseconds = 50
            };

            // ... rest unchanged
        }
        catch (NAudio.MmException ex) { /* existing error handling */ }
    }

    // NEW - static helper for device enumeration
    public static List<AudioDevice> GetAvailableDevices()
    {
        var devices = new List<AudioDevice>();
        for (int i = 0; i < WaveIn.DeviceCount; i++)
        {
            var caps = WaveIn.GetCapabilities(i);
            devices.Add(new AudioDevice
            {
                DeviceNumber = i,
                Name = caps.ProductName // Note: truncated to 32 chars
            });
        }
        return devices;
    }
}

public class AudioDevice // NEW model
{
    public int DeviceNumber { get; set; }
    public string Name { get; set; } = "";
}
```

**Integration impact:**
- TrayApplicationContext sets `_audioCaptureService.DeviceNumber = _settings.AudioInputDeviceNumber` after loading settings
- No changes to existing recording flow or event handlers
- Device enumeration called only in SettingsForm to populate ComboBox

**Known limitation:** ProductName truncated to 32 characters due to Windows API limitation. Alternative: MMDeviceEnumerator for full names, but adds WASAPI dependency.

**Confidence:** HIGH - WaveInEvent.DeviceNumber is standard NAudio pattern

---

#### 4. TranscriptionService
**Current:** Hardcoded language "pt" (Portuguese) in TranscribeAsync
**Modification:** Accept language as constructor parameter

```csharp
public sealed class TranscriptionService : IDisposable
{
    private readonly AzureOpenAIClient _client;
    private readonly AudioClient _audioClient;
    private readonly string _language; // NEW
    private bool _disposed;

    public TranscriptionService(string endpoint, string apiKey,
                                string deployment, string language) // MODIFIED
    {
        // ... existing validation
        _language = language; // NEW
        _client = new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
        _audioClient = _client.GetAudioClient(deployment);
    }

    public async Task<string?> TranscribeAsync(byte[] audioData, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (audioData == null || audioData.Length == 0) return null;

        using var stream = new MemoryStream(audioData);

        var options = new AudioTranscriptionOptions
        {
            ResponseFormat = AudioTranscriptionFormat.Text,
            Language = _language // MODIFIED - was hardcoded "pt"
        };

        var result = await _audioClient.TranscribeAudioAsync(stream, "audio.wav", options, ct);
        return result.Value.Text;
    }
}
```

**Integration impact:**
- TrayApplicationContext passes `_settings.TranscriptionLanguage` to TranscriptionService constructor in TryInitializeTranscriptionService()
- Requires re-initialization when language changes (already happens on settings save)
- Language must be ISO-639-1 format (2-letter codes: en, pt, es, etc.)

**Confidence:** HIGH - Azure OpenAI Whisper supports 50+ languages via ISO-639-1 codes

---

### New Components

#### 5. StartupService (Static Service)
**Responsibility:** Manage Windows startup registry entry
**Pattern:** Static service like ConfigurationService and CredentialService

```csharp
public static class StartupService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "Coxixo";

    public static bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
        if (key == null) return false;

        var value = key.GetValue(AppName) as string;
        if (value == null) return false;

        string exePath = Application.ExecutablePath;
        return value.Equals(exePath, StringComparison.OrdinalIgnoreCase);
    }

    public static void Enable()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
        if (key == null)
            throw new InvalidOperationException("Cannot access Run registry key");

        string exePath = Application.ExecutablePath;
        key.SetValue(AppName, exePath);
    }

    public static void Disable()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
        if (key == null) return;

        if (key.GetValue(AppName) != null)
            key.DeleteValue(AppName);
    }
}
```

**Integration impact:**
- SettingsForm calls StartupService.Enable()/Disable() based on checkbox state
- No interaction with TrayApplicationContext (purely settings-driven)
- Uses HKEY_CURRENT_USER (no admin privileges required)

**Confidence:** HIGH - Standard Windows autostart pattern, widely documented

---

#### 6. SettingsForm Modifications
**Current:** 4 inputs (hotkey, endpoint, API key, deployment)
**New:** +4 controls (modifier checkboxes, device dropdown, language dropdown, startup checkbox)

**Layout strategy:** Add controls vertically, increase form height from 420 to ~580

```
┌─────────────────────────────────────┐
│ [Status Panel]                      │
├─────────────────────────────────────┤
│ HOTKEY                              │
│ [F8________]  [✓] Ctrl  [✓] Alt  [ ]│ <- NEW checkboxes
├─────────────────────────────────────┤
│ MICROPHONE                          │ <- NEW section
│ [Realtek HD Audio_____▼]            │
├─────────────────────────────────────┤
│ LANGUAGE                            │ <- NEW section
│ [Portuguese___________▼]            │
├─────────────────────────────────────┤
│ AZURE ENDPOINT                      │
│ [https://...]                       │
│ API KEY                             │
│ [••••••••]                          │
│ DEPLOYMENT NAME                     │
│ [whisper]                           │
├─────────────────────────────────────┤
│ [✓] Start with Windows              │ <- NEW checkbox
├─────────────────────────────────────┤
│ [Test Connection]                   │
│                     [Cancel] [Save] │
└─────────────────────────────────────┘
```

**New controls:**
```csharp
// Modifier checkboxes
private CheckBox chkCtrl;
private CheckBox chkAlt;
private CheckBox chkShift;

// Microphone selection
private Label lblMicrophone;
private ComboBox cmbMicrophone;

// Language selection
private Label lblLanguage;
private ComboBox cmbLanguage;

// Startup checkbox
private CheckBox chkStartWithWindows;
```

---

## Data Flow

### Settings Load Flow (Startup)
```
Program.cs Main()
    → TrayApplicationContext constructor
        → ConfigurationService.Load() -> AppSettings
        → KeyboardHookService.TargetKey = _settings.HotkeyKey
        → KeyboardHookService.RequiredModifiers = _settings.HotkeyModifiers (NEW)
        → AudioCaptureService.DeviceNumber = _settings.AudioInputDeviceNumber (NEW)
        → TryInitializeTranscriptionService()
            → TranscriptionService(_settings.AzureEndpoint, apiKey,
                                   _settings.WhisperDeployment,
                                   _settings.TranscriptionLanguage) (MODIFIED)
```

### Settings Save Flow (User Changes)
```
User clicks Save in SettingsForm
    → BtnSave_Click()
        → _settings.HotkeyKey = _selectedKey
        → _settings.HotkeyModifiers = modifiers (NEW)
        → _settings.AudioInputDeviceNumber = deviceNumber (NEW)
        → _settings.TranscriptionLanguage = languageCode (NEW)
        → StartupService.Enable()/Disable() based on checkbox (NEW)
        → ConfigurationService.Save(_settings)
        → CredentialService.SaveApiKey(apiKey)
        → DialogResult = OK

    → TrayApplicationContext.OnSettingsClick()
        → _settings = ConfigurationService.Load()
        → _hotkeyService.TargetKey = _settings.HotkeyKey
        → _hotkeyService.RequiredModifiers = _settings.HotkeyModifiers (NEW)
        → _audioCaptureService.DeviceNumber = _settings.AudioInputDeviceNumber (NEW)
        → TryInitializeTranscriptionService() (reinitializes with new language)
```

### Runtime Hotkey Flow (Modified)
```
User presses Ctrl+Alt+F8 (example: modifiers + target key)
    → KeyboardHookService.HookCallback()
        → UpdateModifierState() checks GetAsyncKeyState (NEW)
        → Validates: (key == _targetKey) && (_currentModifiers == _requiredModifiers) (MODIFIED)
        → HotkeyPressed event fires
    → TrayApplicationContext.OnHotkeyPressed()
        → _audioCaptureService.StartCapture() (uses configured device)

User releases F8
    → KeyboardHookService.HookCallback()
        → HotkeyReleased event fires
    → TrayApplicationContext.OnHotkeyReleased()
        → audioData = _audioCaptureService.StopCapture()
        → _transcriptionService.TranscribeWithRetryAsync(audioData) (uses configured language)
```

---

## Integration Points Summary

### Existing → Modified

| Component | Modification | Breaking Change? | Migration Required? |
|-----------|--------------|------------------|---------------------|
| AppSettings | +4 properties | No - JSON deserializer handles missing fields | No - defaults to sensible values |
| KeyboardHookService | +ModifierKeys property, modified hook logic | No - events unchanged | No - existing code works with Keys.None |
| AudioCaptureService | +DeviceNumber property | No - defaults to 0 (system default) | No |
| TranscriptionService | +language constructor parameter | Yes - signature changed | Yes - TrayApplicationContext must pass language |
| SettingsForm | +4 controls, larger form | No - internal only | No |

### New Components

| Component | Dependencies | Used By | Lifetime |
|-----------|--------------|---------|----------|
| StartupService | Microsoft.Win32.Registry | SettingsForm | Static |
| AudioDevice | None | AudioCaptureService, SettingsForm | Transient (DTO) |

---

## Build Order (Dependency-Aware)

Based on integration dependencies, recommended implementation order:

### Phase 1: Data Model & Static Services (Independent)
1. **AppSettings** - Add 4 new properties
   - No dependencies
   - Enables all other features
   - Test: Serialize/deserialize with ConfigurationService

2. **StartupService** - Static service for registry management
   - No dependencies
   - Can be tested independently
   - Test: Enable/Disable/IsEnabled roundtrip

### Phase 2: Service Extensions (Depends on Phase 1)
3. **AudioCaptureService** - Device selection
   - Dependency: AudioDevice model (create inline)
   - Test: Enumerate devices, create WaveInEvent with specific device

4. **TranscriptionService** - Language parameter
   - Dependency: AppSettings.TranscriptionLanguage
   - Breaking change: Constructor signature
   - Test: Transcribe with different language codes

5. **KeyboardHookService** - Modifier support
   - Dependency: AppSettings.HotkeyModifiers
   - Most complex logic (GetAsyncKeyState integration)
   - Test: Trigger hotkey with/without modifiers

### Phase 3: UI Integration (Depends on Phase 2)
6. **SettingsForm** - Add all new controls
   - Dependencies: All modified services
   - Layout work: reposition existing controls, add new sections
   - Test: Load settings, modify all fields, save, verify persistence

### Phase 4: Runtime Integration (Depends on Phase 3)
7. **TrayApplicationContext** - Wire new settings to services
   - Dependencies: All modified components
   - Update: Constructor, OnSettingsClick, TryInitializeTranscriptionService
   - Test: End-to-end workflow (change settings, trigger hotkey, verify behavior)

**Critical path:** AppSettings → Service modifications → SettingsForm → TrayApplicationContext

**Parallelization opportunity:** StartupService and AudioCaptureService can be built in parallel (no interdependency).

---

## Known Limitations & Trade-offs

### 1. Microphone Device Names (ProductName Truncation)
**Issue:** WaveIn.GetCapabilities() truncates device names to 32 characters due to Windows API limitation.

**Impact:** Users may see "Realtek High Definition Audi..." instead of full name.

**Alternative:** Use MMDeviceEnumerator (WASAPI) for full device names.
- **Pro:** Full-length device names via FriendlyName property
- **Con:** Adds WASAPI dependency, different API surface (IMMDevice vs. WaveInEvent)
- **Recommendation:** Start with WaveIn.GetCapabilities (simpler), upgrade to MMDeviceEnumerator if user feedback indicates confusion

**Source:** [NAudio Issue #612 - ProductName truncation](https://github.com/naudio/NAudio/issues/612)

### 2. Hotkey Modifier Timing (GetAsyncKeyState vs. Hook State)
**Issue:** GetAsyncKeyState checks current modifier state asynchronously, which may have slight timing differences from hook callback.

**Impact:** Potential race condition if user releases modifier between hook callback and GetAsyncKeyState check (extremely rare, millisecond window).

**Mitigation:** Call GetAsyncKeyState at the start of HookCallback to minimize timing gap.

**Alternative:** Track modifier state within hook (detect modifier key up/down events).
- **Pro:** More precise, no external API call
- **Con:** More complex logic, must track modifier state across callbacks
- **Recommendation:** Start with GetAsyncKeyState (simpler), optimize if issues reported

### 3. Language Code Validation
**Issue:** TranscriptionService accepts any string for language parameter, but Azure OpenAI Whisper only supports ~50 ISO-639-1 codes.

**Impact:** Invalid language codes may cause API errors or fallback to auto-detection.

**Mitigation:** SettingsForm provides predefined language list (prevents invalid input).

**Enhancement:** Add validation in TranscriptionService constructor to reject invalid codes.
- **Pro:** Fail-fast at initialization rather than during transcription
- **Con:** Requires maintaining whitelist of valid codes
- **Recommendation:** Start with UI-enforced validation, add service-level validation if needed

### 4. Startup Registry Permissions
**Issue:** Registry write to HKCU\Software\...\Run typically doesn't require elevation, but some enterprise policies may restrict.

**Impact:** StartupService.Enable() may throw UnauthorizedAccessException in locked-down environments.

**Mitigation:** Wrap Enable()/Disable() in try-catch in SettingsForm, show error message to user.

**Enhancement:** Check registry write permission before showing checkbox.
- **Pro:** Better UX (disable checkbox if not writable)
- **Con:** Extra complexity, rare scenario
- **Recommendation:** Try-catch approach with user-friendly error message

---

## Sources

### Hotkey Modifiers
- [C# Global Keyboard Listeners – implementation of key hooks](https://frasergreenroyd.com/c-global-keyboard-listeners-implementation-of-key-hooks/)
- [GitHub - NonInvasiveKeyboardHook](https://github.com/kfirprods/NonInvasiveKeyboardHook)
- [Registration of Global Hotkeys Using Codes for Modifier Keys](https://copyprogramming.com/howto/modifier-key-codes-for-global-hotkey-registration)

### Microphone Selection
- [NAudio Enumerate Output Devices](https://github.com/naudio/NAudio/blob/master/Docs/EnumerateOutputDevices.md)
- [NAudio Recording Demo](https://github.com/naudio/NAudio/blob/master/NAudioDemo/RecordingDemo/RecordingPanel.cs)
- [Listing Audio Recording Equipment using NAudio](https://copyprogramming.com/howto/enumerate-recording-devices-in-naudio)
- [NAudio Issue #612 - ProductName truncation](https://github.com/naudio/NAudio/issues/612)

### Windows Startup
- [Add application to Windows start-up registry for Current user](https://learn.microsoft.com/en-us/answers/questions/1363124/add-application-to-windows-start-up-registry-for-c)
- [Run and RunOnce Registry Keys - Microsoft Learn](https://learn.microsoft.com/en-us/windows/win32/setupapi/run-and-runonce-registry-keys)
- [How to make an Application auto run on Windows startup in C#](https://foxlearn.com/windows-forms/how-to-make-an-application-auto-run-on-windows-startup-in-csharp-279.html)

### Language Selection
- [The Whisper model from OpenAI - Microsoft Learn](https://learn.microsoft.com/en-us/azure/ai-services/speech-service/whisper-overview)
- [Exploring the Whisper model in Azure OpenAI Service](https://blog.pieeatingninjas.be/2023/10/03/exploring-whisper-model-in-azure-openai-service/)
- [Speech to text | OpenAI API](https://platform.openai.com/docs/guides/speech-to-text)
