# Architecture Research

**Project:** Coxixo - Windows Voice-to-Clipboard Transcription
**Researched:** 2026-01-17
**Confidence:** HIGH (multiple authoritative sources, well-established patterns)

## Component Overview

The application consists of six major components with clear responsibilities:

| Component | Responsibility | Key Technology |
|-----------|---------------|----------------|
| **Tray Shell** | Application lifecycle, system tray presence, context menu | NotifyIcon (Windows Forms) |
| **Hotkey Manager** | Global keyboard hook, push-to-talk state machine | Win32 RegisterHotKey / low-level hook |
| **Audio Recorder** | Microphone capture, buffer management, WAV encoding | NAudio WaveInEvent |
| **Transcription Client** | Azure OpenAI Whisper API communication | Azure.AI.OpenAI SDK |
| **Clipboard Manager** | Write transcription to system clipboard | System.Windows.Forms.Clipboard |
| **Feedback Controller** | Audio beeps, icon state changes | SystemSounds, Timer-based icon swap |

### Component Boundaries

```
+------------------+
|    Tray Shell    |  <-- Entry point, owns message loop
+------------------+
         |
         v
+------------------+     +---------------------+
|  Hotkey Manager  |---->|  Feedback Controller|
+------------------+     +---------------------+
         |                        |
         v                        v
+------------------+     +------------------+
|  Audio Recorder  |     |   Tray Icon UI   |
+------------------+     +------------------+
         |
         v
+---------------------+
| Transcription Client|
+---------------------+
         |
         v
+------------------+
| Clipboard Manager|
+------------------+
```

## Data Flow

### Primary Flow: Voice to Clipboard

```
1. USER HOLDS HOTKEY
   |
   v
2. Hotkey Manager detects KeyDown
   |-- Notifies Feedback Controller (start beep + icon change)
   |-- Notifies Audio Recorder (StartRecording)
   |
   v
3. Audio Recorder captures microphone input
   |-- WaveInEvent.DataAvailable fires repeatedly
   |-- Buffers accumulate in MemoryStream or BufferedWaveProvider
   |
   v
4. USER RELEASES HOTKEY
   |
   v
5. Hotkey Manager detects KeyUp
   |-- Notifies Audio Recorder (StopRecording)
   |-- Notifies Feedback Controller (stop beep + icon change)
   |
   v
6. Audio Recorder finalizes audio
   |-- Produces WAV/MP3 byte array (25MB limit for Azure)
   |-- Passes to Transcription Client
   |
   v
7. Transcription Client calls Azure Whisper API
   |-- POST audio to endpoint
   |-- Receives text response
   |
   v
8. Clipboard Manager writes text
   |-- Clipboard.SetText(transcription)
   |
   v
9. Feedback Controller signals completion
   |-- Success beep or failure notification
```

### Audio Data Format Flow

```
Microphone --> WaveInEvent (16-bit PCM, 16kHz mono)
           --> byte[] buffer (DataAvailable event)
           --> MemoryStream accumulator
           --> WAV header prepended
           --> Azure Whisper API (accepts WAV, MP3, etc.)
           --> string transcription
           --> Clipboard
```

**Audio Format Recommendation:**
- Sample Rate: 16kHz (Whisper optimized for this)
- Bit Depth: 16-bit signed PCM
- Channels: Mono
- Format for API: WAV (simplest, no encoding overhead)

## Threading Model

### Thread Responsibilities

| Thread | Component | Purpose |
|--------|-----------|---------|
| **UI Thread (STA)** | Tray Shell, Clipboard Manager | Windows message loop, clipboard access |
| **Audio Thread** | Audio Recorder | NAudio callback handling |
| **API Thread** | Transcription Client | Async HTTP calls to Azure |

### Critical Threading Rules

1. **Clipboard requires STA thread**
   - `Clipboard.SetText()` must run on UI thread
   - Use `Control.Invoke()` or `SynchronizationContext` to marshal

2. **NAudio callbacks are on background thread**
   - `WaveInEvent.DataAvailable` fires on audio thread
   - Do NOT update UI directly from this callback
   - Buffer data, then process on completion

3. **API calls should be async**
   - Use `async/await` for Whisper API calls
   - Prevents blocking UI thread during network wait
   - ConfigureAwait(false) for library code, capture context for UI updates

### Recommended Pattern

```csharp
// Pseudo-code for thread-safe flow

// On hotkey release (UI thread):
async void OnHotkeyReleased()
{
    byte[] audioData = _recorder.StopAndGetAudio();  // Audio thread -> UI thread

    // Show "processing" state
    _feedback.SetProcessingState();

    // API call on background thread
    string transcription = await _transcriptionClient
        .TranscribeAsync(audioData)
        .ConfigureAwait(true);  // Return to UI thread

    // Clipboard on UI thread
    Clipboard.SetText(transcription);

    // Success feedback
    _feedback.SetIdleState();
    SystemSounds.Asterisk.Play();
}
```

