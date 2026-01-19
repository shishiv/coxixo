# Roadmap: Coxixo

## Overview

Coxixo delivers frictionless voice-to-clipboard transcription in four phases. We start with the application shell and hotkey detection, add audio capture, connect the Azure transcription loop, then polish with visual identity and settings. By Phase 3 completion, the core value ("hold, speak, release, paste") is fully functional.

## Phases

**Phase Numbering:**
- Integer phases (1, 2, 3, 4): Planned milestone work
- Decimal phases (e.g., 2.1): Urgent insertions (marked INSERTED)

- [x] **Phase 1: Foundation** - System tray app with global hotkey detection ✓
- [x] **Phase 2: Audio Pipeline** - Microphone capture to memory buffer ✓
- [x] **Phase 3: Transcription Loop** - Azure API integration and clipboard output ✓
- [x] **Phase 4: Polish** - Visual identity, settings UI, and feedback refinement ✓

## Phase Details

### Phase 1: Foundation
**Goal**: Establish the application shell with system tray presence and working push-to-talk hotkey detection
**Depends on**: Nothing (first phase)
**Requirements**: CORE-01, CORE-03, CORE-04, INTG-03, CONF-03
**Success Criteria** (what must be TRUE):
  1. App runs in system tray with icon visible
  2. App remains running with minimal memory footprint when idle
  3. User can hold a key and the tray icon changes to indicate "recording" state
  4. User can release the key and the icon returns to idle state
  5. App correctly disposes tray icon on exit (no ghost icons)
**Plans**: 3 plans

Plans:
- [x] 01-01-PLAN.md — Create .NET 8 WinForms project with system tray shell using ApplicationContext pattern ✓
- [x] 01-02-PLAN.md — Implement global hotkey detection with low-level keyboard hook for push-to-talk ✓
- [x] 01-03-PLAN.md — Add configuration persistence (JSON settings) and secure credential storage (DPAPI) ✓

### Phase 2: Audio Pipeline
**Goal**: Capture audio from default microphone into a memory buffer during push-to-talk
**Depends on**: Phase 1
**Requirements**: INTG-01, CORE-06
**Success Criteria** (what must be TRUE):
  1. Audio is captured from default system microphone when hotkey is held
  2. Audio is encoded in format suitable for Whisper API (16kHz mono WAV)
  3. User hears beep when recording starts and stops
  4. App handles microphone permission denied gracefully
**Plans**: 2 plans

Plans:
- [x] 02-01-PLAN.md — NAudio microphone capture with 16kHz mono WAV encoding and minimum duration threshold ✓
- [x] 02-02-PLAN.md — Audio feedback beeps (walkie-talkie chirps) for recording start/stop ✓

### Phase 3: Transcription Loop
**Goal**: Complete the core value loop - send audio to Azure, receive transcription, copy to clipboard
**Depends on**: Phase 2
**Requirements**: CORE-02, CORE-05, CORE-07, INTG-02, CONF-02
**Success Criteria** (what must be TRUE):
  1. User releases hotkey and audio is sent to Azure OpenAI Whisper API
  2. Transcription result is automatically copied to clipboard
  3. User can paste transcribed text into any application
  4. User receives notification if transcription fails (API error, timeout, etc.)
  5. API credentials are configurable and securely stored
**Plans**: 2 plans

Plans:
- [x] 03-01-PLAN.md — Azure Whisper API client with TranscriptionService and retry logic ✓
- [x] 03-02-PLAN.md — Wire transcription into hotkey flow with clipboard output and error handling ✓

### Phase 4: Polish
**Goal**: Apply brand visual identity, build settings UI, refine user experience
**Depends on**: Phase 3
**Requirements**: CONF-01, UI-01, UI-02, UI-03, UI-04, UI-05, UI-06
**Success Criteria** (what must be TRUE):
  1. Tray icon uses sound bar design (3 bars forming "C" shape) per brand guide
  2. Tray icon is white/gray when idle, red with pulsing dot when recording
  3. Settings window allows hotkey customization
  4. Settings window shows API connection status with latency indicator
  5. UI follows dark theme with specified color palette (bg #1E1E1E, Azure Blue #0078D4)
**Plans**: 2 plans

Plans:
- [x] 04-01-PLAN.md — Brand visual identity (tray icons with animation) ✓
- [x] 04-02-PLAN.md — Settings UI with dark theme and hotkey customization ✓

## Progress

**Execution Order:**
Phases execute in numeric order: 1 -> 2 -> 3 -> 4

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 1. Foundation | 3/3 | Complete ✓ | 2026-01-18 |
| 2. Audio Pipeline | 2/2 | Complete ✓ | 2026-01-18 |
| 3. Transcription Loop | 2/2 | Complete ✓ | 2026-01-18 |
| 4. Polish | 2/2 | Complete ✓ | 2026-01-18 |

---
*Roadmap created: 2026-01-17*
*Depth: Quick (3-5 phases)*
*Mode: YOLO*
