# Domain Pitfalls: Windows System Tray App Feature Extensions

**Domain:** Windows system tray voice transcription app
**Features:** Hotkey modifiers, microphone selection, language selection, Windows startup
**Researched:** 2026-02-09

---

## Critical Pitfalls

Mistakes that cause rewrites, system instability, or major user-facing failures.

### Pitfall 1: Modifier Key State Desynchronization (Stuck Modifiers)

**What goes wrong:** After adding Ctrl/Shift/Alt modifier support to WH_KEYBOARD_LL hook, modifier keys can become "stuck" in a pressed state. Users experience Ctrl appearing to stay held down after releasing the physical key, causing every key press to behave as Ctrl+Key. The system believes the modifier is pressed when it physically isn't.

**Why it happens:** Three root causes compound this issue:

1. **Race condition between GetKeyState and actual keyboard events**: Your hook callback runs on a different thread than UI messages. Between checking `GetKeyState(VK_CONTROL) & 0x8000` and firing your hotkey event, the user may have released the physical key. The hook knows the key was released, but your event handler fires with stale modifier state.

2. **Hook chain interference**: When your hook processes a modifier key event and calls `CallNextHookEx`, other hooks in the chain (antivirus software, anti-cheat systems, gaming overlays, accessibility tools) may swallow or transform the WM_KEYUP message. Your hook receives WM_KEYDOWN but never sees the corresponding WM_KEYUP.

3. **Auto-repeat suppression breaking state tracking**: Your existing code filters auto-repeat with `!_isKeyDown` flag (line 110 of KeyboardHookService.cs). Adding modifier checks introduces a second state variable. If you track `_ctrlPressed` separately but the OS generates auto-repeat events for Ctrl while user holds both Ctrl+F8, the flag gets out of sync because you're filtering the very events that would restore correct state.

**Consequences:**
- User must click Ctrl/Alt/Shift physically or use On-Screen Keyboard to "unstick" the modifier
- Every subsequent hotkey press behaves incorrectly (plain F8 triggers as Ctrl+F8)
- Users blame your app for "breaking their keyboard"
- Issue is intermittent and nearly impossible to reproduce in testing (timing-dependent)

**Prevention:**

Use **GetAsyncKeyState inside the hook callback** instead of GetKeyState:

```csharp
// Inside HookCallback, after confirming key == _targetKey
private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
{
    if (nCode >= 0)
    {
        int vkCode = Marshal.ReadInt32(lParam);
        var key = (Keys)vkCode;

        if (key == _targetKey)
        {
            var msg = wParam.ToInt32();

            // Sample CURRENT physical modifier state using GetAsyncKeyState
            // 0x8000 = high bit set means key is currently down
            bool ctrlDown = (GetAsyncKeyState(VK_CONTROL) & 0x8000) != 0;
            bool shiftDown = (GetAsyncKeyState(VK_SHIFT) & 0x8000) != 0;
            bool altDown = (GetAsyncKeyState(VK_MENU) & 0x8000) != 0;

            if ((msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN) && !_isKeyDown)
            {
                _isKeyDown = true;
                // Pass modifier state in event args (create new event type)
                HotkeyPressed?.Invoke(this, new HotkeyEventArgs
                {
                    Ctrl = ctrlDown,
                    Shift = shiftDown,
                    Alt = altDown
                });
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

[DllImport("user32.dll")]
private static extern short GetAsyncKeyState(int vKey);

private const int VK_CONTROL = 0x11;
private const int VK_SHIFT = 0x10;
private const int VK_MENU = 0x12; // Alt key
```

**Why GetAsyncKeyState not GetKeyState**: GetAsyncKeyState queries the **current physical hardware state** at call time, while GetKeyState returns the state as of the last message processed by the calling thread's message queue. Since your hook runs on a different thread, GetKeyState gives you stale information. [GetAsyncKeyState Microsoft Docs](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getasynckeystate)

**Do NOT track modifier state yourself**—do not add `_ctrlDown`, `_shiftDown` flags and toggle them on WM_KEYDOWN/WM_KEYUP. The hook chain may drop events, causing your flags to permanently desync.

**Detection warning signs:**
- Bug reports mention "Ctrl gets stuck after using hotkey"
- Issue only reproducible when specific software running (Discord, OBS, antivirus)
- Works fine in clean Windows install, fails in production environments
- Happens more frequently with Alt than Ctrl/Shift (because Alt triggers WM_SYSKEYDOWN/UP which some hooks handle differently)

**Phase to address:** Phase 1 (Hotkey Modifiers) — prevent during initial implementation. If this manifests as a bug later, the fix requires changing the hook callback's modifier detection strategy, which could destabilize recording state if implemented incorrectly.

---

### Pitfall 2: Microphone Device Enumeration Invalidation

**What goes wrong:** User selects "USB Microphone (Device 3)" from your settings dropdown. Later, they unplug a different USB audio device. NAudio device indices shift—Device 3 is now a webcam microphone. Your app records from the wrong device or crashes with `MmException.BadDeviceId`. The device the user selected no longer exists at the stored index.

**Why it happens:** NAudio's `WaveIn.DeviceCount` and `WaveIn.GetCapabilities(int deviceNumber)` enumerate devices by **index position**, not stable identifier. Windows audio subsystem reindexes devices when hardware changes:

1. User has: [0] Realtek onboard, [1] USB Mic A, [2] USB Mic B, [3] USB Headset
2. User stores `DeviceNumber = 3` in settings (USB Headset)
3. User unplugs USB Mic A
4. Enumeration becomes: [0] Realtek, [1] USB Mic B, [2] USB Headset
5. Your app tries to record from `DeviceNumber = 3` → `BadDeviceId` exception
6. OR Device 3 now exists but is a different device (webcam was plugged in)

This is worse than just throwing an exception—**silent failure means recording from the wrong microphone with no user indication**.

**Consequences:**
- App records from built-in laptop mic when user expected external podcast mic
- `StartCapture()` throws `MmException.BadDeviceId`, recording silently fails
- User reports "settings don't save" because device numbers reassign between app restarts
- Privacy concern: recording from unexpected device

**Prevention:**

Store **device product name + instance identifier**, not just index. On `StartCapture()`, re-enumerate devices and find matching device by name. Fallback to default device if not found:

