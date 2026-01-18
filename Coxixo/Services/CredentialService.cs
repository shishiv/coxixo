using System.Security.Cryptography;
using System.Text;

namespace Coxixo.Services;

/// <summary>
/// Stores and retrieves API credentials using Windows DPAPI encryption.
/// Credentials are encrypted per-user and only readable by the same Windows account.
/// </summary>
public static class CredentialService
{
    private static readonly string AppDataFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Coxixo"
    );

    private static readonly string CredentialsPath = Path.Combine(AppDataFolder, "credentials.dat");

    // Entropy adds additional protection - must be same for encrypt/decrypt
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("Coxixo.v1.2026.Entropy");

    /// <summary>
    /// Saves the API key encrypted with DPAPI.
    /// </summary>
    public static void SaveApiKey(string apiKey)
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            // Empty key - delete credentials file if exists
            if (File.Exists(CredentialsPath))
                File.Delete(CredentialsPath);
            return;
        }

        Directory.CreateDirectory(AppDataFolder);

        byte[] plaintext = Encoding.UTF8.GetBytes(apiKey);
        byte[] encrypted = ProtectedData.Protect(
            plaintext,
            Entropy,
            DataProtectionScope.CurrentUser
        );

        File.WriteAllBytes(CredentialsPath, encrypted);
    }

    /// <summary>
    /// Loads the API key, decrypting with DPAPI.
    /// Returns null if no credentials saved or decryption fails.
    /// </summary>
    public static string? LoadApiKey()
    {
        if (!File.Exists(CredentialsPath))
            return null;

        try
        {
            byte[] encrypted = File.ReadAllBytes(CredentialsPath);
            byte[] decrypted = ProtectedData.Unprotect(
                encrypted,
                Entropy,
                DataProtectionScope.CurrentUser
            );
            return Encoding.UTF8.GetString(decrypted);
        }
        catch (CryptographicException)
        {
            // Credentials corrupted or from different user account
            return null;
        }
    }

    /// <summary>
    /// Checks if credentials are stored.
    /// </summary>
    public static bool HasStoredCredentials() => File.Exists(CredentialsPath);

    /// <summary>
    /// Clears stored credentials.
    /// </summary>
    public static void ClearCredentials()
    {
        if (File.Exists(CredentialsPath))
            File.Delete(CredentialsPath);
    }
}
