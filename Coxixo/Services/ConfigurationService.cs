using System.Text.Json;
using Coxixo.Models;

namespace Coxixo.Services;

/// <summary>
/// Manages loading and saving application settings to JSON file.
/// Settings are stored in %LOCALAPPDATA%\Coxixo\settings.json.
/// </summary>
public static class ConfigurationService
{
    private static readonly string AppDataFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Coxixo"
    );

    private static readonly string SettingsPath = Path.Combine(AppDataFolder, "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Loads settings from the JSON file.
    /// Returns default settings if file doesn't exist or is corrupted.
    /// </summary>
    public static AppSettings Load()
    {
        if (!File.Exists(SettingsPath))
            return new AppSettings();

        try
        {
            var json = File.ReadAllText(SettingsPath);
            return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions)
                   ?? new AppSettings();
        }
        catch (Exception)
        {
            // Corrupted file - return defaults
            return new AppSettings();
        }
    }

    /// <summary>
    /// Saves settings to the JSON file.
    /// Creates the app data folder if it doesn't exist.
    /// </summary>
    public static void Save(AppSettings settings)
    {
        Directory.CreateDirectory(AppDataFolder);
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(SettingsPath, json);
    }

    /// <summary>
    /// Gets the path where settings are stored (for diagnostics).
    /// </summary>
    public static string GetSettingsPath() => SettingsPath;
}
