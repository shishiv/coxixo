using System.Diagnostics;
using System.Reflection;
using NAudio.Wave;

namespace Coxixo.Services;

/// <summary>
/// Provides audio feedback sounds (beeps) for recording events.
/// Uses walkie-talkie style chirps: ascending for start, descending for stop.
/// </summary>
public sealed class AudioFeedbackService : IDisposable
{
    private readonly byte[] _startBeepData;
    private readonly byte[] _stopBeepData;
    private WaveOutEvent? _waveOut;
    private WaveFileReader? _currentReader;
    private MemoryStream? _currentStream;
    private bool _enabled = true;
    private bool _disposed;

    /// <summary>
    /// Gets or sets whether audio feedback is enabled.
    /// When disabled, PlayStartBeep and PlayStopBeep do nothing.
    /// </summary>
    public bool Enabled
    {
        get => _enabled;
        set => _enabled = value;
    }

    public AudioFeedbackService()
    {
        _startBeepData = LoadEmbeddedResource("Coxixo.Resources.beep-start.wav");
        _stopBeepData = LoadEmbeddedResource("Coxixo.Resources.beep-stop.wav");
    }

    /// <summary>
    /// Plays the ascending chirp sound for recording start.
    /// </summary>
    public void PlayStartBeep()
    {
        if (!_enabled) return;
        PlaySound(_startBeepData);
    }

    /// <summary>
    /// Plays the descending chirp sound for recording stop.
    /// </summary>
    public void PlayStopBeep()
    {
        if (!_enabled) return;
        PlaySound(_stopBeepData);
    }

    private void PlaySound(byte[] wavData)
    {
        if (wavData.Length == 0) return;

        try
        {
            // Clean up any previous playback
            CleanupCurrentPlayback();

            // Create new resources for this playback
            _currentStream = new MemoryStream(wavData);
            _currentReader = new WaveFileReader(_currentStream);
            _waveOut = new WaveOutEvent();

            _waveOut.Init(_currentReader);
            _waveOut.Play();

            // WaveOutEvent plays asynchronously, sound will complete naturally
            // Resources cleaned up on next play or dispose
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Audio feedback error: {ex.Message}");
            // Silently fail - audio feedback is nice-to-have
            CleanupCurrentPlayback();
        }
    }

    private void CleanupCurrentPlayback()
    {
        try
        {
            _waveOut?.Stop();
            _waveOut?.Dispose();
            _currentReader?.Dispose();
            _currentStream?.Dispose();
        }
        catch
        {
            // Ignore cleanup errors
        }
        finally
        {
            _waveOut = null;
            _currentReader = null;
            _currentStream = null;
        }
    }

    private static byte[] LoadEmbeddedResource(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            Debug.WriteLine($"Warning: Embedded resource '{resourceName}' not found");
            return Array.Empty<byte>();
        }

        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        CleanupCurrentPlayback();
    }
}
