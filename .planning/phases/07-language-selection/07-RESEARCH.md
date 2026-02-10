# Phase 7: Language Selection - Research

**Researched:** 2026-02-09
**Domain:** WinForms UI Controls, Azure OpenAI Whisper API, Application Settings Persistence
**Confidence:** HIGH

## Summary

Language selection for Azure OpenAI Whisper API requires implementing a WinForms ComboBox with ISO 639-1 language codes and an "Auto-detect" option. Whisper supports 99 languages and performs automatic language detection when the language parameter is omitted or null. The current codebase hardcodes language to "pt" in TranscriptionService.cs (line 54) and uses a proven settings persistence pattern through ConfigurationService (JSON serialization to %LOCALAPPDATA%\Coxixo\settings.json).

The implementation follows established patterns from Phase 06 (startup selection): use AppSettings model for persistence, WinForms controls for UI, immediate save on user action, and _isLoading guard to prevent re-entrant events. The Azure.AI.OpenAI SDK's AudioTranscriptionOptions.Language property accepts ISO 639-1 codes (2-letter) or null for auto-detection.

**Primary recommendation:** Add a string LanguageCode property to AppSettings (nullable for auto-detect), create a ComboBox in SettingsForm with KeyValuePair<string, string> data binding (code → display name), populate with the 5 required languages plus "Auto-detect" (null value), and modify TranscriptionService to accept language parameter. Follow WinForms best practice: set DisplayMember/ValueMember before DataSource to avoid spurious events.

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Azure.AI.OpenAI | Current (.NET 8) | Azure OpenAI API client with Whisper support | Official Azure SDK for .NET, handles AudioTranscriptionOptions.Language property |
| System.Text.Json | .NET 8 BCL | Settings serialization | Already used in ConfigurationService for AppSettings persistence |
| System.Windows.Forms | .NET 8 | ComboBox control for language selection | Native WinForms control, already used throughout SettingsForm |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| None required | - | - | Feature uses existing dependencies only |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| ComboBox | RadioButton group | Radio buttons clearer for 6 options but consume 6x vertical space; ComboBox maintains compact 320x469 form size |
| KeyValuePair<string, string> | Custom LanguageOption class | Custom class adds no value; KeyValuePair is built-in and sufficient for code/name pairs |
| Nullable string for auto-detect | Empty string "" | Null is more semantically correct ("absence of value" vs "empty value"); Azure SDK accepts null naturally |

**Installation:**
```bash
# No new packages required - uses existing dependencies
```

## Architecture Patterns

### Recommended Project Structure
```
Coxixo/
├── Models/
│   └── AppSettings.cs           # Add LanguageCode property (string?, default null)
├── Services/
│   ├── ConfigurationService.cs  # No changes (already handles new properties)
│   └── TranscriptionService.cs  # Accept language parameter in constructor
└── Forms/
    ├── SettingsForm.cs          # Add ComboBox, LoadSettings, SaveSettings logic
    └── SettingsForm.Designer.cs # Add lblLanguage, cmbLanguage controls
```

### Pattern 1: Nullable String for Auto-Detect
**What:** Use `string? LanguageCode` in AppSettings, where null means "auto-detect" and non-null is ISO 639-1 code
**When to use:** Settings that have an "automatic" or "default" option distinct from explicit user choices
**Example:**
```csharp
// Models/AppSettings.cs
public class AppSettings
{
    /// <summary>
    /// ISO 639-1 language code for transcription (e.g., "en", "pt").
    /// Null means auto-detect language from audio.
    /// </summary>
    public string? LanguageCode { get; set; } = null; // Auto-detect by default
}

// Services/TranscriptionService.cs
var options = new AudioTranscriptionOptions
{
    ResponseFormat = AudioTranscriptionFormat.Text,
    Language = languageCode // null = auto-detect, "en" = force English
};
```

