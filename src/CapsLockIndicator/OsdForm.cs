namespace CapsLockIndicator;

/// <summary>
/// Borderless, click-through, non-activating popup shown above the taskbar
/// in the bottom-right corner of the primary screen.
/// </summary>
internal sealed class OsdForm : Form
{
    private const int FadeIntervalMs = 15;
    private const double FadeStep = 0.08;

    private readonly System.Windows.Forms.Timer _hideTimer = new();
    private readonly System.Windows.Forms.Timer _fadeTimer = new() { Interval = FadeIntervalMs };

    private AppSettings _settings;
    private Font? _font;
    private string _text = string.Empty;

    public OsdForm(AppSettings settings)
    {
        _settings = settings;

        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        AutoScaleMode = AutoScaleMode.None;
        DoubleBuffered = true;
        Text = "Caps Lock Indicator";

        _hideTimer.Tick += (_, _) =>
        {
            _hideTimer.Stop();
            _fadeTimer.Start();
        };
        _fadeTimer.Tick += (_, _) =>
        {
            double next = Opacity - FadeStep;
            if (next <= 0)
            {
                _fadeTimer.Stop();
                Hide();
            }
            else
            {
                Opacity = next;
            }
        };

        ApplySettings(settings);
    }

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= NativeMethods.WS_EX_TOPMOST
                          | NativeMethods.WS_EX_TOOLWINDOW   // hide from Alt+Tab
                          | NativeMethods.WS_EX_NOACTIVATE   // never steal focus
                          | NativeMethods.WS_EX_TRANSPARENT; // click-through (requires a layered window)
            return cp;
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);

        // Rounded corners on Windows 11; silently ignored on older versions.
        int preference = NativeMethods.DWMWCP_ROUND;
        try
        {
            NativeMethods.DwmSetWindowAttribute(Handle, NativeMethods.DWMWA_WINDOW_CORNER_PREFERENCE,
                ref preference, sizeof(int));
        }
        catch (DllNotFoundException)
        {
        }
    }

    public void ApplySettings(AppSettings settings)
    {
        _settings = settings;
        _font?.Dispose();
        var style = settings.Bold ? FontStyle.Bold : FontStyle.Regular;
        _font = new Font(settings.FontFamily, settings.FontSize, style, GraphicsUnit.Point);
        BackColor = settings.GetBackgroundColor();
        // A TransparencyKey keeps the window layered even at 100% opacity, which
        // WS_EX_TRANSPARENT needs for click-through. The key must differ from BackColor.
        TransparencyKey = BackColor.ToArgb() == Color.FromArgb(1, 2, 3).ToArgb()
            ? Color.FromArgb(3, 2, 1)
            : Color.FromArgb(1, 2, 3);
        ForeColor = settings.GetTextColor();
        _hideTimer.Interval = settings.DisplayDurationMs;

        if (Visible)
        {
            LayoutAndPosition();
            Invalidate();
        }
    }

    public void ShowState(bool capsOn)
    {
        _text = capsOn ? _settings.OnText : _settings.OffText;

        _fadeTimer.Stop();
        _hideTimer.Stop();

        LayoutAndPosition();
        Opacity = _settings.Opacity / 100.0;

        if (!Visible)
        {
            Show();
        }

        // Re-assert topmost so the popup stays above the taskbar.
        NativeMethods.SetWindowPos(Handle, NativeMethods.HWND_TOPMOST, 0, 0, 0, 0,
            NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_SHOWWINDOW);

        Invalidate();
        _hideTimer.Start();
    }

    private void LayoutAndPosition()
    {
        if (_font == null) return;

        // Measure both texts so the box keeps a stable size between ON and OFF.
        var flags = TextFormatFlags.NoPadding | TextFormatFlags.SingleLine;
        Size onSize = TextRenderer.MeasureText(_settings.OnText, _font, Size.Empty, flags);
        Size offSize = TextRenderer.MeasureText(_settings.OffText, _font, Size.Empty, flags);
        int textWidth = Math.Max(onSize.Width, offSize.Width);
        int textHeight = Math.Max(onSize.Height, offSize.Height);

        // Padding scales with font size.
        int padX = Math.Max(8, (int)(_font.Height * 0.6));
        int padY = Math.Max(4, (int)(_font.Height * 0.3));
        var size = new Size(textWidth + padX * 2, textHeight + padY * 2);

        // WorkingArea excludes the taskbar, so bottom-right of it is just above the taskbar.
        Rectangle area = (Screen.PrimaryScreen ?? Screen.AllScreens[0]).WorkingArea;
        int margin = _settings.Margin;
        var location = new Point(area.Right - size.Width - margin, area.Bottom - size.Height - margin);

        Bounds = new Rectangle(location, size);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (_font == null) return;

        TextRenderer.DrawText(e.Graphics, _text, _font, ClientRectangle, ForeColor, BackColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter
            | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _hideTimer.Dispose();
            _fadeTimer.Dispose();
            _font?.Dispose();
        }
        base.Dispose(disposing);
    }
}
