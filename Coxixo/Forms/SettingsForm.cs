using System.Diagnostics;
using System.Net.Http;
using System.Runtime.InteropServices;
using Coxixo.Controls;
using Coxixo.Models;
using Coxixo.Services;

namespace Coxixo.Forms;

public partial class SettingsForm : Form
{
    private static class DarkTheme
    {
        public static readonly Color Background = Color.FromArgb(0x1E, 0x1E, 0x1E);
        public static readonly Color Surface = Color.FromArgb(0x25, 0x25, 0x26);
        public static readonly Color Text = Color.White;
        public static readonly Color TextMuted = Color.FromArgb(0x80, 0x80, 0x80);
        public static readonly Color Border = Color.FromArgb(0x3C, 0x3C, 0x3C);
        public static readonly Color Primary = Color.FromArgb(0x00, 0x78, 0xD4);
        public static readonly Color Success = Color.FromArgb(0x00, 0xCC, 0x6A);
        public static readonly Color Error = Color.FromArgb(0xE8, 0x11, 0x23);
    }

    private AppSettings _settings = null!; // Initialized in LoadSettings() called from constructor

    public SettingsForm()
    {
        InitializeComponent();
        SetupForm();
        LoadSettings();
        _ = TestConnectionAsync(); // Fire and forget initial check
    }

    private void SetupForm()
    {
        // Form properties
        this.Text = "Coxixo Settings";
        this.Size = new Size(320, 434);
        this.FormBorderStyle = FormBorderStyle.FixedSingle;
        this.MaximizeBox = false;
        this.StartPosition = FormStartPosition.CenterScreen;
        this.Font = new Font("Segoe UI", 9F);

        ApplyDarkTheme(this);

        // Wire up validation message display
        hotkeyPicker.ValidationChanged += OnHotkeyValidationChanged;
    }

    private void ApplyDarkTheme(Control control)
    {
        control.BackColor = DarkTheme.Background;
        control.ForeColor = DarkTheme.Text;

        foreach (Control child in control.Controls)
        {
            if (child is HotkeyPickerControl)
            {
                // HotkeyPickerControl handles its own painting - skip theme override
                continue;
            }
            else if (child is TextBox tb)
            {
                tb.BackColor = DarkTheme.Surface;
                tb.ForeColor = DarkTheme.Text;
                tb.BorderStyle = BorderStyle.FixedSingle;
            }
            else if (child is Button btn)
            {
                btn.FlatStyle = FlatStyle.Flat;
                btn.FlatAppearance.BorderColor = DarkTheme.Border;
                if (btn.Name == "btnSave")
                {
                    btn.BackColor = DarkTheme.Primary;
                    btn.ForeColor = Color.White;
                }
                else
                {
                    btn.BackColor = DarkTheme.Surface;
                }
            }
            else if (child is Panel panel)
            {
                panel.BackColor = DarkTheme.Surface;
            }
            ApplyDarkTheme(child);
        }
    }

    private void LoadSettings()
    {
        _settings = ConfigurationService.Load();
        hotkeyPicker.SelectedCombo = _settings.Hotkey;
        txtEndpoint.Text = _settings.AzureEndpoint;
        txtApiKey.Text = CredentialService.LoadApiKey() ?? "";
        txtDeployment.Text = _settings.WhisperDeployment;
    }

    private void OnHotkeyValidationChanged(object? sender, EventArgs e)
    {
        if (hotkeyPicker.ValidationMessage != null)
        {
            lblHotkeyMessage.Text = hotkeyPicker.ValidationMessage;
            lblHotkeyMessage.ForeColor = hotkeyPicker.ValidationSeverity == "error"
                ? DarkTheme.Error
                : Color.FromArgb(0xFF, 0xB9, 0x00); // Warning yellow
            lblHotkeyMessage.Visible = true;
        }
        else
        {
            lblHotkeyMessage.Visible = false;
        }
    }

