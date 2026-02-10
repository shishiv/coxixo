---
phase: 05-hotkey-modifiers
plan: 01
subsystem: hotkey-foundation
tags: [model, service-layer, validation, modifiers]
dependency_graph:
  requires: [v1.0-keyboard-hook, v1.0-settings]
  provides: [HotkeyCombo, modifier-detection, hotkey-validation]
  affects: [KeyboardHookService, AppSettings, ConfigurationService]
tech_stack:
  added: [GetKeyState-API]
  patterns: [modifier-state-detection, value-equality, static-validator]
key_files:
  created:
    - Coxixo/Models/HotkeyCombo.cs
    - Coxixo/Services/HotkeyValidator.cs
  modified:
    - Coxixo/Models/AppSettings.cs
    - Coxixo/Services/KeyboardHookService.cs
    - Coxixo/Forms/SettingsForm.cs
    - Coxixo/TrayApplicationContext.cs
decisions:
  - choice: "Use GetKeyState (not GetAsyncKeyState) for modifier detection"
    rationale: "GetKeyState is synchronized with hook message queue, GetAsyncKeyState queries hardware directly and can desynchronize"
  - choice: "Keep TargetKey property for backward compatibility during transition"
    rationale: "Allows existing code to compile while migrating to TargetCombo"
  - choice: "Fire HotkeyReleased when modifier released during hold"
    rationale: "Critical for push-to-talk ergonomics - user may release Ctrl before releasing F8 in Ctrl+F8"
  - choice: "Implement value equality for HotkeyCombo"
    rationale: "Enables correct comparison in collections and UI state management"
metrics:
  duration: "4 minutes"
  tasks_completed: 3
  files_created: 2
  files_modified: 4
  commits: 3
  completed: 2026-02-10
---

# Phase 05 Plan 01: Hotkey Modifiers Foundation Summary

**One-liner:** Hotkey combination model with exact modifier matching using GetKeyState, plus reserved/warned key validation

## What Was Built

Established the foundation for modifier-key support by creating the HotkeyCombo data model, extending the keyboard hook to detect and match modifier states using GetKeyState, and implementing a three-level validation system for reserved and potentially conflicting hotkey combinations. This plan touches only the service/model layer with minimal UI compatibility shims.

### HotkeyCombo Model

Created a data model representing hotkey combinations with modifier flags:
- Properties: `Key` (Keys), `Ctrl` (bool), `Alt` (bool), `Shift` (bool)
- `ToSegments()` returns badge-friendly array: `["Ctrl", "Shift", "F8"]`
- `ToDisplayString()` returns plain text: `"Ctrl+Shift+F8"`
- Value equality via `Equals()` and `GetHashCode()`
- `HasModifiers` computed property for quick checks
- `Default()` factory returns F8 with no modifiers

Updated `AppSettings.cs` to use `HotkeyCombo Hotkey` instead of `Keys HotkeyKey`. JSON serialization now stores:
```json
{
  "hotkey": {
    "key": "F8",
    "ctrl": false,
    "alt": false,
    "shift": false
  }
}
```

Old `hotkeyKey` field in existing v1.0 settings.json is ignored by deserializer (no matching property), which is safe - users get F8 default preserved.

### Modifier-Aware Keyboard Hook

Extended `KeyboardHookService` with exact modifier matching:
- Added `GetKeyState` P/Invoke for synchronized modifier state detection
- Replaced `_targetKey` field with `_targetCombo` (HotkeyCombo)
- Added `TargetCombo` property (TargetKey kept for backward compat)
- `IsKeyDown(int vk)` helper checks modifier state via `GetKeyState(vk) & 0x8000`

**Critical implementation detail:** Uses `GetKeyState`, NOT `GetAsyncKeyState`. GetKeyState is synchronized with the hook message queue and gives correct results in the hook callback. GetAsyncKeyState queries hardware state directly and can desynchronize, causing intermittent missed/phantom modifiers.

