# Phase 3: Transcription Loop - Research

**Researched:** 2026-01-18
**Domain:** Azure OpenAI Whisper API integration, HTTP clients, clipboard operations
**Confidence:** HIGH

## Summary

This phase completes the core value loop by sending captured audio to Azure OpenAI Whisper API and copying the transcription to the clipboard. The research covers: Azure OpenAI Whisper API specifics (endpoint format, authentication, request/response structure), .NET 8 HttpClient best practices, clipboard STA thread requirements, and error handling patterns.

The Azure.AI.OpenAI SDK 2.x provides a clean abstraction via `AudioClient.TranscribeAudioAsync()` that accepts a Stream directly, avoiding raw HTTP handling. The existing codebase already has `CredentialService` for secure API key storage and `ConfigurationService` for endpoint/deployment settings. The main integration point is in `TrayApplicationContext.OnHotkeyReleased()` where captured audio bytes are available.

**Primary recommendation:** Use Azure.AI.OpenAI 2.1.0 SDK with `AudioClient.TranscribeAudioAsync(Stream, filename, options)` overload. Handle clipboard operations on the UI thread (already STA in WinForms). Implement simple retry (2 attempts) with exponential backoff for transient failures.

## Standard Stack

The established libraries/tools for this domain:

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Azure.AI.OpenAI | 2.1.0 | Azure OpenAI SDK | Official Microsoft SDK, wraps OpenAI .NET library for Azure |
| OpenAI | 2.1.0+ | Base OpenAI client | Dependency of Azure.AI.OpenAI, provides AudioClient |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| Polly | 8.x | Retry/resilience | If complex retry policies needed (not recommended for this scope) |
| Microsoft.Toolkit.Uwp.Notifications | 7.x | Modern toast notifications | If BalloonTip proves insufficient |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Azure.AI.OpenAI SDK | Raw HttpClient | More control but must handle auth headers, multipart/form-data manually |
| BalloonTip | Modern Toast (Microsoft.Toolkit) | Better UI but requires Windows 10 SDK targeting (`net8.0-windows10.0.17763.0`) |
| Polly retry | Simple manual retry | Polly is overkill for 2-3 retry attempts; manual is simpler for this scope |

**Installation:**
```bash
dotnet add package Azure.AI.OpenAI --version 2.1.0
```

Note: This package brings `OpenAI` as a transitive dependency.

## Architecture Patterns

### Recommended Project Structure
```
Coxixo/
├── Services/
│   ├── TranscriptionService.cs    # NEW: Azure Whisper API client
│   ├── ClipboardService.cs        # NEW: Thread-safe clipboard operations
│   ├── AudioCaptureService.cs     # Existing
│   ├── CredentialService.cs       # Existing (stores API key)
│   └── ConfigurationService.cs    # Existing (stores endpoint, deployment)
├── Models/
│   ├── AppSettings.cs             # Existing (has AzureEndpoint, WhisperDeployment)
│   └── TranscriptionResult.cs     # NEW: Result wrapper with success/error
└── TrayApplicationContext.cs      # Orchestrates the flow
```

### Pattern 1: Service Layer for API Calls
**What:** Encapsulate Azure OpenAI calls in a dedicated `TranscriptionService`
**When to use:** Always for external API integration
**Example:**
```csharp
// Source: Microsoft Azure.AI.OpenAI docs and best practices
public class TranscriptionService : IDisposable
{
    private readonly AzureOpenAIClient _client;
    private readonly AudioClient _audioClient;

    public TranscriptionService(string endpoint, string apiKey, string deployment)
    {
        _client = new AzureOpenAIClient(
            new Uri(endpoint),
            new AzureKeyCredential(apiKey));
        _audioClient = _client.GetAudioClient(deployment);
    }

    public async Task<TranscriptionResult> TranscribeAsync(
        byte[] audioData,
        CancellationToken ct = default)
    {
        using var stream = new MemoryStream(audioData);
        var transcription = await _audioClient.TranscribeAudioAsync(
            stream,
            "audio.wav",  // filename hints format
            cancellationToken: ct);
        return new TranscriptionResult { Text = transcription.Value.Text };
    }
}
```

