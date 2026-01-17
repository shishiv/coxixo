# Project Research Summary

**Project:** Coxixo - Windows Voice-to-Clipboard Transcription
**Domain:** Windows desktop utility / voice transcription
**Researched:** 2026-01-17
**Confidence:** HIGH

## Executive Summary

Coxixo is a lightweight Windows utility that captures voice via push-to-talk hotkey and copies transcribed text to the clipboard using Azure OpenAI Whisper. The research strongly recommends **C# .NET 8 with Windows Forms** as the optimal stack. This combination provides 10-20MB memory footprint (vs 200-400MB for Electron), instant startup via Native AOT, and mature libraries for audio capture (NAudio), system tray (NotifyIcon), and global hotkeys (Win32 RegisterHotKey). The stack is production-proven for exactly this type of Windows utility app.

The product philosophy is deliberate minimalism. Competitors suffer from "identity crisis" by trying to do too much. Coxixo's differentiator is simplicity: hold hotkey, speak, release, paste. No accounts, no subscriptions, no AI rewrites, no meeting transcription, no text editing. Every feature not built is a feature users do not have to configure or be confused by.

Key risks include: (1) ghost tray icons on crash requiring explicit NotifyIcon disposal, (2) global hotkey registration failing silently without a Windows message pump, (3) microphone permissions denied silently on Windows 10/11 privacy settings, and (4) Whisper API latency if audio is not encoded optimally (16kHz mono WAV). All are well-documented with clear prevention strategies.

## Key Findings

### Recommended Stack

C# .NET 8 Windows Forms provides the smallest memory footprint and fastest startup for this use case. The stack avoids bundling a browser engine (Electron, Tauri) which would add unnecessary weight to a tray app with minimal UI.

**Core technologies:**
- **C# .NET 8 Windows Forms:** Application framework — lightest weight, NotifyIcon built-in, 10-20MB RAM
- **NAudio 2.2.x:** Audio capture — de facto .NET audio library, WaveInEvent for microphone, WASAPI support
- **RegisterHotKey (Win32 P/Invoke):** Global hotkey — zero dependencies, most lightweight option
- **HttpClient (built-in):** Azure API calls — no dependencies, async, handles multipart audio upload
- **System.Windows.Forms.NotifyIcon:** System tray — built-in, full Windows integration

**Deployment:** Single-file trimmed executable (15-25MB), self-contained. Native AOT optional for faster startup but experimental for WinForms in .NET 8.

### Expected Features

**Must have (table stakes):**
- Push-to-talk global hotkey (fixed: Ctrl+Shift+Space)
- System tray icon with recording state indicator
- Audio recording via default microphone
- Azure OpenAI Whisper API transcription
- Copy result to clipboard
- Basic error handling (toast or tray notification)

**Should have (v1.1 and beyond):**
- Hotkey customization
- Microphone selection
- Startup with Windows
- Audio feedback sounds (beep on start/stop)
- Multi-language selection

**Defer (v2+):**
- Offline mode (requires whisper.cpp, high complexity)
- Multiple transcription providers
- Text editing/refinement
- Meeting transcription, voice commands, history storage

### Architecture Approach

The application consists of six major components with clear boundaries: Tray Shell (lifecycle, menu), Hotkey Manager (push-to-talk state machine), Audio Recorder (NAudio WaveInEvent), Transcription Client (Azure SDK or HttpClient), Clipboard Manager (thread-safe SetText), and Feedback Controller (sounds, icon state). The threading model requires careful attention: clipboard access must be on the STA/UI thread, NAudio callbacks are on a background thread, and API calls should be async to avoid blocking UI.

**Major components:**
1. **Tray Shell** — application lifecycle, system tray presence, context menu, message pump
2. **Hotkey Manager** — global keyboard hook, KeyDown/KeyUp detection, push-to-talk coordination
3. **Audio Recorder** — microphone capture at 16kHz/16-bit/mono, buffer to MemoryStream
4. **Transcription Client** — Azure Whisper API POST, handles timeout and retry
5. **Clipboard Manager** — thread-safe text write, Unicode format
6. **Feedback Controller** — icon state changes, system sounds

### Critical Pitfalls

1. **Ghost tray icons on crash** — Windows does not auto-clean tray icons. Dispose NotifyIcon in ALL exit paths including unhandled exception handlers. Call `Application.DoEvents()` after disposal.

2. **Global hotkey without message pump** — Hotkeys communicate via Windows messages. Registration on a thread without `Application.Run()` means callbacks never fire. Always register on UI thread.

3. **Microphone permission denied silently** — Windows 10/11 privacy settings can block mic access. Check permission at startup, show clear error with instructions to enable in Settings.

4. **Whisper API timeout on long recordings** — Default audio encoding sends large files. Use 16kHz sample rate, mono channel, WAV format. Target 30 seconds max per request.

5. **Audio device exclusive mode conflicts** — Zoom/Teams/Discord can lock the microphone. Use WASAPI in shared mode (not exclusive), handle device-in-use errors gracefully.

## Implications for Roadmap

Based on research, suggested phase structure:

