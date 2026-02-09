# Project Research Summary

**Project:** Coxixo v1.1 Milestone
**Domain:** Windows system tray voice transcription utility enhancements
**Researched:** 2026-02-09
**Confidence:** HIGH

## Executive Summary

The v1.1 milestone adds four feature enhancements to an existing Windows .NET 8 WinForms system tray app with validated v1.0 architecture. Research confirms all four features (hotkey modifier support, microphone selection, language selection, Windows startup) are achievable with the existing stack requiring zero new dependencies. The existing WH_KEYBOARD_LL hook already captures modifier state via GetAsyncKeyState, NAudio's MMDeviceEnumerator handles device enumeration, Azure OpenAI Whisper accepts ISO-639-1 language codes, and Windows startup uses standard HKCU registry Run key.

The recommended approach is incremental service extension rather than architectural change: extend KeyboardHookService for modifier detection, extend AudioCaptureService for device selection, add language parameter to TranscriptionService constructor, and create a new static StartupService for registry management. All features integrate cleanly into the established ApplicationContext pattern with AppSettings model persistence via ConfigurationService. Implementation risk is low for three features (modifiers, language, startup) and medium for microphone selection due to device enumeration edge cases.

Critical risks center on three areas: modifier key desynchronization causing "stuck modifier" bugs (prevented by using GetAsyncKeyState not GetKeyState), microphone device index invalidation when USB devices are unplugged (prevented by storing device product name not index), and WaveInEvent deadlock during device switching (prevented by stopping recording before changing device number). Additional moderate risks include Whisper API hallucination on silent audio (mitigated by explicit language parameter and client-side energy detection) and microphone permission denial on Windows 11 (handled with proactive permission check and actionable error dialog). All identified pitfalls have documented prevention strategies verified against official Microsoft and library documentation.

## Key Findings

### Recommended Stack

No stack changes required for v1.1. All four features are achievable with the existing .NET 8 + NAudio 2.2.1 + Azure.AI.OpenAI 2.1.0 stack using built-in Windows APIs. The v1.0 architecture (ApplicationContext + static services + WinForms settings UI) accommodates all enhancements without refactoring.

**Core technologies (unchanged):**
- **.NET 8 with WinForms**: Built-in ApplicationContext pattern for formless tray app, Registry API for startup management
- **NAudio 2.2.1**: WaveInEvent.DeviceNumber for device selection, WaveIn.GetCapabilities for enumeration (MMDeviceEnumerator available for future enhancement)
- **Azure.AI.OpenAI 2.1.0**: AudioTranscriptionOptions.Language property supports ISO-639-1 codes (pt, en, es, etc.) and null for auto-detect
- **WH_KEYBOARD_LL hook**: GetAsyncKeyState P/Invoke for modifier state detection (Ctrl, Shift, Alt via VK codes)

**What NOT to add:**
- Third-party hotkey libraries (GetAsyncKeyState is simpler)
- Task Scheduler wrappers (Registry Run key is sufficient for v1.1)
- System.Speech (Whisper handles language detection)

### Expected Features

All four features are table stakes for v1.1 with minimal differentiation opportunities.

**Must have (table stakes):**
- **Hotkey modifiers**: Ctrl/Alt/Shift + Key combinations with conflict detection feedback
- **Microphone selection**: Dropdown with friendly device names, current default indicator, persist selection across restarts
- **Language selection**: Dropdown with common languages (pt, en, es, fr, etc.) and auto-detect option
- **Windows startup**: Simple checkbox toggle writing to HKCU\Software\Microsoft\Windows\CurrentVersion\Run

**Should have (differentiators) — defer to v1.2+:**
- Win key modifier support (nice-to-have for power users, trivial to add later)
- Microphone volume preview (good UX but adds complexity)
- Startup delay option (advanced feature, not essential)

**Anti-features (deliberately avoid):**
- Hotkey profiles/presets (adds complexity, no clear value for single-purpose utility)
- Voice-activated recording (push-to-talk provides explicit control)
- Per-application microphone profiles (out of scope for tray utility)
- Multi-hotkey support (language is a preference setting, not per-recording)

### Architecture Approach