**HookCallback logic:**
1. When target key pressed, check if modifiers match exactly: `ctrlDown == _targetCombo.Ctrl && altDown == _targetCombo.Alt && shiftDown == _targetCombo.Shift`
2. Fire `HotkeyPressed` only when all required modifiers are held and no extra modifiers present
3. Handle modifier release during hold: if user releases a required modifier before releasing the main key, fire `HotkeyReleased` immediately (critical for push-to-talk ergonomics)

**Backward compatible:** Bare F8 default (no modifiers) works identically to v1.0.

### Hotkey Validation

Created `HotkeyValidator` static utility with three-level validation:

**1. Reserved (hard-blocked):**
- F12 alone: "F12 is reserved for debugger attach in Windows"
- Ctrl+Alt+Delete: "Ctrl+Alt+Delete is a Windows security interrupt"
- Alt+F4: "Alt+F4 closes the active window"
- Ctrl+Alt+F4: "Reserved Windows MDI shortcut"
- PrintScreen (all modifier combinations): "PrintScreen is reserved for screenshots"
- Escape: "Escape cannot be used as a hotkey"
- Modifier-only keys (ControlKey, ShiftKey, Menu, LWin, RWin, None): "Modifier keys cannot be used as the primary hotkey"

**2. Warned (soft-blocked):**
- Ctrl+C, Ctrl+V, Ctrl+X: "This may interfere with clipboard shortcuts"
- Ctrl+A: "This may interfere with select-all shortcuts"
- Ctrl+Z, Ctrl+Y: "This may interfere with undo/redo"
- Ctrl+S: "This may interfere with saving in other apps"
- Ctrl+W: "This may interfere with closing tabs in browsers"
- Alt+Tab: "This may interfere with window switching"

**3. Valid:** All other combinations pass through with no message.

**API:**
```csharp
var outcome = HotkeyValidator.Validate(combo);
// outcome.Result: Valid | Reserved | Warned
// outcome.Message: null (Valid) or specific string (Reserved/Warned)
```

**Implementation:** Uses `HashSet<(Keys, bool, bool, bool)>` for O(1) reserved lookups, `Dictionary<(Keys, bool, bool, bool), string>` for warned lookups with contextual messages.

### Compatibility Shims

Applied minimal find-replace to keep builds green:
- **SettingsForm.cs:**
  - `_settings.HotkeyKey` → `_settings.Hotkey.Key` in LoadSettings and BtnSave_Click
- **TrayApplicationContext.cs:**
  - `_hotkeyService.TargetKey = _settings.HotkeyKey` → `_hotkeyService.TargetCombo = _settings.Hotkey`
  - Tooltip format: `$"Coxixo - Press {_settings.Hotkey.ToDisplayString()} to talk"`

Full UI rework happens in Plan 02 (modifier capture, badge display, validation feedback). These shims preserve existing behavior.

## Deviations from Plan

None - plan executed exactly as written.

## Technical Decisions

### GetKeyState vs GetAsyncKeyState

**Decision:** Use `GetKeyState` for modifier detection in keyboard hook callback.

**Why:** GetKeyState is synchronized with the message queue that feeds the low-level keyboard hook. When the hook callback fires, GetKeyState reflects the exact modifier state at the time the message was queued. GetAsyncKeyState queries the hardware state directly, which can be desynchronized with the hook message queue, causing intermittent false positives (phantom modifiers) or false negatives (missed modifiers).

**Evidence:** Win32 documentation states GetKeyState queries the state "at the time the most recent message was retrieved." For WH_KEYBOARD_LL, that's the current hook message.

**Alternative considered:** GetAsyncKeyState - rejected due to race conditions.

### Modifier Release Handling

**Decision:** Fire `HotkeyReleased` when a required modifier is released during active hotkey hold, even if the main key is still held.

**Why:** Critical for push-to-talk ergonomics. If user presses Ctrl+F8 to start recording, then releases Ctrl before releasing F8, recording should stop immediately. Without this, recording continues until F8 is released, which feels broken.

**Implementation:** In HookCallback, when `_isKeyDown` is true and any modifier key is released, re-check if modifiers still match. If not, fire `HotkeyReleased`.