### Phase 1: Core Shell and Foundation
**Rationale:** Must establish application lifecycle, tray presence, and hotkey infrastructure before anything else. Architecture research shows Tray Shell and Hotkey Manager are Level 0-1 dependencies.
**Delivers:** Running tray app with working hotkey detection
**Addresses:** System tray presence, visual recording indicator, push-to-talk hotkey (table stakes)
**Avoids:** Ghost tray icons (#2), no message pump (#3), multiple instances (#7)
**Components:** Tray Shell, Hotkey Manager, Configuration Model

### Phase 2: Audio Pipeline
**Rationale:** Audio capture must work before API integration. NAudio setup and buffer management are foundational.
**Delivers:** Ability to record audio to memory buffer
**Uses:** NAudio WaveInEvent, 16kHz/16-bit/mono format
**Implements:** Audio Recorder component
**Avoids:** WASAPI silence deadlock (#1), mic permission denied (#5), device exclusive mode (#6), buffer format confusion (#10)

### Phase 3: API Integration and Clipboard
**Rationale:** With audio captured, integrate transcription. Clipboard output completes the core loop.
**Delivers:** End-to-end voice-to-clipboard functionality
**Uses:** HttpClient or Azure.AI.OpenAI SDK, System.Windows.Forms.Clipboard
**Implements:** Transcription Client, Clipboard Manager
**Avoids:** API timeout (#4), rate limiting (#14), Unicode encoding issues (#9)

### Phase 4: Feedback and Polish
**Rationale:** Core loop works; add UX polish and feedback mechanisms.
**Delivers:** Professional UX with audio/visual feedback
**Implements:** Feedback Controller (icon states, sounds)
**Avoids:** No visual feedback during recording (#13), cold start latency (#11)

### Phase 5: Settings and Configuration
**Rationale:** After core works, allow customization. Lower priority than working app.
**Delivers:** Settings UI for hotkey, mic selection, startup option
**Addresses:** Hotkey customization, microphone selection, startup with Windows (from v1.1 features)
**Avoids:** Startup registration unreliability (#8), device hot-swap issues (#15)

### Phase Ordering Rationale

- **Shell before Audio:** Hotkey registration requires message pump from tray app
- **Audio before API:** Need audio bytes to test transcription
- **API before Feedback:** Core function before polish
- **Settings last:** Customization is lower priority than working defaults

### Research Flags

Phases likely needing deeper research during planning:
- **Phase 2 (Audio Pipeline):** Push-to-talk key release detection may require low-level keyboard hook (SetWindowsHookEx) instead of RegisterHotKey. Validate during planning.
- **Phase 5 (Settings):** Task Scheduler vs registry for startup reliability needs testing on target Windows versions.

Phases with standard patterns (skip research-phase):
- **Phase 1 (Core Shell):** Well-documented NotifyIcon pattern, established tray app architecture
- **Phase 3 (API Integration):** Azure OpenAI Whisper API is well-documented with clear examples
- **Phase 4 (Feedback):** SystemSounds and icon swap are trivial patterns

## Confidence Assessment

| Area | Confidence | Notes |
|------|------------|-------|
| Stack | HIGH | Production-proven .NET Windows Forms pattern, verified official docs |
| Features | HIGH | Multiple competitor analysis, clear user expectations |
| Architecture | HIGH | Standard patterns from authoritative sources, well-established |
| Pitfalls | HIGH | Cross-referenced multiple sources, documented prevention strategies |

**Overall confidence:** HIGH

### Gaps to Address

- **Push-to-talk key release detection:** RegisterHotKey only fires on key DOWN. For hold-to-talk, need to validate whether NonInvasiveKeyboardHook or SetWindowsHookEx is required. Test early in Phase 2.
- **Native AOT for WinForms:** Experimental in .NET 8, better in .NET 9. If startup performance is critical, may need to upgrade target framework or accept trimmed single-file as compromise.
- **Azure API latency:** Real-world latency depends on Azure region and endpoint. Test with actual deployment to validate <2 second transcription target.

## Sources

### Primary (HIGH confidence)
- [Microsoft Learn: NotifyIcon Class](https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.notifyicon) — system tray implementation
- [Microsoft Learn: Azure OpenAI Whisper Quickstart](https://learn.microsoft.com/en-us/azure/ai-foundry/openai/whisper-quickstart) — API integration
- [NAudio GitHub](https://github.com/naudio/NAudio) — audio capture library, WaveInEvent patterns
- [Microsoft Learn: RegisterHotKey](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-registerhotkey) — global hotkey registration

### Secondary (MEDIUM confidence)
- [Tauri vs Electron comparison](https://www.levminer.com/blog/tauri-vs-electron) — framework memory comparison
- [Zapier Best Dictation Software 2026](https://zapier.com/blog/best-text-dictation-software/) — competitor feature analysis
- [CodeProject - Global Hotkeys](https://www.codeproject.com/Articles/1273010/Global-Hotkeys-within-Desktop-Applications) — hotkey implementation patterns

### Tertiary (LOW confidence)
- [GitHub Issue - WinForms AOT](https://github.com/dotnet/winforms/issues/9911) — AOT support status (needs validation for .NET 9)

---
*Research completed: 2026-01-17*
*Ready for roadmap: yes*
