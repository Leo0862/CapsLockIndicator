namespace CapsLockIndicator;

/// <summary>
/// Settings dialog: font size, font, colors, opacity, display time, texts and auto-start.
/// </summary>
internal sealed class SettingsForm : Form
{
    private readonly NumericUpDown _fontSize = new()
    {
        Minimum = (decimal)AppSettings.MinFontSize,
        Maximum = (decimal)AppSettings.MaxFontSize,
        DecimalPlaces = 0,
        Width = 80,
    };
    private readonly ComboBox _fontFamily = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200 };
    private readonly CheckBox _bold = new() { Text = "粗體 Bold", AutoSize = true };
    private readonly Button _backColor = new() { Width = 80, FlatStyle = FlatStyle.Flat };
    private readonly Button _textColor = new() { Width = 80, FlatStyle = FlatStyle.Flat };
    private readonly NumericUpDown _opacity = new() { Minimum = 20, Maximum = 100, Width = 80 };
    private readonly NumericUpDown _duration = new() { Minimum = 300, Maximum = 10000, Increment = 100, Width = 80 };
    private readonly NumericUpDown _margin = new() { Minimum = 0, Maximum = 500, Width = 80 };
    private readonly TextBox _onText = new() { Width = 200 };
    private readonly TextBox _offText = new() { Width = 200 };
    private readonly CheckBox _startWithWindows = new() { Text = "開機自動啟動 Start with Windows", AutoSize = true };

    private readonly Action<AppSettings> _preview;

    public AppSettings Result { get; private set; }
    public bool StartWithWindows => _startWithWindows.Checked;

    public SettingsForm(AppSettings current, Action<AppSettings> preview)
    {
        Result = current.Clone();
        _preview = preview;

        Text = "Caps Lock Indicator 設定";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Font;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Padding = new Padding(12);
        Font = new Font("Microsoft JhengHei UI", 9f);

        foreach (var family in FontFamily.Families)
        {
            _fontFamily.Items.Add(family.Name);
        }

        _fontSize.Value = (decimal)current.FontSize;
        _fontFamily.SelectedItem = _fontFamily.Items.Contains(current.FontFamily) ? current.FontFamily : "Segoe UI";
        _bold.Checked = current.Bold;
        SetColorButton(_backColor, current.GetBackgroundColor());
        SetColorButton(_textColor, current.GetTextColor());
        _opacity.Value = current.Opacity;
        _duration.Value = current.DisplayDurationMs;
        _margin.Value = current.Margin;
        _onText.Text = current.OnText;
        _offText.Text = current.OffText;
        _startWithWindows.Checked = AppSettings.IsStartWithWindowsEnabled();

        _backColor.Click += (_, _) => PickColor(_backColor);
        _textColor.Click += (_, _) => PickColor(_textColor);

        var table = new TableLayoutPanel
        {
            ColumnCount = 2,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Fill,
        };
        AddRow(table, "文字大小 (pt)", _fontSize);
        AddRow(table, "字型", _fontFamily);
        AddRow(table, "", _bold);
        AddRow(table, "背景顏色", _backColor);
        AddRow(table, "文字顏色", _textColor);
        AddRow(table, "不透明度 (%)", _opacity);
        AddRow(table, "顯示時間 (ms)", _duration);
        AddRow(table, "與邊緣距離 (px)", _margin);
        AddRow(table, "開啟時文字", _onText);
        AddRow(table, "關閉時文字", _offText);
        AddRow(table, "", _startWithWindows);

        var previewButton = new Button { Text = "預覽", AutoSize = true };
        var okButton = new Button { Text = "確定", AutoSize = true, DialogResult = DialogResult.OK };
        var cancelButton = new Button { Text = "取消", AutoSize = true, DialogResult = DialogResult.Cancel };
        previewButton.Click += (_, _) => _preview(Collect());
        okButton.Click += (_, _) => Result = Collect();

        var buttons = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 12, 0, 0),
        };
        buttons.Controls.AddRange(new Control[] { cancelButton, okButton, previewButton });
        table.Controls.Add(buttons, 0, table.RowCount);
        table.SetColumnSpan(buttons, 2);

        Controls.Add(table);
        AcceptButton = okButton;
        CancelButton = cancelButton;
    }

    private static void AddRow(TableLayoutPanel table, string label, Control control)
    {
        int row = table.RowCount++;
        table.Controls.Add(new Label
        {
            Text = label,
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 6, 12, 6),
        }, 0, row);
        control.Anchor = AnchorStyles.Left;
        control.Margin = new Padding(0, 3, 0, 3);
        table.Controls.Add(control, 1, row);
    }

    private static void SetColorButton(Button button, Color color)
    {
        button.BackColor = color;
        button.Text = AppSettings.ToHex(color);
        button.ForeColor = color.GetBrightness() > 0.5f ? Color.Black : Color.White;
    }

    private void PickColor(Button button)
    {
        using var dialog = new ColorDialog { Color = button.BackColor, FullOpen = true };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            SetColorButton(button, dialog.Color);
        }
    }

    private AppSettings Collect()
    {
        var s = Result.Clone();
        s.FontSize = (float)_fontSize.Value;
        s.FontFamily = _fontFamily.SelectedItem as string ?? "Segoe UI";
        s.Bold = _bold.Checked;
        s.BackgroundColor = AppSettings.ToHex(_backColor.BackColor);
        s.TextColor = AppSettings.ToHex(_textColor.BackColor);
        s.Opacity = (int)_opacity.Value;
        s.DisplayDurationMs = (int)_duration.Value;
        s.Margin = (int)_margin.Value;
        s.OnText = _onText.Text;
        s.OffText = _offText.Text;
        return s;
    }
}
