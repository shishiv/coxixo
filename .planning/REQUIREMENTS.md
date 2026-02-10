# Requirements: Coxixo

**Defined:** 2026-02-09
**Core Value:** Frictionless voice input: hold a key, speak, release, paste.

## v1.1 Requirements

Requirements for v1.1 milestone. Each maps to roadmap phases.

### Hotkey

- [ ] **HKEY-01**: User can set a hotkey with Ctrl, Alt, or Shift modifiers (e.g., Ctrl+Shift+F8)
- [ ] **HKEY-02**: User can set multi-modifier combinations (Ctrl+Alt+X, Ctrl+Shift+Y)
- [ ] **HKEY-03**: Hotkey picker displays modifier names clearly (e.g., "Ctrl+Shift+F8")
- [ ] **HKEY-04**: User sees an error if chosen hotkey conflicts with another application
- [ ] **HKEY-05**: Reserved system combinations (Win+X, F12) are blocked with explanation

### Microphone

- [ ] **MIC-01**: User can see all active audio capture devices in a dropdown
- [ ] **MIC-02**: User can select which microphone to use for recording
- [ ] **MIC-03**: Default system device is indicated in the list
- [ ] **MIC-04**: Selected microphone persists across app restarts
- [ ] **MIC-05**: App falls back to default device if selected microphone is unavailable

### Language

- [x] **LANG-01**: User can select transcription language from a dropdown
- [x] **LANG-02**: User can choose "Auto-detect" for automatic language detection
- [x] **LANG-03**: Selected language persists across app restarts
- [x] **LANG-04**: Language selection passes correct ISO 639-1 code to Whisper API

### Startup

- [ ] **START-01**: User can toggle "Start with Windows" checkbox in settings
- [ ] **START-02**: Toggle immediately updates Windows startup registration (HKCU registry)
- [ ] **START-03**: Checkbox reflects current startup state when settings opens

## Future Requirements

Deferred to v1.2+.

### Tray Menu

- **TRAY-01**: User can see recent transcription text in tray context menu

### Enhanced Microphone

- **MIC-06**: User sees live audio level preview when selecting microphone
- **MIC-07**: Device list updates in real-time when devices are plugged/unplugged

### Enhanced Hotkey

- **HKEY-06**: User can use Win key as a modifier
- **HKEY-07**: App suggests alternative hotkeys when conflicts are detected

## Out of Scope

| Feature | Reason |
|---------|--------|
| Hotkey profiles/presets | Adds complexity; single global hotkey is sufficient |
| Voice-activated recording (VAD) | Push-to-talk is explicit and reliable |
| Per-application microphone profiles | Audio routing is OS-level, not app-level |
| Microphone noise reduction | Whisper handles noisy audio well; OS handles enhancement |
| Multi-hotkey language switching | Awkward UX; language is a setting, not per-recording |
| Custom startup arguments UI | Launch minimized by default; no options needed |
| Language auto-switch from clipboard | Complex heuristic, likely confusing |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| HKEY-01 | Phase 5 | Pending |
| HKEY-02 | Phase 5 | Pending |
| HKEY-03 | Phase 5 | Pending |
| HKEY-04 | Phase 5 | Pending |
| HKEY-05 | Phase 5 | Pending |
| MIC-01 | Phase 8 | Pending |
| MIC-02 | Phase 8 | Pending |
| MIC-03 | Phase 8 | Pending |
| MIC-04 | Phase 8 | Pending |
| MIC-05 | Phase 8 | Pending |
| LANG-01 | Phase 7 | Complete |
| LANG-02 | Phase 7 | Complete |
| LANG-03 | Phase 7 | Complete |
| LANG-04 | Phase 7 | Complete |
| START-01 | Phase 6 | Pending |
| START-02 | Phase 6 | Pending |
| START-03 | Phase 6 | Pending |

**Coverage:**
- v1.1 requirements: 17 total
- Mapped to phases: 17 (100%)
- Unmapped: 0

---
*Requirements defined: 2026-02-09*
*Last updated: 2026-02-09 after roadmap creation*
