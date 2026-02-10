# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-02-09)

**Core value:** Frictionless voice input: hold a key, speak, release, paste.
**Current focus:** Phase 7 - Language Selection

## Current Position

Phase: 7 of 8 (Language Selection)
Plan: 1 of 1
Status: Phase complete
Last activity: 2026-02-10 — Phase 07 Plan 01 complete

Progress: [███████░░░] 78% (7 of 9 phases complete, including v1.0)

## Performance Metrics

**Velocity:**
- Total plans completed: 13 (9 v1.0 + 4 v1.1)
- Average duration: 3.6 minutes (v1.1 only)
- Total execution time: ~2 days (2026-01-17 → 2026-02-10)

**By Phase:**

| Phase | Plans | Total | Avg/Plan |
|-------|-------|-------|----------|
| 1. Foundation | 3 | - | - |
| 2. Audio Capture | 2 | - | - |
| 3. Transcription | 2 | - | - |
| 4. Polish | 2 | - | - |
| 5. Hotkey Modifiers | 2 | 8m | 4m |
| 6. Windows Startup | 1 | 2.5m | 2.5m |
| 7. Language Selection | 1 | 3m | 3m |

**Recent Trend:**
- Last plan: 07-01 (3m)
- Trend: Phase 07 complete, language selection implemented

*Updated after each plan completion*

## Accumulated Context

### Decisions

Decisions are logged in PROJECT.md Key Decisions table.
Recent decisions affecting current work:

- v1.0: ApplicationContext pattern chosen for formless tray app
- v1.0: WH_KEYBOARD_LL hook for global hotkey capture
- v1.0: DPAPI with custom entropy for credential storage
- v1.1: Research completed, build order established (Hotkey → Startup → Language → Microphone)
- 05-01: Use GetKeyState (not GetAsyncKeyState) for modifier detection in hook callback
- 05-01: Fire HotkeyReleased when modifier released during hold (push-to-talk ergonomics)
- 05-01: Implement value equality for HotkeyCombo for collection comparisons
- 05-01: Keep TargetKey property for backward compatibility during transition
- [Phase 05]: Use owner-drawn UserControl with GDI+ for badge rendering (avoids FlowLayoutPanel flicker)
- [Phase 05]: Skip RegisterHotKey probe for bare keys (low-level hooks don't conflict)
- [Phase 06]: Use HKCU Run registry key for Windows startup (standard approach, no admin required)
- [Phase 06]: Immediate registry write on checkbox toggle, not deferred to Save (instant feedback)
- [Phase 06]: Read registry state on form load as source of truth (prevents drift)
- [Phase 06]: Use _isLoading guard to prevent re-entrant CheckedChanged during LoadSettings
- [Phase 07]: Use nullable string for LanguageCode (null = auto-detect)
- [Phase 07]: Set DisplayMember/ValueMember before DataSource to prevent spurious events
- [Phase 07]: Use SelectedIndex=0 for null values (WinForms doesn't support SelectedValue=null)
- [Phase 07]: Language saved on Save button click (not immediate), matching existing pattern

### Pending Todos

None yet.

### Blockers/Concerns

None.

## Session Continuity

Last session: 2026-02-10
Stopped at: Phase 07 Plan 01 complete, ready for Phase 08 (Microphone Selection)
Resume file: None

---
*v1.1 started: 2026-02-09*
*Roadmap created: 2026-02-09*
