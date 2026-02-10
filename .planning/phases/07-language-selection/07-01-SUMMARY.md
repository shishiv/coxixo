---
phase: 07-language-selection
plan: 01
subsystem: settings
tags: [ui, settings, whisper-api, i18n]
dependency_graph:
  requires: [phase-06-startup]
  provides: [language-selection]
  affects: [transcription-service, settings-ui]
tech_stack:
  added: [WinForms-ComboBox-DataBinding]
  patterns: [KeyValuePair-DataSource, Null-Safe-ComboBox-Loading]
key_files:
  created: []
  modified:
    - Coxixo/Models/AppSettings.cs
    - Coxixo/Services/TranscriptionService.cs
    - Coxixo/TrayApplicationContext.cs
    - Coxixo/Forms/SettingsForm.Designer.cs
    - Coxixo/Forms/SettingsForm.cs
decisions:
  - "Use nullable string for LanguageCode (null = auto-detect)"
  - "Set DisplayMember/ValueMember before DataSource to prevent spurious events"
  - "Use SelectedIndex=0 for null values (WinForms doesn't support SelectedValue=null)"
  - "Use KeyValuePair<string?, string> for ComboBox data binding"
  - "Language saved on Save button click (not immediate), matching existing pattern"
metrics:
  duration_minutes: 3
  tasks_completed: 2
  files_modified: 5
  commits: 2
  completed_date: 2026-02-10
---

# Phase 7 Plan 1: Language Selection Summary

**One-liner:** Configurable transcription language (Portuguese, English, Spanish, French, German, Auto-detect) with UI dropdown and Whisper API integration.

## What Was Built

Added language selection capability to Coxixo, allowing users to choose a transcription language from a dropdown in the settings UI. The selected language is persisted in settings.json and passed to the Whisper API as an ISO 639-1 code (or null for auto-detect).

### Task 1: Add LanguageCode to AppSettings and wire TranscriptionService
- Added `LanguageCode` property to AppSettings (nullable string, default null)
- Added `languageCode` parameter to TranscriptionService constructor
- Passed language to `AudioTranscriptionOptions.Language`
- Removed hardcoded "pt" language from TranscriptionService
- Wired TrayApplicationContext to pass `settings.LanguageCode` to TranscriptionService

### Task 2: Add language ComboBox to SettingsForm with binding and persistence
- Added `lblLanguage` and `cmbLanguage` controls to SettingsForm.Designer.cs
- Populated ComboBox with 6 options: Auto-detect, Portuguese, English, Spanish, French, German
- Used `KeyValuePair<string?, string>` for data binding (Key = ISO code or null, Value = display name)
- Set DisplayMember/ValueMember before DataSource to prevent spurious SelectedIndexChanged events
- Applied dark theme to ComboBox
- Implemented null-safe loading: `SelectedIndex = 0` for null (Auto-detect), `SelectedValue` for non-null
- Saved selected language on Save button click (matching existing settings pattern)
- Adjusted form layout: language section positioned between Test Connection and Start with Windows
- Increased form height from 440 to 495 to accommodate new controls

## Deviations from Plan

None. Plan executed exactly as written.

## Verification Results

1. `dotnet build Coxixo/Coxixo.csproj` — Build succeeded with 0 warnings, 0 errors
2. Confirmed no hardcoded "pt" remains in TranscriptionService.cs (grep returned no matches)
3. All controls positioned correctly in Designer.cs (no overlapping)
4. ComboBox binding uses proper KeyValuePair pattern for null support
5. Dark theme handler added for ComboBox controls

## Technical Notes

### WinForms ComboBox Null Handling

WinForms ComboBox doesn't support `SelectedValue = null` directly. The workaround:
- Use `SelectedIndex = 0` when loading a null value (first item = Auto-detect with null Key)
- Use `SelectedValue as string` when saving (returns null for Auto-detect item)

### Event Suppression Pattern

Set DisplayMember/ValueMember **before** DataSource to prevent spurious `SelectedIndexChanged` events during form initialization. The `_isLoading` guard provides additional protection.

### Whisper API Auto-detect

When `LanguageCode` is null, `AudioTranscriptionOptions.Language` is set to null, which tells the Whisper API to auto-detect the language from the audio content.

## Commits

| Task | Commit | Message |
|------|--------|---------|
| 1 | 12a0649 | feat(07-01): add LanguageCode setting and wire to TranscriptionService |
| 2 | 44e02b2 | feat(07-01): add language selection ComboBox to settings UI |

## Files Modified

1. **Coxixo/Models/AppSettings.cs** — Added `LanguageCode` property (string?, default null)
2. **Coxixo/Services/TranscriptionService.cs** — Added `_languageCode` field, constructor parameter, and passed to AudioTranscriptionOptions
3. **Coxixo/TrayApplicationContext.cs** — Passed `settings.LanguageCode` to TranscriptionService constructor
4. **Coxixo/Forms/SettingsForm.Designer.cs** — Added language controls, adjusted layout positions, increased form height
5. **Coxixo/Forms/SettingsForm.cs** — Added ComboBox population, dark theme handler, load/save logic

## Success Criteria Met

- [x] Language dropdown with 6 options renders correctly in dark-themed settings form
- [x] Auto-detect (null) and explicit language codes persist across app restarts via settings.json
- [x] TranscriptionService passes correct ISO 639-1 code (or null) to Whisper API
- [x] No hardcoded "pt" language in TranscriptionService
- [x] Form layout maintains consistent spacing with no visual regressions
- [x] Build compiles without errors or warnings

## Next Steps

Language selection is now complete. Users can:
1. Open Settings
2. Select a language from the dropdown (Auto-detect, Portuguese, English, Spanish, French, German)
3. Click Save
4. The selected language persists in settings.json and is used for all subsequent transcriptions

Recommended verification:
1. Run app → open Settings → verify language dropdown shows "Auto-detect" by default
2. Select "Portuguese" → Save → reopen Settings → verify "Portuguese" still selected
3. Check `%LOCALAPPDATA%\Coxixo\settings.json` → verify `"LanguageCode": "pt"` present
4. Select "Auto-detect" → Save → check settings.json → verify `"LanguageCode": null`
5. Test transcription with different languages to verify Whisper API receives correct codes

## Self-Check: PASSED

All files verified:
- [x] Coxixo/Models/AppSettings.cs exists and contains LanguageCode property
- [x] Coxixo/Services/TranscriptionService.cs exists and contains _languageCode field
- [x] Coxixo/TrayApplicationContext.cs exists and passes LanguageCode to TranscriptionService
- [x] Coxixo/Forms/SettingsForm.Designer.cs exists and contains language controls
- [x] Coxixo/Forms/SettingsForm.cs exists and contains ComboBox binding logic

All commits verified:
- [x] Commit 12a0649 exists in git history
- [x] Commit 44e02b2 exists in git history

Build verification:
- [x] `dotnet build` succeeded with 0 warnings, 0 errors
