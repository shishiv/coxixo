# Phase 8: Microphone Selection - Research

**Researched:** 2026-02-09
**Domain:** NAudio Device Enumeration, WinForms ComboBox UI, Audio Device Persistence
**Confidence:** HIGH

## Summary

Microphone selection in NAudio requires enumerating audio input devices and passing the selected device number to WaveInEvent. The application currently hardcodes default device selection (line 70 in AudioCaptureService.cs creates WaveInEvent without specifying DeviceNumber). NAudio provides two APIs for device enumeration: WaveInEvent.GetCapabilities (legacy WinMM API) and MMDeviceEnumerator (modern CoreAudio/WASAPI). Each has trade-offs regarding device name truncation and device identification.

The WaveInEvent.GetCapabilities API truncates device names to 31 characters due to Windows API limitations, which creates poor UX when multiple similar devices exist (e.g., "USB Audio Device (2- Realtek" vs "USB Audio Device (3- Realtek"). MMDeviceEnumerator provides full device names through FriendlyName property, but requires matching MMDevice objects to WaveInEvent device numbers for compatibility with the existing recording pipeline.

**Primary recommendation:** Use hybrid approach - enumerate with MMDeviceEnumerator to get full device names, match to WaveInEvent device numbers by comparing truncated ProductName, store device number in AppSettings, fall back to default device (DeviceNumber = 0) if saved device is unavailable. Follow Phase 7 ComboBox pattern: KeyValuePair<int, string> binding, DisplayMember/ValueMember before DataSource, nullable int in AppSettings for "System Default" option.

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| NAudio | 2.2.1 (existing) | Audio device enumeration and capture | Already used for recording pipeline; provides both WaveInEvent and MMDeviceEnumerator APIs |
| Microsoft.Win32.Registry | Built-in (.NET 8) | No use in this phase | Not needed - device selection doesn't require registry operations |
| System.Windows.Forms | Built-in (.NET 8) | ComboBox control for device selection UI | Already in use for SettingsForm |
| System.Text.Json | Built-in (.NET 8) | Settings serialization | Already used in ConfigurationService for AppSettings persistence |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| None required | - | - | Feature uses existing dependencies only |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| MMDeviceEnumerator + matching | WaveInEvent.GetCapabilities only | GetCapabilities is simpler (no matching logic) but truncates device names to 31 characters, creating poor UX when multiple similar devices exist. MMDeviceEnumerator provides full names with modest matching complexity. |
| Nullable int? for default device | Special sentinel value (-1) | Nullable int is more semantically correct ("no device selected" = use default) vs. magic number. Matches Phase 7 pattern for nullable language code. |
| Store device number | Store device name/ID | Device numbers can change when devices are added/removed, but name matching is unreliable (names not guaranteed unique). Validation on startup (check if device number still valid) + fallback handles device number changes robustly. |
| ComboBox with KeyValuePair | Custom DeviceInfo class | KeyValuePair is built-in and sufficient for number/name pairs. Custom class adds no value. Matches Phase 7 language selection pattern. |

**Installation:**
```bash
# No new packages required - uses NAudio 2.2.1 (already installed)
```

## Architecture Patterns

### Recommended Project Structure
```
Coxixo/
├── Models/
│   └── AppSettings.cs              # MODIFIED - Add MicrophoneDeviceNumber property (int?, default null)
├── Services/
│   ├── AudioCaptureService.cs      # MODIFIED - Accept deviceNumber in StartCapture(), add validation
│   └── ConfigurationService.cs     # EXISTING - No changes (already handles new properties)
└── Forms/
    ├── SettingsForm.cs             # MODIFIED - Add ComboBox, enumerate devices, handle selection
    └── SettingsForm.Designer.cs    # MODIFIED - Add lblMicrophone, cmbMicrophone controls
```

### Pattern 1: Hybrid Device Enumeration with Name Matching

**What:** Use MMDeviceEnumerator for full device names, match to WaveInEvent device numbers by comparing truncated names

**When to use:** When you need human-readable device names but must use WaveInEvent for recording (existing pipeline constraint)

