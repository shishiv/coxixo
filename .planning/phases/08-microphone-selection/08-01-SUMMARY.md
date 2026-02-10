---
phase: 08-microphone-selection
plan: 01
subsystem: audio-input
tags: [microphone, device-selection, audio-capture, settings-ui, naudio]
dependency_graph:
  requires: [phase-07-language-selection]
  provides: [microphone-device-selection]
  affects: [AudioCaptureService, SettingsForm, AppSettings, TrayApplicationContext]
tech_stack:
  added: []
  patterns: [hybrid-device-enumeration, device-fallback, null-coalescing-default]
key_files:
  created: []
  modified:
    - Coxixo/Models/AppSettings.cs
    - Coxixo/Services/AudioCaptureService.cs
    - Coxixo/Forms/SettingsForm.Designer.cs
    - Coxixo/Forms/SettingsForm.cs
    - Coxixo/TrayApplicationContext.cs
key_decisions:
  - decision: Use hybrid MMDeviceEnumerator + WaveInEvent device matching
    rationale: MMDeviceEnumerator provides full friendly names (non-truncated), WaveInEvent provides device indices for recording
    alternatives: [WaveInEvent-only enumeration (truncated names), MMDeviceEnumerator-only (complex ID mapping)]
  - decision: Nullable int for MicrophoneDeviceNumber (null = system default)
    rationale: Clear semantic meaning, matches language selection pattern, avoids magic number for default
    alternatives: [int with -1 for default, separate boolean flag]
  - decision: Enumerate devices fresh on each settings open
    rationale: Device list changes when USB devices are plugged/unplugged, avoids stale device list
    alternatives: [Enumerate once on form construction, device change notifications]
  - decision: Fallback retry on BadDeviceId error
    rationale: Gracefully handles unplugged devices without user intervention, recording continues with default device
    alternatives: [Show error and block recording, prompt user to select new device]
metrics:
  duration: 4m
  tasks_completed: 2
  files_modified: 5
  commits: 2
  completed_at: 2026-02-10T01:43:09Z
---

# Phase 08 Plan 01: Microphone Selection Summary

**One-liner:** Users can select which audio input device to use for recording from a dropdown showing all active capture devices with full friendly names, with automatic fallback to default device if selected device becomes unavailable.

## What Was Built

Added microphone selection capability to Coxixo, allowing users to choose which audio input device captures their voice instead of always using the system default microphone.

### Task 1: Model and Service Layer (Commit 8c67152)

**AppSettings.cs:**
- Added `MicrophoneDeviceNumber` property (nullable int, default null)
- Null value semantically represents "use system default device"

**AudioCaptureService.cs:**
- Modified `StartCapture()` to accept optional `deviceNumber` parameter
- Set `WaveInEvent.DeviceNumber` from parameter (fallback to 0 for default)
- Updated BadDeviceId error message from "No microphone found" to "Selected microphone not found. Falling back to default device."
- Added fallback retry logic: if selected device fails with BadDeviceId, recursively call `StartCapture(null)` to retry with default device

### Task 2: UI and Wiring (Commit 4effa76)

**SettingsForm.Designer.cs:**
- Added `lblMicrophone` and `cmbMicrophone` fields
- Positioned microphone section between language dropdown and startup checkbox
- Shifted existing controls down by 55px (chkStartWithWindows from Y=425 to Y=480, buttons from Y=455 to Y=510)
- Updated ClientSize from (304, 495) to (304, 550)

**SettingsForm.cs:**
- Added using directives for `NAudio.Wave` and `NAudio.CoreAudioApi`
- Added `EnumerateAudioDevices()` method implementing hybrid enumeration:
  - Uses MMDeviceEnumerator to get full friendly names (non-truncated)
  - Matches MMDevices with WaveInEvent device indices by ProductName prefix
  - Marks system default device with "(System Default)" suffix
  - Fallback to basic WaveInEvent enumeration on CoreAudio failure
  - Returns list with "System Default" as first item (null key)
- Added `ValidateDeviceNumber()` method:
  - Checks device index bounds (0 to WaveInEvent.DeviceCount - 1)
  - Probes device with GetCapabilities() to verify it still exists
  - Returns null for invalid/missing devices
- Updated `LoadSettings()`:
  - Enumerate devices fresh on each settings open (handles USB device changes)
  - Set DisplayMember/ValueMember before DataSource (Phase 7 pattern)
  - Validate saved device number, fallback to "System Default" if invalid
