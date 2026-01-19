# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-01-17)

**Core value:** Frictionless voice input: hold a key, speak, release, paste.
**Current focus:** Phase 4 - Polish

## Current Position

Phase: 4 of 4 (Polish)
Plan: 2 of 2 in current phase
Status: Phase complete
Last activity: 2026-01-19 - Completed 04-02-PLAN.md (Settings UI)

Progress: [##########] 100% (9 of 9 total plans)

## Performance Metrics

**Velocity:**
- Total plans completed: 9
- Average duration: ~5 min
- Total execution time: ~47 min

**By Phase:**

| Phase | Plans | Total | Avg/Plan |
|-------|-------|-------|----------|
| 01-foundation | 3 | ~17 min | ~6 min |
| 02-audio-pipeline | 2 | ~13 min | ~6.5 min |
| 03-transcription-loop | 2 | ~7 min | ~3.5 min |
| 04-polish | 2 | ~10 min | ~5 min |

**Recent Trend:**
- Last 5 plans: 02-02 (~5 min), 03-01 (~3 min), 03-02 (~4 min), 04-01 (~5 min), 04-02 (~5 min)
- Trend: fast (simple wiring tasks)

*Updated after each plan completion*

## Accumulated Context

### Decisions

Decisions are logged in PROJECT.md Key Decisions table.
Recent decisions affecting current work:

| Decision | Phase | Rationale |
|----------|-------|-----------|
| ApplicationContext pattern | 01-01 | Formless tray app without hidden window |
| Global mutex naming | 01-01 | `Global\CoxixoSingleInstance` for system-wide single instance |
| Embedded icon resources | 01-01 | Icons loaded via Assembly.GetManifestResourceStream |
| WH_KEYBOARD_LL hook | 01-02 | Low-level hook captures both keydown and keyup globally |
| Auto-repeat prevention | 01-02 | Track `_isKeyDown` state to filter repeated events |
| Delegate field storage | 01-02 | Prevent GC collection of callback during hook lifetime |
| LocalApplicationData folder | 01-03 | Settings stored in %LOCALAPPDATA%\Coxixo\ (not Roaming) |
| DPAPI with custom entropy | 01-03 | Additional protection for credential encryption |
| Static service pattern | 01-03 | ConfigurationService and CredentialService are static classes |
| WaveInEvent over WaveIn | 02-01 | Better for background apps - handles own message loop |
| 16kHz mono WAV format | 02-01 | Whisper API optimal format - reduces bandwidth |
| 500ms minimum duration | 02-01 | Filters accidental taps without noticeable delay |
| BalloonTip for errors | 02-01 | No extra dependencies needed for basic notifications |
| Programmatic WAV generation | 02-02 | Precise frequency sweeps without external audio files |
| Ascending/descending chirps | 02-02 | Classic walkie-talkie style: 800->1200 Hz start, 1200->800 Hz stop |
| Silent discard for short recordings | 02-02 | No stop beep on discarded recordings keeps UX non-intrusive |
| Instance service pattern | 03-01 | TranscriptionService holds SDK clients needing disposal |
| Portuguese language hardcoded | 03-01 | Primary use case per PROJECT.md is Portuguese dictation |
| Retry transient errors only | 03-01 | 5xx/429/408 are transient; 4xx won't resolve on retry |
| Lazy TranscriptionService init | 03-02 | Only created when credentials and endpoint are valid |
| Status-specific error messages | 03-02 | Map HTTP codes to user-friendly messages (401, 403, 404, 429, 5xx) |
| Clipboard.SetText on UI thread | 03-02 | Async continuation runs on UI SynchronizationContext |
| Focus-based hotkey capture | 04-02 | Click field to enter capture mode, press key to set |
| ProcessCmdKey override | 04-02 | Capture Tab, F-keys, and other command keys for hotkey |
| Keyboard hook pause during settings | 04-02 | Prevents triggering while typing in settings |

### Pending Todos

None - project complete.

### Blockers/Concerns

None.

## Session Continuity

Last session: 2026-01-19
Stopped at: Completed 04-02-PLAN.md (Settings UI) - Project complete
Resume file: None
