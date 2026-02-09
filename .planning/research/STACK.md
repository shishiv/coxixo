# Stack Research - v1.1 Feature Additions

**Project:** Coxixo - Windows voice-to-clipboard transcription
**Milestone:** v1.1 - Hotkey modifiers, microphone selection, language selection, Windows startup
**Researched:** 2026-02-09
**Mode:** Incremental feature additions to validated v1.0 stack

---

## Executive Summary

v1.1 requires **zero new dependencies**. All new features (hotkey modifiers, microphone enumeration, language selection, Windows startup) are achievable with the existing .NET 8 + NAudio 2.2.1 + Azure.AI.OpenAI 2.1.0 stack, using built-in Windows APIs and configuration adjustments.

**Key finding:** The existing WH_KEYBOARD_LL hook already captures modifier states. NAudio's MMDeviceEnumerator handles microphone enumeration. Azure OpenAI Whisper accepts ISO-639-1 language codes. Windows startup uses registry Run key or Task Scheduler — both accessible via built-in .NET APIs.

**Confidence: HIGH** — All features verified as achievable with current stack.

---

## Stack Status: NO CHANGES REQUIRED

### Existing Stack (Validated, Unchanged)

| Technology | Version | Purpose | Status for v1.1 |
|------------|---------|---------|-----------------|
| .NET | 8 | Runtime | **No change** |
| WinForms | Built-in | System tray & minimal UI | **No change** |
| NAudio | 2.2.1 | Audio capture | **Used for new feature** (device enumeration) |
| Azure.AI.OpenAI | 2.1.0 | Whisper API | **Used for new feature** (language parameter) |
| WH_KEYBOARD_LL hook | Win32 API | Global hotkey | **Used for new feature** (modifier detection) |
| DPAPI | Built-in | Credential encryption | **No change** |
| ApplicationContext | Built-in | Formless tray app | **No change** |

---

## Feature Implementation Strategy

### 1. Hotkey Modifier Support (Ctrl+X, Shift+Y)

**Goal:** Allow users to configure hotkeys with modifier keys (Ctrl, Shift, Alt, Win) + base key.

**Existing capability:** WH_KEYBOARD_LL hook already captures all keyboard input, including modifier states.

**What's needed:**
- **Modifier detection:** Check `GetAsyncKeyState()` for Ctrl/Shift/Alt/Win when base key is pressed.
- **Configuration storage:** Store modifier + key combination in settings (e.g., `"Ctrl+Shift+X"`).

**Implementation approach:**

```csharp
// Modifier constants (existing in Windows API)
private const int MOD_ALT = 0x0001;
private const int MOD_CONTROL = 0x0002;
private const int MOD_SHIFT = 0x0004;
private const int MOD_WIN = 0x0008;

// In keyboard hook callback
private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
{
    if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
    {
        int vkCode = Marshal.ReadInt32(lParam);

        // Check modifier states
        bool ctrlPressed = (GetAsyncKeyState(VK_CONTROL) & 0x8000) != 0;
        bool shiftPressed = (GetAsyncKeyState(VK_SHIFT) & 0x8000) != 0;
        bool altPressed = (GetAsyncKeyState(VK_MENU) & 0x8000) != 0;
        bool winPressed = (GetAsyncKeyState(VK_LWIN) & 0x8000) != 0 ||
                          (GetAsyncKeyState(VK_RWIN) & 0x8000) != 0;

        // Match against configured hotkey
        if (vkCode == configuredKey &&
            ctrlPressed == configuredCtrl &&
            shiftPressed == configuredShift &&
            altPressed == configuredAlt &&
            winPressed == configuredWin)
        {
            // Trigger recording
        }
    }
    return CallNextHookEx(_hookID, nCode, wParam, lParam);
}

[DllImport("user32.dll")]
private static extern short GetAsyncKeyState(int vKey);
```

**New P/Invoke required:**

```csharp
[DllImport("user32.dll")]
private static extern short GetAsyncKeyState(int vKey);
```

**Constants needed:**

```csharp
private const int VK_CONTROL = 0x11;
private const int VK_SHIFT = 0x10;
private const int VK_MENU = 0x12;  // Alt key
private const int VK_LWIN = 0x5B;
private const int VK_RWIN = 0x5C;
```

**Confidence: HIGH** — GetAsyncKeyState is standard Win32 API, works with existing WH_KEYBOARD_LL hook.