v1.1 extends existing services rather than creating new architecture. The ApplicationContext + static services pattern accommodates all features cleanly via AppSettings model expansion and service modifications.

**Major components (modified):**
1. **AppSettings model**: +4 properties (HotkeyModifiers, AudioInputDeviceNumber, TranscriptionLanguage, StartWithWindows) — ConfigurationService handles serialization automatically, backwards compatible
2. **KeyboardHookService**: Add ModifierKeys property, modify HookCallback to check GetAsyncKeyState for Ctrl/Shift/Alt state before firing events
3. **AudioCaptureService**: Add DeviceNumber property passed to WaveInEvent constructor, add static GetAvailableDevices() helper for enumeration
4. **TranscriptionService**: Accept language parameter in constructor, pass to AudioTranscriptionOptions.Language (null for auto-detect)
5. **StartupService (new)**: Static service for HKCU registry Run key management (IsEnabled/Enable/Disable methods)
6. **SettingsForm**: +4 UI controls (modifier checkboxes, microphone dropdown, language dropdown, startup checkbox)

**Integration pattern:** All features follow established flow: AppSettings storage → ConfigurationService persistence → SettingsForm UI → service initialization in TrayApplicationContext. No breaking changes to v1.0 event handlers or recording flow.

### Critical Pitfalls

1. **Modifier key state desynchronization (stuck modifiers)** — Use GetAsyncKeyState inside hook callback to sample current physical key state, never track modifier state yourself in separate flags. Hook chain interference and auto-repeat suppression cause state flags to desync, making Ctrl appear stuck after release. Prevention: Always query hardware state, don't cache.

2. **Microphone device enumeration invalidation** — Store device ProductName (not just index) in settings and re-enumerate devices on StartCapture() to find matching device by name. NAudio device indices shift when USB devices are unplugged, causing silent failure (recording from wrong mic) or BadDeviceId exception. Prevention: Resolve device by name with fallback to default.

3. **WaveInEvent deadlock during device switching** — Stop recording completely before changing DeviceNumber property to avoid WME callback thread deadlock. NAudio's WME backend has known deadlock issues when disposing or modifying WaveInEvent during active recording. Prevention: Call StopCapture() before switching devices, never dispose while recording.

4. **Windows startup registry permission denied** — Use HKEY_CURRENT_USER not HKEY_LOCAL_MACHINE to avoid requiring administrator elevation. Quote executable path to handle spaces. HKLM writes fail silently or throw UnauthorizedAccessException, causing feature to appear enabled but not work on login. Prevention: Always use HKCU\Software\...\Run with quoted path.

5. **Whisper API language hallucination on silence** — Keep explicit language selection as default, avoid auto-detect. Add client-side energy threshold check before sending to API. Whisper hallucinates phrases like "thank you for watching" or "so" on silent/non-speech audio, worse with auto-detect mode. Prevention: Explicit language parameter, silence detection, hallucination filtering.

## Implications for Roadmap

Based on research, suggested phase structure follows feature independence with complexity-aware ordering:

### Phase 1: Hotkey Modifier Support
**Rationale:** Builds incrementally on existing hotkey picker UI and hook infrastructure, lowest integration risk, establishes GetAsyncKeyState pattern for other features.
**Delivers:** Ctrl/Alt/Shift + Key combinations with modifier detection, conflict feedback, UI to display selected modifiers
**Addresses:** Table stakes feature, unblocks power users who need non-conflicting hotkeys
**Avoids:** Modifier desynchronization pitfall by using GetAsyncKeyState sampling strategy from the start
**Research needed:** None (well-documented Win32 API, verified pattern)

### Phase 2: Windows Startup Toggle
**Rationale:** Simplest feature with zero service integration, quick win to build momentum, establishes registry service pattern
**Delivers:** Checkbox in settings writing to HKCU Run key, detect and display current state
**Addresses:** Table stakes feature, enables always-available utility behavior users expect from tray apps
**Avoids:** Permission pitfall by using HKCU not HKLM, path quoting handles spaces
**Research needed:** None (standard Windows pattern, well-documented)