### Pattern 2: Thread-Safe Clipboard Access
**What:** Use `Control.Invoke` or direct call since WinForms main thread is already STA
**When to use:** Always for clipboard operations from async callbacks
**Example:**
```csharp
// Source: Microsoft Clipboard.SetText docs
// WinForms app main thread is STA by default ([STAThread] on Main)
// Clipboard must be accessed from STA thread

// Option 1: If on UI thread already (e.g., in event handler)
Clipboard.SetText(transcribedText);

// Option 2: If on background thread, marshal to UI thread
_trayIcon.ContextMenuStrip.Invoke(() => Clipboard.SetText(transcribedText));
```

### Pattern 3: Simple Retry with Exponential Backoff
**What:** Manual retry loop without Polly dependency
**When to use:** For transient API failures (5xx, timeouts)
**Example:**
```csharp
// Source: Best practices for resilient HTTP calls
public async Task<TranscriptionResult> TranscribeWithRetryAsync(
    byte[] audioData,
    int maxRetries = 2,
    CancellationToken ct = default)
{
    Exception? lastException = null;

    for (int attempt = 0; attempt <= maxRetries; attempt++)
    {
        try
        {
            return await TranscribeAsync(audioData, ct);
        }
        catch (RequestFailedException ex) when (IsTransient(ex) && attempt < maxRetries)
        {
            lastException = ex;
            var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt)); // 1s, 2s, 4s
            await Task.Delay(delay, ct);
        }
    }

    throw lastException!;
}

private static bool IsTransient(RequestFailedException ex)
{
    return ex.Status >= 500 || ex.Status == 408 || ex.Status == 429;
}
```

### Anti-Patterns to Avoid
- **Creating HttpClient per request:** Azure.AI.OpenAI SDK handles this internally, but if using raw HttpClient, use singleton with PooledConnectionLifetime
- **Blocking async on UI thread:** Never use `.Result` or `.Wait()` on Task; use `async/await` throughout
- **Clipboard access from thread pool:** Always marshal to STA thread

## Don't Hand-Roll

Problems that look simple but have existing solutions:

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Azure OpenAI authentication | Manual header injection | `AzureKeyCredential` or `DefaultAzureCredential` | SDK handles auth header format, token refresh |
| Multipart form-data for audio | Manual boundary construction | `AudioClient.TranscribeAudioAsync(Stream)` | Multipart encoding is error-prone |
| HTTP connection pooling | Per-request HttpClient | Azure SDK internal handling | SDK manages SocketsHttpHandler properly |
| Retry logic (simple) | Complex Polly setup | Simple for-loop with delay | Polly is overkill for 2-3 retries |

**Key insight:** The Azure.AI.OpenAI 2.x SDK abstracts away HTTP complexities. The `AudioClient` handles multipart/form-data encoding, authentication headers, and provides strongly-typed responses. Don't bypass it for raw HTTP unless you need something very specific.

## Common Pitfalls

### Pitfall 1: Filename Not Set for Stream Transcription
**What goes wrong:** Whisper may misinterpret audio format, causing transcription failures or garbage output
**Why it happens:** `TranscribeAudioAsync(Stream, filename)` uses filename extension to infer audio format
**How to avoid:** Always pass filename with correct extension matching the audio format
**Warning signs:** Transcription returns empty string or garbled text

```csharp
// BAD - no filename
await audioClient.TranscribeAudioAsync(stream);

// GOOD - WAV format specified
await audioClient.TranscribeAudioAsync(stream, "audio.wav");
```

### Pitfall 2: Clipboard Access from Background Thread
**What goes wrong:** `ThreadStateException: Current thread must be set to single thread apartment (STA) mode`
**Why it happens:** Clipboard uses OLE internally, requires STA thread
**How to avoid:** Marshal clipboard calls to UI thread via `Control.Invoke`
**Warning signs:** Exception thrown when copying after async API call completes