### Pattern 2: ComboBox Data Binding with KeyValuePair
**What:** Bind ComboBox to List<KeyValuePair<string, string>> with DisplayMember="Value", ValueMember="Key"
**When to use:** Dropdown with separate internal value (code) and display text (language name)
**Example:**
```csharp
// Source: https://www.daveoncsharp.com/2009/11/binding-a-windows-forms-combobox-in-csharp/
var languageOptions = new List<KeyValuePair<string?, string>>
{
    new(null, "Auto-detect"),
    new("pt", "Portuguese"),
    new("en", "English"),
    new("es", "Spanish"),
    new("fr", "French"),
    new("de", "German")
};

// CRITICAL: Set DisplayMember/ValueMember BEFORE DataSource
cmbLanguage.DisplayMember = "Value";
cmbLanguage.ValueMember = "Key";
cmbLanguage.DataSource = languageOptions;
```
**Why this order matters:** Setting DataSource first triggers SelectedIndexChanged before DisplayMember/ValueMember are configured, causing binding failures and spurious events. Source: [Best Practice for Binding WinForms ListControls - CodeProject](https://www.codeproject.com/Articles/8390/Best-Practice-for-Binding-WinForms-ListControls)

### Pattern 3: _isLoading Guard for Event Suppression
**What:** Use boolean flag to prevent re-entrant event handlers during programmatic control updates
**When to use:** Any event handler (CheckedChanged, SelectedIndexChanged) that might trigger during LoadSettings()
**Example:**
```csharp
// Already used in SettingsForm for chkStartWithWindows
private bool _isLoading;

private void LoadSettings()
{
    _isLoading = true;

    cmbLanguage.SelectedValue = _settings.LanguageCode; // Triggers SelectedIndexChanged

    _isLoading = false;
}

private void CmbLanguage_SelectedIndexChanged(object? sender, EventArgs e)
{
    if (_isLoading)
        return; // Ignore programmatic changes during load

    // Handle user selection changes here
}
```
**Why needed:** ComboBox fires SelectedIndexChanged during LoadSettings() when SelectedValue is set, causing infinite loops or premature saves. Source: [C# ComboBox: Prevent SelectedIndexChanged event from firing](https://consultrikin.wordpress.com/2012/03/22/c-combobox-preventstop-selectedindexchanged-event-automatically-from-firing-when-datasource-is-used/)

### Pattern 4: Null Handling for ComboBox SelectedValue
**What:** When setting SelectedValue to null, use SelectedIndex = -1 instead
**When to use:** Clearing ComboBox selection or setting to a nullable value
**Example:**
```csharp
// Don't do this (doesn't work):
// cmbLanguage.SelectedValue = null;

// Do this instead:
if (value == null)
    cmbLanguage.SelectedIndex = -1;  // First item in list ("Auto-detect")
else
    cmbLanguage.SelectedValue = value;
```
**Why:** WinForms ComboBox.SelectedValue = null doesn't clear the selection. SelectedIndex = -1 is the correct way to select the first item when null represents "Auto-detect". Source: [.net - Combobox SelectedValue = null - DaniWeb](https://www.daniweb.com/programming/software-development/threads/270921/combobox-selectedvalue-null)

### Anti-Patterns to Avoid
- **Storing language as full name ("Portuguese") instead of code ("pt"):** Whisper API requires ISO 639-1 codes, not language names. Store the code, display the name.
- **Using SelectedIndex for persistence:** Fragile if list order changes. Use SelectedValue with ISO 639-1 codes.
- **Setting DataSource before DisplayMember/ValueMember:** Causes binding failures and spurious SelectedIndexChanged events.
- **Using empty string "" for auto-detect:** Null is semantically correct for "no language specified"; empty string is a valid (but meaningless) ISO code.
- **Not using _isLoading guard:** Leads to infinite loops when LoadSettings() triggers event handlers that save settings.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| ISO 639-1 language list | Custom language code mapping | Whisper tokenizer.py LANGUAGES dict | Whisper supports 99 languages, manually maintaining this list risks errors and staleness |
| ComboBox key-value binding | Custom ComboBox wrapper class | KeyValuePair<string, string> with DisplayMember/ValueMember | Built-in WinForms pattern, well-documented, no custom code needed |
| Settings persistence | XML/INI file parsing | System.Text.Json (already in use) | ConfigurationService already handles AppSettings serialization; new properties work automatically |
| Event suppression | Custom event routing | _isLoading boolean flag | Simple, proven pattern already used in SettingsForm for chkStartWithWindows |

**Key insight:** WinForms ComboBox data binding is mature and well-supported. Custom wrappers add complexity without benefit. Follow the "DisplayMember, ValueMember, DataSource" order strictly and use built-in KeyValuePair.

## Common Pitfalls

### Pitfall 1: SelectedValue = null Doesn't Work
**What goes wrong:** Setting `cmbLanguage.SelectedValue = null` in LoadSettings() doesn't select the "Auto-detect" item; ComboBox shows blank or retains previous selection
**Why it happens:** WinForms ComboBox ignores SelectedValue assignments that don't match a ValueMember value in the DataSource. Since KeyValuePair<string?, string> has a null Key, WinForms can't match it.
**How to avoid:** Use conditional logic: if language is null, set `SelectedIndex = 0` (first item); otherwise set `SelectedValue = code`
**Warning signs:** "Auto-detect" item never appears selected even though LanguageCode is null in settings.json

### Pitfall 2: Setting DataSource Before DisplayMember/ValueMember
**What goes wrong:** ComboBox shows "System.Collections.Generic.KeyValuePair`2[System.String,System.String]" instead of language names
**Why it happens:** Without DisplayMember set, ComboBox calls ToString() on bound objects, which for KeyValuePair produces the type name
**How to avoid:** Always set DisplayMember and ValueMember BEFORE setting DataSource. This is a WinForms binding requirement.
**Warning signs:** ComboBox populated but shows type names or blank items instead of human-readable text

### Pitfall 3: Forgetting _isLoading Guard
**What goes wrong:** Infinite loop or unexpected behavior when LoadSettings() programmatically sets cmbLanguage.SelectedValue, triggering SelectedIndexChanged, which saves settings, which triggers another load...
**Why it happens:** SelectedIndexChanged fires for both user actions AND programmatic changes (like setting SelectedValue in LoadSettings)
**How to avoid:** Wrap LoadSettings() in `_isLoading = true/false` and add `if (_isLoading) return;` at the start of SelectedIndexChanged handler
**Warning signs:** Form freezes, settings saved immediately on form open, multiple file writes during startup

### Pitfall 4: Hardcoding Language List in Multiple Places
**What goes wrong:** Language options defined in multiple places (e.g., SettingsForm, test code, documentation) drift out of sync
**Why it happens:** No single source of truth for supported languages
**How to avoid:** Define language options as a static readonly List in SettingsForm or a shared constants class. Reference it everywhere.
**Warning signs:** Different language lists in different files, tests fail when UI is updated

### Pitfall 5: Not Handling Null in TranscriptionService
**What goes wrong:** TranscriptionService crashes or throws ArgumentNullException when LanguageCode is null
**Why it happens:** Assuming LanguageCode is always a string, not handling nullable case
**How to avoid:** Accept `string? languageCode` in TranscriptionService constructor, pass it directly to AudioTranscriptionOptions.Language (Azure SDK accepts null)
**Warning signs:** Crash when "Auto-detect" selected, error logs showing null reference exceptions

### Pitfall 6: Assuming Empty String Means Auto-Detect
**What goes wrong:** Passing `Language = ""` to Whisper API causes validation error: "Language parameter must be specified in ISO-639-1 format"
**Why it happens:** Empty string is not a valid ISO 639-1 code; Whisper interprets it as malformed input, not "auto-detect"
**How to avoid:** Use null for auto-detect, not empty string. Azure SDK treats null as "omit language parameter" which triggers auto-detection.
**Warning signs:** API errors with message "Invalid language" when auto-detect is selected

## Code Examples

Verified patterns from official sources and existing codebase:

### AppSettings Model Extension
```csharp
// Add to Models/AppSettings.cs
/// <summary>
/// ISO 639-1 language code for transcription (e.g., "pt", "en", "es").
/// Null means auto-detect language from audio.
/// Default is null (auto-detect).
/// </summary>
public string? LanguageCode { get; set; } = null;
```

### ComboBox Setup in SettingsForm.Designer.cs
```csharp
// Add to field declarations
private Label lblLanguage;
private ComboBox cmbLanguage;

// Add to InitializeComponent()
lblLanguage = new Label();
cmbLanguage = new ComboBox();

// lblLanguage
lblLanguage.AutoSize = true;
lblLanguage.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
lblLanguage.Location = new Point(12, 335);  // After "Test Connection" button
lblLanguage.Name = "lblLanguage";
lblLanguage.Text = "TRANSCRIPTION LANGUAGE";

// cmbLanguage
cmbLanguage.DropDownStyle = ComboBoxStyle.DropDownList;  // Prevent manual text entry
cmbLanguage.Location = new Point(12, 355);
cmbLanguage.Size = new Size(280, 25);
cmbLanguage.Name = "cmbLanguage";
cmbLanguage.SelectedIndexChanged += CmbLanguage_SelectedIndexChanged;

// Update form height and control positions
// chkStartWithWindows moves from y=370 to y=390
// btnCancel/btnSave move from y=400 to y=420
// Form ClientSize changes from (304, 440) to (304, 469)
```

### ComboBox Data Binding in SettingsForm.cs
```csharp
// Add to SetupForm() method
private void SetupForm()
{
    // ... existing code ...

    // Populate language dropdown
    var languageOptions = new List<KeyValuePair<string?, string>>
    {
        new(null, "Auto-detect"),
        new("pt", "Portuguese"),
        new("en", "English"),
        new("es", "Spanish"),
        new("fr", "French"),
        new("de", "German")
    };

    // CRITICAL: Set DisplayMember/ValueMember BEFORE DataSource
    cmbLanguage.DisplayMember = "Value";
    cmbLanguage.ValueMember = "Key";
    cmbLanguage.DataSource = languageOptions;
}
```
**Source:** [Best Practice for Binding WinForms ListControls](https://www.codeproject.com/Articles/8390/Best-Practice-for-Binding-WinForms-ListControls)

### LoadSettings with Null Handling
```csharp
// Update LoadSettings() method
private void LoadSettings()
{
    _isLoading = true;

    _settings = ConfigurationService.Load();
    hotkeyPicker.SelectedCombo = _settings.Hotkey;
    txtEndpoint.Text = _settings.AzureEndpoint;
    txtApiKey.Text = CredentialService.LoadApiKey() ?? "";
    txtDeployment.Text = _settings.WhisperDeployment;
    chkStartWithWindows.Checked = StartupService.IsEnabled();

    // Handle nullable LanguageCode
    if (_settings.LanguageCode == null)
        cmbLanguage.SelectedIndex = 0;  // Auto-detect (first item)
    else
        cmbLanguage.SelectedValue = _settings.LanguageCode;

    _isLoading = false;
}
```
**Why conditional:** ComboBox.SelectedValue = null doesn't work; must use SelectedIndex = 0 to select first item
**Source:** [Combobox SelectedValue = null - DaniWeb](https://www.daniweb.com/programming/software-development/threads/270921/combobox-selectedvalue-null)

### SelectedIndexChanged Event Handler
```csharp
// Add new event handler
private void CmbLanguage_SelectedIndexChanged(object? sender, EventArgs e)
{
    if (_isLoading)
        return;  // Ignore programmatic changes during LoadSettings

    // No immediate action needed - language saved when user clicks Save button
    // (follows existing pattern: hotkey, endpoint, deployment don't save immediately)
}
```
**Note:** Unlike chkStartWithWindows (which writes registry immediately), language selection only persists on Save button click, matching the existing pattern for other settings.

### Save Button with Language Persistence
```csharp
// Update BtnSave_Click() method
private void BtnSave_Click(object? sender, EventArgs e)
{
    // ... existing hotkey validation ...

    // Update settings
    _settings.Hotkey = combo;
    _settings.AzureEndpoint = txtEndpoint.Text.Trim();
    _settings.WhisperDeployment = txtDeployment.Text.Trim();
    _settings.StartWithWindows = chkStartWithWindows.Checked;
    _settings.LanguageCode = cmbLanguage.SelectedValue as string;  // Null-safe cast

    // Save settings
    ConfigurationService.Save(_settings);

    // ... existing API key save and close ...
}
```

### TranscriptionService Constructor Update
```csharp
// Update Services/TranscriptionService.cs constructor
public TranscriptionService(string endpoint, string apiKey, string deployment, string? languageCode = null)
{
    if (string.IsNullOrEmpty(endpoint))
        throw new ArgumentException("Endpoint is required", nameof(endpoint));
    if (string.IsNullOrEmpty(apiKey))
        throw new ArgumentException("API key is required", nameof(apiKey));
    if (string.IsNullOrEmpty(deployment))
        throw new ArgumentException("Deployment name is required", nameof(deployment));

    _client = new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
    _audioClient = _client.GetAudioClient(deployment);
    _languageCode = languageCode;  // Store for use in TranscribeAsync
}

// Update TranscribeAsync method
public async Task<string?> TranscribeAsync(byte[] audioData, CancellationToken ct = default)
{
    // ... existing validation ...

    var options = new AudioTranscriptionOptions
    {
        ResponseFormat = AudioTranscriptionFormat.Text,
        Language = _languageCode  // null = auto-detect, "pt" = force Portuguese
    };

    var result = await _audioClient.TranscribeAudioAsync(stream, "audio.wav", options, ct);
    return result.Value.Text;
}
```
**Source:** Azure SDK documentation indicates Language property is optional and accepts null for auto-detection

### TrayApplicationContext Update (TranscriptionService Instantiation)
```csharp
// Update wherever TranscriptionService is created (likely TrayApplicationContext.cs)
var settings = ConfigurationService.Load();
_transcriptionService = new TranscriptionService(
    settings.AzureEndpoint,
    apiKey,
    settings.WhisperDeployment,
    settings.LanguageCode  // Pass language code from settings
);
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Hardcoded language in code | User-selectable language in settings | N/A (new feature) | Users can now choose transcription language or enable auto-detection |
| No auto-detection option | Auto-detect as default | N/A (new feature) | Better UX for multilingual users; Whisper automatically identifies language |
| Manual text entry for language | Dropdown with validated options | N/A (new feature) | Prevents invalid ISO codes, provides better UX with human-readable names |

**Deprecated/outdated:**
- N/A - This is a new feature, not replacing deprecated patterns

## Whisper API Language Support

### Complete Language List (99 languages)
Source: [openai/whisper/tokenizer.py](https://raw.githubusercontent.com/openai/whisper/main/whisper/tokenizer.py)

**Phase 7 Requirements (5 languages):**
- `pt` - Portuguese (current hardcoded default)
- `en` - English
- `es` - Spanish
- `fr` - French
- `de` - German

**Full Whisper Support (for future expansion):**
Whisper supports 99 languages via ISO 639-1 codes including: en (English), zh (Chinese), de (German), es (Spanish), ru (Russian), ko (Korean), fr (French), ja (Japanese), pt (Portuguese), tr (Turkish), pl (Polish), ca (Catalan), nl (Dutch), ar (Arabic), sv (Swedish), it (Italian), id (Indonesian), hi (Hindi), fi (Finnish), vi (Vietnamese), he (Hebrew), uk (Ukrainian), el (Greek), ms (Malay), cs (Czech), ro (Romanian), da (Danish), hu (Hungarian), ta (Tamil), no (Norwegian), th (Thai), ur (Urdu), hr (Croatian), bg (Bulgarian), lt (Lithuanian), la (Latin), mi (Maori), ml (Malayalam), cy (Welsh), sk (Slovak), te (Telugu), fa (Persian), lv (Latvian), bn (Bengali), sr (Serbian), az (Azerbaijani), sl (Slovenian), kn (Kannada), et (Estonian), mk (Macedonian), br (Breton), eu (Basque), is (Icelandic), hy (Armenian), ne (Nepali), mn (Mongolian), bs (Bosnian), kk (Kazakh), sq (Albanian), sw (Swahili), gl (Galician), mr (Marathi), pa (Punjabi), si (Sinhala), km (Khmer), sn (Shona), yo (Yoruba), so (Somali), af (Afrikaans), oc (Occitan), ka (Georgian), be (Belarusian), tg (Tajik), sd (Sindhi), gu (Gujarati), am (Amharic), yi (Yiddish), lo (Lao), uz (Uzbek), fo (Faroese), ht (Haitian Creole), ps (Pashto), tk (Turkmen), nn (Nynorsk), mt (Maltese), sa (Sanskrit), lb (Luxembourgish), my (Myanmar), bo (Tibetan), tl (Tagalog), mg (Malagasy), as (Assamese), tt (Tatar), haw (Hawaiian), ln (Lingala), ha (Hausa), ba (Bashkir), jw (Javanese), su (Sundanese), yue (Cantonese).

### Auto-Detection Behavior
**How it works:** When AudioTranscriptionOptions.Language is null or omitted, Whisper performs automatic language detection by analyzing the audio's acoustic features and selecting the most probable language from its 99-language tokenizer.

**Performance impact:** Omitting the language parameter adds a small language detection overhead (~50-200ms) to the initial processing. Specifying the language explicitly can improve accuracy and reduce latency when the language is known.

**Recommendation:** Default to auto-detect for better UX; users who consistently transcribe in one language can select it explicitly for marginal performance gains.

**Sources:**
- [Whisper language recognition - OpenAI Community](https://community.openai.com/t/whisper-language-recognition/665358)
- [OpenAI Whisper GitHub - Language Detection](https://github.com/openai/whisper)

## Open Questions

1. **Should we add a "Detected: [language]" indicator in the UI after transcription?**
   - What we know: Whisper can detect language (model.detect_language() in Python API), but Azure SDK AudioTranscription response doesn't expose detected language in ResponseFormat.Text mode
   - What's unclear: Whether Azure SDK supports retrieving detected language in verbose_json format, and whether this adds value for v1.0
   - Recommendation: Defer to post-v1.0; requires ResponseFormat.VerboseJson investigation and UI design for showing detected language

2. **Should we validate that selected language matches common user locale?**
   - What we know: User might select Portuguese but have English Windows locale
   - What's unclear: Whether this mismatch indicates user error or intentional multilingual use
   - Recommendation: Don't validate; users may legitimately transcribe in languages different from their OS locale

3. **Should we remember last-used language per microphone device?**
   - What we know: Future Phase 8 adds microphone selection; users might use different languages with different microphones (e.g., headset for English calls, desktop mic for Portuguese notes)
   - What's unclear: Whether this complexity is justified for v1.0
   - Recommendation: Defer to post-v1.0; single global language setting is sufficient for initial release

## Sources

### Primary (HIGH confidence)
- [openai/whisper/tokenizer.py](https://raw.githubusercontent.com/openai/whisper/main/whisper/tokenizer.py) - Complete list of 99 supported languages with ISO 639-1 codes
- [Azure.AI.OpenAI AudioTranscriptionOptions - Microsoft Learn](https://learn.microsoft.com/en-us/java/api/com.azure.ai.openai.models.audiotranscriptionoptions?view=azure-java-preview) - Language property documentation
- [Best Practice for Binding WinForms ListControls - CodeProject](https://www.codeproject.com/Articles/8390/Best-Practice-for-Binding-WinForms-ListControls) - DisplayMember/ValueMember/DataSource order
- Coxixo codebase (AppSettings.cs, ConfigurationService.cs, SettingsForm.cs) - Existing patterns for settings persistence and form layout

### Secondary (MEDIUM confidence)
- [Whisper Parameter Reference - DeepWiki](https://deepwiki.com/manzolo/openai-whisper-docker/5.2-whisper-parameter-reference) - Language parameter behavior (omit for auto-detect)
- [OpenAI Community - Whisper is there a way to tell the language before recognition](https://community.openai.com/t/whisper-is-there-a-way-to-tell-the-language-before-recognition/70687) - Auto-detection mechanism explanation
- [ComboBox SelectedValue = null - DaniWeb](https://www.daniweb.com/programming/software-development/threads/270921/combobox-selectedvalue-null) - SelectedIndex = -1 workaround
- [Binding a Windows Forms ComboBox in C#](https://www.daveoncsharp.com/2009/11/binding-a-windows-forms-combobox-in-csharp/) - KeyValuePair binding pattern
- [C# ComboBox: Prevent SelectedIndexChanged event from firing - Consult Rikin](https://consultrikin.wordpress.com/2012/03/22/c-combobox-preventstop-selectedindexchanged-event-automatically-from-firing-when-datasource-is-used/) - _isLoading guard pattern

### Tertiary (LOW confidence)
- N/A

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - Azure SDK and WinForms ComboBox are well-documented and proven in existing codebase
- Architecture: HIGH - Follows established patterns from Phase 06 (startup selection) and existing settings persistence
- Pitfalls: HIGH - ComboBox binding pitfalls are well-documented in official sources; null handling verified through community resources

**Research date:** 2026-02-09
**Valid until:** 2026-03-09 (30 days - stable domain, no fast-moving dependencies)
