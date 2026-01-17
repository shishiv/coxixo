# Stack Research

**Project:** Coxixo - Windows voice-to-clipboard transcription
**Researched:** 2026-01-17
**Mode:** Ecosystem research with emphasis on lightweight/low memory

---

## Executive Summary

For a lightweight Windows system tray app with audio capture and cloud API integration, **C# with .NET 8 Windows Forms** is the recommended stack. It provides the smallest memory footprint, fastest startup, native Windows integration, and mature audio/hotkey libraries — all without bundling a browser engine.

**Key finding:** Electron (200-400MB RAM) and even Tauri (30-40MB RAM) are overkill for this use case. A native .NET Windows Forms app can run with 10-20MB RAM when idle, with instant startup via Native AOT compilation.

---

## Recommended Stack

### Core Framework

| Technology | Version | Purpose | Why |
|------------|---------|---------|-----|
| C# / .NET | 8.0 or 9.0 | Runtime & language | Native Windows support, AOT compilation, mature ecosystem |
| Windows Forms | Built-in (.NET 8+) | System tray & minimal UI | Lightest weight UI framework, NotifyIcon built-in |

### Audio Capture

| Technology | Version | Purpose | Why |
|------------|---------|---------|-----|
| NAudio | 2.2.1+ | Microphone recording | De facto .NET audio library, WASAPI support, actively maintained |

### Global Hotkeys

| Technology | Version | Purpose | Why |
|------------|---------|---------|-----|
| RegisterHotKey (P/Invoke) | Win32 API | Push-to-talk hotkey | Zero dependencies, most lightweight option |
| *OR* NonInvasiveKeyboardHook | 2.2.0 | Alternative if P/Invoke is complex | NuGet package, simpler API |

### HTTP Client

| Technology | Version | Purpose | Why |
|------------|---------|---------|-----|
| HttpClient | Built-in (.NET 8) | Azure OpenAI API calls | Built-in, no dependencies, async/await |
| *OR* Azure.AI.OpenAI | 2.x | Typed SDK | Better auth handling, but adds ~5MB |

### Supporting Libraries

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| System.Text.Json | Built-in | JSON parsing | API responses |
| System.Windows.Forms.NotifyIcon | Built-in | System tray icon | Tray presence |

---

## Language/Framework Evaluation

### Options Considered

| Option | Pros | Cons | Memory (Idle) | Verdict |
|--------|------|------|---------------|---------|
| **C# .NET 8 WinForms** | Native, tiny footprint, AOT, mature audio libs | Windows-only (acceptable) | 10-20MB | **WINNER** |
| Tauri 2.0 | Cross-platform, Rust backend, 30-40MB RAM | Audio plugins immature, learning curve | 30-40MB | Runner-up |
| Go + systray | Fast, single binary, cross-platform | Audio capture is harder, less Windows-native | 15-30MB | Viable |
| Python + pystray | Rapid development, NAudio-like libs exist | Requires Python runtime, GIL issues | 40-60MB | Avoid |
| Electron | Mature ecosystem, easy dev | 200-400MB RAM, slow startup | 200-400MB | **REJECTED** |
| C++/Win32 | Ultimate control, smallest possible | Development time, complexity | 5-10MB | Overkill |

### Winner: C# .NET 8 Windows Forms

**Rationale:**
1. **Memory**: Windows Forms apps are extremely lightweight (10-20MB idle)
2. **Startup**: Native AOT compilation produces instant startup (<100ms cold)
3. **Audio**: NAudio is the definitive .NET audio library, well-documented
4. **System Tray**: NotifyIcon is built into Windows Forms — zero dependencies
5. **Hotkeys**: RegisterHotKey Win32 API via P/Invoke is trivial
6. **HTTP**: Built-in HttpClient handles Azure API calls perfectly
7. **Single File**: .NET 8 supports single-file deployment with trimming

**Confidence: HIGH** — This stack is production-proven for exactly this type of Windows utility app.

---

## Audio Capture Deep Dive

### NAudio (Recommended)

**What it is:** Open-source .NET audio library by Mark Heath

**Why NAudio:**
- Supports WaveInEvent for microphone capture
- WASAPI support for low-latency recording
- Can output to WAV format (required by Whisper API)
- Actively maintained (v2.2.1 released 2024)
- Excellent documentation and examples

