---
phase: 06-windows-startup
plan: 01
subsystem: startup
tags: [registry, settings-ui, windows-integration]
dependency_graph:
  requires: [ConfigurationService, SettingsForm]
  provides: [StartupService, StartWithWindows-setting]
  affects: [SettingsForm-layout]
tech_stack:
  added: [Microsoft.Win32.Registry]
  patterns: [HKCU-registry-access, immediate-registry-write, re-entrant-guard]
key_files:
  created:
    - Coxixo/Services/StartupService.cs
  modified:
    - Coxixo/Models/AppSettings.cs
    - Coxixo/Forms/SettingsForm.cs
    - Coxixo/Forms/SettingsForm.Designer.cs
decisions:
  - what: Use HKCU Run registry key for Windows startup
    why: Standard Windows mechanism, per-user, no admin required
    alternatives: [Task Scheduler, Startup folder]
    impact: Windows-only, requires registry permissions
  - what: Immediate registry write on checkbox toggle (not deferred to Save)
    why: User expects instant feedback, aligns with system settings UX patterns
    alternatives: [Defer to Save button]
    impact: Requires error handling and checkbox revert on failure
  - what: Read registry state on form load, not settings JSON
    why: Registry is source of truth, settings file can drift
    alternatives: [Read from AppSettings]
    impact: Accurate state reflection even if registry modified externally
  - what: Use _isLoading guard to prevent re-entrant CheckedChanged during LoadSettings
    why: Setting Checked property fires CheckedChanged, causing redundant registry write
    alternatives: [Unsubscribe/resubscribe to event]
    impact: Cleaner pattern, prevents unnecessary registry operations
metrics:
  duration: 2m 29s
  tasks_completed: 2
  files_created: 1
  files_modified: 3
  commits: 2
  completed_at: 2026-02-10T00:46:10Z
---

# Phase 06 Plan 01: Windows Startup Registration Summary

**One-liner:** Windows startup registration via HKCU Run registry key with immediate checkbox toggle in settings UI.

## What Was Built

Added "Start with Windows" checkbox to SettingsForm that enables users to configure Coxixo to launch automatically at Windows login. The checkbox directly manipulates the Windows registry (HKCU\Software\Microsoft\Windows\CurrentVersion\Run) and reflects the actual registry state when the settings window opens.

**Key Components:**

1. **StartupService.cs** - Static service for Windows startup registration
   - `IsEnabled()`: Checks registry for Coxixo entry, compares path to current executable
   - `Enable()`: Writes quoted executable path to HKCU Run key
   - `Disable()`: Removes Coxixo from Run key
   - Proper exception handling for registry access failures and missing keys

2. **AppSettings.StartWithWindows** - Persists user's last choice to settings JSON
   - Note: Registry is source of truth, this property is for audit trail only

3. **SettingsForm Updates** - Checkbox integration with immediate registry toggle
   - Checkbox positioned between Test Connection and Save buttons
   - Registry state read on form load (not settings file)
   - Toggle immediately writes/removes registry entry
   - Error handling with user-friendly dialogs and checkbox revert
   - Re-entrant guard (`_isLoading`) prevents redundant writes during form load
   - Dark theme applied to checkbox
   - Form height increased to accommodate new control

## Deviations from Plan

None - plan executed exactly as written.

## Auth Gates Encountered

None.

## Technical Highlights

**Registry Access Pattern:**
- All RegistryKey usage with `using` statements for proper disposal
- `IsEnabled()` returns safe default (false) on any exception
- `Enable()` throws InvalidOperationException if Run key missing (system corruption)
- `Disable()` silently succeeds on UnauthorizedAccessException (user intent achieved)

**Re-entrant Guard:**
- Setting `chkStartWithWindows.Checked = StartupService.IsEnabled()` in LoadSettings fires CheckedChanged event
- Without guard, this would trigger redundant registry write on every form open
- `_isLoading` flag blocks event handler during initial load

**Source of Truth:**
- Checkbox always reads from registry on form load, never from AppSettings
- Prevents drift if registry modified externally (user, policy, other tools)
- Settings JSON stores last user choice for audit, not as authoritative state

## Verification Results

- Build: SUCCESS (0 warnings, 0 errors)
- StartupService.cs created with IsEnabled/Enable/Disable methods
- AppSettings.StartWithWindows property added
- SettingsForm.Designer.cs declares chkStartWithWindows
- SettingsForm.cs implements CheckedChanged handler with error handling
- Form height increased from 405 to 440 pixels
- Dark theme applied to checkbox
- Re-entrant guard prevents unnecessary registry writes

## Success Criteria Met

- [x] START-01: User can toggle "Start with Windows" checkbox in settings
- [x] START-02: Toggle immediately updates Windows startup registry (HKCU Run key with quoted path)
- [x] START-03: Checkbox reflects current startup registration when settings window opens (reads from registry, not settings file)
- [x] Registry write failure handled gracefully with error dialog and checkbox revert
- [x] Build succeeds with zero errors

## Files Changed

**Created:**
- `Coxixo/Services/StartupService.cs` (80 lines) - Windows startup registration service

**Modified:**
- `Coxixo/Models/AppSettings.cs` - Added StartWithWindows property
- `Coxixo/Forms/SettingsForm.cs` - Added checkbox logic, event handler, dark theme
- `Coxixo/Forms/SettingsForm.Designer.cs` - Added checkbox control, repositioned buttons

## Commits

| Hash | Message |
|------|---------|
| 78a54b8 | feat(06-01): add StartupService and StartWithWindows property |
| 4949e3b | feat(06-01): add Start with Windows checkbox to settings |

## Self-Check

Verifying all claimed files and commits exist.

**Files:**
- FOUND: Coxixo/Services/StartupService.cs

**Commits:**
- FOUND: 78a54b8
- FOUND: 4949e3b

**Result: PASSED** - All claimed artifacts verified.
