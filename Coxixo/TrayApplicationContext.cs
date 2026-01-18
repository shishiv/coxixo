using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace Coxixo;

public class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _trayIcon;

    public TrayApplicationContext()
    {
        // Load icon from embedded resource
        var icon = LoadEmbeddedIcon("Coxixo.Resources.icon-idle.ico");

        _trayIcon = new NotifyIcon
        {
            Icon = icon,
            Text = "Coxixo - Push to Talk",
            Visible = true,
            ContextMenuStrip = CreateContextMenu()
        };

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
            CleanupTrayIcon();
        }
        base.Dispose(disposing);
    }
}
