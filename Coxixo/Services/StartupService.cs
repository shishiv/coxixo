using Microsoft.Win32;
using System.Windows.Forms;

namespace Coxixo.Services;

/// <summary>
/// Manages Windows startup registration via HKCU Run registry key.
/// </summary>
public static class StartupService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "Coxixo";

    /// <summary>
    /// Checks whether Coxixo is registered to start with Windows.
    /// </summary>
    /// <returns>True if registered and path matches current executable, false otherwise.</returns>
    public static bool IsEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
            if (key == null)
                return false;

            var value = key.GetValue(AppName) as string;
            if (value == null)
                return false;

            var expectedPath = $"\"{Application.ExecutablePath}\"";
            return string.Equals(value, expectedPath, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            // Safe default on any registry access error
            return false;
        }
    }

    /// <summary>
    /// Registers Coxixo to start with Windows.
    /// </summary>
    /// <exception cref="InvalidOperationException">If Run registry key is missing (system corruption).</exception>
    /// <exception cref="UnauthorizedAccessException">If user lacks permission to modify registry.</exception>
    public static void Enable()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
        if (key == null)
            throw new InvalidOperationException("Windows Run registry key is missing. System may be corrupted.");

        var path = $"\"{Application.ExecutablePath}\"";
        key.SetValue(AppName, path, RegistryValueKind.String);
    }

    /// <summary>
    /// Removes Coxixo from Windows startup registration.
    /// </summary>
    public static void Disable()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
            if (key == null)
                return; // Nothing to do

            key.DeleteValue(AppName, throwOnMissingValue: false);
        }
        catch (UnauthorizedAccessException)
        {
            // User intent is "not started" - permission failure achieves same outcome
            // Silently succeed
        }
    }
}