### Synchronization Requirements

| Operation | Thread Requirement | Solution |
|-----------|-------------------|----------|
| Start recording | Any | NAudio handles internally |
| Stop recording | Any | NAudio handles internally |
| Get audio buffer | Must synchronize | Lock or Interlocked on MemoryStream |
| API call | Background preferred | async/await |
| Set clipboard | STA/UI thread | Invoke/marshal |
| Change tray icon | UI thread | Invoke/marshal |
| Play system sound | Any | SystemSounds is thread-safe |

## Component Dependencies

### Dependency Graph (Build Order)

```
Level 0 (No dependencies):
  - Configuration (settings model)

Level 1 (Depends on config):
  - Transcription Client (needs API key, endpoint)
  - Audio Recorder (needs device selection)
  - Hotkey Manager (needs hotkey configuration)

Level 2 (Depends on Level 1):
  - Feedback Controller (coordinates recorder state)

Level 3 (Depends on multiple Level 1-2):
  - Tray Shell (orchestrates all components)
```

### Package Dependencies

| Component | NuGet Package | Version Note |
|-----------|--------------|--------------|
| Audio Recorder | NAudio | 2.2.x stable, .NET 6+ support |
| Transcription Client | Azure.AI.OpenAI | 1.0.0+ (check for Whisper support) |
| Hotkey Manager | None (Win32 P/Invoke) | Manual implementation |
| Tray Shell | System.Windows.Forms | Built-in with .NET |
| Clipboard | System.Windows.Forms | Built-in with .NET |
| Feedback | System.Media | Built-in with .NET |

## Suggested Build Order

Based on component dependencies and testability, build in this order:

### Phase 1: Core Infrastructure

1. **Configuration Model**
   - API key, endpoint, hotkey settings
   - JSON file storage
   - Validates before other components start

2. **Tray Shell (Minimal)**
   - NotifyIcon with static icon
   - Context menu: Exit only
   - Proves application lifecycle works

### Phase 2: Audio Pipeline

3. **Audio Recorder**
   - WaveInEvent setup
   - Start/Stop methods
   - Buffer to MemoryStream
   - Unit test: record 2 seconds, verify WAV bytes

4. **Transcription Client**
   - Azure.AI.OpenAI integration
   - Accepts byte[] audio, returns string
   - Unit test: transcribe sample.wav file

### Phase 3: Input and Output

5. **Hotkey Manager**
   - Global hotkey registration
   - KeyDown/KeyUp events
   - Push-to-talk state machine
   - Manual test: log key events

6. **Clipboard Manager**
   - Thread-safe SetText wrapper
   - Unit test: set text, verify via GetText

### Phase 4: Integration and Polish

7. **Feedback Controller**
   - Connect hotkey state to icon changes
   - Play beeps on state transitions
   - Multiple icon assets (idle, recording, processing)

8. **Settings UI**
   - WPF Window or WinForms Form
   - API key entry, hotkey configuration
   - Accessible from tray context menu

### Phase 5: Error Handling and Edge Cases

9. **Error Recovery**
   - API timeout handling
   - Microphone unavailable
   - Clipboard locked by another app
   - Network failures

## Integration Points

### Interface Contracts

```csharp
// Core interfaces for loose coupling

interface IAudioRecorder
{
    void StartRecording();
    byte[] StopRecording();  // Returns WAV bytes
    event EventHandler<float> LevelChanged;  // Optional: for future level meter
}

interface ITranscriptionClient
{
    Task<string> TranscribeAsync(byte[] audioData, string language);
}

interface IHotkeyManager : IDisposable
{
    void Register(Keys key, Keys modifiers);
    void Unregister();
    event EventHandler HotkeyPressed;
    event EventHandler HotkeyReleased;
}

interface IFeedbackController
{
    void SetState(AppState state);  // Idle, Recording, Processing
}

interface IClipboardService
{
    void SetText(string text);
}
```

### Event Flow Between Components

```
HotkeyManager.HotkeyPressed
    --> FeedbackController.SetState(Recording)
    --> AudioRecorder.StartRecording()

HotkeyManager.HotkeyReleased
    --> AudioRecorder.StopRecording() -> byte[]
    --> FeedbackController.SetState(Processing)
    --> TranscriptionClient.TranscribeAsync(audio) -> string
    --> ClipboardService.SetText(transcription)
    --> FeedbackController.SetState(Idle)
```

### Error Propagation

```
TranscriptionClient throws
    --> Catch in orchestrator
    --> FeedbackController.SetState(Error)
    --> Show balloon notification with error
    --> FeedbackController.SetState(Idle) after timeout
```

## Key Architectural Decisions

### Decision 1: Windows Forms over WPF for Tray