**Example:**
```csharp
// Source: Based on NAudio GitHub issues #612, #646 discussing truncation workarounds
using NAudio.Wave;
using NAudio.CoreAudioApi;

public static List<KeyValuePair<int?, string>> EnumerateDevices()
{
    var devices = new List<KeyValuePair<int?, string>>
    {
        new(null, "System Default")  // null = use default device
    };

    // Get full names from CoreAudio
    var enumerator = new MMDeviceEnumerator();
    var mmDevices = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active).ToList();

    // Match to WaveInEvent device numbers
    for (int deviceNumber = 0; deviceNumber < WaveInEvent.DeviceCount; deviceNumber++)
    {
        var caps = WaveInEvent.GetCapabilities(deviceNumber);
        string truncatedName = caps.ProductName;  // Max 31 characters

        // Find matching MMDevice by checking if full name starts with truncated name
        var mmDevice = mmDevices.FirstOrDefault(d =>
            d.FriendlyName.StartsWith(truncatedName, StringComparison.OrdinalIgnoreCase));

        string displayName = mmDevice?.FriendlyName ?? truncatedName;
        devices.Add(new(deviceNumber, displayName));
    }

    return devices;
}
```

**Why this works:** Windows device names are deterministic. If MMDevice.FriendlyName starts with the 31-character truncated name, it's the same device. Source: [NAudio Issue #646](https://github.com/naudio/NAudio/issues/646)

### Pattern 2: Nullable Int for Default Device Selection

**What:** Use `int? MicrophoneDeviceNumber` in AppSettings, where null means "use system default" (DeviceNumber = 0)

**When to use:** Settings that have a "use default" option distinct from explicit device selection

**Example:**
```csharp
// Models/AppSettings.cs
public class AppSettings
{
    /// <summary>
    /// Selected microphone device number (0-based index).
    /// Null means use system default device.
    /// </summary>
    public int? MicrophoneDeviceNumber { get; set; } = null;  // Default to system default
}

// Services/AudioCaptureService.cs
public void StartCapture(int? deviceNumber = null)
{
    // ... existing validation ...

    _waveIn = new WaveInEvent
    {
        DeviceNumber = deviceNumber ?? 0,  // null or 0 both use default device
        WaveFormat = waveFormat,
        BufferMilliseconds = 50
    };

    // ... rest of capture setup ...
}
```

**Why nullable:** Semantically correct ("no device specified" vs. arbitrary index 0), matches Phase 7 nullable language code pattern, allows distinguishing "user selected default" from "never configured".

### Pattern 3: Device Validation with Graceful Fallback

**What:** On app startup, validate that saved device number still exists; fall back to default if unavailable

**When to use:** Any persisted hardware reference (devices can be unplugged, disabled, or reordered)

**Example:**
```csharp
// In TrayApplicationContext or service initialization
private int? ValidateAndGetMicrophoneDevice(int? savedDeviceNumber)
{
    // Null means use default - always valid
    if (savedDeviceNumber == null)
        return null;

    // Check if saved device number still exists
    if (savedDeviceNumber.Value >= 0 && savedDeviceNumber.Value < WaveInEvent.DeviceCount)
    {
        // Device exists - validate it's working
        try
        {
            var caps = WaveInEvent.GetCapabilities(savedDeviceNumber.Value);
            return savedDeviceNumber;  // Device valid
        }
        catch
        {
            // Device exists but can't get capabilities - fall back to default
        }
    }

    // Device no longer exists - fall back to default
    return null;  // Will use DeviceNumber = 0
}
```

