# Feature Landscape: v1.1 Enhancement Features

**Domain:** Windows voice-to-text utility apps (enhancement milestone)
**Researched:** 2026-02-09
**Focus:** Four specific features for v1.1 (hotkey modifiers, microphone selection, language selection, Windows startup)

## Context

This research covers the **v1.1 milestone** features for Coxixo, an existing system tray voice-to-clipboard app. The base app (v1.0) already has:
- System tray presence
- Global push-to-talk hotkey (single keys only: F8, Home, PageUp)
- Audio capture from default microphone
- Azure OpenAI Whisper API integration
- Clipboard auto-copy
- Audio feedback (walkie-talkie chirps)
- Visual feedback (tray icon animation)
- Settings UI (dark theme, WinForms, hotkey picker using ProcessCmdKey)
- Portuguese BR hardcoded

**This document focuses ONLY on what's needed for the four new v1.1 features.**

---

## Table Stakes

Features users expect from these four domains. Missing = product feels incomplete or frustrating to use.

### 1. Hotkey Modifier Support

| Feature | Why Expected | Complexity | Notes |
|---------|--------------|------------|-------|
| **Ctrl + Key combinations** | Standard Windows pattern; users expect Ctrl+Shift+X style hotkeys | Low | RegisterHotKey API supports MOD_CONTROL flag |
| **Alt + Key combinations** | Common for app-specific shortcuts | Low | RegisterHotKey API supports MOD_ALT flag |
| **Shift + Key combinations** | Expected for modifier variation | Low | RegisterHotKey API supports MOD_SHIFT flag |
| **Multi-modifier support** | Users expect Ctrl+Alt+X, Ctrl+Shift+X, etc. | Low | Flags are bitwise OR'd: MOD_CONTROL \| MOD_ALT |
| **Conflict detection feedback** | If hotkey registration fails, user needs to know why | Medium | RegisterHotKey returns false on conflict; must surface this |
| **Clear modifier display in picker** | Show "Ctrl+Shift+F8" not "F8" | Low | UI text formatting based on modifier state |
| **Reserved combo avoidance** | Don't allow Win+X (system reserved) or F12 (debugger) | Low | Validation logic before registration |

**Rationale:** Every hotkey picker examined shows modifiers as checkboxes or detected keys. Users expect standard Windows modifier behavior (Ctrl, Alt, Shift, and sometimes Win). Single-key hotkeys (current Coxixo implementation) feel limited and conflict with normal typing.