### Phase 3: Language Selection
**Rationale:** Isolated API parameter change with minimal risk, no audio pipeline interaction, establishes language validation pattern
**Delivers:** Dropdown with common languages (pt, en, es, fr, de), auto-detect option, persist selection
**Addresses:** Table stakes feature for multilingual users, improves accuracy over auto-detect for known languages
**Avoids:** Hallucination pitfall by defaulting to explicit language, adds energy threshold check for silence
**Research needed:** Optional (may want to test hallucination filtering during implementation)

### Phase 4: Microphone Selection
**Rationale:** Most complex feature touching audio capture pipeline, highest bug potential, benefits from other features being stable
**Delivers:** Dropdown with device enumeration, persist by ProductName, fallback to default on device removal, permission check dialog
**Addresses:** Table stakes feature for users with multiple mics, enables USB mic preference
**Avoids:** Device invalidation pitfall by storing ProductName and resolving on StartCapture, deadlock pitfall by stopping before switching
**Research needed:** Optional (device hot-swap edge cases may need experimentation)

### Phase Ordering Rationale

- **Dependency-aware:** No strict technical dependencies between features (all extend different services), but ordering follows complexity gradient to maximize learning and minimize integration risk
- **Integration risk ascending:** Modifiers (hook only) → Startup (registry only) → Language (API only) → Microphone (audio pipeline + device management)
- **Quick wins first:** Phases 1-2 are low-hanging fruit establishing patterns; Phase 3 proves API flexibility; Phase 4 tackles complex audio device lifecycle
- **Pitfall prevention baked in:** Each phase addresses its critical pitfall during initial implementation (not retrofitted), preventing technical debt

### Research Flags

**Phases with standard patterns (skip research-phase):**
- **Phase 1 (Hotkey Modifiers):** GetAsyncKeyState is well-documented Win32 API with established usage patterns in keyboard hook contexts
- **Phase 2 (Windows Startup):** Registry Run key approach is standard Windows pattern, exhaustively documented by Microsoft
- **Phase 3 (Language Selection):** Azure OpenAI Whisper language codes are verified in official docs, ISO-639-1 is stable standard

**Phases potentially needing targeted research:**
- **Phase 4 (Microphone Selection):** Device hot-swap behavior and NAudio device state management have edge cases. If testing reveals unexpected behavior with specific USB audio devices (e.g., multi-channel interfaces, virtual audio cables), may need targeted NAudio research. Otherwise standard WaveInEvent patterns are sufficient.

**Overall:** All four phases have HIGH confidence implementations from research. No phase requires pre-implementation research cycle. Any issues discovered during Phase 4 implementation can be researched just-in-time.

## Confidence Assessment

| Area | Confidence | Notes |
|------|------------|-------|
| Stack | HIGH | All features verified against official Microsoft and library documentation. Zero new dependencies required. NAudio 2.2.1 and Azure.AI.OpenAI 2.1.0 capabilities confirmed in API docs and source code. |
| Features | HIGH | All four features are table stakes backed by multiple reference implementations (Windows Settings, Discord, OBS, PowerToys). Clear user expectations established by existing utility apps. |
| Architecture | HIGH | Integration points clearly defined, no architectural changes required. Service extension pattern is established in v1.0 codebase. All modifications are additive (no breaking changes). |
| Pitfalls | HIGH | All critical pitfalls sourced from official Microsoft docs, NAudio GitHub issues, and production bug reports from similar apps. Prevention strategies verified against library source code. |

**Overall confidence:** HIGH

All technical approaches verified through primary sources (Microsoft Learn, NAudio GitHub, Azure OpenAI docs). Research uncovered no unknowns or unresolvable technical blockers. Integration risks are well-understood with documented mitigation strategies.

### Gaps to Address

**Minor gaps requiring validation during implementation:**

- **Microphone device ProductName collision**: Multiple identical USB microphones have same ProductName (e.g., two "USB Audio Device" entries). Current strategy (match by ProductName) may select wrong device. Workaround: Use last known index as secondary hint. Enhancement (defer to v1.2): Use MMDeviceEnumerator.ID for stable device GUID, but requires WASAPI integration. **Action:** Test with duplicate devices during Phase 4, document limitation if unfixable.

