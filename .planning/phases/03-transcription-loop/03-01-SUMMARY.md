---
phase: 03
plan: 01
subsystem: transcription
tags: [azure, openai, whisper, api, retry]

dependency-graph:
  requires:
    - 02: "Audio capture provides WAV bytes"
  provides:
    - "TranscriptionService for Azure Whisper API"
    - "Retry logic for transient failures"
  affects:
    - 03-02: "Integration loop wires TranscriptionService to audio capture"

tech-stack:
  added:
    - Azure.AI.OpenAI 2.1.0
    - OpenAI SDK (transitive)
  patterns:
    - Instance service with IDisposable (holds SDK clients)
    - Exponential backoff retry for transient errors

key-files:
  created:
    - Coxixo/Services/TranscriptionService.cs
  modified:
    - Coxixo/Coxixo.csproj

decisions:
  - id: instance-service-pattern
    choice: "Instance class with IDisposable, not static"
    rationale: "Holds AzureOpenAIClient and AudioClient instances that need disposal"
  - id: portuguese-language
    choice: "Hardcoded Language = 'pt' in transcription options"
    rationale: "Per PROJECT.md, primary use case is Portuguese dictation"
  - id: retry-transient-only
    choice: "Retry only on 5xx, 429, 408 status codes"
    rationale: "These are transient; 4xx errors are client issues that won't resolve on retry"

metrics:
  duration: "~3 min"
  completed: "2026-01-18"
---

# Phase 03 Plan 01: Whisper Service Summary

**One-liner:** Azure Whisper transcription client with exponential backoff retry for transient failures (5xx/429/408).

## What Was Built

### TranscriptionService.cs (107 lines)

A focused service that wraps Azure OpenAI's Whisper API:

**Constructor:**
- Takes endpoint, apiKey, deployment parameters
- Creates `AzureOpenAIClient` with `AzureKeyCredential`
- Gets `AudioClient` for the specified Whisper deployment

**TranscribeAsync:**
- Accepts WAV audio bytes, returns transcribed text
- Configures Portuguese language (`Language = "pt"`)
- Uses `AudioTranscriptionFormat.Text` for plain text output

**TranscribeWithRetryAsync:**
- Wraps TranscribeAsync with retry logic
- Exponential backoff: 1s, 2s delays
- Only retries transient errors: 5xx server errors, 429 rate limit, 408 timeout
- Default 2 retries (configurable)

**Disposal:**
- Implements IDisposable to clean up SDK clients

### Package Addition

Added `Azure.AI.OpenAI` version 2.1.0 which brings:
- Azure.Core 1.44.1
- OpenAI 2.1.0 (transitive)
- System.ClientModel 1.2.1

## Key Implementation Details

```csharp
// Service initialization
_client = new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
_audioClient = _client.GetAudioClient(deployment);

// Transcription with retry
for (int attempt = 0; attempt <= maxRetries; attempt++)
{
    try { return await TranscribeAsync(audioData, ct); }
    catch (RequestFailedException ex) when (IsTransient(ex) && attempt < maxRetries)
    {
        var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
        await Task.Delay(delay, ct);
    }
}

// Transient error detection
private static bool IsTransient(RequestFailedException ex)
    => ex.Status >= 500 || ex.Status == 408 || ex.Status == 429;
```

## Deviations from Plan

None - plan executed exactly as written.

## Verification Results

| Check | Result |
|-------|--------|
| Project builds | Pass |
| TranscriptionService.cs exists | Pass (107 lines) |
| Contains AzureOpenAIClient | Pass |
| Contains AudioClient | Pass |
| Contains TranscribeAsync | Pass |
| Contains TranscribeWithRetryAsync | Pass |
| Azure.AI.OpenAI 2.1.0 in csproj | Pass |

## Commits

| Hash | Type | Description |
|------|------|-------------|
| 466dc0b | chore | Add Azure.AI.OpenAI SDK package |
| 11c7b32 | feat | Create TranscriptionService for Azure Whisper API |

## Next Phase Readiness

**Ready for 03-02:** TranscriptionService is complete and ready to be wired into TrayApplicationContext.

**Integration points:**
- Constructor needs: endpoint, apiKey (from CredentialService), deployment (from AppSettings)
- Call TranscribeWithRetryAsync with audio bytes from AudioCaptureService
- Handle null/empty return for silence
- Dispose service when app exits

**No blockers.**
