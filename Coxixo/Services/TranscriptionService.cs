using Azure;
using Azure.AI.OpenAI;
using OpenAI.Audio;

namespace Coxixo.Services;

/// <summary>
/// Transcribes audio to text using Azure OpenAI Whisper API.
/// Includes retry logic for transient failures.
/// </summary>
public sealed class TranscriptionService : IDisposable
{
    private readonly AzureOpenAIClient _client;
    private readonly AudioClient _audioClient;
    private readonly string? _languageCode;
    private bool _disposed;

    /// <summary>
    /// Creates a new TranscriptionService.
    /// </summary>
    /// <param name="endpoint">Azure OpenAI endpoint URL (e.g., https://xxx.openai.azure.com/)</param>
    /// <param name="apiKey">Azure OpenAI API key</param>
    /// <param name="deployment">Whisper deployment name</param>
    /// <param name="languageCode">ISO 639-1 language code (e.g., "pt", "en"). Null for auto-detect.</param>
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
        _languageCode = languageCode;
    }

    /// <summary>
    /// Transcribes audio data to text.
    /// </summary>
    /// <param name="audioData">WAV audio bytes (16kHz mono recommended)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Transcribed text, or null/empty for silence</returns>
    public async Task<string?> TranscribeAsync(byte[] audioData, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (audioData == null || audioData.Length == 0)
            return null;

        using var stream = new MemoryStream(audioData);

        var options = new AudioTranscriptionOptions
        {
            ResponseFormat = AudioTranscriptionFormat.Text,
            Language = _languageCode
        };

        var result = await _audioClient.TranscribeAudioAsync(stream, "audio.wav", options, ct);
        return result.Value.Text;
    }

    /// <summary>
    /// Transcribes audio data with retry for transient failures.
    /// </summary>
    /// <param name="audioData">WAV audio bytes (16kHz mono recommended)</param>
    /// <param name="maxRetries">Maximum retry attempts (default 2)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Transcribed text, or null/empty for silence</returns>
    public async Task<string?> TranscribeWithRetryAsync(byte[] audioData, int maxRetries = 2, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                return await TranscribeAsync(audioData, ct);
            }
            catch (RequestFailedException ex) when (IsTransient(ex) && attempt < maxRetries)
            {
                // Exponential backoff: 1s, 2s, 4s...
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                await Task.Delay(delay, ct);
            }
        }

        // Should not reach here, but satisfy compiler
        return await TranscribeAsync(audioData, ct);
    }

    /// <summary>
    /// Determines if an exception is transient and worth retrying.
    /// </summary>
    private static bool IsTransient(RequestFailedException ex)
    {
        // Transient: 5xx server errors, 408 timeout, 429 rate limit
        return ex.Status >= 500 || ex.Status == 408 || ex.Status == 429;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        // AzureOpenAIClient implements IDisposable
        (_client as IDisposable)?.Dispose();
    }
}