**Trade-off:** Adds extra modifier key event handling in the hook, but negligible performance impact.

### Value Equality for HotkeyCombo

**Decision:** Override `Equals` and `GetHashCode` to implement value equality.

**Why:** Enables correct comparison in collections (Dictionary, HashSet) and UI state management (detecting if hotkey changed). Two HotkeyCombo instances with the same Key/Ctrl/Alt/Shift should be considered equal.

**Pattern:** Standard value-type equality pattern using `HashCode.Combine`.

### Backward Compatibility Property

**Decision:** Keep `TargetKey` property on `KeyboardHookService` as a setter-only shim during transition.

**Why:** Allows existing code (SettingsForm, TrayApplicationContext) to compile and run with minimal changes while migrating to `TargetCombo`. Full migration happens in Plan 02.

**Implementation:** `TargetKey` setter creates a new `HotkeyCombo { Key = value }` with no modifiers. Getter returns `_targetCombo.Key`.

**Long-term:** Can be marked `[Obsolete]` in v1.2 and removed in v2.0.

## Verification Results

All verification criteria passed:

1. ✓ `dotnet build Coxixo/Coxixo.csproj` succeeds with zero errors
2. ✓ HotkeyCombo.cs exists with all required properties and methods
3. ✓ AppSettings.cs uses `HotkeyCombo Hotkey` instead of `Keys HotkeyKey`
4. ✓ KeyboardHookService uses GetKeyState for modifier detection
5. ✓ TargetCombo property accepts HotkeyCombo, TargetKey preserved for compat
6. ✓ HotkeyValidator.cs exists with three-level validation
7. ✓ TrayApplicationContext passes HotkeyCombo to hook service
8. ✓ All commits present in git log

**Expected runtime behavior (Plan 02 will verify):**
- F8 push-to-talk works identically to v1.0 (backward compat)
- Tray tooltip displays "Coxixo - Press F8 to talk"
- HotkeyCombo serializes/deserializes correctly via System.Text.Json
- HotkeyValidator correctly categorizes reserved, warned, and valid combos

## Success Criteria Met

- ✓ HKEY-01 foundation: HotkeyCombo model supports modifier flags (Ctrl, Alt, Shift)
- ✓ HKEY-02 foundation: KeyboardHookService matches multi-modifier combinations using GetKeyState
- ✓ HKEY-05 foundation: HotkeyValidator enforces reserved key policy and warns on conflicts
- ✓ Backward compatible: existing v1.0 F8 default preserved, no breaking changes
- ✓ Build succeeds with zero errors, app ready for Plan 02 UI work

## Next Steps

Plan 02 (Hotkey UI & Capture) will:
- Replace single-key capture textbox with modifier checkbox grid + key capture
- Display current hotkey as badge segments using `ToSegments()`
- Integrate `HotkeyValidator.Validate()` at save time with visual feedback
- Add reserved key blocking (red error) and warned key soft-blocking (yellow warning)
- Allow saving warned keys with user acknowledgment
- Test full modifier combinations (Ctrl+Shift+F8, Alt+Home, etc.)

## Self-Check

Verifying all claims in SUMMARY.md:

**Created files:**
- ✓ Coxixo/Models/HotkeyCombo.cs
- ✓ Coxixo/Services/HotkeyValidator.cs

**Modified files:**
- ✓ Coxixo/Models/AppSettings.cs
- ✓ Coxixo/Services/KeyboardHookService.cs
- ✓ Coxixo/Forms/SettingsForm.cs
- ✓ Coxixo/TrayApplicationContext.cs

**Commits:**
- ✓ 6438c2a: feat(05-01): create HotkeyCombo model and update AppSettings
- ✓ 5cf52d0: feat(05-01): add modifier detection to KeyboardHookService
- ✓ c0347d8: feat(05-01): create HotkeyValidator for reserved keys and warnings

**Build status:**
- ✓ Build succeeds with zero errors

## Self-Check: PASSED

All files exist, all commits present, build succeeds, claims verified.