**Sources:**
- [RegisterHotKey function - Microsoft Learn](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-registerhotkey)
- [Remap Keys and Shortcuts with PowerToys Keyboard Manager - Microsoft Learn](https://learn.microsoft.com/en-us/windows/powertoys/keyboard-manager)
- [How to resolve hotkey conflicts in Windows - Tom's Hardware](https://www.tomshardware.com/software/windows/how-to-resolve-hotkey-conflicts-in-windows)

### 2. Microphone Selection

| Feature | Why Expected | Complexity | Notes |
|---------|--------------|------------|-------|
| **List all active capture devices** | Users with multiple mics expect to see all available options | Medium | MMDeviceEnumerator with DataFlow.Capture, DeviceState.Active |
| **Show friendly device names** | Display "Blue Yeti USB Microphone" not "Device {GUID}" | Low | IMMDevice.FriendlyName property |
| **Current default indicator** | Mark which device Windows considers default | Low | MMDeviceEnumerator.GetDefaultAudioEndpoint |
| **Dropdown or list selection** | Standard UI pattern in Windows Settings > Sound | Low | ComboBox in WinForms |
| **Persist selection across restarts** | App remembers chosen microphone | Low | Save device ID to settings JSON |
| **Handle device removal gracefully** | If selected mic unplugged, fall back to default | Medium | Listen for MMNotificationClient events or validate on recording start |
| **Real-time device list refresh** | If user plugs in new mic, show it without app restart | Medium | MMNotificationClient.OnDeviceAdded event |

**Rationale:** Windows Settings > Sound shows a dropdown with all capture devices. Every audio recording app (OBS, Discord, Zoom) provides device selection. Users with USB mics, headset mics, and internal mics expect to choose which one to use.

**Sources:**
- [Enumerating Audio Devices - Microsoft Learn](https://learn.microsoft.com/en-us/windows/win32/coreaudio/enumerating-audio-devices)
- [NAudio - EnumerateOutputDevices.md](https://github.com/naudio/NAudio/blob/master/Docs/EnumerateOutputDevices.md)
- [How to Choose Your Default Microphone on Windows 10 - How-To Geek](https://www.howtogeek.com/700440/how-to-choose-your-default-microphone-on-windows-10/)
- [Select an audio input device with the Speech SDK - Microsoft Learn](https://learn.microsoft.com/en-us/azure/ai-services/speech-service/how-to-select-audio-input-devices)

### 3. Language Selection

| Feature | Why Expected | Complexity | Notes |
|---------|--------------|------------|-------|
| **Dropdown with common languages** | Standard UI pattern; Whisper supports 99 languages | Low | ComboBox with language display names |
| **ISO 639-1 language codes** | Whisper API expects "en", "es", "pt", "zh", etc. | Low | Map display name to code ("English" → "en") |
| **Auto-detect option** | Convenient default; Whisper can detect language from audio | Low | Optional parameter in API call; if null, auto-detects |
| **Show Portuguese BR vs PT distinction** | PT has regional variants | Low | Use "pt" for Portuguese (Whisper doesn't distinguish BR/PT variants) |
| **Persist language choice** | App remembers selected language across restarts | Low | Save language code to settings JSON |
| **Clear default state** | Either "Auto-detect (recommended)" or user's system language | Low | Default to "auto" or "pt" for Coxixo's BR audience |

**Rationale:** Whisper supports 99 languages but auto-detection adds latency and may guess wrong for less common languages. Users in multilingual environments (e.g., Portuguese speaker dictating English) expect manual override. Standard pattern: dropdown with "Auto-detect" option at top.

**Important:** Manual language selection improves accuracy over auto-detect, especially for languages with lower WER (Word Error Rate) in Whisper's training data. Auto-detect analyzes the first 30 seconds, which adds delay.

**Sources:**
- [Supported Languages - Whisper API Docs](https://whisper-api.com/docs/languages/)
- [Whisper Supported Languages - Whisper AI Guide](https://whisper.pmq.ai/languages)
- [GitHub - openai/whisper](https://github.com/openai/whisper)
- [Understanding the --language Flag in Whisper - GitHub Discussion](https://github.com/openai/whisper/discussions/1456)
- [Language Identification vs. Whisper Autodetect Mode - Phonexia](https://docs.phonexia.com/products/speech-platform-4/technologies/language-identification/lid-vs-whisper-autodetect)

### 4. Windows Startup

| Feature | Why Expected | Complexity | Notes |
|---------|--------------|------------|-------|
| **Simple toggle checkbox** | "Start with Windows" checkbox in settings | Low | Standard UI pattern in all system tray utilities |
| **Current user scope (recommended)** | Register in HKEY_CURRENT_USER not HKEY_LOCAL_MACHINE | Low | No admin rights required; per-user preference |
| **Registry Run key approach** | `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` | Low | Most reliable method; Windows 10/11 standard |
| **Absolute path to executable** | Registry value must be full path to .exe | Low | Get via Application.ExecutablePath |
| **Immediate apply** | Checkbox toggle takes effect without restart | Low | Write/delete registry key on checkbox change |
| **Verify on app launch** | Check if registry entry exists and update checkbox state | Low | Read registry on settings dialog open |
| **Handle registry write failure gracefully** | If permission denied, show error message | Low | Try-catch on registry write |

**Rationale:** Every system tray utility (Steam, Discord, Dropbox, 1Password) offers "Start with Windows" as a checkbox in settings. Users expect utilities to be always-available without manual launch. Registry Run key approach is the Windows standard (Task Manager's Startup tab shows these entries).

**Alternative not recommended:** Startup folder (`shell:startup`) works but isn't user-scoped and harder to toggle programmatically. Registry approach is cleaner.

**Sources:**
- [Run and RunOnce Registry Keys - Microsoft Learn](https://learn.microsoft.com/en-us/windows/win32/setupapi/run-and-runonce-registry-keys)
- [Configure Startup Applications in Windows - Microsoft Support](https://support.microsoft.com/en-us/windows/configure-startup-applications-in-windows-115a420a-0bff-4a6f-90e0-1934c844e473)
- [How to Disable Startup Programs in Windows - How-To Geek](https://www.howtogeek.com/74523/how-to-disable-startup-programs-in-windows/)
- [List of Startup Paths, Folders and Registry Settings in Windows 11/10 - The Windows Club](https://www.thewindowsclub.com/windows-startup-paths-folders-and-registry-settings)

---

## Differentiators

Features that go beyond table stakes. Not expected, but valued if present.

| Feature | Value Proposition | Complexity | Notes |
|---------|-------------------|------------|-------|
| **Win (Windows key) modifier support** | Power users may want Win+X hotkeys | Low | RegisterHotKey supports MOD_WIN flag |
| **Microphone volume/gain preview** | Show live audio level bar to verify mic is working | Medium | Requires audio level monitoring during device selection |
| **Per-language Whisper model selection** | Whisper has different models (tiny, base, small, medium, large) | Medium | API parameter; most users don't understand difference |
| **Startup delay option** | "Start with Windows but wait 10 seconds" | Low | Registry value can include delay argument |
| **Hotkey conflict resolver UI** | If Ctrl+Shift+F fails, suggest Ctrl+Shift+G | High | Requires querying all registered hotkeys (difficult on Windows) |
| **Device hot-swap mid-session** | Switch mics without app restart | Medium | Would need to stop/restart audio capture pipeline |
| **Language auto-switch based on clipboard history** | Detect if user was typing English, auto-switch to English | High | Complex heuristic; likely confusing |
| **Regional language variants** | Portuguese (Brazil) vs Portuguese (Portugal), English (US) vs English (UK) | Low | Whisper doesn't distinguish; cosmetic UI only |

**Analysis:**
- **High value, low effort:** Win key modifier support (nice for power users, trivial to add)
- **Medium value, medium effort:** Microphone volume preview (good UX, requires additional UI + audio monitoring)
- **Low value:** Language auto-switch (confusing, error-prone), Regional variants (Whisper doesn't differentiate)

**Recommendation:** Add Win key modifier support. Defer microphone preview to v1.2+. Skip language auto-switch entirely.

---

## Anti-Features

Features to deliberately NOT build. Common in competitors but add complexity without clear value for Coxixo's lean utility philosophy.

| Anti-Feature | Why Avoid | What Competitors Do | What Coxixo Should Do |
|--------------|-----------|---------------------|----------------------|
| **Hotkey profiles/presets** | "Gaming mode", "Work mode" with different hotkeys | Some macro apps offer profiles | Single global hotkey only; simple |
| **Voice-activated recording (VAD)** | Start recording on speech detected, no hotkey | Dragon, some AI apps use VAD | Keep push-to-talk; explicit control |
| **Automatic language detection without manual override** | Force auto-detect only, no manual selection | Some simple apps | Offer both auto-detect AND manual selection |
| **Per-application microphone profiles** | Different mic for Zoom vs Discord vs Coxixo | Advanced audio routers like Voicemeeter | System tray utility; not an audio router |
| **Microphone noise reduction/enhancement** | Built-in audio processing (noise gate, compressor) | Professional recording software | Let users configure in Windows Sound settings |
| **Custom startup arguments UI** | Advanced options like `/minimized`, `/debug` | Enterprise software | Launch minimized by default; no options needed |
| **Registry vs Startup Folder choice** | Let user pick startup method | Some apps offer both | Registry only; simpler |
| **Language-specific custom prompts** | Different Whisper prompts for English vs Portuguese | Advanced transcription apps | Single global prompt (future feature) |
| **Multi-hotkey support** | F8 for Portuguese, F9 for English, etc. | Some productivity apps | Single hotkey; language is a setting |
| **Audio device monitoring/troubleshooting UI** | Show waveform, sample rate, buffer size | Professional audio apps like Reaper | Keep it simple; if mic doesn't work, user checks Windows settings |

**Rationale:**
- **Hotkey profiles:** Adds complexity. Users rarely need multiple hotkey modes for a simple transcription utility.
- **VAD:** Push-to-talk gives explicit control. VAD can trigger accidentally (background noise, other people talking).
- **Per-app mic profiles:** Out of scope; this is an OS-level feature.
- **Audio enhancement:** Whisper is robust to noise. Let users configure in Windows if needed.
- **Multi-hotkey language switching:** Awkward UX. Language is a preference setting, not a per-recording choice.

---

## Feature Dependencies

Understanding what depends on what helps prioritize implementation order.

### Dependency Map

```
Settings UI (already exists in v1.0)
  ├─> Hotkey Modifier Support
  │     └─> Requires: hotkey picker control update
  │     └─> Blocks: nothing (independent feature)
  │
  ├─> Microphone Selection
  │     └─> Requires: audio capture pipeline modification
  │     └─> Blocks: microphone volume preview (future)
  │
  ├─> Language Selection
  │     └─> Requires: API call modification (add language parameter)
  │     └─> Blocks: nothing (independent feature)
  │
  └─> Windows Startup Toggle
        └─> Requires: registry access logic
        └─> Blocks: nothing (independent feature)
```

### Implementation Order Recommendation

**No strict ordering required** — all four features are independent. However, suggested order for logical flow:

1. **Hotkey Modifier Support** (enhances existing hotkey picker)
2. **Windows Startup Toggle** (simplest feature; quick win)
3. **Language Selection** (API parameter change; low risk)
4. **Microphone Selection** (most complex; requires audio pipeline changes)

**Rationale:**
- Start with hotkey modifiers because it builds on the existing hotkey picker UI (incremental change).
- Windows startup is trivial (just registry writes); good morale booster.
- Language selection is an API parameter change (isolated, low risk).
- Microphone selection touches the audio capture pipeline (most potential for bugs); do last when other features are stable.

---

## Complexity Assessment

Quick overview of implementation effort and risk for each feature.

| Feature | UI Changes | Backend Changes | Testing Complexity | Overall Effort |
|---------|------------|-----------------|-------------------|----------------|
| **Hotkey Modifiers** | Update hotkey picker to show Ctrl/Alt/Shift checkboxes or detect modifier keys | Add MOD_CONTROL/ALT/SHIFT flags to RegisterHotKey call | Test conflicts with system hotkeys, other apps | **Low-Medium** |
| **Microphone Selection** | Add ComboBox to settings; populate with device names | NAudio MMDeviceEnumerator; modify audio capture to use selected device ID | Test device removal/reconnection, default fallback | **Medium** |
| **Language Selection** | Add ComboBox with 99 languages (or top 20 + "Other") | Add `language` parameter to Whisper API call | Test auto-detect vs manual; verify API accepts codes | **Low** |
| **Windows Startup** | Add checkbox to settings | Write/delete registry key at `HKCU\...\Run` | Test on clean Windows install; verify Task Manager Startup tab | **Low** |

### Risk Assessment

| Feature | Technical Risk | UX Risk | Notes |
|---------|---------------|---------|-------|
| **Hotkey Modifiers** | Low | Low | Well-documented API; standard pattern |
| **Microphone Selection** | Medium | Low | Device hot-swap edge cases; persistence logic |
| **Language Selection** | Low | Low | API parameter; straightforward |
| **Windows Startup** | Low | Low | Standard registry approach; Task Manager integration |

**High-risk areas to watch:**
- **Microphone selection:** Device removal while app is running (needs graceful fallback).
- **Hotkey modifiers:** Conflicts with existing system or application hotkeys (need clear error messaging).

---

## MVP Recommendation (v1.1 Feature Set)

### Must Have (All Four Features)

Implement all four features as **table stakes functionality** (no differentiators needed for v1.1).

1. **Hotkey Modifier Support**
   - Allow Ctrl, Alt, Shift modifiers (not Win key initially)
   - Update hotkey picker UI to show selected modifiers
   - Validate against reserved combinations (F12, Win+X, etc.)
   - Show error if hotkey conflicts with another app

2. **Windows Startup Toggle**
   - Checkbox in settings: "Start with Windows"
   - Write to `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`
   - Verify checkbox state on settings open

3. **Language Selection**
   - Dropdown with top 20 languages + "Auto-detect (recommended)"
   - Save selection to settings JSON
   - Pass `language` parameter to Whisper API (null for auto-detect)

4. **Microphone Selection**
   - Dropdown with all active capture devices
   - Show friendly names + default indicator
   - Persist device ID to settings
   - Fall back to default if selected device unavailable

### Defer to v1.2+

- Win key modifier support
- Microphone volume preview
- Language-specific Whisper prompts
- Real-time microphone hot-swap
- Hotkey conflict resolver UI

### Explicitly Out of Scope

- Everything in Anti-Features list
- Multiple hotkey support
- VAD (voice-activated detection)
- Audio enhancement/noise reduction
- Per-application device profiles

---

## Expected Behavior Patterns (Reference Implementation)

Based on research, here's how these features work in existing Windows utilities:

### Hotkey Modifier Behavior

**Windows PowerToys Keyboard Manager:**
- UI shows "Shortcut" field: click to activate, press keys, shows "Ctrl + Shift + A"
- Validates as you type: warns if shortcut conflicts
- Modifiers always listed first (Ctrl, Alt, Shift, Win) then action key

**Standard pattern:**
1. User focuses hotkey picker field
2. Presses keys (app detects Ctrl, Alt, Shift state + main key)
3. Displays formatted string: "Ctrl+Shift+F8"
4. On save, calls `RegisterHotKey(handle, id, MOD_CONTROL | MOD_SHIFT, VK_F8)`
5. If registration fails, shows error: "Hotkey already in use"

**Sources:**
- [PowerToys Keyboard Manager - Microsoft Learn](https://learn.microsoft.com/en-us/windows/powertoys/keyboard-manager)

### Microphone Selection Behavior

**Windows Settings > Sound:**
- Dropdown shows: "Microphone Array (Built-in)" ⭐ Default
- Click dropdown, see all devices: "Blue Yeti USB", "Headset Microphone"
- Selection takes effect immediately (no Apply button)
- If device unplugged, Windows auto-switches to next available

**Standard pattern:**
1. Enumerate devices: `MMDeviceEnumerator.EnumerateAudioEndpoints(DataFlow.Capture, DeviceState.Active)`
2. Populate ComboBox with `FriendlyName` values
3. Mark default device with indicator (star emoji, "(Default)" suffix)
4. On selection change, save device ID to settings
5. On recording start, validate device still exists; if not, use default

**Sources:**
- [Enumerating Audio Devices - Microsoft Learn](https://learn.microsoft.com/en-us/windows/win32/coreaudio/enumerating-audio-devices)

### Language Selection Behavior

**Whisper-based apps (SuperWhisper, winWhisper):**
- Dropdown with "Auto-detect (recommended)" at top
- Then alphabetical list: "English", "Español", "Français", "Português", etc.
- Some apps show flags 🇺🇸 🇪🇸 🇫🇷 (optional polish)
- Selection persists across app restarts

**Standard pattern:**
1. Create language mapping: `{ "English": "en", "Español": "es", "Português": "pt" }`
2. Populate ComboBox with display names
3. On selection change, save language code ("en") to settings
4. On API call, pass language code or null for auto-detect
5. Default to user's system language or "Auto-detect"

**Sources:**
- [Whisper Supported Languages - Whisper API Docs](https://whisper-api.com/docs/languages/)
- [Language Detection - Superwhisper](https://superwhisper.com/docs/common-issues/language-detection)

### Windows Startup Behavior

**Discord, Steam, Dropbox settings:**
- Checkbox: ☑ "Open [AppName] on startup"
- Toggle immediately updates registry
- Task Manager > Startup tab shows app as "Enabled"

**Standard pattern:**
1. Read registry on settings open: check if `HKCU\...\Run\Coxixo` exists
2. If exists, check checkbox; else uncheck
3. On checkbox toggle:
   - If checked: write registry value `"C:\Path\To\Coxixo.exe"`
   - If unchecked: delete registry value
4. No app restart required

**Sources:**
- [Run and RunOnce Registry Keys - Microsoft Learn](https://learn.microsoft.com/en-us/windows/win32/setupapi/run-and-runonce-registry-keys)

---

## Common Pitfalls (Specific to These Features)

### Hotkey Modifiers

**Pitfall:** Allowing Win+X combinations that conflict with Windows shell hotkeys
- **Prevention:** Validate against known reserved combinations before registration
- **Detection:** RegisterHotKey returns false; surface this to user

**Pitfall:** Not handling ProcessCmdKey modifier state correctly
- **Prevention:** Use `Control.ModifierKeys` to detect Ctrl/Alt/Shift state
- **Detection:** Hotkey picker doesn't show modifiers

**Sources:**
- [RegisterHotKey function - Microsoft Learn](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-registerhotkey)
- [Hotkey Conflict show Windows reserved function - PowerToys Issue](https://github.com/microsoft/PowerToys/issues/44416)

### Microphone Selection

**Pitfall:** Storing device GUID that becomes invalid when device unplugged/replugged
- **Prevention:** Validate device ID on recording start; fall back to default if invalid
- **Detection:** MMDeviceEnumerator.GetDevice() throws exception

**Pitfall:** Not refreshing device list when user plugs in new microphone
- **Prevention:** Implement MMNotificationClient to listen for device changes OR re-enumerate on settings dialog open
- **Detection:** New mic doesn't appear in dropdown

**Sources:**
- [Enumerating Audio Devices - Microsoft Learn](https://learn.microsoft.com/en-us/windows/win32/coreaudio/enumerating-audio-devices)

### Language Selection

**Pitfall:** Assuming auto-detect is always better
- **Prevention:** Default to auto-detect but allow manual override
- **Detection:** Users in multilingual environments report wrong language detected

**Pitfall:** Using wrong language codes (e.g., "por" instead of "pt")
- **Prevention:** Use ISO 639-1 two-letter codes ("en", "es", "pt", "zh")
- **Detection:** Whisper API returns error or transcribes incorrectly

**Sources:**
- [Understanding the --language Flag in Whisper - GitHub Discussion](https://github.com/openai/whisper/discussions/1456)
- [Whisper Language Codes - Whisper API Docs](https://whisper-api.com/docs/languages/)

### Windows Startup

**Pitfall:** Writing to HKEY_LOCAL_MACHINE (requires admin rights)
- **Prevention:** Always use HKEY_CURRENT_USER
- **Detection:** Registry write fails with access denied

**Pitfall:** Using relative path in registry value
- **Prevention:** Use `Application.ExecutablePath` (absolute path)
- **Detection:** App doesn't start on Windows login

**Sources:**
- [Run and RunOnce Registry Keys - Microsoft Learn](https://learn.microsoft.com/en-us/windows/win32/setupapi/run-and-runonce-registry-keys)

---

## Sources Summary

### Hotkey Modifiers
- [RegisterHotKey function - Microsoft Learn](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-registerhotkey)
- [Remap Keys and Shortcuts with PowerToys Keyboard Manager - Microsoft Learn](https://learn.microsoft.com/en-us/windows/powertoys/keyboard-manager)
- [How to resolve hotkey conflicts in Windows - Tom's Hardware](https://www.tomshardware.com/software/windows/how-to-resolve-hotkey-conflicts-in-windows)
- [Hotkey Conflict show Windows reserved function - PowerToys Issue](https://github.com/microsoft/PowerToys/issues/44416)

### Microphone Selection
- [Enumerating Audio Devices - Microsoft Learn](https://learn.microsoft.com/en-us/windows/win32/coreaudio/enumerating-audio-devices)
- [NAudio - EnumerateOutputDevices.md](https://github.com/naudio/NAudio/blob/master/Docs/EnumerateOutputDevices.md)
- [How to Choose Your Default Microphone on Windows 10 - How-To Geek](https://www.howtogeek.com/700440/how-to-choose-your-default-microphone-on-windows-10/)
- [Select an audio input device with the Speech SDK - Microsoft Learn](https://learn.microsoft.com/en-us/azure/ai-services/speech-service/how-to-select-audio-input-devices)
- [Complete Guide on Managing Audio Input Devices - TechCommuters](https://www.techcommuters.com/guide-on-managing-audio-input-devices/)

### Language Selection
- [Supported Languages - Whisper API Docs](https://whisper-api.com/docs/languages/)
- [Whisper Supported Languages - Whisper AI Guide](https://whisper.pmq.ai/languages)
- [GitHub - openai/whisper](https://github.com/openai/whisper)
- [Understanding the --language Flag in Whisper - GitHub Discussion](https://github.com/openai/whisper/discussions/1456)
- [Language Identification vs. Whisper Autodetect Mode - Phonexia](https://docs.phonexia.com/products/speech-platform-4/technologies/language-identification/lid-vs-whisper-autodetect)
- [Language Detection - Superwhisper](https://superwhisper.com/docs/common-issues/language-detection)

### Windows Startup
- [Run and RunOnce Registry Keys - Microsoft Learn](https://learn.microsoft.com/en-us/windows/win32/setupapi/run-and-runonce-registry-keys)
- [Configure Startup Applications in Windows - Microsoft Support](https://support.microsoft.com/en-us/windows/configure-startup-applications-in-windows-115a420a-0bff-4a6f-90e0-1934c844e473)
- [How to Disable Startup Programs in Windows - How-To Geek](https://www.howtogeek.com/74523/how-to-disable-startup-programs-in-windows/)
- [List of Startup Paths, Folders and Registry Settings in Windows 11/10 - The Windows Club](https://www.thewindowsclub.com/windows-startup-paths-folders-and-registry-settings)