- Added `CmbMicrophone_SelectedIndexChanged` event handler with `_isLoading` guard
- Updated `BtnSave_Click`: save `MicrophoneDeviceNumber` from ComboBox selection

**TrayApplicationContext.cs:**
- Modified `OnHotkeyPressed`: pass `_settings.MicrophoneDeviceNumber` to `StartCapture()`

## Deviations from Plan

None - plan executed exactly as written.

## Verification Results

All verification points passed:

1. ✅ Build succeeds with 0 errors, 0 warnings
2. ✅ AppSettings.cs contains `int? MicrophoneDeviceNumber` with default null
3. ✅ AudioCaptureService.StartCapture accepts `int? deviceNumber` parameter with fallback retry on BadDeviceId
4. ✅ SettingsForm has microphone label and ComboBox between language dropdown and startup checkbox
5. ✅ EnumerateAudioDevices uses MMDeviceEnumerator for full names with WaveInEvent matching
6. ✅ System default device has "(System Default)" suffix in dropdown
7. ✅ ValidateDeviceNumber checks device index bounds and GetCapabilities before use
8. ✅ BtnSave_Click saves MicrophoneDeviceNumber from ComboBox selection
9. ✅ TrayApplicationContext.OnHotkeyPressed passes _settings.MicrophoneDeviceNumber to StartCapture
10. ✅ Form controls don't overlap, ClientSize accommodates all controls, consistent 55px spacing for microphone section

## Success Criteria Met

- ✅ Microphone dropdown in settings lists all active capture devices with full names
- ✅ "System Default" is the first item in the dropdown (selected when MicrophoneDeviceNumber is null)
- ✅ Active system default device has "(System Default)" suffix on its display name
- ✅ Selected microphone device number saved to settings.json on Save button click
- ✅ App uses selected microphone device for recording via StartCapture(deviceNumber)
- ✅ Invalid/missing device gracefully falls back to system default (no crash)
- ✅ Build compiles with 0 errors

## Technical Highlights

**Hybrid Device Enumeration Strategy:**
The implementation uses a sophisticated hybrid approach combining two NAudio APIs:
- **MMDeviceEnumerator (CoreAudioApi):** Provides full-length friendly names like "Microphone (Realtek High Definition Audio)" instead of truncated ProductName
- **WaveInEvent:** Provides 0-based device indices required for actual recording
- **Matching logic:** Fuzzy match by ProductName prefix, handling cases where MMDevice.FriendlyName contains more context than WaveIn.ProductName
- **Graceful degradation:** Falls back to basic WaveInEvent enumeration if CoreAudio fails (handles edge cases on older Windows versions or restricted environments)

**Device Validation and Fallback:**
- Settings open: ValidateDeviceNumber checks bounds and probes with GetCapabilities
- Recording start: If selected device fails (unplugged USB mic), AudioCaptureService automatically retries with default device
- User experience: Recording "just works" even if saved device is missing, no error dialogs during hotkey press

**UI Consistency:**
- Follows Phase 7 language selection pattern: nullable value type, SelectedIndex=0 for null, DisplayMember/ValueMember before DataSource
- Consistent 55px spacing for new section, all controls shifted down proportionally
- Form size increased to accommodate new section without cramping

## Self-Check: PASSED

**Files created:**
- `.planning/phases/08-microphone-selection/08-01-SUMMARY.md` ✅ FOUND

**Files modified (expected changes present):**
- `Coxixo/Models/AppSettings.cs` ✅ FOUND (contains MicrophoneDeviceNumber)
- `Coxixo/Services/AudioCaptureService.cs` ✅ FOUND (StartCapture accepts deviceNumber)
- `Coxixo/Forms/SettingsForm.Designer.cs` ✅ FOUND (cmbMicrophone controls, ClientSize=550)
- `Coxixo/Forms/SettingsForm.cs` ✅ FOUND (EnumerateAudioDevices, MMDeviceEnumerator, ValidateDeviceNumber)
- `Coxixo/TrayApplicationContext.cs` ✅ FOUND (StartCapture with MicrophoneDeviceNumber)

**Commits exist:**
- `8c67152`: feat(08-01): add MicrophoneDeviceNumber and device-aware StartCapture ✅ FOUND
- `4effa76`: feat(08-01): add microphone selection UI with hybrid device enumeration ✅ FOUND

All artifacts present and verified.
