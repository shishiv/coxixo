# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-01-17)

**Core value:** Frictionless voice input: hold a key, speak, release, paste.
**Current focus:** Phase 2 - Audio Pipeline

## Current Position

Phase: 2 of 4 (Audio Pipeline)
Plan: 2 of 2 in current phase
Status: Phase 02 complete, ready for Phase 03
Last activity: 2026-01-18 - Completed 02-02-PLAN.md (audio feedback beeps)

Progress: [#####.....] ~42% (5 of ~12 total plans)

## Performance Metrics

**Velocity:**
- Total plans completed: 5
- Average duration: ~6 min
- Total execution time: ~30 min

**By Phase:**

| Phase | Plans | Total | Avg/Plan |
|-------|-------|-------|----------|
| 01-foundation | 3 | ~17 min | ~6 min |
| 02-audio-pipeline | 2 | ~13 min | ~6.5 min |

**Recent Trend:**
- Last 5 plans: 01-02 (~6 min), 01-03 (~6 min), 02-01 (~8 min), 02-02 (~5 min)
- Trend: stable

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

### Pending Todos

None yet.

### Blockers/Concerns

None yet.

## Session Continuity

Last session: 2026-01-18
Stopped at: Completed 02-02-PLAN.md (Phase 02 complete), ready for Phase 03 (Whisper API)
Resume file: None
