---
phase: 01-foundation
plan: 02
subsystem: infra
tags: [keyboard-hook, winforms, push-to-talk, pinvoke, user32, WH_KEYBOARD_LL]

# Dependency graph
requires:
  - phase: 01-01
    provides: "TrayApplicationContext with NotifyIcon and context menu"
provides:
  - "Global keyboard hook service (WH_KEYBOARD_LL)"
  - "HotkeyPressed/HotkeyReleased events for push-to-talk"
  - "Recording state tray icon with visual feedback"
  - "Auto-repeat prevention for held keys"
affects: [01-03, 02-whisper, 03-voice-pipeline]

# Tech tracking
tech-stack:
  added: [pinvoke-user32, low-level-keyboard-hook]
  patterns: [push-to-talk, event-driven-state, icon-state-feedback]

key-files:
  created:
    - Coxixo/Services/KeyboardHookService.cs
    - Coxixo/Resources/icon-recording.ico
  modified:
    - Coxixo/TrayApplicationContext.cs
    - Coxixo/Coxixo.csproj

key-decisions:
  - "WH_KEYBOARD_LL for global hook (works when any app has focus)"
  - "Auto-repeat prevention via _isKeyDown state tracking"
  - "Delegate stored in field to prevent GC collection"
  - "Recording icon: 16x16 red filled circle placeholder"

patterns-established:
  - "Push-to-talk pattern: HotkeyPressed starts action, HotkeyReleased ends action"
  - "Icon state feedback: Visual tray icon change reflects app state"
  - "Hook callback minimal work: Quick return to avoid Windows timeout"

# Metrics
duration: ~9min
completed: 2026-01-18
---

# Phase 1 Plan 2: Global Keyboard Hook Summary

**WH_KEYBOARD_LL keyboard hook with HotkeyPressed/HotkeyReleased events for push-to-talk, tray icon state feedback**

## Performance

- **Duration:** ~9 min
- **Started:** 2026-01-18T04:22:15Z
- **Completed:** 2026-01-18T04:31:00Z
- **Tasks:** 2
- **Files created/modified:** 4

## Accomplishments
- KeyboardHookService with WH_KEYBOARD_LL global hook captures both key press and release
- Auto-repeat prevention ensures single event per press/release cycle
- Recording state icon (red circle) provides visual feedback when hotkey held
- TrayApplicationContext wired to change icon on HotkeyPressed/HotkeyReleased

## Task Commits

Each task was committed atomically:

1. **Task 1: Implement KeyboardHookService with low-level hook** - `e5f855a` (feat)
2. **Task 2: Wire hotkey service to tray icon state changes** - `dc343ee` (feat)

## Files Created/Modified
- `Coxixo/Services/KeyboardHookService.cs` - WH_KEYBOARD_LL hook with HotkeyPressed/HotkeyReleased events
- `Coxixo/Resources/icon-recording.ico` - 16x16 red circle recording state icon (1150 bytes)
- `Coxixo/TrayApplicationContext.cs` - Event handlers for icon state changes
- `Coxixo/Coxixo.csproj` - Added icon-recording.ico as embedded resource

## Decisions Made
- **WH_KEYBOARD_LL (13):** Low-level hook captures both WM_KEYDOWN and WM_KEYUP globally
- **Auto-repeat prevention:** Track `_isKeyDown` state to filter repeated keydown events while holding
- **Delegate field storage:** `_proc` stored as field to prevent garbage collection during hook lifetime
- **System key handling:** Handle both WM_SYSKEYDOWN/WM_SYSKEYUP for keys combined with Alt
- **Default hotkey F8:** Configurable via TargetKey property (Plan 01-03 uses configurable settings)

## Deviations from Plan

None - plan executed exactly as written.

Note: Plan 01-03 was executed concurrently and added ConfigurationService/AppSettings integration to TrayApplicationContext. These changes enhanced the implementation by making the hotkey configurable but did not conflict with this plan's requirements.

## Issues Encountered

None - build and integration succeeded on first attempt.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness
- Keyboard hook ready for voice pipeline integration (Phase 2-3)
- HotkeyPressed will trigger audio capture start
- HotkeyReleased will trigger audio capture stop and transcription
- Recording icon provides user feedback during capture
- Configurable hotkey (via Plan 01-03) allows user customization

---
*Phase: 01-foundation*
*Completed: 2026-01-18*
