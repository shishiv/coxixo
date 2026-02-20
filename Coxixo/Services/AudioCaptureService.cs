using System.Diagnostics;
using NAudio.Wave;

namespace Coxixo.Services;

/// <summary>
/// Captures audio from the default system microphone and encodes it as 16kHz mono WAV
/// suitable for Whisper API. Supports minimum duration threshold to filter accidental taps.
/// </summary>
public sealed class AudioCaptureService : IDisposable
{
    private const int SampleRate = 16000;
    private const int BitsPerSample = 16;
    private const int Channels = 1;
    private const int MinDurationMs = 500;

    private WaveInEvent? _waveIn;
    private MemoryStream? _buffer;
    private WaveFileWriter? _writer;
    private DateTime _recordingStart;
    private bool _isRecording;
    private bool _disposed;

    /// <summary>
    /// Fired when recording starts successfully.
    /// </summary>
    public event EventHandler? RecordingStarted;

    /// <summary>
    /// Fired when recording stops and audio was captured (duration >= minimum).
    /// </summary>
    public event EventHandler? RecordingStopped;

    /// <summary>
    /// Fired when recording was too short and discarded.
    /// </summary>
    public event EventHandler? RecordingDiscarded;

    /// <summary>
    /// Fired when microphone capture fails (permission denied, device not found, etc.).
    /// Event argument contains the error message.
    /// </summary>
    public event EventHandler<string>? CaptureError;

    /// <summary>
    /// Gets whether audio is currently being recorded.
    /// </summary>
    public bool IsRecording => _isRecording;

    /// <summary>
    /// Gets the duration of the last recording (whether captured or discarded).
    /// </summary>
    public TimeSpan LastRecordingDuration { get; private set; }

    /// <summary>
    /// Starts capturing audio from the specified microphone.
    /// </summary>
    /// <param name="deviceNumber">Device number (0-based index), or null for system default.</param>
    public void StartCapture(int? deviceNumber = null)
    {
        if (_isRecording)
            return;

        try
        {
            // Create fresh buffer and writer for each recording
            _buffer = new MemoryStream();
            var waveFormat = new WaveFormat(SampleRate, BitsPerSample, Channels);
            _writer = new WaveFileWriter(_buffer, waveFormat);

            _waveIn = new WaveInEvent
            {
                DeviceNumber = deviceNumber ?? 0,
                WaveFormat = waveFormat,
                BufferMilliseconds = 50  // Low latency buffer
            };

            _waveIn.DataAvailable += OnDataAvailable;
            _waveIn.RecordingStopped += OnRecordingStoppedInternal;

            _recordingStart = DateTime.UtcNow;
            _waveIn.StartRecording();
            _isRecording = true;

            RecordingStarted?.Invoke(this, EventArgs.Empty);
        }
        catch (NAudio.MmException ex)
        {
            // NAudio-specific exceptions for device errors
            CleanupRecording();
            var message = ex.Result switch
            {
                NAudio.MmResult.BadDeviceId => "Selected microphone not found. Falling back to default device.",
                NAudio.MmResult.NoDriver => "No audio driver installed.",
                NAudio.MmResult.NotEnabled => "Microphone access denied. Check Windows privacy settings.",
                _ => $"Microphone error: {ex.Message}"
            };
            CaptureError?.Invoke(this, message);

            // Fallback: if specific device failed, retry with default device
            if (ex.Result == NAudio.MmResult.BadDeviceId && deviceNumber != null)
            {
                StartCapture(null);
            }
        }
        catch (Exception ex)
        {
            CleanupRecording();
            CaptureError?.Invoke(this, $"Microphone access failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Stops recording and returns the captured audio as WAV bytes.
    /// Returns null if the recording was too short (below minimum duration threshold).
    /// </summary>
    /// <returns>WAV audio bytes, or null if recording was discarded.</returns>
    public byte[]? StopCapture()
    {
        if (!_isRecording)
            return null;

        _isRecording = false;
        LastRecordingDuration = DateTime.UtcNow - _recordingStart;

        try
        {
            _waveIn?.StopRecording();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error stopping recording: {ex.Message}");
        }

        // Check minimum duration
        if (LastRecordingDuration.TotalMilliseconds < MinDurationMs)
        {
            CleanupRecording();
            RecordingDiscarded?.Invoke(this, EventArgs.Empty);
            return null;
        }

        // Finalize WAV file and get bytes
        byte[]? audioData = null;
        try
        {
            if (_writer != null && _buffer != null)
            {
                // Flush and close writer to finalize WAV headers
                _writer.Flush();
                _writer.Dispose();
                _writer = null;

                audioData = _buffer.ToArray();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error finalizing audio: {ex.Message}");
        }

        CleanupRecording();

        if (audioData != null && audioData.Length > 44)  // More than just WAV header
        {
            RecordingStopped?.Invoke(this, EventArgs.Empty);
            return audioData;
        }

        RecordingDiscarded?.Invoke(this, EventArgs.Empty);
        return null;
    }

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        if (_writer != null && e.BytesRecorded > 0)
        {
            _writer.Write(e.Buffer, 0, e.BytesRecorded);
        }
    }

    private void OnRecordingStoppedInternal(object? sender, StoppedEventArgs e)
    {
        if (e.Exception != null)
        {
            Debug.WriteLine($"Recording stopped with error: {e.Exception.Message}");
        }
    }

    private void CleanupRecording()
    {
        try
        {
            if (_waveIn != null)
            {
                _waveIn.DataAvailable -= OnDataAvailable;
                _waveIn.RecordingStopped -= OnRecordingStoppedInternal;
                _waveIn.Dispose();
                _waveIn = null;
            }

            if (_writer != null)
            {
                _writer.Dispose();
                _writer = null;
            }

            if (_buffer != null)
            {
                _buffer.Dispose();
                _buffer = null;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error during cleanup: {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_isRecording)
        {
            _isRecording = false;
            try { _waveIn?.StopRecording(); } catch { }
        }

        CleanupRecording();
    }
}