**Sources:**
- [Microsoft Learn: GetAsyncKeyState function](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getasynckeystate)
- [How to register a global hotkey for your application in C#](https://www.fluxbytes.com/csharp/how-to-register-a-global-hotkey-for-your-application-in-c/)
- [Check which modifier key is pressed - Windows Forms .NET](https://learn.microsoft.com/en-us/dotnet/desktop/winforms/input-keyboard/how-to-check-modifier-key?view=netdesktop-8.0)

---

### 2. Microphone Selection (Enumerate Devices)

**Goal:** Allow users to select which microphone to use for recording.

**Existing capability:** NAudio 2.2.1 already includes `NAudio.CoreAudioApi.MMDeviceEnumerator`.

**What's needed:**
- **Enumerate input devices:** Use `MMDeviceEnumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active)`.
- **Store device selection:** Save selected device ID in settings.
- **Apply on recording:** Set `WaveInEvent.DeviceNumber` to selected device index.

**Implementation approach:**

```csharp
using NAudio.CoreAudioApi;
using NAudio.Wave;

// Enumerate available microphones
public List<AudioDevice> GetAvailableMicrophones()
{
    var devices = new List<AudioDevice>();

    // Method 1: Using MMDeviceEnumerator (preferred - full device names)
    var enumerator = new MMDeviceEnumerator();
    int deviceIndex = 0;
    foreach (var device in enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active))
    {
        devices.Add(new AudioDevice
        {
            Index = deviceIndex,
            Name = device.FriendlyName,
            Id = device.ID
        });
        deviceIndex++;
    }

    // Method 2: Using WaveIn.GetCapabilities (alternative, 31-char limit)
    // for (int i = 0; i < WaveIn.DeviceCount; i++)
    // {
    //     var capabilities = WaveIn.GetCapabilities(i);
    //     devices.Add(new AudioDevice { Index = i, Name = capabilities.ProductName });
    // }

    return devices;
}

// Apply selected device
var waveIn = new WaveInEvent
{
    DeviceNumber = selectedDeviceIndex,  // User's selection
    WaveFormat = new WaveFormat(16000, 16, 1)
};
```

**Device mapping:**
- `MMDeviceEnumerator` provides full device names and IDs.
- `WaveInEvent.DeviceNumber` expects an index (0 to DeviceCount-1).
- Map `MMDevice` index to `WaveInEvent.DeviceNumber` via enumeration order.

**Default device:**
- If user hasn't selected a device, use `DeviceNumber = 0` (system default microphone).

**Confidence: HIGH** — NAudio.CoreAudioApi is included in NAudio 2.2.1. MMDeviceEnumerator is the recommended approach.

**Sources:**
- [NAudio GitHub: EnumerateOutputDevices.md](https://github.com/naudio/NAudio/blob/master/Docs/EnumerateOutputDevices.md)
- [Microsoft Q&A: Show all available input audio devices](https://learn.microsoft.com/en-us/answers/questions/1367599/my-question-is-how-to-show-all-the-available-input)
- [Access Microphone Audio with C#](https://swharden.com/csdv/audio/naudio/)
- [Listing Audio Recording Equipment using NAudio](https://copyprogramming.com/howto/enumerate-recording-devices-in-naudio)

---

### 3. Language Selection (PT/EN/Auto-Detect)

**Goal:** Allow users to specify transcription language (Portuguese, English, or auto-detect).

**Existing capability:** Azure.AI.OpenAI 2.1.0 `AudioTranscriptionOptions` includes `Language` property.

**What's needed:**
- **Language parameter:** Pass ISO-639-1 code to `AudioTranscriptionOptions.Language`.
- **Supported codes:** `"pt"` (Portuguese), `"en"` (English), `null` (auto-detect).

**Implementation approach:**

```csharp
using Azure.AI.OpenAI;

// Language configuration options
public enum TranscriptionLanguage
{
    AutoDetect,  // null (Whisper auto-detects)
    Portuguese,  // "pt"
    English      // "en"
}

// Apply language to transcription
var options = new AudioTranscriptionOptions
{
    DeploymentName = "whisper-deployment",
    AudioData = BinaryData.FromBytes(audioBytes),
    Filename = "audio.wav",
    Language = selectedLanguage switch
    {
        TranscriptionLanguage.Portuguese => "pt",
        TranscriptionLanguage.English => "en",
        TranscriptionLanguage.AutoDetect => null,
        _ => null
    }
};

var result = await client.GetAudioTranscriptionAsync(options);
string transcription = result.Value.Text;
```

**Supported languages:**
- Whisper API supports 99 languages (Afrikaans, Arabic, Chinese, English, French, German, Hindi, Italian, Japanese, Korean, Portuguese, Russian, Spanish, and 86+ more).
- Language codes follow ISO-639-1 format (two-letter codes: `"en"`, `"pt"`, `"es"`, `"fr"`, etc.).
- If `Language` is `null`, Whisper auto-detects language from audio (default behavior).

**Trade-offs:**
- **Auto-detect (null):** Flexible, but may add 1-2 seconds to processing time for language detection.
- **Specified language:** Faster processing, better accuracy if user knows language in advance.

**For Coxixo use case:**
- **Portuguese users:** Set `Language = "pt"` (faster, more accurate).
- **English users:** Set `Language = "en"`.
- **Mixed use:** Use auto-detect, accept slight latency.

**Confidence: HIGH** — Azure.AI.OpenAI 2.1.0 supports language parameter. ISO-639-1 codes verified.

**Sources:**
- [Speech to text with the Azure OpenAI Whisper model](https://learn.microsoft.com/en-us/azure/ai-foundry/openai/whisper-quickstart?view=foundry-classic)
- [OpenAI Whisper Configuration | FoloToy Docs](https://docs.folotoy.com/docs/configuration/stt/openai-whisper/)
- [Supported Languages | Whisper API Docs](https://whisper-api.com/docs/languages/)
- [Whisper (transcribe) API verbose_json results, format of language property?](https://community.openai.com/t/whisper-transcribe-api-verbose-json-results-format-of-language-property/646014)

---

### 4. Windows Startup (Launch on Boot)

**Goal:** Allow app to start automatically when Windows boots.

**Two approaches available:**

#### Approach A: Registry Run Key (Recommended for Simplicity)

**How it works:**
- Add executable path to `HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run`.
- Windows launches app automatically when user logs in.

**Implementation:**

```csharp
using Microsoft.Win32;

public void EnableStartup(bool enable)
{
    const string appName = "Coxixo";
    string exePath = Application.ExecutablePath;

    using (RegistryKey key = Registry.CurrentUser.OpenSubKey(
        @"Software\Microsoft\Windows\CurrentVersion\Run", true))
    {
        if (enable)
        {
            key.SetValue(appName, exePath);
        }
        else
        {
            key.DeleteValue(appName, false);
        }
    }
}

public bool IsStartupEnabled()
{
    const string appName = "Coxixo";
    using (RegistryKey key = Registry.CurrentUser.OpenSubKey(
        @"Software\Microsoft\Windows\CurrentVersion\Run", false))
    {
        return key?.GetValue(appName) != null;
    }
}
```

**Pros:**
- Simple to implement (5 lines of code).
- No dependencies beyond `Microsoft.Win32` (built-in).
- Standard pattern for user-level startup.

**Cons:**
- User-level only (HKEY_CURRENT_USER). For all-users, need HKEY_LOCAL_MACHINE (requires admin).
- No advanced scheduling (delay, retry, conditions).
- Less robust than Task Scheduler (e.g., no recovery on failure).

**Confidence: HIGH** — Standard approach for lightweight startup. Built-in .NET API.

**Sources:**
- [Run and RunOnce Registry Keys - Win32 apps](https://learn.microsoft.com/en-us/windows/win32/setupapi/run-and-runonce-registry-keys)
- [Guide: Add and Remove Windows Startup Programs](https://www.ninjaone.com/blog/check-and-manage-windows-startup-programs/)

#### Approach B: Task Scheduler (Recommended for Production)

**How it works:**
- Create scheduled task with "At startup" trigger.
- More robust, supports delayed start, retry on failure, elevation without UAC prompts.

**Implementation:**

```csharp
using System.Diagnostics;

public void EnableStartupViaTaskScheduler(bool enable)
{
    const string taskName = "CoxixoStartup";
    string exePath = Application.ExecutablePath;

    if (enable)
    {
        // Create scheduled task using schtasks.exe
        var psi = new ProcessStartInfo
        {
            FileName = "schtasks.exe",
            Arguments = $"/Create /TN \"{taskName}\" /TR \"{exePath}\" " +
                       "/SC ONLOGON /RL HIGHEST /F",
            UseShellExecute = false,
            CreateNoWindow = true
        };
        Process.Start(psi)?.WaitForExit();
    }
    else
    {
        // Delete scheduled task
        var psi = new ProcessStartInfo
        {
            FileName = "schtasks.exe",
            Arguments = $"/Delete /TN \"{taskName}\" /F",
            UseShellExecute = false,
            CreateNoWindow = true
        };
        Process.Start(psi)?.WaitForExit();
    }
}

public bool IsStartupEnabledViaTaskScheduler()
{
    const string taskName = "CoxixoStartup";
    var psi = new ProcessStartInfo
    {
        FileName = "schtasks.exe",
        Arguments = $"/Query /TN \"{taskName}\"",
        UseShellExecute = false,
        CreateNoWindow = true,
        RedirectStandardOutput = true
    };

    var process = Process.Start(psi);
    process?.WaitForExit();
    return process?.ExitCode == 0;  // 0 = task exists, 1 = task not found
}
```

**Pros:**
- More robust than registry Run key.
- Supports delayed start (e.g., 30 seconds after logon to avoid boot congestion).
- Supports elevated privileges without UAC prompts (if needed).
- Includes execution history and failure recovery.

**Cons:**
- Slightly more complex (requires spawning schtasks.exe).
- Task Scheduler service must be running (default on Windows).

**Confidence: HIGH** — Standard Windows approach. schtasks.exe is built into Windows.

**Sources:**
- [Task Scheduler for developers - Win32 apps](https://learn.microsoft.com/en-us/windows/win32/taskschd/task-scheduler-start-page)
- [Automating Service Start on Windows Startup with Task Scheduler](https://medium.com/@mcansener/automating-service-restart-on-windows-startup-with-task-scheduler-ca1e48879648)
- [Troubleshoot Task Scheduler Service Startup Failure](https://learn.microsoft.com/en-us/troubleshoot/windows-client/system-management-components/task-scheduler-service-not-start)

#### Recommendation: Start with Registry Run Key, Offer Task Scheduler Later

**v1.1 MVP:**
- Use **Registry Run key** for simplicity and zero complexity.
- Sufficient for most users (tray app doesn't need elevation).

**Future enhancement (v1.2+):**
- Add Task Scheduler option for users who want delayed start or enterprise deployment.

**Confidence: HIGH** — Both approaches are well-documented and production-proven.

---

## What NOT to Add (Avoiding Unnecessary Dependencies)

### NO New NuGet Packages Required

| Feature | Existing Solution | Why Not Add Package |
|---------|-------------------|---------------------|
| Hotkey modifiers | GetAsyncKeyState() P/Invoke | Built-in Win32 API, zero overhead |
| Microphone enumeration | NAudio.CoreAudioApi (already included) | Already in NAudio 2.2.1 |
| Language selection | Azure.AI.OpenAI Language property | Already in Azure.AI.OpenAI 2.1.0 |
| Windows startup | Registry or schtasks.exe | Built-in .NET and Windows APIs |

### Rejected Third-Party Options

| Library | Feature | Why Not |
|---------|---------|---------|
| GlobalHotKeys (NuGet) | Hotkey modifiers | Unnecessary — GetAsyncKeyState() is simpler |
| TaskScheduler (NuGet wrapper) | Windows startup | Unnecessary — schtasks.exe or Registry API works |
| System.Speech (built-in) | Language detection | Not needed — Whisper auto-detects |

---

## Version Verification

| Component | Current Version | v1.1 Status | Notes |
|-----------|----------------|-------------|-------|
| .NET | 8.0 | **Compatible** | All features work with .NET 8 |
| NAudio | 2.2.1 | **Compatible** | MMDeviceEnumerator included |
| Azure.AI.OpenAI | 2.1.0 | **Compatible** | Language property supported |
| WH_KEYBOARD_LL hook | Win32 API | **Compatible** | GetAsyncKeyState() works with existing hook |

**Confidence: HIGH** — No version upgrades needed. All features achievable with current stack.

---

## Integration Points with Existing Code

### 1. Hotkey Modifier Detection
**Integrates with:** Existing `SetHook()` and `HookCallback()` in keyboard hook implementation.
**Change required:** Add `GetAsyncKeyState()` checks in `HookCallback()` before triggering recording.

### 2. Microphone Enumeration
**Integrates with:** Existing `WaveInEvent` initialization in audio capture logic.
**Change required:** Add settings UI for device selection. Apply `DeviceNumber` property.

### 3. Language Selection
**Integrates with:** Existing Azure OpenAI API call in transcription logic.
**Change required:** Add `Language` property to `AudioTranscriptionOptions`. Default to `null` (auto-detect).

### 4. Windows Startup
**Integrates with:** Settings/preferences system (new).
**Change required:** Add checkbox in settings UI. Call registry/schtasks functions on toggle.

**All integrations are additive** — no breaking changes to existing v1.0 code.

---

## Confidence Summary

| Feature | Implementation | Confidence | Reason |
|---------|---------------|------------|--------|
| **Hotkey modifiers** | GetAsyncKeyState() P/Invoke | HIGH | Standard Win32 API, verified in existing hook pattern |
| **Microphone selection** | NAudio.CoreAudioApi.MMDeviceEnumerator | HIGH | Already in NAudio 2.2.1, verified in docs |
| **Language selection** | Azure.AI.OpenAI Language property | HIGH | Verified in Azure.AI.OpenAI 2.1.0 API docs |
| **Windows startup (Registry)** | Microsoft.Win32.Registry API | HIGH | Built-in .NET, standard pattern |
| **Windows startup (Task Scheduler)** | schtasks.exe via Process.Start() | HIGH | Standard Windows utility, production-proven |

**Overall confidence: HIGH** — All features verified as achievable with existing stack. Zero new dependencies required.

---

## Sources

### Official Microsoft Documentation
- [Microsoft Learn: GetAsyncKeyState function](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getasynckeystate)
- [Microsoft Learn: Check which modifier key is pressed - Windows Forms .NET](https://learn.microsoft.com/en-us/dotnet/desktop/winforms/input-keyboard/how-to-check-modifier-key?view=netdesktop-8.0)
- [Microsoft Learn: Run and RunOnce Registry Keys](https://learn.microsoft.com/en-us/windows/win32/setupapi/run-and-runonce-registry-keys)
- [Microsoft Learn: Task Scheduler for developers](https://learn.microsoft.com/en-us/windows/win32/taskschd/task-scheduler-start-page)
- [Microsoft Learn: Speech to text with the Azure OpenAI Whisper model](https://learn.microsoft.com/en-us/azure/ai-foundry/openai/whisper-quickstart?view=foundry-classic)
- [Microsoft Learn: Troubleshoot Task Scheduler Service Startup Failure](https://learn.microsoft.com/en-us/troubleshoot/windows-client/system-management-components/task-scheduler-service-not-start)

### NAudio Documentation and Examples
- [NAudio GitHub: EnumerateOutputDevices.md](https://github.com/naudio/NAudio/blob/master/Docs/EnumerateOutputDevices.md)
- [Microsoft Q&A: Show all available input audio devices](https://learn.microsoft.com/en-us/answers/questions/1367599/my-question-is-how-to-show-all-the-available-input)
- [Access Microphone Audio with C#](https://swharden.com/csdv/audio/naudio/)
- [Listing Audio Recording Equipment using NAudio](https://copyprogramming.com/howto/enumerate-recording-devices-in-naudio)
- [C# Microphone Level Monitor](https://swharden.com/blog/2021-07-03-csharp-microphone/)

### Whisper API Language Support
- [Supported Languages | Whisper API Docs](https://whisper-api.com/docs/languages/)
- [OpenAI Whisper Configuration | FoloToy Docs](https://docs.folotoy.com/docs/configuration/stt/openai-whisper/)
- [Whisper (transcribe) API verbose_json results, format of language property?](https://community.openai.com/t/whisper-transcribe-api-verbose-json-results-format-of-language-property/646014)
- [GitHub: openai/whisper](https://github.com/openai/whisper)

### Global Hotkey Implementation
- [How to register a global hotkey for your application in C#](https://www.fluxbytes.com/csharp/how-to-register-a-global-hotkey-for-your-application-in-c/)
- [pinvoke.net: registerhotkey (user32)](https://www.pinvoke.net/default.aspx/user32/registerhotkey.html)
- [Global Hotkeys within Desktop Applications - CodeProject](https://www.codeproject.com/Articles/1273010/Global-Hotkeys-within-Desktop-Applications)

### Windows Startup Methods
- [Guide: Add and Remove Windows Startup Programs](https://www.ninjaone.com/blog/check-and-manage-windows-startup-programs/)
- [How to Run Apps at Windows 11 Startup: 4 Reliable Methods](https://windowsforum.com/threads/how-to-run-apps-at-windows-11-startup-4-reliable-methods.389680/)
- [Automating Service Start on Windows Startup with Task Scheduler](https://medium.com/@mcansener/automating-service-restart-on-windows-startup-with-task-scheduler-ca1e48879648)
