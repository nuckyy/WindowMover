using System.Reflection;

namespace WindowMover;

internal sealed class TrayApplicationContext : ApplicationContext
{
    private readonly SettingsService _settingsService = new();
    private readonly WindowMoverService _windowMover = new();
    private readonly HotkeyWindow _hotkeyWindow = new();
    private readonly NotifyIcon _notifyIcon;
    private readonly ToolStripMenuItem _moveItem;
    private readonly ToolStripMenuItem _enabledItem;
    private readonly ToolStripMenuItem _settingsItem;
    private readonly ToolStripMenuItem _aboutItem;
    private readonly ToolStripMenuItem _exitItem;
    private AppSettings _settings;
    private bool _moving;
    private bool _exiting;

    public TrayApplicationContext()
    {
        _settings = _settingsService.Load();
        Localization.ApplyLanguage(_settings.Language);

        _moveItem = new ToolStripMenuItem();
        _moveItem.Click += (_, _) => MoveForegroundWindow();

        _enabledItem = new ToolStripMenuItem
        {
            CheckOnClick = true,
            Checked = _settings.Enabled
        };
        _enabledItem.CheckedChanged += EnabledItemCheckedChanged;

        _settingsItem = new ToolStripMenuItem();
        _settingsItem.Click += (_, _) => ShowSettings();

        _aboutItem = new ToolStripMenuItem();
        _aboutItem.Click += (_, _) => ShowAbout();

        _exitItem = new ToolStripMenuItem();
        _exitItem.Click += (_, _) => ExitThread();

        var menu = new ContextMenuStrip();
        menu.Items.AddRange(
        [
            _moveItem,
            _enabledItem,
            new ToolStripSeparator(),
            _settingsItem,
            _aboutItem,
            new ToolStripSeparator(),
            _exitItem
        ]);

        _notifyIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            ContextMenuStrip = menu,
            Visible = true
        };
        _notifyIcon.DoubleClick += (_, _) => ShowSettings();

        _hotkeyWindow.Pressed += (_, _) => MoveForegroundWindow();
        ApplyInitialState();
        ApplyLocalizedText();
    }

    private void ApplyInitialState()
    {
        if (!_settings.Enabled)
        {
            return;
        }

        try
        {
            _hotkeyWindow.Register(_settings.GetHotkey());
        }
        catch (Exception exception)
        {
            _settings.Enabled = false;
            _enabledItem.Checked = false;
            SaveSettings();
            ShowError(exception.Message);
        }
    }

    private void MoveForegroundWindow()
    {
        if (_moving || _exiting)
        {
            return;
        }

        try
        {
            _moving = true;
            var result = _windowMover.MoveForegroundWindowToNextMonitor();
            if (!result.IsSuccess)
            {
                ShowError(result.Message);
            }
        }
        catch (Exception exception)
        {
            AppLog.Error(Localization.Text("MoveOperationFailedLog"), exception);
            ShowError(Localization.Format("MoveOperationFailed", exception.Message));
        }
        finally
        {
            _moving = false;
        }
    }

    private void EnabledItemCheckedChanged(object? sender, EventArgs eventArgs)
    {
        if (_exiting || _enabledItem.Checked == _settings.Enabled)
        {
            return;
        }

        if (_enabledItem.Checked)
        {
            try
            {
                _hotkeyWindow.Register(_settings.GetHotkey());
                _settings.Enabled = true;
            }
            catch (Exception exception)
            {
                _enabledItem.Checked = false;
                ShowError(exception.Message);
                return;
            }
        }
        else
        {
            _hotkeyWindow.Unregister();
            _settings.Enabled = false;
        }

        SaveSettings();
        UpdateTrayText();
    }

    private void ShowSettings()
    {
        using var form = new SettingsForm(
            _settings.GetHotkey(),
            StartupManager.IsEnabled(),
            _settings.Language);
        if (form.ShowDialog() != DialogResult.OK)
        {
            return;
        }

        var previousHotkey = _settings.GetHotkey();
        var candidate = form.SelectedHotkey;

        if (_settings.Enabled && candidate != previousHotkey)
        {
            try
            {
                _hotkeyWindow.Register(candidate);
            }
            catch (Exception exception)
            {
                try
                {
                    _hotkeyWindow.Register(previousHotkey);
                }
                catch (Exception restoreException)
                {
                    AppLog.Error(Localization.Text("PreviousHotkeyRestoreFailed"), restoreException);
                    _settings.Enabled = false;
                    _enabledItem.Checked = false;
                }

                ShowError(exception.Message);
                return;
            }
        }

        _settings.Modifiers = candidate.Modifiers;
        _settings.HotkeyKey = candidate.Key;
        _settings.Language = Localization.NormalizeLanguage(form.SelectedLanguage);
        Localization.ApplyLanguage(_settings.Language);
        SaveSettings();
        ApplyLocalizedText();

        try
        {
            StartupManager.SetEnabled(form.StartWithWindows);
        }
        catch (Exception exception)
        {
            AppLog.Error(Localization.Text("StartupSettingFailedLog"), exception);
            ShowError(Localization.Format("StartupSettingFailed", exception.Message));
        }
    }

    private void SaveSettings()
    {
        try
        {
            _settingsService.Save(_settings);
        }
        catch (Exception exception)
        {
            AppLog.Error(Localization.Text("SettingsSaveFailedLog"), exception);
            ShowError(Localization.Format("SettingsSaveFailed", exception.Message));
        }
    }

    private void ApplyLocalizedText()
    {
        _enabledItem.Text = Localization.Text("MenuHotkeyEnabled");
        _settingsItem.Text = Localization.Text("MenuSettings");
        _aboutItem.Text = Localization.Text("MenuAbout");
        _exitItem.Text = Localization.Text("MenuExit");
        UpdateTrayText();
    }

    private void UpdateTrayText()
    {
        var state = _settings.Enabled ? _settings.GetHotkey().ToString() : Localization.Text("TrayDisabled");
        _notifyIcon.Text = $"WindowMover – {state}";
        _moveItem.Text = Localization.Format("MenuMoveWithHotkey", _settings.GetHotkey());
    }

    private void ShowError(string message)
    {
        _notifyIcon.BalloonTipTitle = "WindowMover";
        _notifyIcon.BalloonTipText = message;
        _notifyIcon.BalloonTipIcon = ToolTipIcon.Warning;
        _notifyIcon.ShowBalloonTip(4000);
    }

    private static void ShowAbout()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.1.0";
        MessageBox.Show(
            Localization.Format("AboutMessage", version),
            Localization.Text("AboutTitle"),
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    protected override void ExitThreadCore()
    {
        if (_exiting)
        {
            return;
        }

        _exiting = true;
        _notifyIcon.Visible = false;
        _hotkeyWindow.Dispose();
        _notifyIcon.ContextMenuStrip?.Dispose();
        _notifyIcon.Dispose();
        base.ExitThreadCore();
    }
}
