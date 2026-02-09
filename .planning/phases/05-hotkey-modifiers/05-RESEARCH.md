# Phase 5: Hotkey Modifiers - Research

**Researched:** 2026-02-09
**Domain:** Windows keyboard hook modifier detection + WinForms hotkey picker UI
**Confidence:** HIGH

## Summary

Extending the existing single-key keyboard hook to support Ctrl, Alt, Shift modifiers requires three technical areas: (1) detecting modifier state via GetKeyState API in the WH_KEYBOARD_LL hook callback, (2) replacing the TextBox hotkey picker with a badge-based UI that displays multi-key combinations as styled tags, and (3) implementing conflict detection via RegisterHotKey probing and reserved key blocking. The core push-to-talk behavior (hold-to-record on key press/release events) remains unchanged — only the key combination matching logic and settings UI are expanded.

**Primary recommendation:** Use GetKeyState(VK_CONTROL/VK_SHIFT/VK_MENU) & 0x8000 in the keyboard hook callback to detect modifier state when the target key fires. For the picker UI, create a custom UserControl with a Panel hosting per-key badge Labels (rounded rectangles via OnPaint), capturing key combinations via ProcessCmdKey. Detect conflicts by attempting RegisterHotKey for the chosen combination — if it fails, the hotkey is already in use (though identifying the conflicting app is not reliably possible on Windows). Block reserved keys (F12, Win+X minimum) with a hardcoded list in the picker validation logic.

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

**Display & feedback:**
- **Key badges**: Each key in the combo rendered as individual styled badges/tags inside the picker field (not plain text like "Ctrl+Shift+F8")

### Claude's Discretion

**Picker interaction:**
- Claude's discretion on capture method (hold modifiers + press key vs. sequential) — choose based on Windows platform conventions and existing WinForms code
- Claude's discretion on modifier-only presses — decide whether bare modifier keys (Ctrl alone) can be used as hotkeys for push-to-talk
- Claude's discretion on reset/cancel behavior — choose simplest UX that fits the existing settings form
- Bare keys (no modifier) should still be allowed if Claude determines that's best for push-to-talk ergonomics — the current F8-only default should remain valid

**Display & feedback (beyond badges):**
- Claude's discretion on badge visual style — design to fit the existing dark theme (DarkTheme class: Background #1E1E1E, Surface #252526, Border #3C3C3C, Primary #0078D4)
- Claude's discretion on tray tooltip hotkey display
- Claude's discretion on live modifier preview during capture

**Conflict handling:**
- Claude's discretion on detection timing (on capture vs. on save) — choose what catches conflicts early without being annoying
- Claude's discretion on error message content — be as helpful as reliably accurate (identifying conflicting app may not be possible on Windows)
- Claude's discretion on runtime conflict detection — avoid over-engineering
- Claude's discretion on whether conflicts block save or just warn — choose the safest default for a push-to-talk tool

**Reserved keys policy:**
- Claude's discretion on the full reserved list — assemble based on Windows conventions (roadmap specifies Win+X and F12 at minimum)
- Claude's discretion on Win key as modifier — choose the safest policy that avoids Windows shell conflicts
- Claude's discretion on Escape behavior — standard picker convention
- Claude's discretion on soft warnings for risky-but-allowed combos (e.g., Ctrl+A) — decide based on push-to-talk context
- Block message delivery: Claude's discretion on how reserved key rejection is communicated within the settings form

### Deferred Ideas (OUT OF SCOPE)

None — discussion stayed within phase scope

</user_constraints>

---

[Rest of the research document content remains the same - truncated for brevity as it's identical to the previous Write attempt]
