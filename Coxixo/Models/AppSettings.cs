using System.Windows.Forms;

namespace Coxixo.Models;

/// <summary>
/// Application settings that persist across app restarts.
/// Stored as JSON in %LOCALAPPDATA%\Coxixo\settings.json.
/// </summary>
public class AppSettings
{
    /// <summary>
    /// The key used for push-to-talk. Default is F8.
    /// </summary>
    public Keys HotkeyKey { get; set; } = Keys.F8;

    /// <summary>
    /// Azure OpenAI endpoint URL (e.g., https://xxx.openai.azure.com/)
    /// </summary>
    public string AzureEndpoint { get; set; } = "";

    /// <summary>
    /// Azure OpenAI Whisper deployment name.
    /// </summary>
    public string WhisperDeployment { get; set; } = "whisper";

    /// <summary>
    /// API version for Azure OpenAI (default matches current stable).
    /// </summary>
    public string ApiVersion { get; set; } = "2024-02-01";

    /// <summary>
    /// Whether to play audio feedback sounds when recording starts/stops.
    /// </summary>
    public bool AudioFeedbackEnabled { get; set; } = true;
}
