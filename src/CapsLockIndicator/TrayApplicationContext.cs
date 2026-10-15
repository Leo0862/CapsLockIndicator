namespace CapsLockIndicator;

/// <summary>
/// Owns the tray icon, keyboard hook and popup window for the lifetime of the app.
/// </summary>
internal sealed class TrayApplicationContext : ApplicationContext
{
    // Fallback polling catches state changes that do not come from a physical
    // key press (on-screen keyboard, remote desktop, other apps).
    private const int PollIntervalMs = 250;

    private readonly NotifyIcon _trayIcon;
    private readonly KeyboardHook _hook;
    private readonly OsdForm _osd;
    private readonly System.Windows.Forms.Timer _pollTimer = new() { Interval = PollIntervalMs };
    private readonly Icon _iconOn = TrayIconFactory.Create(true);
    private readonly Icon _iconOff = TrayIconFactory.Create(false);

    private AppSettings _settings;
    private bool _lastState;
    private SettingsForm? _settingsForm;

    public TrayApplicationContext()
    {
        _settings = AppSettings.Load();
        _osd = new OsdForm(_settings);

        var menu = new ContextMenuStrip();
        menu.Items.Add("設定 Settings...", null, (_, _) => OpenSettings());
        menu.Items.Add("測試顯示 Show now", null, (_, _) => _osd.ShowState(NativeMethods.IsCapsLockOn()));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("結束 Exit", null, (_, _) => ExitThread());

        _lastState = NativeMethods.IsCapsLockOn();
        _trayIcon = new NotifyIcon
        {
            ContextMenuStrip = menu,
            Visible = true,
        };
        _trayIcon.DoubleClick += (_, _) => OpenSettings();
        UpdateTrayIcon(_lastState);

        _hook = new KeyboardHook();
        _hook.CapsLockPressed += (_, _) => CheckState(forceShow: true);

        _pollTimer.Tick += (_, _) => CheckState(forceShow: false);
        _pollTimer.Start();
    }

    private void CheckState(bool forceShow)
    {
        bool state = NativeMethods.IsCapsLockOn();
        bool changed = state != _lastState;
        _lastState = state;

        if (changed)
        {
            UpdateTrayIcon(state);
        }
        if (changed || forceShow)
        {
            _osd.ShowState(state);
        }
    }

    private void UpdateTrayIcon(bool capsOn)
    {
        _trayIcon.Icon = capsOn ? _iconOn : _iconOff;
        _trayIcon.Text = capsOn ? "Caps Lock: ON" : "Caps Lock: OFF";
    }

    private void OpenSettings()
    {
        if (_settingsForm != null)
        {
            _settingsForm.Activate();
            return;
        }

        var original = _settings;
        using (_settingsForm = new SettingsForm(_settings, preview =>
               {
                   _osd.ApplySettings(preview);
                   _osd.ShowState(NativeMethods.IsCapsLockOn());
               }))
        {
            if (_settingsForm.ShowDialog() == DialogResult.OK)
            {
                _settings = _settingsForm.Result;
                try
                {
                    _settings.Save();
                    AppSettings.SetStartWithWindows(_settingsForm.StartWithWindows);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"無法儲存設定：{ex.Message}", "Caps Lock Indicator",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else
            {
                _settings = original;
            }
        }
        _settingsForm = null;
        _osd.ApplySettings(_settings);
    }

    protected override void ExitThreadCore()
    {
        _pollTimer.Stop();
        _trayIcon.Visible = false;
        base.ExitThreadCore();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _hook.Dispose();
            _pollTimer.Dispose();
            _trayIcon.Dispose();
            _osd.Dispose();
            _iconOn.Dispose();
            _iconOff.Dispose();
        }
        base.Dispose(disposing);
    }
}