```csharp
// AppSettings.cs
public class AppSettings
{
    // OLD: public int MicrophoneDeviceNumber { get; set; } = -1;
    // NEW: Store identifying information
    public string? MicrophoneDeviceProductName { get; set; } = null; // e.g., "USB Audio Device"
    public int MicrophoneDeviceNumber { get; set; } = -1; // Used as hint, not source of truth
}

// AudioCaptureService.cs
public void StartCapture()
{
    int deviceNumber = ResolveMicrophoneDevice(
        _settings.MicrophoneDeviceProductName,
        _settings.MicrophoneDeviceNumber
    );

    // existing code but use resolved deviceNumber
    _waveIn = new WaveInEvent
    {
        DeviceNumber = deviceNumber, // -1 for default, or resolved index
        WaveFormat = waveFormat,
        BufferMilliseconds = 50
    };
    // ... rest of existing StartCapture code
}

private int ResolveMicrophoneDevice(string? productName, int lastKnownIndex)
{
    // No saved device? Use default
    if (string.IsNullOrEmpty(productName))
        return -1;

    // Try to find device by product name
    for (int i = 0; i < WaveIn.DeviceCount; i++)
    {
        var caps = WaveIn.GetCapabilities(i);
        if (caps.ProductName == productName)
        {
            // Found matching device by name
            return i;
        }
    }

    // Device name not found (unplugged?). Try last known index as fallback
    if (lastKnownIndex >= 0 && lastKnownIndex < WaveIn.DeviceCount)
    {
        var caps = WaveIn.GetCapabilities(lastKnownIndex);
        Debug.WriteLine($"Saved device '{productName}' not found. Using device at saved index: {caps.ProductName}");
        return lastKnownIndex;
    }

    // Fallback to default device
    Debug.WriteLine($"Saved device '{productName}' not found. Using default microphone.");
    return -1;
}
```

**Why product name**: `WaveInCapabilities.ProductName` is relatively stable across device connections. It's the driver-reported name like "USB Audio Device" or "Realtek High Definition Audio". Not perfect (multiple identical USB mics have same name), but vastly better than raw index.

**Detection warning signs:**
- "Microphone settings reset when I restart the app"
- "App uses wrong microphone after unplugging my headset"
- `BadDeviceId` exceptions in logs when device count < saved device number

**Phase to address:** Phase 2 (Microphone Selection) — implement device resolution from the start. Retrofitting this after shipping with index-only storage means user settings data migration.

