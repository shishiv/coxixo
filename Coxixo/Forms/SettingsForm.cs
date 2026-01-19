using System.Diagnostics;
using System.Net.Http;
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

    private Keys _selectedKey;
    private AppSettings _settings = null!; // Initialized in LoadSettings() called from constructor
    private bool _isCapturingHotkey = false;

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
        this.Size = new Size(320, 420);
        this.FormBorderStyle = FormBorderStyle.FixedSingle;
        this.MaximizeBox = false;
        this.StartPosition = FormStartPosition.CenterScreen;
        this.Font = new Font("Segoe UI", 9F);

        ApplyDarkTheme(this);
    }

    private void ApplyDarkTheme(Control control)
    {
        control.BackColor = DarkTheme.Background;
        control.ForeColor = DarkTheme.Text;

        foreach (Control child in control.Controls)
        {
            if (child is TextBox tb)
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
        _selectedKey = _settings.HotkeyKey;

        txtHotkey.Text = _selectedKey.ToString();
        txtEndpoint.Text = _settings.AzureEndpoint;
        txtApiKey.Text = CredentialService.LoadApiKey() ?? "";
        txtDeployment.Text = _settings.WhisperDeployment;
    }

    private void TxtHotkey_Enter(object? sender, EventArgs e)
    {
        _isCapturingHotkey = true;
        txtHotkey.Text = "Press a key...";
    }

    private void TxtHotkey_Leave(object? sender, EventArgs e)
    {
        _isCapturingHotkey = false;
        txtHotkey.Text = _selectedKey.ToString();
    }

    private void TxtHotkey_KeyDown(object? sender, KeyEventArgs e)
    {
        if (!_isCapturingHotkey) return;

        e.Handled = true;
        e.SuppressKeyPress = true;

        // Ignore modifier-only presses
        if (e.KeyCode == Keys.ControlKey || e.KeyCode == Keys.ShiftKey ||
            e.KeyCode == Keys.Menu || e.KeyCode == Keys.LWin || e.KeyCode == Keys.RWin)
            return;

        _selectedKey = e.KeyCode;
        txtHotkey.Text = _selectedKey.ToString();
        _isCapturingHotkey = false;
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        // Allow Tab and other navigation keys to be captured as hotkey
        if (_isCapturingHotkey && txtHotkey.Focused)
        {
            var key = keyData & Keys.KeyCode;
            if (key != Keys.None && key != Keys.ControlKey && key != Keys.ShiftKey && key != Keys.Menu)
            {
                _selectedKey = key;
                txtHotkey.Text = _selectedKey.ToString();
                _isCapturingHotkey = false;
                return true;
            }
        }
        return base.ProcessCmdKey(ref msg, keyData);
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
        // Update settings
        _settings.HotkeyKey = _selectedKey;
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
}
