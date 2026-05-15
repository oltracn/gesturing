using System.Drawing;
using System.Reflection;
using Gesturing.Models;
using Gesturing.UI;
using Microsoft.Win32;

namespace Gesturing.Services;

public class TrayManager : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly SettingsForm _settingsForm;
    private readonly ToolStripMenuItem _enableItem;
    private readonly ToolStripMenuItem _autoStartItem;
    private readonly AppConfig _config;
    private bool _isEnabled = true;

    public Action<bool>? OnToggleEnable { get; set; }

    public TrayManager(AppConfig config, Action? onConfigChanged)
    {
        _config = config;
        _settingsForm = new SettingsForm(config, onConfigChanged);
        // Force handle creation on UI thread so BeginInvoke works from background threads
        _ = _settingsForm.Handle;

        var contextMenu = new ContextMenuStrip();

        var settingsItem = new ToolStripMenuItem("设置", null, (s, e) => ShowSettings());
        _enableItem = new ToolStripMenuItem("启用", null, (s, e) => ToggleEnable())
        {
            CheckOnClick = true,
            Checked = _isEnabled
        };
        _autoStartItem = new ToolStripMenuItem("开机自启", null, (s, e) => ToggleAutoStart())
        {
            CheckOnClick = true,
            Checked = config.AutoStart
        };
        var exitItem = new ToolStripMenuItem("退出", null, (s, e) => Exit());

        contextMenu.Items.Add(settingsItem);
        contextMenu.Items.Add("-");
        contextMenu.Items.Add(_enableItem);
        contextMenu.Items.Add(_autoStartItem);
        contextMenu.Items.Add("-");
        contextMenu.Items.Add(exitItem);

        _notifyIcon = new NotifyIcon
        {
            Icon = LoadAppIcon(),
            Text = "Gesturing",
            ContextMenuStrip = contextMenu,
            Visible = true
        };

        _notifyIcon.DoubleClick += (s, e) => ShowSettings();

        // Sync registry with config on startup (handles manual registry edits or config changes)
        SyncAutoStartRegistry(config.AutoStart);
    }

    public void ShowSettings()
    {
        if (_settingsForm.Visible)
        {
            _settingsForm.Activate();
        }
        else
        {
            _settingsForm.Show();
        }
    }

    public void InvokeShowSettings()
    {
        if (_settingsForm.IsDisposed)
        {
            return;
        }

        if (_settingsForm.InvokeRequired)
        {
            _settingsForm.BeginInvoke(() => ShowSettings());
        }
        else
        {
            ShowSettings();
        }
    }

    private void ToggleEnable()
    {
        _isEnabled = !_isEnabled;
        _enableItem.Checked = _isEnabled;
        OnToggleEnable?.Invoke(_isEnabled);
    }

    private void ToggleAutoStart()
    {
        var enabled = !_autoStartItem.Checked;
        _autoStartItem.Checked = enabled;
        _config.AutoStart = enabled;
        ConfigManager.Save(_config);
        SyncAutoStartRegistry(enabled);
    }

    private static void SyncAutoStartRegistry(bool enabled)
    {
        const string runKey = "Software\\Microsoft\\Windows\\CurrentVersion\\Run";
        using var key = Registry.CurrentUser.OpenSubKey(runKey, true);
        if (key == null) return;

        if (enabled)
        {
            key.SetValue("Gesturing", Application.ExecutablePath);
        }
        else
        {
            key.DeleteValue("Gesturing", false);
        }
    }

    public void Exit()
    {
        _notifyIcon.Visible = false;
        _settingsForm.Dispose();
        Application.Exit();
    }

    public void Dispose()
    {
        _notifyIcon.Dispose();
        _settingsForm.Dispose();
    }

    private static Icon LoadAppIcon()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream("app.ico");
        if (stream != null)
            return new Icon(stream);
        return Icon.ExtractAssociatedIcon(Application.ExecutablePath)!;
    }
}