### Pitfall 3: API Key Not Configured
**What goes wrong:** `RequestFailedException` with 401 Unauthorized or null reference
**Why it happens:** User hasn't configured credentials yet; `CredentialService.LoadApiKey()` returns null
**How to avoid:** Check credentials at first use, show helpful error message
**Warning signs:** Immediate failure on first transcription attempt

```csharp
// Check before attempting transcription
var apiKey = CredentialService.LoadApiKey();
if (string.IsNullOrEmpty(apiKey))
{
    ShowNotification("Configure API key in Settings first");
    return;
}
```

### Pitfall 4: Timeout on Long Audio
**What goes wrong:** `TaskCanceledException` or gateway timeout (524) for audio > 60 seconds
**Why it happens:** Default HttpClient timeout is 100 seconds; Whisper processing time scales with audio length
**How to avoid:** Set appropriate timeout (e.g., 2-3 minutes) or limit audio duration
**Warning signs:** Timeouts only on longer recordings

### Pitfall 5: Forgetting to Dispose MemoryStream
**What goes wrong:** Memory leak if transcription is called repeatedly
**Why it happens:** MemoryStream wrapping byte[] should be disposed after use
**How to avoid:** Use `using` statement
**Warning signs:** Memory growth over time in long sessions

## Code Examples

Verified patterns from official sources:

### Azure OpenAI Client Initialization
```csharp
// Source: Azure.AI.OpenAI quickstart docs
// https://learn.microsoft.com/en-us/azure/ai-foundry/openai/whisper-quickstart

// With API Key (current codebase uses this)
var endpoint = settings.AzureEndpoint;  // e.g., "https://myresource.openai.azure.com/"
var apiKey = CredentialService.LoadApiKey();
var deployment = settings.WhisperDeployment;  // e.g., "whisper"

var client = new AzureOpenAIClient(
    new Uri(endpoint),
    new AzureKeyCredential(apiKey));

var audioClient = client.GetAudioClient(deployment);
```

### Transcription with Options
```csharp
// Source: Azure.AI.OpenAI migration guide
// https://learn.microsoft.com/en-us/azure/ai-foundry/openai/how-to/dotnet-migration

var options = new AudioTranscriptionOptions
{
    ResponseFormat = AudioTranscriptionFormat.Text,  // Simple text, no metadata
    Language = "pt"  // Portuguese (PROJECT.md mentions pt-BR as initial language)
};

using var stream = new MemoryStream(audioBytes);
var result = await audioClient.TranscribeAudioAsync(stream, "audio.wav", options);

string transcribedText = result.Value.Text;
```

### Complete Transcription Flow
```csharp
// Source: Combination of Azure docs and codebase patterns

// In TrayApplicationContext.OnHotkeyReleased:
private async void OnHotkeyReleased(object? sender, EventArgs e)
{
    var audioData = _audioCaptureService.StopCapture();
    _trayIcon.Icon = _idleIcon;
    _trayIcon.Text = $"Coxixo - Press {_settings.HotkeyKey} to talk";

    if (audioData == null)
        return;  // Recording was discarded (too short)

    try
    {
        _trayIcon.Text = "Coxixo - Transcribing...";

        var text = await _transcriptionService.TranscribeAsync(audioData);

        if (!string.IsNullOrWhiteSpace(text))
        {
            Clipboard.SetText(text);
            // Success feedback (optional)
        }
        else
        {
            ShowNotification("No speech detected", ToolTipIcon.Info);
        }
    }
    catch (RequestFailedException ex) when (ex.Status == 401)
    {
        ShowNotification("Invalid API credentials. Check Settings.", ToolTipIcon.Error);
    }
    catch (Exception ex)
    {
        ShowNotification($"Transcription failed: {ex.Message}", ToolTipIcon.Warning);
    }
    finally
    {
        _trayIcon.Text = $"Coxixo - Press {_settings.HotkeyKey} to talk";
    }
}
```