- **Hallucination filter tuning**: Energy threshold value (500 in research example) and hallucination phrase list need empirical tuning based on actual usage patterns and user recording environments. Research provides starting values but production data will inform refinement. **Action:** Implement configurable thresholds, gather telemetry if possible, iterate based on user feedback.

- **Hook health monitoring threshold**: 60-second window for detecting "hook stopped receiving events" is arbitrary. Production environment with anti-cheat software may need different threshold or additional heuristics. **Action:** Implement conservative check, add logging, tune based on production issue reports.

**No blocking gaps identified.** All features can proceed to implementation with current research.

## Sources

### Primary (HIGH confidence)

**Microsoft Official Documentation:**
- [Microsoft Learn: GetAsyncKeyState function](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getasynckeystate) — Modifier state detection
- [Microsoft Learn: RegisterHotKey function](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-registerhotkey) — Hotkey modifier flags
- [Microsoft Learn: Run and RunOnce Registry Keys](https://learn.microsoft.com/en-us/windows/win32/setupapi/run-and-runonce-registry-keys) — Windows startup standard
- [Microsoft Learn: Speech to text with Azure OpenAI Whisper](https://learn.microsoft.com/en-us/azure/ai-foundry/openai/whisper-quickstart?view=foundry-classic) — Language parameter usage
- [Microsoft Learn: Enumerating Audio Devices](https://learn.microsoft.com/en-us/windows/win32/coreaudio/enumerating-audio-devices) — MMDeviceEnumerator reference
- [Microsoft Learn: Task Scheduler for developers](https://learn.microsoft.com/en-us/windows/win32/taskschd/task-scheduler-start-page) — Startup alternative approach

**Library Documentation and Source Code:**
- [NAudio GitHub: EnumerateOutputDevices.md](https://github.com/naudio/NAudio/blob/master/Docs/EnumerateOutputDevices.md) — Device enumeration patterns
- [NAudio GitHub: WaveInEvent.cs source](https://github.com/naudio/NAudio/blob/master/NAudio.WinMM/WaveInEvent.cs) — DeviceNumber property behavior
- [NAudio GitHub Issue #1203](https://github.com/naudio/NAudio/issues/1203) — Device switching deadlock confirmation
- [NAudio GitHub Issue #612](https://github.com/naudio/NAudio/issues/612) — ProductName truncation limitation
- [OpenAI GitHub: whisper](https://github.com/openai/whisper) — Language code reference implementation

### Secondary (MEDIUM confidence)

**Community Knowledge and Reference Implementations:**
- [PowerToys Keyboard Manager](https://learn.microsoft.com/en-us/windows/powertoys/keyboard-manager) — Hotkey modifier UI patterns
- [Whisper API Docs: Supported Languages](https://whisper-api.com/docs/languages/) — ISO-639-1 code list
- [Whisper GitHub Discussion #1606](https://github.com/openai/whisper/discussions/1606) — Hallucination on silent audio (reproducible)
- [AutoHotkey Community: Anti-cheat conflicts](https://www.autohotkey.com/boards/viewtopic.php?t=38423) — Hook blocking by security software
- [Jon Egerton: Determining modifier key state when hooking keyboard input](https://jonegerton.com/dotnet/determining-the-state-of-modifier-keys-when-hooking-keyboard-input/) — GetAsyncKeyState vs GetKeyState analysis
- [How-To Geek: Windows startup programs](https://www.howtogeek.com/74523/how-to-disable-startup-programs-in-windows/) — Registry approach confirmation

### Tertiary (LOW confidence)

**Research and Analysis (requires validation):**
- [Calm-Whisper Research Paper (arXiv:2505.12969v1)](https://arxiv.org/html/2505.12969v1) — Hallucination statistics (55.2% of silent audio transcribed as "so"), informs severity but specific percentages may vary by model version
- [Phonexia: Language ID vs Whisper Autodetect](https://docs.phonexia.com/products/speech-platform-4/technologies/language-identification/lid-vs-whisper-autodetect) — Auto-detect latency claims (1-2 seconds), not officially documented by OpenAI

---
**Research completed:** 2026-02-09
**Ready for roadmap:** Yes