    private async Task TestConnectionAsync()
    {
        var endpoint = txtEndpoint.Text.Trim();
        var apiKey = txtApiKey.Text.Trim();

        if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey))
        {
            UpdateConnectionStatus(false, 0, "Configure credentials");
            return;
        }

        UpdateConnectionStatus(null, 0, "Testing...");

        var stopwatch = Stopwatch.StartNew();
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("api-key", apiKey);
            client.Timeout = TimeSpan.FromSeconds(5);

            var url = $"{endpoint.TrimEnd('/')}/openai/deployments?api-version=2024-02-01";
            var response = await client.GetAsync(url);
            stopwatch.Stop();

            var latency = (int)stopwatch.ElapsedMilliseconds;
            var connected = response.IsSuccessStatusCode ||
                           response.StatusCode == System.Net.HttpStatusCode.NotFound;

            UpdateConnectionStatus(connected, latency,
                connected ? "Connected" : $"Error: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            UpdateConnectionStatus(false, 0, $"Error: {ex.Message}");
        }
    }

    private void UpdateConnectionStatus(bool? connected, int latencyMs, string message)
    {
        if (InvokeRequired)
        {
            Invoke(() => UpdateConnectionStatus(connected, latencyMs, message));
            return;
        }

        if (connected == true)
        {
            pnlStatus.BackColor = Color.FromArgb(20, 0, 80, 0);
            lblStatusIcon.ForeColor = DarkTheme.Success;
            lblStatusIcon.Text = "\u25CF";
            lblStatusText.Text = "Whisper API Connected";
            lblStatusText.ForeColor = DarkTheme.Success;
            lblLatency.Text = $"Latency: {latencyMs}ms";
            lblLatency.Visible = true;
        }
        else if (connected == false)
        {
            pnlStatus.BackColor = Color.FromArgb(20, 80, 0, 0);
            lblStatusIcon.ForeColor = DarkTheme.Error;
            lblStatusIcon.Text = "\u25CF";
            lblStatusText.Text = message;
            lblStatusText.ForeColor = DarkTheme.Error;
            lblLatency.Visible = false;
        }
        else // null = testing
        {
            pnlStatus.BackColor = DarkTheme.Surface;
            lblStatusIcon.ForeColor = DarkTheme.TextMuted;
            lblStatusIcon.Text = "\u25CB";
            lblStatusText.Text = message;
            lblStatusText.ForeColor = DarkTheme.TextMuted;
            lblLatency.Visible = false;
        }
    }

    private void BtnTestConnection_Click(object? sender, EventArgs e)
    {
        _ = TestConnectionAsync();
    }

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        var combo = hotkeyPicker.SelectedCombo ?? HotkeyCombo.Default();

        // Re-validate (in case state changed)
        var validation = HotkeyValidator.Validate(combo);
        if (validation.Result == HotkeyValidator.ValidationResult.Reserved)
        {
            lblHotkeyMessage.Text = validation.Message ?? "This combination is reserved.";
            lblHotkeyMessage.ForeColor = DarkTheme.Error;
            lblHotkeyMessage.Visible = true;
            return; // Block save
        }

        // Probe for conflicts using RegisterHotKey
        if (!ProbeHotkeyConflict(combo))
        {
            lblHotkeyMessage.Text = "This hotkey is already in use by another application. Choose a different combination.";
            lblHotkeyMessage.ForeColor = DarkTheme.Error;
            lblHotkeyMessage.Visible = true;
            return; // Block save
        }

        // Update settings
        _settings.Hotkey = combo;
        _settings.AzureEndpoint = txtEndpoint.Text.Trim();
        _settings.WhisperDeployment = txtDeployment.Text.Trim();

        // Save settings
        ConfigurationService.Save(_settings);

        // Save API key securely
        var apiKey = txtApiKey.Text.Trim();
        if (!string.IsNullOrEmpty(apiKey))
        {
            CredentialService.SaveApiKey(apiKey);
        }

        this.DialogResult = DialogResult.OK;
        this.Close();
    }

    private void BtnCancel_Click(object? sender, EventArgs e)
    {
        this.DialogResult = DialogResult.Cancel;
        this.Close();
    }

    #region RegisterHotKey Conflict Detection

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private const uint MOD_ALT = 0x0001;
    private const uint MOD_CONTROL = 0x0002;
    private const uint MOD_SHIFT = 0x0004;

    /// <summary>
    /// Probes whether the hotkey combination is available by temporarily registering it.
    /// Returns true if available, false if conflicting with another app.
    /// </summary>
    private bool ProbeHotkeyConflict(HotkeyCombo combo)
    {
        // RegisterHotKey requires at least one modifier for non-F-key/non-special keys
        // For bare keys (no modifiers), skip probe — low-level hooks don't conflict with RegisterHotKey
        if (!combo.HasModifiers)
            return true;

        uint modifiers = 0;
        if (combo.Ctrl) modifiers |= MOD_CONTROL;
        if (combo.Alt) modifiers |= MOD_ALT;
        if (combo.Shift) modifiers |= MOD_SHIFT;

        bool registered = RegisterHotKey(this.Handle, 0x7FFF, modifiers, (uint)combo.Key);
        if (registered)
        {
            UnregisterHotKey(this.Handle, 0x7FFF);
            return true;
        }
        return false;
    }

    #endregion
}
