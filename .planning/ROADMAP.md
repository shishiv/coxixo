# Roadmap: Coxixo

## Milestones

- ✅ **v1.0 MVP** - Phases 1-4 (shipped 2026-01-19)
- 🚧 **v1.1 Configuration & Flexibility** - Phases 5-8 (in progress)

## Phases

<details>
<summary>✅ v1.0 MVP (Phases 1-4) - SHIPPED 2026-01-19</summary>

### Phase 1: Foundation
**Goal**: Establish project structure and core services
**Plans**: 3 plans

Plans:
- [x] 01-01: Project scaffolding and tray application
- [x] 01-02: Configuration service and settings persistence
- [x] 01-03: Global keyboard hook implementation

### Phase 2: Audio Capture
**Goal**: Record audio from microphone with push-to-talk
**Plans**: 2 plans

Plans:
- [x] 02-01: NAudio integration and WAV recording
- [x] 02-02: Push-to-talk state machine

### Phase 3: Transcription
**Goal**: Integrate Azure Whisper API for speech-to-text
**Plans**: 2 plans

Plans:
- [x] 03-01: Azure OpenAI Whisper integration
- [x] 03-02: Clipboard automation and error handling

### Phase 4: Polish
**Goal**: Complete brand identity and user feedback systems
**Plans**: 2 plans

Plans:
- [x] 04-01: Brand-compliant tray icons and visual identity
- [x] 04-02: Audio feedback and settings UI refinement

</details>

### 🚧 v1.1 Configuration & Flexibility (In Progress)

**Milestone Goal:** Expand hotkey support, add device and language selection, and enable auto-start — making Coxixo configurable for different workflows.

#### Phase 5: Hotkey Modifiers
**Goal**: Users can set hotkeys with Ctrl, Alt, or Shift modifiers
**Depends on**: Phase 4 (extends existing hotkey picker)
**Requirements**: HKEY-01, HKEY-02, HKEY-03, HKEY-04, HKEY-05
**Success Criteria** (what must be TRUE):
  1. User can set single-modifier combinations (Ctrl+F8, Alt+X, Shift+Home)
  2. User can set multi-modifier combinations (Ctrl+Alt+X, Ctrl+Shift+Y)
  3. Hotkey picker displays full combination with modifier names (e.g., "Ctrl+Shift+F8")
  4. User sees clear error message when chosen hotkey conflicts with another application
  5. System combinations (Win+X, F12) are blocked with explanation of why they're reserved
**Plans**: 2 plans

Plans:
- [x] 05-01: HotkeyCombo model, modifier-aware keyboard hook, and hotkey validator
- [x] 05-02: Badge-based hotkey picker control, settings form integration, conflict detection

#### Phase 6: Windows Startup
**Goal**: Users can configure Coxixo to launch automatically with Windows
**Depends on**: Phase 5
**Requirements**: START-01, START-02, START-03
**Success Criteria** (what must be TRUE):
  1. User can toggle "Start with Windows" checkbox in settings
  2. Checkbox state immediately updates Windows startup registry (HKCU Run key)
  3. Checkbox accurately reflects current startup registration when settings window opens
**Plans**: 1 plan

Plans:
- [x] 06-01: StartupService, AppSettings extension, and SettingsForm checkbox with immediate registry toggle

#### Phase 7: Language Selection
**Goal**: Users can choose transcription language or enable auto-detection
**Depends on**: Phase 6
**Requirements**: LANG-01, LANG-02, LANG-03, LANG-04
**Success Criteria** (what must be TRUE):
  1. User can select transcription language from a dropdown (pt, en, es, fr, de)
  2. User can choose "Auto-detect" option for automatic language detection
  3. Selected language persists across app restarts
  4. Whisper API receives correct ISO 639-1 language code for selected language
**Plans**: 1 plan

Plans:
- [x] 07-01: LanguageCode model, TranscriptionService wiring, and SettingsForm ComboBox with persistence

#### Phase 8: Microphone Selection
**Goal**: Users can choose which audio input device to use for recording
**Depends on**: Phase 7
**Requirements**: MIC-01, MIC-02, MIC-03, MIC-04, MIC-05
**Success Criteria** (what must be TRUE):
  1. User can see all active audio capture devices listed in a dropdown with friendly names
  2. User can select which microphone to use for recording
  3. System default microphone is clearly indicated in the device list
  4. Selected microphone persists across app restarts and is used on next recording
  5. App gracefully falls back to default device if selected microphone becomes unavailable
**Plans**: 1 plan

Plans:
- [ ] 08-01-PLAN.md -- MicrophoneDeviceNumber model, hybrid device enumeration, ComboBox UI, AudioCaptureService wiring with fallback

## Progress

**Execution Order:**
Phases execute in numeric order: 5 → 6 → 7 → 8

| Phase | Milestone | Plans Complete | Status | Completed |
|-------|-----------|----------------|--------|-----------|
| 1. Foundation | v1.0 | 3/3 | Complete | 2026-01-17 |
| 2. Audio Capture | v1.0 | 2/2 | Complete | 2026-01-17 |
| 3. Transcription | v1.0 | 2/2 | Complete | 2026-01-18 |
| 4. Polish | v1.0 | 2/2 | Complete | 2026-01-19 |
| 5. Hotkey Modifiers | v1.1 | 2/2 | Complete | 2026-02-09 |
| 6. Windows Startup | v1.1 | 1/1 | Complete | 2026-02-10 |
| 7. Language Selection | v1.1 | 1/1 | Complete | 2026-02-10 |
| 8. Microphone Selection | v1.1 | 0/1 | Not started | - |