### Error Response Handling
```csharp
// Source: Azure OpenAI reference docs
// Error response structure: { "error": { "code": "...", "message": "...", "type": "..." } }

try
{
    var result = await audioClient.TranscribeAudioAsync(stream, "audio.wav");
}
catch (RequestFailedException ex)
{
    var errorMessage = ex.Status switch
    {
        400 => "Invalid audio format or request",
        401 => "Invalid API key - check credentials",
        403 => "Access denied - check permissions",
        404 => "Whisper deployment not found - check deployment name",
        429 => "Rate limit exceeded - try again later",
        >= 500 => "Azure service error - try again",
        _ => $"API error: {ex.Message}"
    };

    ShowNotification(errorMessage, ToolTipIcon.Warning);
}
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Azure.AI.OpenAI 1.0 Beta (GetAudioTranscriptionAsync) | Azure.AI.OpenAI 2.x (AudioClient.TranscribeAudioAsync) | Dec 2024 | Simpler API, better typed responses |
| Manual api-version parameter | SDK handles version internally | 2024 | Less config needed |
| BinaryData.FromStream() | Direct Stream parameter | 2.0 SDK | Cleaner API |

**Deprecated/outdated:**
- `OpenAIClient.GetAudioTranscriptionAsync()` from 1.0 Beta - use `AudioClient.TranscribeAudioAsync()` instead
- Manual `AudioTranscriptionOptions.AudioData` property - pass Stream directly to method

## Open Questions

Things that couldn't be fully resolved:

1. **Exact timeout for Azure OpenAI Whisper**
   - What we know: Default is ~100 seconds; some users report 524 timeouts on longer audio
   - What's unclear: Official Azure timeout for Whisper specifically (varies by tier?)
   - Recommendation: Set 2-minute timeout; typical push-to-talk recordings are < 30 seconds

2. **Rate limit specifics for Azure Whisper**
   - What we know: Free tier has 3 RPM limit; paid tier can be increased via support
   - What's unclear: Exact limits for each tier without checking Azure portal
   - Recommendation: Implement 429 handling with exponential backoff; typical use won't hit limits

3. **SDK behavior with disposed stream**
   - What we know: SDK reads stream internally
   - What's unclear: Whether SDK copies data or streams directly to HTTP
   - Recommendation: Keep stream alive until TranscribeAudioAsync completes (use `await` properly)

## Sources

### Primary (HIGH confidence)
- [Azure OpenAI REST API Reference](https://learn.microsoft.com/en-us/azure/ai-services/openai/reference) - Endpoint format, request/response structure
- [Azure.AI.OpenAI Whisper Quickstart](https://learn.microsoft.com/en-us/azure/ai-foundry/openai/whisper-quickstart) - C# SDK usage
- [Azure.AI.OpenAI Migration Guide 1.0 to 2.0](https://learn.microsoft.com/en-us/azure/ai-foundry/openai/how-to/dotnet-migration) - Current API patterns
- [Azure.AI.OpenAI NuGet Package](https://www.nuget.org/packages/Azure.AI.OpenAI) - Version 2.1.0, dependencies
- [HttpClient Guidelines for .NET](https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/http/httpclient-guidelines) - HttpClient lifecycle
- [Clipboard.SetText Documentation](https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.clipboard.settext) - STA thread requirement

### Secondary (MEDIUM confidence)
- [OpenAI .NET GitHub Repository](https://github.com/openai/openai-dotnet) - AudioClient overloads
- [Polly GitHub Repository](https://github.com/App-vNext/Polly) - Retry patterns (not used but researched)
- [Azure Samples Whisper Processing Guide](https://github.com/Azure-Samples/openai/blob/main/Basic_Samples/Whisper/dotnet/csharp/Whisper_processing_guide.ipynb) - BinaryData patterns

### Tertiary (LOW confidence)
- [OpenAI Community Forums](https://community.openai.com/t/whisper-api-limits-transcriptions/167507) - Rate limit discussions (may not apply to Azure)
- Various Medium articles on HttpClient patterns - Cross-verified with official docs

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - Official Microsoft SDK, well-documented
- Architecture: HIGH - Patterns match existing codebase style
- Pitfalls: HIGH - Documented in SDK issues and migration guides
- Rate limits/timeouts: MEDIUM - Azure-specific details vary by subscription

**Research date:** 2026-01-18
**Valid until:** 2026-03-18 (60 days - SDK is stable, minor version updates expected)
