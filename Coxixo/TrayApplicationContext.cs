using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using Coxixo.Models;
using Coxixo.Services;

namespace Coxixo;

public class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _trayIcon;
    private readonly KeyboardHookService _hotkeyService;
    private readonly Icon _idleIcon;
    private readonly Icon _recordingIcon;
    private readonly AppSettings _settings;

    public TrayApplicationContext()
    {
        // Load settings first
        _settings = ConfigurationService.Load();

        // Load icons from embedded resources
        _idleIcon = LoadEmbeddedIcon("Coxixo.Resources.icon-idle.ico");
        _recordingIcon = LoadEmbeddedIcon("Coxixo.Resources.icon-recording.ico");

        _trayIcon = new NotifyIcon
        {
            Icon = _idleIcon,
            Text = $"Coxixo - Press {_settings.HotkeyKey} to talk",
            Visible = true,
            ContextMenuStrip = CreateContextMenu()
        };

        // Initialize and start keyboard hook with configured key
        _hotkeyService = new KeyboardHookService();
        _hotkeyService.TargetKey = _settings.HotkeyKey;
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
        _trayIcon.Icon = _recordingIcon;
        _trayIcon.Text = "Coxixo - Recording...";
    }

    private void OnHotkeyReleased(object? sender, EventArgs e)
    {
        _trayIcon.Icon = _idleIcon;
        _trayIcon.Text = $"Coxixo - Press {_settings.HotkeyKey} to talk";
    }

    private void OnSettingsClick(object? sender, EventArgs e)
    {
        // Placeholder - will show settings in Phase 4
        MessageBox.Show("Settings coming soon", "Coxixo",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            _hotkeyService?.Dispose();
            CleanupTrayIcon();
            _idleIcon?.Dispose();
            _recordingIcon?.Dispose();
        }
        base.Dispose(disposing);
    }
}
