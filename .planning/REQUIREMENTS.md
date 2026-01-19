# Requirements: Coxixo

**Defined:** 2026-01-17
**Core Value:** Frictionless voice input: hold a key, speak, release, paste.

## v1 Requirements

Requirements for initial release. Each maps to roadmap phases.

### Core Experience

- [x] **CORE-01**: User can hold a global hotkey to record audio (push-to-talk) ✓
- [x] **CORE-02**: User can release the hotkey to trigger transcription ✓
- [x] **CORE-03**: App runs as system tray icon with minimal footprint ✓
- [x] **CORE-04**: Tray icon visually indicates recording state (see UI-02 for design) ✓
- [x] **CORE-05**: Transcription result is automatically copied to clipboard ✓
- [x] **CORE-06**: User hears audio feedback when recording starts/stops (beep) ✓
- [x] **CORE-07**: User receives error feedback if transcription fails (notification) ✓

### Integration

- [x] **INTG-01**: Audio is captured from default system microphone ✓
- [x] **INTG-02**: Audio is sent to Azure OpenAI Whisper API for transcription ✓
- [x] **INTG-03**: API credentials (endpoint, key) are securely stored ✓

### Configuration

- [x] **CONF-01**: User can customize the push-to-talk hotkey ✓
- [x] **CONF-02**: User can configure Azure API credentials (endpoint, key) ✓
- [x] **CONF-03**: Settings persist across app restarts ✓

### Visual Identity (Brand Guide)

- [x] **UI-01**: Tray icon uses sound bar design (3 bars forming "C" shape) ✓
- [x] **UI-02**: Tray icon is white/gray when idle, red with pulsing dot when recording ✓
- [x] **UI-03**: Settings window follows dark theme (bg #1E1E1E, surface #252526) ✓
- [x] **UI-04**: Settings window shows API connection status with latency indicator ✓
- [x] **UI-05**: Color palette: Azure Blue #0078D4, Accent Green #00CC6A ✓
- [x] **UI-06**: Typography: Segoe UI (system font) ✓

## v2 Requirements

Deferred to future release. Tracked but not in current roadmap.

### Configuration

- **CONF-04**: User can select which microphone to use
- **CONF-05**: User can enable/disable startup with Windows
- **CONF-06**: User can select transcription language (PT, EN, auto-detect)

### Polish

- **POLH-01**: App shows minimal overlay during recording (optional)
- **POLH-02**: User can see recent transcription in tray menu

## Out of Scope

Explicitly excluded. Documented to prevent scope creep.

| Feature | Reason |
|---------|--------|
| Meeting transcription | Different use case (Otter.ai territory) |
| File upload/batch transcription | Stay realtime-only |
| Text editing/refinement | Output raw transcription, no AI rewrites |
| Voice commands | Just transcribe text, no parsing |
| Transcription history/storage | Clipboard-only = no privacy concerns |
| Account/login system | Local config only |
| Multiple AI providers | Azure OpenAI Whisper only |
| Local/offline Whisper | Architectural complexity, stay cloud-only |
| Auto-paste to active window | Clipboard simpler and more predictable |

## Traceability

Which phases cover which requirements. Updated during roadmap creation.

| Requirement | Phase | Status |
|-------------|-------|--------|
| CORE-01 | Phase 1 | Complete ✓ |
| CORE-02 | Phase 3 | Complete ✓ |
| CORE-03 | Phase 1 | Complete ✓ |
| CORE-04 | Phase 1 | Complete ✓ |
| CORE-05 | Phase 3 | Complete ✓ |
| CORE-06 | Phase 2 | Complete ✓ |
| CORE-07 | Phase 3 | Complete ✓ |
| INTG-01 | Phase 2 | Complete ✓ |
| INTG-02 | Phase 3 | Complete ✓ |
| INTG-03 | Phase 1 | Complete ✓ |
| CONF-01 | Phase 4 | Complete ✓ |
| CONF-02 | Phase 3 | Complete ✓ |
| CONF-03 | Phase 1 | Complete ✓ |
| UI-01 | Phase 4 | Complete ✓ |
| UI-02 | Phase 4 | Complete ✓ |
| UI-03 | Phase 4 | Complete ✓ |
| UI-04 | Phase 4 | Complete ✓ |
| UI-05 | Phase 4 | Complete ✓ |
| UI-06 | Phase 4 | Complete ✓ |

**Coverage:**
- v1 requirements: 19 total
- Mapped to phases: 19
- Unmapped: 0

---
*Requirements defined: 2026-01-17*
*Last updated: 2026-01-18 after Phase 4 completion - All v1 requirements complete*