**Recommendation:** Use Windows Forms NotifyIcon

**Rationale:**
- WPF has no native system tray support (requires WinForms interop anyway)
- WinForms NotifyIcon is simple, well-documented, and reliable
- Settings UI can still be WPF if desired (separate window)
- Minimal overhead for a tray-only app

**Source:** [Simple Talk - Creating Tray Applications in .NET](https://www.red-gate.com/simple-talk/development/dotnet-development/creating-tray-applications-in-net-a-practical-guide/)

### Decision 2: NAudio WaveInEvent over WasapiCapture

**Recommendation:** Use WaveInEvent for simplicity

**Rationale:**
- WaveInEvent is event-based, perfect for push-to-talk pattern
- Works on all Windows versions (WASAPI requires Vista+)
- Sufficient quality for speech (16kHz/16-bit is fine for Whisper)
- WasapiCapture adds complexity without benefit for this use case

**Source:** [NAudio Documentation](https://github.com/naudio/NAudio)

### Decision 3: RegisterHotKey over Low-Level Hook

**Recommendation:** Use Win32 RegisterHotKey

**Rationale:**
- Simpler implementation for single hotkey
- OS handles the hook, less error-prone
- Caveat: Cannot detect key release directly with RegisterHotKey
- Alternative: Use a low-level keyboard hook if key release detection is critical

**Note:** For true hold-to-talk (detect release), you need a low-level keyboard hook (SetWindowsHookEx with WH_KEYBOARD_LL). RegisterHotKey only fires on press.

**Source:** [CodeProject - Global Hotkeys](https://www.codeproject.com/Articles/1273010/Global-Hotkeys-within-Desktop-Applications)

### Decision 4: In-Memory Audio Buffer

**Recommendation:** Buffer to MemoryStream, not file

**Rationale:**
- Faster (no disk I/O)
- Simpler cleanup (no temp files)
- 25MB limit for Azure Whisper (in-memory is fine for short recordings)
- Typical speech: ~16KB/sec at 16kHz/16-bit mono, so 1 minute = ~1MB

**Formula:** `bytes = sampleRate * bytesPerSample * channels * seconds`
`16000 * 2 * 1 * 60 = 1,920,000 bytes (~1.8MB per minute)`

### Decision 5: Synchronous Sound Playback

**Recommendation:** Use `SystemSounds.Beep.Play()` for feedback

**Rationale:**
- Non-blocking (plays async by default)
- No file management
- Consistent with Windows UX
- Alternative: Custom WAV files with SoundPlayer for branded sounds

**Source:** [Microsoft Learn - SystemSounds](https://learn.microsoft.com/en-us/dotnet/api/system.media.systemsounds)

## Sources

**System Tray Architecture:**
- [Simple Talk - Creating Tray Applications in .NET](https://www.red-gate.com/simple-talk/development/dotnet-development/creating-tray-applications-in-net-a-practical-guide/)
- [CodeProject - Formless System Tray Application](https://www.codeproject.com/Articles/290013/Formless-System-Tray-Application)

**Audio Capture:**
- [NAudio GitHub](https://github.com/naudio/NAudio)
- [NAudio DeepWiki - Architecture](https://deepwiki.com/naudio/NAudio)
- [swharden.com - Access Microphone Audio with C#](https://swharden.com/csdv/audio/naudio/)

**Global Hotkeys:**
- [CodeProject - Global Hotkeys](https://www.codeproject.com/Articles/1273010/Global-Hotkeys-within-Desktop-Applications)
- [NHotkey Library](https://github.com/thomaslevesque/NHotkey)
- [GlobalHotKeys NuGet](https://github.com/8/GlobalHotKeys)

**Azure OpenAI Whisper:**
- [Microsoft Learn - Speech to text with Whisper](https://learn.microsoft.com/en-us/azure/ai-foundry/openai/whisper-quickstart)
- [Azure-Samples - Whisper C# Guide](https://github.com/Azure-Samples/openai/blob/main/Basic_Samples/Whisper/dotnet/csharp/Whisper_processing_guide.ipynb)

**Threading:**
- [Microsoft Learn - WPF Threading Model](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/threading-model)
- [Microsoft Learn - BackgroundWorker](https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.backgroundworker)
- [Stephen Cleary - Task.Run vs BackgroundWorker](https://blog.stephencleary.com/2013/05/taskrun-vs-backgroundworker-round-1.html)

**Clipboard:**
- [Microsoft Learn - Clipboard Class](https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.clipboard)

**Tray Icon Animation:**
- [Microsoft Learn - Animation in System Tray](https://learn.microsoft.com/en-us/archive/blogs/abhinaba/animation-and-text-in-system-tray-using-c)
- [C# Corner - Animated System Tray Icon](https://www.c-sharpcorner.com/code/1366/c-sharp-animated-system-tray-icon.aspx)