**References:**
- [NAudio Device Enumeration Docs](https://github.com/naudio/NAudio/blob/master/Docs/EnumerateOutputDevices.md)
- [NAudio WaveIn.GetCapabilities Example](https://www.csharpcodi.com/csharp-examples/NAudio.Wave.WaveIn.GetCapabilities(int)/)

---

### Pitfall 3: WaveInEvent Deadlock During Device Switching

**What goes wrong:** User switches microphone in settings dropdown. UI freezes, application becomes unresponsive. Task Manager shows app is "Running" but won't respond. Must force-kill process. Happens specifically when switching devices while recording, or rapidly changing device selection.

**Why it happens:** NAudio's WME (Windows Multimedia Extensions) backend has known deadlock issues when `WaveInEvent` is disposed or device properties changed while actively recording. The WME callback thread and your UI thread lock on different resources in opposite order:

1. UI thread: Calls `StopCapture()` → locks on `_waveIn` → calls `waveIn.StopRecording()` → waits for WME callback thread to exit
2. WME callback thread: In `OnDataAvailable`, tries to acquire lock on `_writer` → your dispose code holds this lock
3. Classic A→B, B→A deadlock

Compounded by [NAudio Issue #1203](https://github.com/naudio/NAudio/issues/1203): "Frequent switching of input devices cause StartRecording function deadlock. Rapidly and frequently plugging and unplugging USB devices can lead to a deadlock when the program runs to the StartRecording function of WaveInEvent, causing applications to become unresponsive."

**Consequences:**
- Application hard lock requiring force quit
- User loses any unsaved configuration
- Terrible UX—microphone settings UI becomes dangerous to interact with
- Happens inconsistently (race condition), making it hard to test

**Prevention:**

1. **Never dispose WaveInEvent while recording**—always stop first, wait for stop confirmation, then dispose:

```csharp
// Current code from AudioCaptureService.cs line 219 is CORRECT for this:
public void Dispose()
{
    if (_disposed) return;
    _disposed = true;

    if (_isRecording)
    {
        _isRecording = false;
        try { _waveIn?.StopRecording(); } catch { }
    }

    CleanupRecording(); // Only cleanup AFTER stopping
}
```

2. **When adding device switching**, stop recording before switching:

```csharp
public void SwitchMicrophone(string productName, int deviceNumber)
{
    bool wasRecording = _isRecording;

    // CRITICAL: Stop existing recording completely before switching
    if (_isRecording)
    {
        StopCapture(); // This calls StopRecording and CleanupRecording
        // DO NOT just call _waveIn.StopRecording() — you need full cleanup
    }

    // Update settings
    _settings.MicrophoneDeviceProductName = productName;
    _settings.MicrophoneDeviceNumber = deviceNumber;

    // If was recording, restart with new device
    // DON'T restart immediately—let user explicitly start recording again
    // or add small delay: await Task.Delay(100);
}
```

3. **Avoid recreating WaveInEvent on every hotkey press**—reuse instance:

Current code creates fresh `WaveInEvent` in `StartCapture()` (line 70). This is fine for push-to-talk, but when adding device selection, you might be tempted to recreate it every time settings change. **Don't**. Only recreate when device actually changes:

```csharp
private int _currentDeviceNumber = -1;

public void StartCapture()
{
    if (_isRecording) return;

    // Only recreate WaveInEvent if device changed
    int resolvedDevice = ResolveMicrophoneDevice(...);
    if (_waveIn != null && _currentDeviceNumber != resolvedDevice)
    {
        // Device changed, must recreate
        CleanupRecording();
        _waveIn = null;
    }

    if (_waveIn == null)
    {
        _currentDeviceNumber = resolvedDevice;
        _waveIn = new WaveInEvent { DeviceNumber = resolvedDevice, ... };
        _waveIn.DataAvailable += OnDataAvailable;
        _waveIn.RecordingStopped += OnRecordingStoppedInternal;
    }

    // Reuse existing _waveIn instance if device unchanged
    // (existing code continues)
}
```

**Detection warning signs:**
- UI freeze when changing device in settings dropdown
- Deadlock happens specifically during recording state
- Stack traces show thread waiting in `waveInStop()` or `waveInClose()`
- More frequent on slower machines or USB audio devices

**Phase to address:** Phase 2 (Microphone Selection) — build device switching logic with deadlock prevention from the start. Retrofitting this requires refactoring the entire lifecycle of WaveInEvent.

**References:**
- [NAudio Issue #1203: Device switching deadlock](https://github.com/naudio/NAudio/issues/1203)
- [NAudio WaveInEvent Source](https://github.com/naudio/NAudio/blob/master/NAudio.WinMM/WaveInEvent.cs)

---

### Pitfall 4: Windows Startup Registry Write Permission Denied

**What goes wrong:** User enables "Start with Windows" in settings. App shows success message, but on next login the app doesn't auto-start. No error visible to user. Registry write silently failed due to insufficient permissions.

**Why it happens:** Writing to `HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Run` requires **administrator elevation**. Your app runs as standard user. Writing to this key fails with `UnauthorizedAccessException` or silently does nothing depending on how you call the registry API.

Two failure modes:

1. **Hard failure**: `Registry.SetValue()` throws `UnauthorizedAccessException`. You catch it but don't inform user. Checkbox appears enabled but feature doesn't work.

2. **Silent failure via UAC virtualization**: On some Windows configurations, writes to HKLM are virtualized to `HKEY_CURRENT_USER\Software\Classes\VirtualStore\MACHINE\...`. Your app thinks write succeeded, but Windows doesn't read from virtualized location on startup. Feature appears to work in your testing, fails in production.

**Consequences:**
- "Start with Windows doesn't work" bug reports
- User re-enables setting multiple times, assuming it's broken
- Confusing because it works for users who "Run as Administrator" (making bug hard to reproduce)

**Prevention:**

**Use `HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Run` instead of HKLM**. This key does NOT require elevation and works for per-user startup (which is what you want):

```csharp
public void SetStartWithWindows(bool enabled)
{
    const string appName = "Coxixo";
    const string keyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

    try
    {
        // CRITICAL: Use CurrentUser, not LocalMachine
        using var key = Registry.CurrentUser.OpenSubKey(keyPath, writable: true);
        if (key == null)
        {
            throw new InvalidOperationException("Could not open registry key");
        }

        if (enabled)
        {
            string exePath = Application.ExecutablePath;

            // CRITICAL: Quote the path to handle spaces
            string quotedPath = $"\"{exePath}\"";

            // Optional: Add startup argument to detect auto-start
            // string quotedPath = $"\"{exePath}\" --autostart";

            key.SetValue(appName, quotedPath, RegistryValueKind.String);
        }
        else
        {
            // Remove registry value
            key.DeleteValue(appName, throwOnMissingValue: false);
        }
    }
    catch (UnauthorizedAccessException ex)
    {
        // Should NOT happen with HKCU, but handle anyway
        throw new InvalidOperationException(
            "Cannot modify startup settings. Registry access denied.", ex);
    }
    catch (Exception ex)
    {
        throw new InvalidOperationException(
            $"Failed to update startup settings: {ex.Message}", ex);
    }
}
```

**Critical details:**

1. **Quote the executable path**: If path contains spaces (e.g., `C:\Program Files\Coxixo\Coxixo.exe`), unquoted path will be parsed as `C:\Program` with arguments `Files\Coxixo\...`. Windows won't find the executable. [Microsoft Docs: Quoting Requirements](https://devblogs.microsoft.com/oldnewthing/20070515-00/?p=26863)

2. **HKCU vs HKLM trade-off**:
   - HKCU: Per-user, no elevation needed, safe choice for tray apps
   - HKLM: System-wide (all users), requires admin, appropriate for system services only

3. **Detect if already registered** before showing checkbox state:

```csharp
public bool IsStartWithWindowsEnabled()
{
    const string appName = "Coxixo";
    const string keyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

    try
    {
        using var key = Registry.CurrentUser.OpenSubKey(keyPath, writable: false);
        var value = key?.GetValue(appName) as string;
        return value != null;
    }
    catch
    {
        return false;
    }
}
```

**Alternative approach: Startup folder shortcut** (avoid if possible, registry is cleaner):

```csharp
string startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
string shortcutPath = Path.Combine(startupFolder, "Coxixo.lnk");

if (enabled)
{
    // Requires COM interop to create .lnk file (IWshShortcut)
    // More complex, avoid unless registry approach fails
}
```

**Detection warning signs:**
- Works when you test running as Administrator
- Fails for standard users
- Bug reports: "I enabled it but it doesn't start on login"
- No exception logs (because write succeeded to virtualized location)

**Phase to address:** Phase 4 (Windows Startup) — use HKCU from the start. No reason to ever use HKLM for a per-user tray app.

**References:**
- [Run and RunOnce Registry Keys - Microsoft Docs](https://learn.microsoft.com/en-us/windows/win32/setupapi/run-and-runonce-registry-keys)
- [UAC Registry Virtualization](https://learn.microsoft.com/en-us/windows/security/application-security/application-control/user-account-control/settings-and-configuration)
- [Command Line Quoting - Old New Thing](https://devblogs.microsoft.com/oldnewthing/20070515-00/?p=26863)

---

## Moderate Pitfalls

Fixable but create significant user friction or edge-case bugs.

### Pitfall 5: Whisper API Language Hallucination on Silence

**What goes wrong:** User briefly taps hotkey by accident (under 500ms, correctly discarded by your minimum duration filter). Or user holds hotkey for 1 second but doesn't speak—just background noise. Whisper API returns transcription like "Thank you for watching!" or "you" or "so" instead of empty string. Garbage text gets pasted into user's active window.

**Why it happens:** Whisper has a well-documented hallucination problem on silent or non-speech audio. According to [Whisper GitHub Discussion #1606](https://github.com/openai/whisper/discussions/1606), "hallucination on audio with no speech" is reproducible. The model interprets background hiss, breathing, or ambient noise as speech. [Research shows](https://arxiv.org/html/2505.12969v1) "55.2% of non-speech audios being transcribed into 'so'", and "8.5% of hallucinations span more than five tokens".

This is WORSE when you omit the `language` parameter (auto-detection mode). Per [OpenAI Community discussion](https://community.openai.com/t/whisper-is-there-a-way-to-tell-the-language-before-recognition/70687): "When you leave off the language option, Whisper attempts to guess the language from the first 30 seconds of audio. This guess will be less reliable than if you had explicitly told Whisper which language to use."

Current code hardcodes `Language = "pt"` (line 54 of TranscriptionService.cs), which helps. But when you add language selection with "Auto-detect" option, hallucination rate will spike.

**Consequences:**
- Random phrases paste into user's document when they didn't speak
- "Thank you for watching" is common hallucination (from YouTube video training data)
- User reports: "App types random words when I don't say anything"
- Undermines trust in transcription accuracy

**Prevention:**

1. **Keep explicit language selection, avoid auto-detect as default**:

```csharp
// In TranscriptionService.TranscribeAsync
var options = new AudioTranscriptionOptions
{
    ResponseFormat = AudioTranscriptionFormat.Text,
    Language = _language ?? "pt"  // Never null, always explicit
};
```

If you must offer auto-detect, make it opt-in and warn user about hallucination risk.

2. **Client-side silence detection BEFORE sending to API**:

```csharp
// In AudioCaptureService, before returning audio data
public byte[]? StopCapture()
{
    // ... existing code ...

    if (audioData != null && audioData.Length > 44)
    {
        // Add basic energy threshold check
        if (IsEffectivelySilent(audioData))
        {
            Debug.WriteLine("Audio below energy threshold, treating as silence");
            RecordingDiscarded?.Invoke(this, EventArgs.Empty);
            return null;
        }

        RecordingStopped?.Invoke(this, EventArgs.Empty);
        return audioData;
    }

    // ...
}

private bool IsEffectivelySilent(byte[] wavData)
{
    // Skip WAV header (44 bytes), analyze 16-bit samples
    const int headerSize = 44;
    const int energyThreshold = 500; // Tune based on testing

    long totalEnergy = 0;
    int sampleCount = (wavData.Length - headerSize) / 2;

    for (int i = headerSize; i < wavData.Length - 1; i += 2)
    {
        short sample = (short)(wavData[i] | (wavData[i + 1] << 8));
        totalEnergy += Math.Abs(sample);
    }

    double averageEnergy = totalEnergy / (double)sampleCount;
    return averageEnergy < energyThreshold;
}
```

This prevents sending near-silent audio to Whisper API entirely, saving API costs and avoiding hallucination.

3. **Post-process transcription to detect hallucinations**:

```csharp
public async Task<string?> TranscribeAsync(byte[] audioData, CancellationToken ct = default)
{
    // ... existing API call ...

    var result = await _audioClient.TranscribeAudioAsync(stream, "audio.wav", options, ct);
    string? text = result.Value.Text;

    // Filter common hallucinations
    if (IsLikelyHallucination(text))
    {
        Debug.WriteLine($"Detected likely hallucination: '{text}'");
        return null; // Treat as silence
    }

    return text;
}

private bool IsLikelyHallucination(string? text)
{
    if (string.IsNullOrWhiteSpace(text))
        return false;

    text = text.Trim().ToLowerInvariant();

    // Common Whisper hallucinations on silence
    string[] hallucinations = new[]
    {
        "thank you for watching",
        "thanks for watching",
        "you",
        "so",
        "uh",
        "um",
        // Add more based on your testing
    };

    return hallucinations.Contains(text);
}
```

**Detection warning signs:**
- User reports app "types random phrases when room is quiet"
- Common phrases: "thank you for watching", "so", "you"
- Happens more with auto-detect language than explicit language
- Happens more with short recordings (< 3 seconds of speech)

**Phase to address:**
- Phase 3 (Language Selection): When implementing auto-detect, add hallucination filtering
- Can retrofit into Phase 1 if you want energy-based silence detection early

**References:**
- [Whisper Hallucination on Silent Audio](https://github.com/openai/whisper/discussions/1606)
- [Calm-Whisper: Hallucination Research](https://arxiv.org/html/2505.12969v1)
- [OpenAI Community: Hallucination Solutions](https://community.openai.com/t/whisper-hallucination-how-to-recognize-and-solve/218307)

---

### Pitfall 6: Microphone Permission Denied on Windows 11

**What goes wrong:** User installs app on Windows 11, tries to record. App throws `MmException` with result `NotEnabled`. No audio captured. User sees error: "Microphone access denied. Check Windows privacy settings."

From AudioCaptureService.cs line 93, you already handle this case:

```csharp
NAudio.MmResult.NotEnabled => "Microphone access denied. Check Windows privacy settings."
```

But the error message doesn't tell user HOW to fix it, and Windows 11's privacy UI is buried deep.

**Why it happens:** Windows 11 has per-app microphone permissions similar to mobile OS. Even if microphone hardware exists and works in other apps, **your app specifically** can be blocked from accessing it. This is separate from UAC—it's a privacy toggle.

Default behavior: **Desktop apps are DENIED microphone access** until user explicitly grants permission in Settings > Privacy & security > Microphone > "Let desktop apps access your microphone".

**Consequences:**
- First-run experience is broken—user can't record anything
- Error message appears but user doesn't know what to do
- Support burden: "App says microphone denied but my mic works in Discord"

**Prevention:**

1. **Proactive permission check on first run**:

```csharp
// Call this in TrayApplicationContext initialization or first hotkey press
private async Task CheckMicrophonePermission()
{
    try
    {
        // Attempt to create and immediately dispose a test recording
        using var testWaveIn = new WaveInEvent();
        testWaveIn.StartRecording();
        testWaveIn.StopRecording();
        // If we get here, permission granted
    }
    catch (NAudio.MmException ex) when (ex.Result == NAudio.MmResult.NotEnabled)
    {
        // Permission denied - show helpful dialog
        ShowMicrophonePermissionDialog();
    }
}

private void ShowMicrophonePermissionDialog()
{
    var result = MessageBox.Show(
        "Coxixo needs microphone access to transcribe your voice.\n\n" +
        "Windows is currently blocking microphone access.\n\n" +
        "Click OK to open Privacy Settings, then:\n" +
        "1. Enable 'Microphone access'\n" +
        "2. Enable 'Let desktop apps access your microphone'\n\n" +
        "After enabling, restart Coxixo.",
        "Microphone Permission Required",
        MessageBoxButtons.OKCancel,
        MessageBoxIcon.Warning
    );

    if (result == DialogResult.OK)
    {
        // Open Windows Settings to microphone privacy page
        Process.Start(new ProcessStartInfo
        {
            FileName = "ms-settings:privacy-microphone",
            UseShellExecute = true
        });
    }
}
```

2. **Improve existing error message** in AudioCaptureService.cs:

```csharp
NAudio.MmResult.NotEnabled =>
    "Microphone access denied. Open Settings > Privacy & security > Microphone and enable 'Let desktop apps access your microphone'. Then restart Coxixo."
```

3. **Document in README/docs** that Windows 11 users must grant microphone permission before first use.

**Detection warning signs:**
- Works on Windows 10, fails on Windows 11
- Works for user A, fails for user B on same hardware
- `MmResult.NotEnabled` in exception logs
- User says "my microphone works in other apps"

**Phase to address:** Phase 2 (Microphone Selection) — add permission check UI. Can also add to Phase 1 if you want better first-run experience.

**References:**
- [Windows Microphone Privacy Settings - Microsoft](https://support.microsoft.com/en-us/windows/turn-on-app-permissions-for-your-microphone-in-windows-94991183-f69d-b4cf-4679-c98ca45f577a)
- [Dell: Microphone Not Working Due to Privacy Settings](https://www.dell.com/support/kbdoc/en-us/000133024/windows-10-microphone-not-working-due-to-privacy-settings)

---

### Pitfall 7: Keyboard Hook Conflicts with Anti-Cheat Software

**What goes wrong:** User reports "App stopped working after I installed [game with anti-cheat]". Your `SetWindowsHookEx` call starts failing with error code, or hook installs successfully but never receives events. App appears functional but hotkey doesn't respond.

**Why it happens:** Anti-cheat systems (Easy Anti-Cheat, BattleEye, FACEIT, Vanguard) and some antivirus software actively block or interfere with low-level keyboard hooks because they're a common attack vector:

- **Hook chain blocking**: Anti-cheat DLL inserts itself into the hook chain and filters/blocks events before they reach your hook
- **Hook installation denial**: System security software prevents `SetWindowsHookEx` from succeeding, returns null handle
- **Process flagging**: Your app is flagged as "suspicious macro tool" and terminated or sandboxed

Per [AutoHotkey Community](https://www.autohotkey.com/boards/viewtopic.php?t=38423): "Anti-cheat programs like XIGNCode have detected tools like AutoHotkey running, and some anti-cheat software flags programs with keyboard hook capabilities as potential cheating tools."

This is NOT a bug you can fix—it's intentional interference by security software.

**Consequences:**
- Hotkey silently stops working, no error visible to user
- App works fine on dev machine, fails in production for gamers
- User blames your app, not the anti-cheat software
- Support requests: "App worked yesterday, doesn't work today" (they installed a game)

**Prevention:**

1. **Robust hook installation error handling** (already present in KeyboardHookService.cs line 68-74, good):

```csharp
_hookId = SetHook(_proc);
if (_hookId == IntPtr.Zero)
{
    var error = Marshal.GetLastWin32Error();
    throw new InvalidOperationException(
        $"Failed to install keyboard hook. Error code: {error}");
}
```

2. **Detect when hook stops receiving events** (add heartbeat check):

```csharp
private DateTime _lastHookEvent = DateTime.UtcNow;

private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
{
    _lastHookEvent = DateTime.UtcNow; // Update on every callback
    // ... existing code
}

// Call this periodically (e.g., on a timer every 30 seconds)
private void CheckHookHealth()
{
    if (IsRunning && (DateTime.UtcNow - _lastHookEvent).TotalSeconds > 60)
    {
        // Hook hasn't received ANY keyboard events in 60 seconds - suspicious
        // Either hook is broken or user hasn't touched keyboard (unlikely for 60s)
        Debug.WriteLine("WARNING: Keyboard hook appears unhealthy");
        // Optionally: Show tray notification, attempt to reinstall hook
    }
}
```

3. **User-facing error message with actionable guidance**:

```csharp
catch (InvalidOperationException ex) when (ex.Message.Contains("keyboard hook"))
{
    MessageBox.Show(
        "Failed to start global hotkey monitoring.\n\n" +
        "This can happen if anti-cheat or security software is blocking keyboard hooks.\n\n" +
        "Try:\n" +
        "• Add Coxixo to your antivirus exclusion list\n" +
        "• Temporarily disable anti-cheat software\n" +
        "• Run Coxixo before starting games with anti-cheat\n\n" +
        $"Technical details: {ex.Message}",
        "Hotkey System Error",
        MessageBoxButtons.OK,
        MessageBoxIcon.Error
    );
}
```

4. **Document known conflicts** in README:

> **Known Compatibility Issues:**
> - Anti-cheat systems (Easy Anti-Cheat, BattleEye, Vanguard) may block hotkey functionality
> - Some antivirus software flags keyboard hooks as suspicious
> - Workaround: Add Coxixo.exe to security software exclusion list

**You cannot fix this programmatically**—anti-cheat working as designed. Best you can do is detect failure and inform user.

**Detection warning signs:**
- Bug reports mentioning specific games or antivirus software
- Hook installation fails with specific error codes (5 = Access Denied, 1428 = Hook procedure not found)
- Works on clean Windows, fails with security software installed
- User reports "worked before, stopped after installing [software]"

**Phase to address:** Phase 1 (Hotkey Modifiers) — add robust error handling and health checks. Document known conflicts in Phase 4 (Polish/docs).

**References:**
- [AutoHotkey and Anti-Cheat Conflicts](https://www.autohotkey.com/boards/viewtopic.php?t=38423)
- [Easy Anti-Cheat Compatibility Issues](https://4ddig.tenorshare.com/windows-fix/easy-anti-cheat-download-and-issue-fixes.html)

---

## Integration Gotchas

Specific to existing Coxixo architecture and the Azure/NAudio/WinForms stack.

### Gotcha 1: KeyboardHookService Thread Safety with AudioCaptureService

**The issue:** Your current KeyboardHookService (line 113) fires `HotkeyPressed` and `HotkeyReleased` events **on the hook callback thread**, which is a background thread managed by Windows. AudioCaptureService subscribes to these events and calls `StartCapture()` and `StopCapture()`.

NAudio's `WaveInEvent` is thread-safe for Start/Stop operations, but **creating** a new `WaveInEvent` (line 70) while disposing an old one from a different thread can cause race conditions.

When you add modifier key support, you might introduce conditional logic: "only start recording if Ctrl is pressed". This adds more complexity to the event handler, increasing risk of threading bugs.

**What goes wrong:**

```csharp
// In TrayApplicationContext or wherever you wire up events
_keyboardHook.HotkeyPressed += (s, e) => _audioCaptureService.StartCapture();
_keyboardHook.HotkeyReleased += (s, e) => _audioCaptureService.StopCapture();
```

This executes `StartCapture()` **on the hook callback thread**, not the UI thread. If `StartCapture()` throws an exception, it crashes the hook callback, breaking all keyboard input system-wide until hook is reinstalled.

**Prevention:**

**Marshal event handling to UI thread** using `Control.BeginInvoke` or similar:

```csharp
// In TrayApplicationContext
private void InitializeKeyboardHook()
{
    _keyboardHook.HotkeyPressed += OnHotkeyPressed;
    _keyboardHook.HotkeyReleased += OnHotkeyReleased;
}

private void OnHotkeyPressed(object? sender, EventArgs e)
{
    // CRITICAL: Marshal to UI thread (or use SynchronizationContext)
    // This ensures StartCapture runs on main thread, not hook callback thread
    if (_trayIcon.InvokeRequired) // Or use any Control/Form instance
    {
        _trayIcon.BeginInvoke(new Action(() => OnHotkeyPressed(sender, e)));
        return;
    }

    // Now safe to call StartCapture on UI thread
    try
    {
        _audioCaptureService.StartCapture();
    }
    catch (Exception ex)
    {
        // Handle gracefully - show notification, log, etc.
        Debug.WriteLine($"StartCapture failed: {ex.Message}");
    }
}

private void OnHotkeyReleased(object? sender, EventArgs e)
{
    if (_trayIcon.InvokeRequired)
    {
        _trayIcon.BeginInvoke(new Action(() => OnHotkeyReleased(sender, e)));
        return;
    }

    try
    {
        var audioData = _audioCaptureService.StopCapture();
        // ... existing transcription logic
    }
    catch (Exception ex)
    {
        Debug.WriteLine($"StopCapture failed: {ex.Message}");
    }
}
```

**Why this matters:** Hook callback thread has strict timing requirements. If you block it with long-running operations (creating NAudio instances, initializing audio devices), Windows may unhook your callback thinking it's unresponsive.

**Phase to address:** Phase 1 (Hotkey Modifiers) — verify existing event handlers are properly marshaled. Add explicit thread safety checks.

---

### Gotcha 2: AppSettings Migration for New Properties

**The issue:** You're currently storing settings in `%LOCALAPPDATA%\Coxixo\settings.json` (per ConfigurationService pattern). When you add new properties to `AppSettings`:

```csharp
public class AppSettings
{
    public Keys HotkeyKey { get; set; } = Keys.F8;
    // NEW in v1.1:
    public Keys HotkeyModifiers { get; set; } = Keys.None;
    public string? MicrophoneDeviceProductName { get; set; } = null;
    public int MicrophoneDeviceNumber { get; set; } = -1;
    public string TranscriptionLanguage { get; set; } = "pt";
    public bool StartWithWindows { get; set; } = false;
}
```

Existing user installations have `settings.json` files **without these properties**. JSON deserialization will use default values (good), but you need to handle:

1. **Property renames**: If you change `HotkeyKey` to `Hotkey`, existing settings will lose the user's configured key
2. **Semantics changes**: If you change `MicrophoneDeviceNumber` meaning from "index" to "stable ID", existing values are invalid
3. **Version detection**: How to know if loaded settings are from v1.0 or v1.1?

**What goes wrong:**
- User upgrades from v1.0 to v1.1
- v1.1 loads `settings.json`, gets default values for new properties
- User's F9 hotkey preference overwritten with F8 default
- No migration, user manually reconfigures

**Prevention:**

1. **Add version to AppSettings**:

```csharp
public class AppSettings
{
    public int SettingsVersion { get; set; } = 2; // v1.1
    // ... rest
}
```

2. **Implement migration logic** in ConfigurationService:

```csharp
public AppSettings LoadSettings()
{
    // ... existing load code ...

    var settings = JsonSerializer.Deserialize<AppSettings>(json)
                   ?? new AppSettings();

    // Migrate from older versions
    if (settings.SettingsVersion < 2)
    {
        MigrateFrom_V1_To_V2(settings);
        settings.SettingsVersion = 2;
        SaveSettings(settings); // Persist migration
    }

    return settings;
}

private void MigrateFrom_V1_To_V2(AppSettings settings)
{
    // v1.0 had Keys HotkeyKey, v1.1 splits to HotkeyKey + HotkeyModifiers
    // No migration needed, defaults are fine

    // If MicrophoneDeviceNumber was set in v1.0 as raw index,
    // it's now unreliable (Pitfall 2). Clear it to force re-selection:
    if (settings.MicrophoneDeviceNumber >= 0
        && string.IsNullOrEmpty(settings.MicrophoneDeviceProductName))
    {
        // Had device index but no product name - can't trust index
        settings.MicrophoneDeviceNumber = -1; // Reset to default
    }

    Debug.WriteLine("Migrated settings from v1.0 to v1.1");
}
```

3. **Never rename properties** that contain user data. Add new properties instead:

```csharp
// BAD:
// public Keys Hotkey { get; set; } // renamed from HotkeyKey

// GOOD:
public Keys HotkeyKey { get; set; } = Keys.F8; // Keep old name
```

**Phase to address:** Phase 1 (Hotkey Modifiers) — add `SettingsVersion` property and migration framework before shipping any new settings. Costs minimal effort now, prevents pain later.

---

### Gotcha 3: TranscriptionService Language Parameter Validation

**The issue:** Current code hardcodes `Language = "pt"` (TranscriptionService.cs line 54). When you add user-selectable language, you need to validate that Azure OpenAI Whisper accepts the language code.

Whisper accepts ISO 639-1 two-letter codes ("en", "pt", "fr", etc.), but if user somehow configures invalid code (typo in settings file, corruption, future refactoring bug), the API call fails.

**What goes wrong:**

```csharp
// User edits settings.json manually, typos:
{ "TranscriptionLanguage": "por" } // Wrong, should be "pt"

// API call fails with validation error
// No transcription, user sees generic error
```

**Prevention:**

1. **Validate language on settings load**:

```csharp
public class AppSettings
{
    private string _transcriptionLanguage = "pt";

    public string TranscriptionLanguage
    {
        get => _transcriptionLanguage;
        set => _transcriptionLanguage = ValidateLanguageCode(value);
    }

    private static string ValidateLanguageCode(string? code)
    {
        // Whisper supported languages (subset most relevant to users)
        string[] supportedLanguages = new[]
        {
            "en", "pt", "es", "fr", "de", "it", "ja", "ko", "zh",
            "ru", "ar", "hi", "nl", "pl", "tr", "vi", "id", "th"
        };

        if (string.IsNullOrEmpty(code) || !supportedLanguages.Contains(code.ToLower()))
        {
            Debug.WriteLine($"Invalid language code '{code}', defaulting to 'pt'");
            return "pt"; // Safe default
        }

        return code.ToLower();
    }
}
```

2. **UI dropdown only shows supported languages**:

```csharp
// In SettingsForm
private void PopulateLanguageDropdown()
{
    var languages = new[]
    {
        new { Code = "pt", Name = "Portuguese (Português)" },
        new { Code = "en", Name = "English" },
        new { Code = "es", Name = "Spanish (Español)" },
        // ... etc
    };

    languageComboBox.DataSource = languages;
    languageComboBox.DisplayMember = "Name";
    languageComboBox.ValueMember = "Code";
}
```

3. **Handle auto-detect as special case**:

```csharp
// In TranscriptionService
var options = new AudioTranscriptionOptions
{
    ResponseFormat = AudioTranscriptionFormat.Text,
    // Omit Language property entirely for auto-detect (don't pass empty string)
};

if (!string.IsNullOrEmpty(_language) && _language != "auto")
{
    options.Language = _language;
}
```

**Phase to address:** Phase 3 (Language Selection) — validate on implementation. Very easy to forget, catches bugs early.

---

## Technical Debt Risks

Things that won't break immediately but make future changes harder.

### Debt 1: Hardcoded Constant Duplication Across Services

**What it is:** Magic numbers and constants scattered across multiple files:

- `MinDurationMs = 500` in AudioCaptureService.cs (line 15)
- Whisper API model name "whisper" in AppSettings.cs (line 24)
- Key codes like `VK_CONTROL = 0x11` will be duplicated in KeyboardHookService when adding modifiers
- Error messages duplicated between services

When you add features, you'll introduce more: device selection timeout, language validation lists, startup retry delays, etc.

**Why it's debt:**

- Want to change minimum recording duration? Must remember it's in AudioCaptureService, not configurable
- Want to add Whisper model selection? Must change both AppSettings default and any validation logic
- Refactoring requires grep-and-replace, easy to miss instances

**Prevention:**

Create `AppConstants.cs`:

```csharp
public static class AppConstants
{
    public static class Audio
    {
        public const int SampleRate = 16000;
        public const int MinRecordingDurationMs = 500;
        public const int RecordingBufferMs = 50;
    }

    public static class Whisper
    {
        public const string DefaultDeployment = "whisper";
        public const string DefaultLanguage = "pt";
        public static readonly string[] SupportedLanguages = new[]
        {
            "en", "pt", "es", "fr", "de", "it", "ja", "ko", "zh"
        };
    }

    public static class Keyboard
    {
        public const int VK_CONTROL = 0x11;
        public const int VK_SHIFT = 0x10;
        public const int VK_MENU = 0x12; // Alt
    }

    public static class Defaults
    {
        public static readonly Keys HotkeyKey = Keys.F8;
    }
}
```

Reference from services:

```csharp
// AudioCaptureService.cs
private const int MinDurationMs = AppConstants.Audio.MinRecordingDurationMs;

// TranscriptionService.cs
Language = _language ?? AppConstants.Whisper.DefaultLanguage
```

**Phase to address:** Refactor in Phase 1 before adding modifiers. Small upfront cost, prevents sprawl.

---

### Debt 2: No Logging Infrastructure

**What it is:** Current code uses `Debug.WriteLine()` (AudioCaptureService.cs lines 124, 151, 178, 207). These logs:

- Only visible when debugging in Visual Studio
- Not persisted to disk
- Not accessible to users experiencing issues
- Can't be toggled at runtime

When you add four new features, debugging user-reported issues becomes impossible without logs.

**Why it's debt:**

- User reports "microphone selection doesn't work" → you ask "can you send logs?" → no logs exist
- Intermittent bugs (modifier key sticking, device deadlock) are impossible to diagnose without traces
- You end up shipping debug builds to specific users just to add logging

**Prevention:**

Add minimal file-based logging:

```csharp
// SimpleLogger.cs
public static class Logger
{
    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Coxixo", "logs", $"coxixo_{DateTime.Now:yyyyMMdd}.log"
    );

    private static readonly object _lock = new object();

    public static void Info(string message) => Write("INFO", message);
    public static void Warning(string message) => Write("WARN", message);
    public static void Error(string message, Exception? ex = null)
    {
        var msg = ex == null ? message : $"{message} - {ex}";
        Write("ERROR", msg);
    }

    private static void Write(string level, string message)
    {
        var logLine = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}";

        Debug.WriteLine(logLine); // Still write to debug output

        lock (_lock)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
                File.AppendAllText(LogPath, logLine + Environment.NewLine);
            }
            catch { /* Logging failure shouldn't crash app */ }
        }
    }
}
```

Use in services:

```csharp
// AudioCaptureService.cs
Logger.Info("Starting audio capture");
Logger.Error("Microphone access failed", ex);

// KeyboardHookService.cs
Logger.Warning($"Keyboard hook unhealthy - no events for {elapsed}s");
```

Add "Open Logs" to tray icon context menu.

**Phase to address:** Add in Phase 1 or 2. Essential for debugging production issues across all four features.

---

## "Looks Done But Isn't" Checklist

Features that appear complete but have hidden gaps.

### Hotkey Modifiers (Phase 1)

- [ ] Modifier state sampled with `GetAsyncKeyState`, not `GetKeyState`
- [ ] No separate `_ctrlDown`/`_shiftDown` state flags (prevents desync)
- [ ] Hotkey display in tray tooltip shows modifiers: "Ctrl+Shift+F8"
- [ ] Settings UI validates modifier+key combinations (prevent Ctrl+Alt+Del, Win+L, etc.)
- [ ] Settings UI prevents duplicate hotkeys (single F8 vs Ctrl+F8 should be allowed)
- [ ] Hook health monitoring detects when hook stops receiving events
- [ ] Error handling for anti-cheat software with actionable user message
- [ ] Thread safety: hook events marshaled to UI thread before calling services

### Microphone Selection (Phase 2)

- [ ] Device stored by ProductName, not just index
- [ ] Device resolution re-enumerates on `StartCapture()`, finds by name
- [ ] Fallback to default device if saved device not found
- [ ] Device switching stops recording before changing `DeviceNumber`
- [ ] No deadlock when switching devices rapidly (stop → cleanup → recreate)
- [ ] Settings UI shows current device status (available, unplugged, in use by other app)
- [ ] First-run microphone permission check with helpful error dialog
- [ ] Handle `MmResult.NotEnabled` with link to Windows privacy settings (`ms-settings:privacy-microphone`)
- [ ] UI refreshes device list when user plugs/unplugs USB device (requires device change notifications)

### Language Selection (Phase 3)

- [ ] Validate language codes against supported list
- [ ] UI shows display name ("Portuguese (Português)"), stores code ("pt")
- [ ] Auto-detect option warns about hallucination risk
- [ ] Client-side silence detection (energy threshold) before API call
- [ ] Post-process transcription to filter common hallucinations ("thank you for watching", "so")
- [ ] Hallucination filtering configurable (on/off toggle in settings)
- [ ] Empty/null transcription doesn't paste anything into clipboard
- [ ] Language selection persists in settings and survives app restart

### Windows Startup (Phase 4)

- [ ] Uses `HKEY_CURRENT_USER\...\Run`, NOT `HKEY_LOCAL_MACHINE`
- [ ] Executable path is quoted to handle spaces: `"C:\Program Files\Coxixo\Coxixo.exe"`
- [ ] Optional: Add `--autostart` argument to registry value to detect auto-started vs manual launch
- [ ] Settings UI checkbox state reflects actual registry state (query on load)
- [ ] Handle registry write failures with user-visible error
- [ ] Startup delay optional (avoid slowing boot) - launch minimized to tray
- [ ] Remove registry key cleanly on uninstall (requires installer support)
- [ ] Test with path containing spaces, special characters, and non-ASCII

### Cross-Cutting (All Phases)

- [ ] Settings versioning and migration framework in place
- [ ] Centralized constants (no magic numbers)
- [ ] File-based logging for production debugging
- [ ] All new properties in `AppSettings` have defaults that work for new AND upgraded users
- [ ] Thread safety: all UI updates on UI thread, all service calls from correct thread
- [ ] Exception handling: no unhandled exceptions crash the app
- [ ] User-facing errors are actionable ("do X to fix"), not technical dumps

---

## Sources

**Modifier Key Detection and Keyboard Hooks:**
- [Determining the state of modifier keys when hooking keyboard input | Jon Egerton](https://jonegerton.com/dotnet/determining-the-state-of-modifier-keys-when-hooking-keyboard-input/)
- [GetAsyncKeyState function - Microsoft Docs](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getasynckeystate)
- [GetKeyState function - Microsoft Docs](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getkeystate)
- [Modifier keys stuck discussion - AutoHotkey Community](https://www.autohotkey.com/boards/viewtopic.php?t=93955)
- [Modifier keys stuck discussion - Kanata GitHub](https://github.com/jtroo/kanata/discussions/423)

**NAudio Device Enumeration and Management:**
- [NAudio Device Enumeration Documentation](https://github.com/naudio/NAudio/blob/master/Docs/EnumerateOutputDevices.md)
- [NAudio WaveInEvent Source](https://github.com/naudio/NAudio/blob/master/NAudio.WinMM/WaveInEvent.cs)
- [NAudio WaveIn.GetCapabilities Example](https://www.csharpcodi.com/csharp-examples/NAudio.Wave.WaveIn.GetCapabilities(int)/)
- [NAudio Device Switching Deadlock Issue #1203](https://github.com/naudio/NAudio/issues/1203)

**Windows Microphone Privacy and Permissions:**
- [Turn on app permissions for microphone - Microsoft Support](https://support.microsoft.com/en-us/windows/turn-on-app-permissions-for-your-microphone-in-windows-94991183-f69d-b4cf-4679-c98ca45f577a)
- [Windows camera, microphone, and privacy - Microsoft Support](https://support.microsoft.com/en-us/windows/windows-camera-microphone-and-privacy-a83257bc-e990-d54a-d212-b5e41beba857)
- [Microphone Not Working Due to Privacy Settings - Dell](https://www.dell.com/support/kbdoc/en-us/000133024/windows-10-microphone-not-working-due-to-privacy-settings)

**Windows Startup and Registry:**
- [Run and RunOnce Registry Keys - Microsoft Docs](https://learn.microsoft.com/en-us/windows/win32/setupapi/run-and-runonce-registry-keys)
- [Command lines need to be quoted; paths don't - Old New Thing](https://devblogs.microsoft.com/oldnewthing/20070515-00/?p=26863)
- [UAC Self Elevation - Microsoft Q&A](https://learn.microsoft.com/en-us/answers/questions/918783/uac-self-elevation)
- [Registry Run Keys Security Implications - MITRE ATT&CK](https://attack.mitre.org/techniques/T1547/001/)

**Anti-Cheat and Security Software Conflicts:**
- [AutoHotkey and Anti-Cheat Discussion](https://www.autohotkey.com/boards/viewtopic.php?t=38423)
- [Easy Anti-Cheat Download & Issue Fixes](https://4ddig.tenorshare.com/windows-fix/easy-anti-cheat-download-and-issue-fixes.html)

**Whisper API Language Detection and Hallucination:**
- [Whisper Hallucination on Silent Audio - GitHub Discussion](https://github.com/openai/whisper/discussions/1606)
- [Whisper Hallucination Solutions - OpenAI Community](https://community.openai.com/t/whisper-hallucination-how-to-recognize-and-solve/218307)
- [Calm-Whisper: Reduce Hallucination Research](https://arxiv.org/html/2505.12969v1)
- [Whisper Language Detection - OpenAI Community](https://community.openai.com/t/whisper-is-there-a-way-to-tell-the-language-before-recognition/70687)
- [Whisper API Configuration Options](https://whisper-api.com/docs/transcription-options/)