**Why needed:** USB devices can be unplugged, Bluetooth devices can disconnect, device order can change when hardware is added/removed. Validation prevents crashes and provides silent fallback. Source: [NAudio Issue #657](https://github.com/naudio/NAudio/issues/657) - WaveIn blocks forever when USB device unplugged.

### Pattern 4: ComboBox Binding for Device Selection

**What:** Bind ComboBox to List<KeyValuePair<int?, string>> with DisplayMember="Value", ValueMember="Key"

**When to use:** Dropdown with separate internal value (device number) and display text (device name)

**Example:**
```csharp
// Source: Phase 7 language selection pattern (07-RESEARCH.md)
private void SetupForm()
{
    // ... existing code ...

    // Populate microphone dropdown
    var devices = EnumerateDevices();  // List<KeyValuePair<int?, string>>

    // CRITICAL: Set DisplayMember/ValueMember BEFORE DataSource
    cmbMicrophone.DisplayMember = "Value";
    cmbMicrophone.ValueMember = "Key";
    cmbMicrophone.DataSource = devices;
}

private void LoadSettings()
{
    _isLoading = true;

    _settings = ConfigurationService.Load();
    // ... other settings ...

    // Handle nullable MicrophoneDeviceNumber
    if (_settings.MicrophoneDeviceNumber == null)
        cmbMicrophone.SelectedIndex = 0;  // "System Default" (first item)
    else
        cmbMicrophone.SelectedValue = _settings.MicrophoneDeviceNumber;

    _isLoading = false;
}
```

**Why this order matters:** Setting DataSource first triggers SelectedIndexChanged before DisplayMember/ValueMember are configured, causing binding failures. Same pattern used in Phase 7.

### Pattern 5: System Default Device Indicator

**What:** Mark system default device in ComboBox list with " (System Default)" suffix

**When to use:** Showing which device is the OS default helps users understand what "System Default" option will use

**Example:**
```csharp
public static List<KeyValuePair<int?, string>> EnumerateDevices()
{
    var devices = new List<KeyValuePair<int?, string>>
    {
        new(null, "System Default")
    };

    // Get default device from Windows
    var enumerator = new MMDeviceEnumerator();
    var defaultDevice = enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Console);
    string defaultDeviceId = defaultDevice.ID;

    var mmDevices = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active).ToList();

    for (int deviceNumber = 0; deviceNumber < WaveInEvent.DeviceCount; deviceNumber++)
    {
        var caps = WaveInEvent.GetCapabilities(deviceNumber);
        var mmDevice = mmDevices.FirstOrDefault(d =>
            d.FriendlyName.StartsWith(caps.ProductName, StringComparison.OrdinalIgnoreCase));

        string displayName = mmDevice?.FriendlyName ?? caps.ProductName;

        // Mark system default
        if (mmDevice != null && mmDevice.ID == defaultDeviceId)
            displayName += " (System Default)";

        devices.Add(new(deviceNumber, displayName));
    }

    return devices;
}
```

**Why helpful:** Users see "Microphone Array (System Default)" in list and understand that selecting "System Default" dropdown option will use that device. Satisfies MIC-03 requirement.

### Anti-Patterns to Avoid

- **Using WaveInEvent.GetCapabilities alone for UI:** Truncates names to 31 characters, making devices indistinguishable when multiple similar devices exist (e.g., multiple USB mics)
- **Not validating device on startup:** Causes crashes when saved device is unplugged; always validate and fall back to default
- **Setting DataSource before DisplayMember/ValueMember:** Causes binding failures and shows type names instead of device names
- **Forgetting _isLoading guard:** Triggers spurious save operations when LoadSettings() programmatically sets ComboBox selection
- **Storing device name/ID instead of number:** Device names aren't guaranteed unique; WaveInEvent requires device number; validation + fallback handles device changes
- **Not providing "System Default" option:** Users expect ability to track Windows default device changes automatically

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Audio device enumeration | Parse Windows Device Manager or registry | NAudio MMDeviceEnumerator + WaveInEvent | NAudio abstracts COM interop, handles device state, provides type-safe API. Registry parsing is fragile and breaks across Windows versions. |
| Device name truncation workaround | Read full names from registry | MMDeviceEnumerator.FriendlyName | Registry device keys are complex and undocumented. CoreAudio API is official and stable. |
| Device change detection | Polling WaveInEvent.DeviceCount | Not needed for Phase 8 | Device list is only refreshed when settings window opens. Real-time device hotplug detection (MMNotificationClient) is complex and out of scope for v1.0. |
| ComboBox key-value binding | Custom wrapper class | KeyValuePair<int?, string> | Built-in, matches Phase 7 pattern, no custom code needed. |

**Key insight:** NAudio provides two device enumeration APIs with complementary strengths. Hybrid approach (MMDeviceEnumerator for names, WaveInEvent for compatibility) is the recommended solution in NAudio community when full names are needed but WaveInEvent pipeline is required.

## Common Pitfalls

### Pitfall 1: WaveInCapabilities Truncates Device Names

**What goes wrong:** ComboBox shows "USB Audio Device (2- Realtek" and "USB Audio Device (3- Realtek" - users can't tell which is which

**Why it happens:** WaveInCapabilities.ProductName is limited to 31 characters by Windows waveIn API structure (WAVEINCAPS). This is a fundamental Windows API limitation, not a NAudio bug.

**How to avoid:** Use MMDeviceEnumerator to get full FriendlyName, match to WaveInEvent device numbers by comparing first 31 characters

**Warning signs:** Multiple devices with identical truncated names in dropdown, user complaints about "can't tell devices apart"

**Source:** [NAudio Issue #646 - WaveInCapabilities truncates device names to 32 characters](https://github.com/naudio/NAudio/issues/646)

### Pitfall 2: Saved Device Number Becomes Invalid

**What goes wrong:** App crashes on StartCapture() with "BadDeviceId" exception after user unplugs USB microphone that was saved in settings

**Why it happens:** WaveInEvent device numbers are 0-based indices that change when devices are added/removed. Saved device number 2 may refer to different hardware after device changes.

**How to avoid:** Validate device number on startup (check < WaveInEvent.DeviceCount), fall back to null (system default) if invalid. Handle NAudio.MmException with BadDeviceId result.

**Warning signs:** Crashes on app startup after hardware changes, "No microphone found" errors when microphone is actually connected

**Source:** [NAudio Issue #657 - WaveIn blocks forever when USB device unplugged](https://github.com/naudio/NAudio/issues/657)

### Pitfall 3: Not Handling Device Disconnection During Recording

**What goes wrong:** App hangs or crashes if user unplugs active microphone during recording session

**Why it happens:** NAudio WaveInEvent.StopRecording() can block indefinitely or throw exceptions when device is removed while recording

**How to avoid:** Existing AudioCaptureService already has CaptureError event and try-catch in StopCapture (line 120-125). Ensure UI subscribes to CaptureError and shows notification. Add device validation before next recording.

**Warning signs:** App freezes when unplugging mic during recording, crash logs showing blocked threads in WaveInEvent

**Source:** [NAudio Issue #535 - Hang when disposing WaveOut after device removed](https://github.com/naudio/NAudio/issues/535)

### Pitfall 4: SelectedValue = null Doesn't Work for ComboBox

**What goes wrong:** Setting `cmbMicrophone.SelectedValue = null` in LoadSettings() doesn't select "System Default" item

**Why it happens:** WinForms ComboBox ignores SelectedValue assignments that don't match ValueMember. KeyValuePair<int?, string> with null Key requires SelectedIndex approach.

**How to avoid:** Use conditional: if device number is null, set SelectedIndex = 0; otherwise set SelectedValue = deviceNumber

**Warning signs:** "System Default" never appears selected even though MicrophoneDeviceNumber is null in settings.json

**Source:** Phase 7 research (07-RESEARCH.md) - same pitfall applies to nullable ComboBox binding

### Pitfall 5: MMDeviceEnumerator and WaveInEvent List Different Order

**What goes wrong:** Device at MMDeviceEnumerator index 2 doesn't match WaveInEvent DeviceNumber = 2, causing wrong device to be selected

**Why it happens:** MMDeviceEnumerator and WaveInEvent use different enumeration orders. Direct index mapping doesn't work.

**How to avoid:** Match by name comparison (MMDevice.FriendlyName starts with WaveInCapabilities.ProductName), don't assume indices align

**Warning signs:** User selects "Microphone Array" but app records from "Line In", device selection seems random

**Source:** [NAudio Output Devices - Mark Heath](https://www.markheath.net/post/naudio-audio-output-devices)

### Pitfall 6: Not Refreshing Device List When Settings Window Reopens

**What goes wrong:** User plugs in new microphone, opens settings, but device doesn't appear in list

**Why it happens:** Device list populated once in SetupForm() and never refreshed

**How to avoid:** Enumerate devices in LoadSettings() instead of SetupForm(), so list refreshes every time settings window opens. Small performance cost (< 50ms) acceptable for infrequent operation.

**Warning signs:** Users must restart app to see new devices, device list shows disconnected devices as available

## Code Examples

Verified patterns from official sources and existing codebase:

### AppSettings Model Extension

```csharp
// Add to Models/AppSettings.cs
/// <summary>
/// Selected microphone device number (0-based index from WaveInEvent.DeviceCount).
/// Null means use system default device (DeviceNumber = 0).
/// </summary>
public int? MicrophoneDeviceNumber { get; set; } = null;  // Default to system default
```

### Device Enumeration with Full Names

```csharp
// Add to SettingsForm.cs or new MicrophoneDeviceService.cs
using NAudio.Wave;
using NAudio.CoreAudioApi;

private List<KeyValuePair<int?, string>> EnumerateAudioDevices()
{
    var devices = new List<KeyValuePair<int?, string>>
    {
        new(null, "System Default")
    };

    try
    {
        // Get full names from CoreAudio
        var enumerator = new MMDeviceEnumerator();
        var defaultDevice = enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Console);
        var mmDevices = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active).ToList();

        // Match to WaveInEvent device numbers
        for (int deviceNumber = 0; deviceNumber < WaveInEvent.DeviceCount; deviceNumber++)
        {
            var caps = WaveInEvent.GetCapabilities(deviceNumber);
            string truncatedName = caps.ProductName;  // Max 31 characters

            // Find matching MMDevice by name prefix
            var mmDevice = mmDevices.FirstOrDefault(d =>
                d.FriendlyName.StartsWith(truncatedName, StringComparison.OrdinalIgnoreCase));

            string displayName = mmDevice?.FriendlyName ?? truncatedName;

            // Mark system default device
            if (mmDevice != null && mmDevice.ID == defaultDevice.ID)
                displayName += " (System Default)";

            devices.Add(new(deviceNumber, displayName));
        }
    }
    catch (Exception ex)
    {
        Debug.WriteLine($"Error enumerating audio devices: {ex.Message}");
        // Fall back to basic enumeration if CoreAudio fails
        for (int i = 0; i < WaveInEvent.DeviceCount; i++)
        {
            var caps = WaveInEvent.GetCapabilities(i);
            devices.Add(new(i, caps.ProductName));
        }
    }

    return devices;
}
```

**Source:** Hybrid approach recommended in [NAudio Issue #612](https://github.com/naudio/NAudio/issues/612) and [NAudio Issue #646](https://github.com/naudio/NAudio/issues/646)

### ComboBox Setup in SettingsForm.Designer.cs

```csharp
// Add to field declarations
private Label lblMicrophone;
private ComboBox cmbMicrophone;

// Add to InitializeComponent()
lblMicrophone = new Label();
cmbMicrophone = new ComboBox();

// lblMicrophone
lblMicrophone.AutoSize = true;
lblMicrophone.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
lblMicrophone.Location = new Point(12, 390);  // After language dropdown
lblMicrophone.Name = "lblMicrophone";
lblMicrophone.Text = "MICROPHONE";

// cmbMicrophone
cmbMicrophone.DropDownStyle = ComboBoxStyle.DropDownList;  // Prevent manual text entry
cmbMicrophone.Location = new Point(12, 410);
cmbMicrophone.Size = new Size(280, 25);
cmbMicrophone.Name = "cmbMicrophone";
cmbMicrophone.SelectedIndexChanged += CmbMicrophone_SelectedIndexChanged;

// Update chkStartWithWindows position: y = 445
// Update btnCancel/btnSave positions: y = 475
// Update Form ClientSize: (304, 524)
```

### LoadSettings with Device List Population

```csharp
// Update LoadSettings() method in SettingsForm.cs
private void LoadSettings()
{
    _isLoading = true;

    _settings = ConfigurationService.Load();
    hotkeyPicker.SelectedCombo = _settings.Hotkey;
    txtEndpoint.Text = _settings.AzureEndpoint;
    txtApiKey.Text = CredentialService.LoadApiKey() ?? "";
    txtDeployment.Text = _settings.WhisperDeployment;
    chkStartWithWindows.Checked = StartupService.IsEnabled();

    // Language selection (Phase 7)
    if (_settings.LanguageCode == null)
        cmbLanguage.SelectedIndex = 0;
    else
        cmbLanguage.SelectedValue = _settings.LanguageCode;

    // Microphone selection (Phase 8) - enumerate fresh on each settings open
    var devices = EnumerateAudioDevices();
    cmbMicrophone.DisplayMember = "Value";
    cmbMicrophone.ValueMember = "Key";
    cmbMicrophone.DataSource = devices;

    // Validate saved device number still exists
    int? validatedDevice = ValidateDeviceNumber(_settings.MicrophoneDeviceNumber);
    if (validatedDevice == null)
        cmbMicrophone.SelectedIndex = 0;  // System Default
    else
        cmbMicrophone.SelectedValue = validatedDevice;

    _isLoading = false;
}

private int? ValidateDeviceNumber(int? deviceNumber)
{
    if (deviceNumber == null)
        return null;  // System Default always valid

    // Check if device still exists
    if (deviceNumber.Value >= 0 && deviceNumber.Value < WaveInEvent.DeviceCount)
    {
        try
        {
            var caps = WaveInEvent.GetCapabilities(deviceNumber.Value);
            return deviceNumber;  // Device valid
        }
        catch
        {
            // Device exists but can't get capabilities
        }
    }

    return null;  // Fall back to system default
}
```

**Why enumerate in LoadSettings:** Refreshes device list every time settings window opens, allowing users to see newly connected devices without restarting app.

### SelectedIndexChanged Event Handler

```csharp
// Add to SettingsForm.cs
private void CmbMicrophone_SelectedIndexChanged(object? sender, EventArgs e)
{
    if (_isLoading)
        return;  // Ignore programmatic changes during LoadSettings

    // No immediate action needed - device selection saved when user clicks Save button
    // (matches existing pattern: hotkey, language, etc. don't save immediately)
}
```

### Save Button with Device Persistence

```csharp
// Update BtnSave_Click() in SettingsForm.cs
private void BtnSave_Click(object? sender, EventArgs e)
{
    // ... existing hotkey validation ...

    // Update settings
    _settings.Hotkey = combo;
    _settings.AzureEndpoint = txtEndpoint.Text.Trim();
    _settings.WhisperDeployment = txtDeployment.Text.Trim();
    _settings.StartWithWindows = chkStartWithWindows.Checked;
    _settings.LanguageCode = cmbLanguage.SelectedValue as string;
    _settings.MicrophoneDeviceNumber = cmbMicrophone.SelectedValue as int?;  // NEW

    // Save settings
    ConfigurationService.Save(_settings);

    // ... existing API key save and close ...
}
```

### AudioCaptureService.StartCapture Update

```csharp
// Update Services/AudioCaptureService.cs
/// <summary>
/// Starts capturing audio from the specified microphone.
/// </summary>
/// <param name="deviceNumber">Device number (0-based index), or null for system default.</param>
public void StartCapture(int? deviceNumber = null)
{
    if (_isRecording)
        return;

    try
    {
        // Create fresh buffer and writer for each recording
        _buffer = new MemoryStream();
        var waveFormat = new WaveFormat(SampleRate, BitsPerSample, Channels);
        _writer = new WaveFileWriter(_buffer, waveFormat);

        _waveIn = new WaveInEvent
        {
            DeviceNumber = deviceNumber ?? 0,  // null means use default device (index 0)
            WaveFormat = waveFormat,
            BufferMilliseconds = 50
        };

        _waveIn.DataAvailable += OnDataAvailable;
        _waveIn.RecordingStopped += OnRecordingStoppedInternal;

        _recordingStart = DateTime.UtcNow;
        _waveIn.StartRecording();
        _isRecording = true;

        RecordingStarted?.Invoke(this, EventArgs.Empty);
    }
    catch (NAudio.MmException ex)
    {
        CleanupRecording();
        var message = ex.Result switch
        {
            NAudio.MmResult.BadDeviceId => "Selected microphone not found. Using default device.",
            NAudio.MmResult.NoDriver => "No audio driver installed.",
            NAudio.MmResult.NotEnabled => "Microphone access denied. Check Windows privacy settings.",
            _ => $"Microphone error: {ex.Message}"
        };
        CaptureError?.Invoke(this, message);

        // If BadDeviceId, try falling back to default device
        if (ex.Result == NAudio.MmResult.BadDeviceId && deviceNumber != null)
        {
            StartCapture(null);  // Retry with default device
        }
    }
    catch (Exception ex)
    {
        CleanupRecording();
        CaptureError?.Invoke(this, $"Microphone access failed: {ex.Message}");
    }
}
```

**Key changes:**
1. Accept `int? deviceNumber` parameter (default null)
2. Set `WaveInEvent.DeviceNumber = deviceNumber ?? 0`
3. Improve BadDeviceId error message
4. Add fallback retry with default device when BadDeviceId occurs

### TrayApplicationContext Integration

```csharp
// Update TrayApplicationContext.cs to pass device number
private void OnHotkeyPressed(object? sender, EventArgs e)
{
    _trayIcon.Icon = _recordingIcon;
    _trayIcon.Text = "Coxixo - Recording...";

    // Pass selected device number from settings
    _audioCaptureService.StartCapture(_settings.MicrophoneDeviceNumber);
}
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Hardcoded default device | User-selectable device in settings | N/A (new feature) | Users can select specific microphone when multiple devices available |
| WaveInCapabilities only | MMDeviceEnumerator + matching | 2020+ (community practice) | Full device names improve UX when multiple similar devices present |
| No device validation | Validate on startup + fallback | N/A (best practice) | Prevents crashes when saved device unplugged or reordered |

**Deprecated/outdated:**
- **DirectSoundIn:** Deprecated in NAudio 2.x in favor of WaveInEvent (background threads) or WasapiCapture (lower latency). WaveInEvent is correct choice for background tray app.

## Open Questions

1. **Should we switch to WasapiCapture for lower latency?**
   - What we know: WasapiCapture accepts MMDevice objects directly (no device number matching needed), has lower latency than WaveInEvent
   - What's unclear: Whether latency improvement (typically 10-30ms) is noticeable for push-to-talk use case, whether WasapiCapture has different compatibility/stability profile
   - Recommendation: Keep WaveInEvent for Phase 8 (proven in existing codebase since Phase 2). Evaluate WasapiCapture post-v1.0 if users report latency issues.

2. **Should we show device state (Active/Disabled/Unplugged) in UI?**
   - What we know: MMDeviceEnumerator can filter by DeviceState.Active vs DeviceState.All
   - What's unclear: Whether showing disabled/unplugged devices adds value vs. confusion
   - Recommendation: Only show Active devices (current pattern in code example). Disabled devices can't be used for recording.

3. **Should we add real-time device hotplug detection?**
   - What we know: MMNotificationClient interface provides device change notifications, would allow updating ComboBox when devices added/removed
   - What's unclear: Whether complexity justified for v1.0; settings window is transient (user opens, selects, closes)
   - Recommendation: Defer to post-v1.0. Current approach (enumerate on settings window open) handles 95% of use cases with zero complexity.

4. **Should we remember "last used device" if saved device unavailable?**
   - What we know: Current pattern validates saved device, falls back to system default if invalid
   - What's unclear: Whether tracking device history adds value (e.g., user's USB mic disconnected, remember it for when reconnected)
   - Recommendation: Defer to post-v1.0. Simple fallback to default is more predictable UX than "app remembers devices you used to have."

## Sources

### Primary (HIGH confidence)
- [NAudio WaveInEvent.cs - GitHub](https://github.com/naudio/NAudio/blob/master/NAudio.WinMM/WaveInEvent.cs) - DeviceNumber property and GetCapabilities implementation
- [NAudio MMDeviceEnumerator.cs - GitHub](https://github.com/naudio/NAudio/blob/master/NAudio.Wasapi/CoreAudioApi/MMDeviceEnumerator.cs) - CoreAudio device enumeration API
- [NAudio Issue #646 - WaveInCapabilities truncates device names to 32 characters](https://github.com/naudio/NAudio/issues/646) - Confirmed truncation limitation and workarounds
- [NAudio Issue #612 - ProductName properties truncates device](https://github.com/naudio/NAudio/issues/612) - MMDeviceEnumerator solution recommended
- Coxixo codebase (AudioCaptureService.cs, AppSettings.cs, SettingsForm.cs) - Existing patterns for settings persistence and service architecture
- Phase 7 Research (07-RESEARCH.md) - ComboBox binding patterns with nullable values

### Secondary (MEDIUM confidence)
- [NAudio Issue #657 - WaveIn blocks forever when USB device unplugged](https://github.com/naudio/NAudio/issues/657) - Device disconnection behavior
- [NAudio Issue #535 - Hang when disposing WaveOut after device removed](https://github.com/naudio/NAudio/issues/535) - Disposal issues with removed devices
- [NAudio Output Devices - Mark Heath](https://www.markheath.net/post/naudio-audio-output-devices) - Device enumeration patterns (output devices, but same principles apply)
- [Listing Audio Recording Equipment using NAudio - CopyProgramming](https://copyprogramming.com/howto/enumerate-recording-devices-in-naudio) - Device enumeration tutorial
- [Access Microphone Audio with C# - Scott W Harden](https://swharden.com/csdv/audio/naudio/) - NAudio microphone capture patterns

### Tertiary (LOW confidence)
- N/A

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - NAudio 2.2.1 already in use, MMDeviceEnumerator well-documented in official repo
- Architecture: HIGH - Follows Phase 7 ComboBox pattern, existing AudioCaptureService architecture proven in production
- Pitfalls: HIGH - Device truncation, validation, and disconnection issues extensively documented in NAudio GitHub issues with confirmed workarounds

**Research date:** 2026-02-09
**Valid until:** 2026-03-09 (30 days - stable domain, NAudio 2.x mature)
