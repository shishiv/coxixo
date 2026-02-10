using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using Azure;
using Coxixo.Forms;
using Coxixo.Models;
using Coxixo.Services;

namespace Coxixo;

public class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _trayIcon;
    private readonly KeyboardHookService _hotkeyService;
    private readonly AudioCaptureService _audioCaptureService;
    private readonly AudioFeedbackService _audioFeedbackService;
    private readonly Icon _idleIcon;
    private readonly Icon _recordingIcon;
    private readonly Icon _recordingPulseIcon;
    private AppSettings _settings;
    private TranscriptionService? _transcriptionService;

    // Animation state for recording indicator
    private System.Windows.Forms.Timer? _animationTimer;
    private Icon[] _recordingFrames = null!;
    private int _currentFrame = 0;

    public TrayApplicationContext()
    {
        // Load settings first
        _settings = ConfigurationService.Load();

        // Load icons from embedded resources
        _idleIcon = LoadEmbeddedIcon("Coxixo.Resources.icon-idle.ico");
        _recordingIcon = LoadEmbeddedIcon("Coxixo.Resources.icon-recording.ico");
        _recordingPulseIcon = LoadEmbeddedIcon("Coxixo.Resources.icon-recording-pulse.ico");

        // Initialize recording animation frames
        _recordingFrames = new[] { _recordingIcon, _recordingPulseIcon };

        _trayIcon = new NotifyIcon
        {
            Icon = _idleIcon,
            Text = $"Coxixo - Press {_settings.Hotkey.ToDisplayString()} to talk",
            Visible = true,
            ContextMenuStrip = CreateContextMenu()
        };

        // Initialize audio feedback service
        _audioFeedbackService = new AudioFeedbackService();
        _audioFeedbackService.Enabled = _settings.AudioFeedbackEnabled;

        // Initialize transcription service (if credentials available)
        TryInitializeTranscriptionService();

        // Initialize audio capture service
        _audioCaptureService = new AudioCaptureService();
        _audioCaptureService.RecordingStarted += OnRecordingStarted;
        _audioCaptureService.RecordingStopped += OnRecordingStopped;
        _audioCaptureService.RecordingDiscarded += OnRecordingDiscarded;
        _audioCaptureService.CaptureError += OnCaptureError;

        // Initialize and start keyboard hook with configured key
        _hotkeyService = new KeyboardHookService();
        _hotkeyService.TargetKey = _settings.Hotkey.Key;
        _hotkeyService.HotkeyPressed += OnHotkeyPressed;
        _hotkeyService.HotkeyReleased += OnHotkeyReleased;
        _hotkeyService.Start();

        Application.ApplicationExit += OnApplicationExit;
    }

    private Icon LoadEmbeddedIcon(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            // Fallback to application icon
            return Icon.ExtractAssociatedIcon(Application.ExecutablePath)
                   ?? SystemIcons.Application;
        }
        return new Icon(stream);
    }

    private ContextMenuStrip CreateContextMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Settings...", null, OnSettingsClick);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, OnExitClick);
        return menu;
    }

    private void OnHotkeyPressed(object? sender, EventArgs e)
    {
        StartRecordingAnimation();
        _trayIcon.Text = "Coxixo - Recording...";
        _audioCaptureService.StartCapture();
    }

    private async void OnHotkeyReleased(object? sender, EventArgs e)
    {
        var audioData = _audioCaptureService.StopCapture();
        StopRecordingAnimation();

        if (audioData == null)
        {
            _trayIcon.Text = $"Coxixo - Press {_settings.Hotkey.ToDisplayString()} to talk";
            return;
        }

        Debug.WriteLine($"Captured {audioData.Length} bytes of audio ({_audioCaptureService.LastRecordingDuration.TotalSeconds:F1}s)");

        // Check if transcription service is available
        if (_transcriptionService == null)
        {
            ShowNotification("Configure API credentials in Settings", ToolTipIcon.Warning);
            _trayIcon.Text = $"Coxixo - Press {_settings.Hotkey.ToDisplayString()} to talk";
            return;
        }

        _trayIcon.Text = "Coxixo - Transcribing...";

        try
        {
            var text = await _transcriptionService.TranscribeWithRetryAsync(audioData);

            if (!string.IsNullOrWhiteSpace(text))
            {
                Clipboard.SetText(text);
                Debug.WriteLine($"Transcription copied to clipboard: {text}");
            }
            else
            {
                ShowNotification("No speech detected", ToolTipIcon.Info);
            }
        }
        catch (RequestFailedException ex)
        {
            var message = ex.Status switch
            {
                401 => "Invalid API credentials. Check Settings.",
                403 => "Access denied. Check API permissions.",
                404 => "Whisper deployment not found. Check deployment name.",
                429 => "Rate limit exceeded. Try again later.",
                >= 500 => "Azure service error. Try again.",
                _ => $"API error: {ex.Message}"
            };
            ShowNotification(message, ToolTipIcon.Error);
            Debug.WriteLine($"Transcription failed: {ex.Status} - {ex.Message}");
        }
        catch (Exception ex)
        {
            ShowNotification($"Transcription failed: {ex.Message}", ToolTipIcon.Error);
            Debug.WriteLine($"Transcription error: {ex}");
        }
        finally
        {
            _trayIcon.Text = $"Coxixo - Press {_settings.Hotkey.ToDisplayString()} to talk";
        }
    }

    private bool TryInitializeTranscriptionService()
    {
        var apiKey = CredentialService.LoadApiKey();
        if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(_settings.AzureEndpoint))
        {
            _transcriptionService = null;
            return false;
        }

        _transcriptionService?.Dispose();
        _transcriptionService = new TranscriptionService(
            _settings.AzureEndpoint,
            apiKey,
            _settings.WhisperDeployment);
        return true;
    }

    private void ShowNotification(string message, ToolTipIcon icon = ToolTipIcon.Warning)
    {
        _trayIcon.BalloonTipTitle = "Coxixo";
        _trayIcon.BalloonTipText = message;
        _trayIcon.BalloonTipIcon = icon;
        _trayIcon.ShowBalloonTip(3000);
    }

    #region Recording Animation

    private void StartRecordingAnimation()
    {
        _currentFrame = 0;
        _trayIcon.Icon = _recordingFrames[0];

        _animationTimer = new System.Windows.Forms.Timer();
        _animationTimer.Interval = 500; // 500ms pulse for visible animation
        _animationTimer.Tick += OnAnimationTick;
        _animationTimer.Start();
    }

    private void OnAnimationTick(object? sender, EventArgs e)
    {
        _currentFrame = (_currentFrame + 1) % _recordingFrames.Length;
        _trayIcon.Icon = _recordingFrames[_currentFrame];
    }

    private void StopRecordingAnimation()
    {
        _animationTimer?.Stop();
        _animationTimer?.Dispose();
        _animationTimer = null;
        _currentFrame = 0;
        _trayIcon.Icon = _idleIcon;
    }

    #endregion

    private void OnRecordingStarted(object? sender, EventArgs e)
    {
        Debug.WriteLine("Recording started");
        _audioFeedbackService.PlayStartBeep();
    }

    private void OnRecordingStopped(object? sender, EventArgs e)
    {
        Debug.WriteLine("Recording stopped - audio captured");
        _audioFeedbackService.PlayStopBeep();
    }

    private void OnRecordingDiscarded(object? sender, EventArgs e)
    {
        // No beep on RecordingDiscarded - silent discard keeps it non-intrusive
        Debug.WriteLine("Recording too short, discarded (no beep)");
    }

    private void OnCaptureError(object? sender, string message)
    {
        // Use NotifyIcon balloon for toast-like notification
        _trayIcon.BalloonTipTitle = "Coxixo - Microphone Error";
        _trayIcon.BalloonTipText = message;
        _trayIcon.BalloonTipIcon = ToolTipIcon.Warning;
        _trayIcon.ShowBalloonTip(5000);
    }

    private void OnSettingsClick(object? sender, EventArgs e)
    {
        using var settingsForm = new SettingsForm();

        // Temporarily unhook keyboard while settings is open
        // (so user can change hotkey without triggering recording)
        _hotkeyService.Stop();

        try
        {
            settingsForm.ShowDialog();

            if (settingsForm.DialogResult == DialogResult.OK)
            {
                // Reload settings
                _settings = ConfigurationService.Load();

                // Update hotkey
                _hotkeyService.TargetKey = _settings.Hotkey.Key;

                // Update tooltip
                _trayIcon.Text = $"Coxixo - Press {_settings.Hotkey.ToDisplayString()} to talk";

                // Reinitialize transcription service with new credentials
                TryInitializeTranscriptionService();
            }
        }
        finally
        {
            // Re-enable keyboard hook
            _hotkeyService.Start();
        }
    }

    private void OnExitClick(object? sender, EventArgs e)
    {
        Application.Exit();
    }

    private void OnApplicationExit(object? sender, EventArgs e)
    {
        CleanupTrayIcon();
    }

    private void CleanupTrayIcon()
    {
        _trayIcon.Visible = false;
        _trayIcon.Icon?.Dispose();
        _trayIcon.Dispose();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _animationTimer?.Stop();
            _animationTimer?.Dispose();
            _hotkeyService?.Dispose();
            _audioCaptureService?.Dispose();
            _audioFeedbackService?.Dispose();
            _transcriptionService?.Dispose();
            CleanupTrayIcon();
            _idleIcon?.Dispose();
            _recordingIcon?.Dispose();
            _recordingPulseIcon?.Dispose();
        }
        base.Dispose(disposing);
    }
}