**Recording Pattern:**
```csharp
var waveIn = new WaveInEvent {
    DeviceNumber = 0,  // default microphone
    WaveFormat = new WaveFormat(16000, 16, 1),  // 16kHz, 16-bit, mono (Whisper optimal)
    BufferMilliseconds = 50
};
waveIn.DataAvailable += (s, e) => memoryStream.Write(e.Buffer, 0, e.BytesRecorded);
waveIn.StartRecording();
// ... on hotkey release
waveIn.StopRecording();
```

**Format for Whisper API:**
- Whisper resamples to 16kHz internally
- Recording at 16kHz, 16-bit PCM, mono is optimal
- Reduces file size and upload time
- Supported formats: wav, mp3, mp4, mpeg, mpga, m4a, webm

**Memory Efficiency:**
- Use MemoryStream to accumulate audio in RAM
- Typical 30-second recording at 16kHz mono = ~1MB
- Dispose WaveInEvent when not recording

**Confidence: HIGH** — NAudio is the standard choice, verified via [official GitHub](https://github.com/naudio/NAudio) and [Mark Heath's blog](https://markheath.net/post/how-to-record-and-play-audio-at-same).

### Alternatives Considered

| Library | Status | Why Not |
|---------|--------|---------|
| NAudio | **RECOMMENDED** | — |
| Windows.Media.Capture (UWP) | Available | More complex, requires UWP APIs, larger footprint |
| DirectShow | Legacy | Microsoft recommends Media Foundation instead |
| WASAPI (direct P/Invoke) | Works | NAudio wraps this already, no benefit to raw |

---

## System Tray Implementation

### NotifyIcon (Built-in)

**What it is:** System.Windows.Forms.NotifyIcon — built into .NET Windows Forms

**Why NotifyIcon:**
- Zero external dependencies
- Full Windows system tray integration
- Supports icon changes (for recording state)
- Context menu support (for settings, exit)
- Balloon tip notifications

**Implementation Pattern:**
```csharp
var notifyIcon = new NotifyIcon {
    Icon = new Icon("icon.ico"),
    Visible = true,
    Text = "Coxixo",
    ContextMenuStrip = new ContextMenuStrip()
};
notifyIcon.ContextMenuStrip.Items.Add("Settings", null, OnSettings);
notifyIcon.ContextMenuStrip.Items.Add("Exit", null, OnExit);
```

**Visual Feedback:**
- Change Icon property to show recording state (red dot, microphone active)
- Use ShowBalloonTip for notifications (optional)

**Lightweight Run Pattern:**
Instead of a Form, use ApplicationContext for headless operation:
```csharp
Application.Run(new TrayApplicationContext());
```

**Confidence: HIGH** — This is the standard approach, verified via [Microsoft Learn documentation](https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.notifyicon?view=windowsdesktop-9.0).

### Alternatives Considered

| Library | Status | Why Not |
|---------|--------|---------|
| NotifyIcon (WinForms) | **RECOMMENDED** | — |
| H.NotifyIcon (WPF) | Good | Adds WPF dependency, heavier |
| Hardcodet.NotifyIcon (WPF) | Good | Same — WPF overhead unnecessary |

---

## Global Hotkeys Implementation

### RegisterHotKey P/Invoke (Recommended)

**What it is:** Direct Win32 API call via P/Invoke

**Why P/Invoke:**
- Zero external dependencies
- Most lightweight option
- Full control over hotkey registration
- Standard Windows pattern

**Implementation Pattern:**
```csharp
[DllImport("user32.dll")]
private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

[DllImport("user32.dll")]
private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

// Register Ctrl+Shift+Space as push-to-talk
RegisterHotKey(this.Handle, 1, MOD_CONTROL | MOD_SHIFT, VK_SPACE);

// Handle WM_HOTKEY in WndProc
protected override void WndProc(ref Message m) {
    if (m.Msg == WM_HOTKEY) {
        // Start/stop recording
    }
    base.WndProc(ref m);
}
```

**Push-to-Talk Challenge:**
- RegisterHotKey only fires on key DOWN
- For push-to-talk (hold to record), need to detect key UP
- Solution: Use low-level keyboard hook (SetWindowsHookEx) or NonInvasiveKeyboardHook

**Confidence: MEDIUM** — RegisterHotKey is straightforward, but push-to-talk (hold/release) may need keyboard hook. See [Microsoft documentation](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-registerhotkey).

### Alternative: NonInvasiveKeyboardHook

**What it is:** NuGet package that wraps low-level keyboard hooks

**When to use:** If push-to-talk requires key-up detection

```csharp
var keyboardHookManager = new KeyboardHookManager();
keyboardHookManager.Start();
keyboardHookManager.RegisterHotkey(new[] { ModifierKeys.Control }, Keys.Space, () => {
    // Key pressed - start recording
});
```

**Trade-off:** Adds ~50KB dependency, but simplifies key-up detection

**Confidence: MEDIUM** — Library is maintained, but verify key-up detection capabilities via [GitHub](https://github.com/kfirprods/NonInvasiveKeyboardHook).

---

## HTTP Client for Azure OpenAI

### HttpClient (Built-in) - Recommended

**What it is:** Built-in .NET HTTP client

**Why HttpClient:**
- Zero dependencies
- Fully async
- Handles multipart/form-data for audio upload
- Works with Azure API key authentication

**Implementation Pattern:**
```csharp
using var client = new HttpClient();
client.DefaultRequestHeaders.Add("api-key", apiKey);

using var content = new MultipartFormDataContent();
content.Add(new ByteArrayContent(audioBytes), "file", "audio.wav");
content.Add(new StringContent("pt"), "language");

var response = await client.PostAsync(
    $"{endpoint}/openai/deployments/{deployment}/audio/transcriptions?api-version=2024-02-01",
    content
);
var result = await response.Content.ReadAsStringAsync();
```

**Azure OpenAI Whisper Specifics:**
- Max file size: 25MB
- Supported formats: mp3, mp4, mpeg, mpga, m4a, wav, webm
- Endpoint format: `{endpoint}/openai/deployments/{deployment}/audio/transcriptions`
- Required header: `api-key: {your-key}`
- Language hint: `language=pt` for Portuguese

**Confidence: HIGH** — Standard approach, verified via [Azure OpenAI documentation](https://learn.microsoft.com/en-us/azure/ai-foundry/openai/whisper-quickstart?view=foundry-classic).

### Alternative: Azure.AI.OpenAI SDK

**What it is:** Official Microsoft SDK for Azure OpenAI

**When to use:** If you want typed responses, better error handling, or Entra ID auth

```csharp
var client = new AzureOpenAIClient(
    new Uri(endpoint),
    new ApiKeyCredential(apiKey)
);
var result = await client.GetAudioTranscriptionAsync(
    "whisper-deployment",
    audioStream,
    new AudioTranscriptionOptions { Language = "pt" }
);
```

**Trade-off:** Adds ~5MB to binary, but provides:
- Typed responses
- Better error handling
- Support for Microsoft Entra ID auth (recommended for production)

**Confidence: HIGH** — Official SDK, well-documented via [Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/api/overview/azure/ai.openai-readme?view=azure-dotnet).

---

## Rejected Alternatives

### Electron - REJECTED

| Aspect | Issue |
|--------|-------|
| Memory | 200-400MB RAM idle — 10-20x heavier than WinForms |
| Startup | 1-2 seconds cold start vs <100ms for AOT .NET |
| Binary Size | 80-120MB vs 15-20MB for trimmed .NET |
| Rationale | Bundles entire Chromium engine for a tray app with no UI |

**Source:** [Tauri vs Electron comparison](https://www.levminer.com/blog/tauri-vs-electron)

### Tauri 2.0 - Considered but Not Recommended

| Aspect | Status |
|--------|--------|
| Memory | 30-40MB — better than Electron, but still 2-3x WinForms |
| Audio | tauri-plugin-mic-recorder exists but is immature (v2.0.0 March 2025) |
| Learning Curve | Requires Rust knowledge for backend |
| Verdict | Good option if cross-platform is needed later, overkill for Windows-only v1 |

**Source:** [tauri-plugin-mic-recorder on crates.io](https://crates.io/crates/tauri-plugin-mic-recorder)

### Python - REJECTED

| Aspect | Issue |
|--------|-------|
| Memory | 40-60MB due to Python runtime |
| Startup | Slow interpreter startup |
| Distribution | Requires Python installation or bundled runtime |
| Threading | GIL complicates audio + UI + network concurrency |

### Go - Viable Alternative

| Aspect | Status |
|--------|--------|
| Memory | 15-30MB — competitive |
| Audio | Less mature Windows audio libraries |
| System Tray | fyne.io/systray works well |
| Verdict | Viable if team prefers Go, but .NET has better Windows integration |

**Source:** [fyne.io/systray on Go Packages](https://pkg.go.dev/fyne.io/systray)

---

## Deployment & Size Optimization

### .NET 8 Single-File with Trimming

**Configuration:**
```xml
<PropertyGroup>
    <PublishSingleFile>true</PublishSingleFile>
    <SelfContained>true</SelfContained>
    <RuntimeIdentifier>win-x64</RuntimeIdentifier>
    <PublishTrimmed>true</PublishTrimmed>
    <TrimMode>link</TrimMode>
</PropertyGroup>
```

**Expected Size:** 15-25MB single executable (self-contained)

### Native AOT (Optional - .NET 9 Better Support)

**Configuration:**
```xml
<PropertyGroup>
    <PublishAot>true</PublishAot>
</PropertyGroup>
```

**Benefits:**
- Instant startup (<100ms)
- 30-40% less memory at runtime
- Single native executable

**Caveats:**
- WinForms AOT support is experimental in .NET 8
- Better support in .NET 9
- May need `<_SuppressWinFormsTrimError>true</_SuppressWinFormsTrimError>`

**Expected Size:** 15-20MB native executable

**Confidence: MEDIUM** — AOT for WinForms is improving but not fully production-ready. Trimmed single-file is the safe bet.

**Source:** [GitHub issue on WinForms AOT size](https://github.com/dotnet/winforms/issues/9911)

---

## Installation Commands

### Create Project

```bash
dotnet new winforms -n Coxixo -f net8.0
cd Coxixo
```

### Add Dependencies

```bash
# Audio capture
dotnet add package NAudio

# Optional: Azure SDK (if not using raw HttpClient)
dotnet add package Azure.AI.OpenAI

# Optional: Keyboard hook helper (if P/Invoke is complex)
dotnet add package NonInvasiveKeyboardHookLibrary
```

### Project File Configuration

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWindowsForms>true</UseWindowsForms>
    <ApplicationIcon>icon.ico</ApplicationIcon>
    <PublishSingleFile>true</PublishSingleFile>
    <SelfContained>true</SelfContained>
    <RuntimeIdentifier>win-x64</RuntimeIdentifier>
    <PublishTrimmed>true</PublishTrimmed>
  </PropertyGroup>
</Project>
```

---

## Confidence Summary

| Component | Recommendation | Confidence | Reason |
|-----------|---------------|------------|--------|
| **Language/Runtime** | C# .NET 8 | HIGH | Production-proven, optimal for Windows utilities |
| **UI Framework** | Windows Forms | HIGH | Lightest weight, NotifyIcon built-in |
| **Audio Capture** | NAudio 2.2.x | HIGH | De facto standard, excellent docs |
| **System Tray** | NotifyIcon | HIGH | Built-in, zero dependencies |
| **Global Hotkeys** | RegisterHotKey + Hook | MEDIUM | P/Invoke straightforward, push-to-talk may need hook |
| **HTTP Client** | HttpClient (built-in) | HIGH | Zero dependencies, async, handles multipart |
| **Deployment** | Single-file trimmed | HIGH | Well-supported in .NET 8 |
| **Native AOT** | Optional | MEDIUM | Experimental for WinForms, better in .NET 9 |

---

## Sources

### Official Documentation
- [Microsoft Learn: NotifyIcon Class](https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.notifyicon?view=windowsdesktop-9.0)
- [Microsoft Learn: RegisterHotKey function](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-registerhotkey)
- [Microsoft Learn: Azure OpenAI Whisper Quickstart](https://learn.microsoft.com/en-us/azure/ai-foundry/openai/whisper-quickstart?view=foundry-classic)
- [Microsoft Learn: Native AOT deployment](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/)
- [Microsoft Learn: .NET Single-file deployment](https://learn.microsoft.com/en-us/dotnet/core/deploying/single-file/overview)

### Libraries
- [NAudio GitHub](https://github.com/naudio/NAudio)
- [NonInvasiveKeyboardHook GitHub](https://github.com/kfirprods/NonInvasiveKeyboardHook)
- [Azure.AI.OpenAI NuGet](https://www.nuget.org/packages/Azure.AI.OpenAI)

### Comparisons
- [Tauri vs Electron - Levminer](https://www.levminer.com/blog/tauri-vs-electron)
- [Tauri vs Electron 2025 - Codeology](https://codeology.co.nz/articles/tauri-vs-electron-2025-desktop-development.html)
- [Native AOT performance - ABP.IO](https://abp.io/community/articles/native-aot-how-to-fasten-startup-time-and-memory-footprint-3gsfre75)

### Audio Format
- [Whisper API optimal format - DEV Community](https://dev.to/mxro/optimise-openai-whisper-api-audio-format-sampling-rate-and-quality-29fj)
- [NAudio Microphone Recording - swharden.com](https://swharden.com/csdv/audio/naudio/)
