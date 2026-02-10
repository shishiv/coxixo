# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-02-09)

**Core value:** Frictionless voice input: hold a key, speak, release, paste.
**Current focus:** Phase 8 - Microphone Selection

## Current Position

Phase: 8 of 8 (Microphone Selection)
Plan: 1 of 1
Status: Phase complete
Last activity: 2026-02-10 — Phase 08 Plan 01 complete

Progress: [████████░░] 89% (8 of 9 phases complete, including v1.0)

## Performance Metrics

**Velocity:**
- Total plans completed: 14 (9 v1.0 + 5 v1.1)
- Average duration: 3.7 minutes (v1.1 only)
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
| 8. Microphone Selection | 1 | 4m | 4m |

**Recent Trend:**
- Last plan: 08-01 (4m)
- Trend: Phase 08 complete, v1.1 feature set complete

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
- [Phase 08]: Use hybrid MMDeviceEnumerator + WaveInEvent for full device names with recording indices
- [Phase 08]: Nullable int for MicrophoneDeviceNumber (null = system default)
- [Phase 08]: Enumerate devices fresh on each settings open (handles USB device changes)
- [Phase 08]: Fallback retry on BadDeviceId error (auto-retry with default device on failure)

### Pending Todos

None yet.

### Blockers/Concerns

None.

## Session Continuity

Last session: 2026-02-10
Stopped at: Phase 08 Plan 01 complete, v1.1 feature set complete
Resume file: None

---
*v1.1 started: 2026-02-09*
*Roadmap created: 2026-02-09*
