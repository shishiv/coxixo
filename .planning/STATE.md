# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-01-17)

**Core value:** Frictionless voice input: hold a key, speak, release, paste.
**Current focus:** Phase 2 - Audio Pipeline

## Current Position

Phase: 2 of 4 (Audio Pipeline)
Plan: 1 of 2 in current phase
Status: Plan 02-01 complete, ready for 02-02
Last activity: 2026-01-18 - Completed 02-01-PLAN.md (NAudio microphone capture)

Progress: [####......] ~33% (4 of ~12 total plans)

## Performance Metrics

**Velocity:**
- Total plans completed: 4
- Average duration: ~6.25 min
- Total execution time: ~25 min

**By Phase:**

| Phase | Plans | Total | Avg/Plan |
|-------|-------|-------|----------|
| 01-foundation | 3 | ~17 min | ~6 min |
| 02-audio-pipeline | 1 | ~8 min | ~8 min |

**Recent Trend:**
- Last 5 plans: 01-01 (~5 min), 01-02 (~6 min), 01-03 (~6 min), 02-01 (~8 min)
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

### Pending Todos

None yet.

### Blockers/Concerns

None yet.

## Session Continuity

Last session: 2026-01-18
Stopped at: Completed 02-01-PLAN.md, ready for 02-02-PLAN.md (audio feedback)
Resume file: None
