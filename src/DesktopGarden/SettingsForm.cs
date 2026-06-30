using DesktopGarden.Core;

namespace DesktopGarden;

internal sealed class SettingsForm : Form
{
    private readonly ComboBox _monitor = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TrackBar _scale = new() { Minimum = 50, Maximum = 120, TickFrequency = 10, SmallChange = 5, LargeChange = 10 };
    private readonly Label _scaleValue = new() { AutoSize = true };
    private readonly TrackBar _gapScale = new() { Minimum = 60, Maximum = 800, TickFrequency = 20, SmallChange = 10, LargeChange = 20 };
    private readonly Label _gapScaleValue = new() { AutoSize = true };
    private readonly CheckBox _topMost = new() { Text = "始终置顶", AutoSize = true };
    private readonly CheckBox _locked = new() { Text = "锁定后鼠标穿透", AutoSize = true };
    private readonly CheckBox _showGrass = new() { Text = "显示草地背景", AutoSize = true };
    private readonly CheckBox _sound = new() { Text = "交互/提醒音效", AutoSize = true };
    private readonly CheckBox _startup = new() { Text = "开机自动启动", AutoSize = true };

    public SettingsForm(AppSettings settings)
    {
        Text = "Lovely Plants 设置";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(470, 540);
        MinimumSize = new Size(470, 540);
        BackColor = Color.FromArgb(248, 247, 243);
        Font = new Font("Microsoft YaHei UI", 9f);

        foreach (var screen in Screen.AllScreens)
        {
            _monitor.Items.Add($"{screen.DeviceName}  {screen.Bounds.Width} x {screen.Bounds.Height}");
        }

        _monitor.SelectedIndex = Math.Clamp(settings.MonitorIndex, 0, Math.Max(0, _monitor.Items.Count - 1));
        _scale.Value = (int)Math.Round(Math.Clamp(settings.Scale, 0.5f, 1.2f) * 100);
        _gapScale.Value = (int)Math.Round(Math.Clamp(settings.GapScale, 0.6f, 8f) * 100);
        _topMost.Checked = settings.AlwaysOnTop;
        _locked.Checked = settings.InteractionLocked;
        _showGrass.Checked = settings.ShowGrassBackground;
        _sound.Checked = settings.SoundEnabled;
        _startup.Checked = settings.StartWithWindows;

        UpdateScaleLabel();
        UpdateGapScaleLabel();
        _scale.ValueChanged += (_, _) =>
        {
            UpdateScaleLabel();
            PreviewChanged?.Invoke(CaptureSettings());
        };
        _gapScale.ValueChanged += (_, _) =>
        {
            UpdateGapScaleLabel();
            PreviewChanged?.Invoke(CaptureSettings());
        };
        _topMost.CheckedChanged += (_, _) => PreviewChanged?.Invoke(CaptureSettings());
        _locked.CheckedChanged += (_, _) => PreviewChanged?.Invoke(CaptureSettings());
        _showGrass.CheckedChanged += (_, _) => PreviewChanged?.Invoke(CaptureSettings());
        _sound.CheckedChanged += (_, _) => PreviewChanged?.Invoke(CaptureSettings());
        _startup.CheckedChanged += (_, _) => PreviewChanged?.Invoke(CaptureSettings());
        _monitor.SelectedIndexChanged += (_, _) => PreviewChanged?.Invoke(CaptureSettings());

        var content = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20, 18, 20, 12),
            AutoScroll = true
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            RowCount = 10,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        layout.Controls.Add(new Label { Text = "显示器", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 8, 8, 8) }, 0, 0);
        _monitor.Width = 250;
        layout.Controls.Add(_monitor, 1, 0);

        layout.Controls.Add(new Label { Text = "整体大小", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 12, 8, 8) }, 0, 1);
        layout.Controls.Add(BuildSliderRow(_scale, _scaleValue), 1, 1);

        layout.Controls.Add(new Label { Text = "花盆间距", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 12, 8, 8) }, 0, 2);
        layout.Controls.Add(BuildSliderRow(_gapScale, _gapScaleValue), 1, 2);

        _topMost.Margin = new Padding(0, 16, 0, 0);
        _locked.Margin = new Padding(0, 10, 0, 0);
        _showGrass.Margin = new Padding(0, 10, 0, 0);
        _sound.Margin = new Padding(0, 10, 0, 0);
        _startup.Margin = new Padding(0, 10, 0, 0);
        layout.Controls.Add(_topMost, 1, 3);
        layout.Controls.Add(_locked, 1, 4);
        layout.Controls.Add(_showGrass, 1, 5);
        layout.Controls.Add(_sound, 1, 6);
        layout.Controls.Add(_startup, 1, 7);

        var soundHint = new Label
        {
            Text = "用于浇水、番茄钟结束和提示气泡的音效。",
            AutoSize = true,
            ForeColor = FluentTheme.TextMuted,
            Margin = new Padding(2, 4, 0, 0)
        };
        layout.Controls.Add(soundHint, 1, 8);

        var reset = new Button
        {
            Text = "重置花园数据",
            AutoSize = true,
            ForeColor = Color.FromArgb(166, 65, 48),
            Margin = new Padding(0, 18, 0, 0)
        };
        reset.Click += (_, _) => ResetRequested?.Invoke();
        layout.Controls.Add(reset, 1, 9);

        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 58,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(12, 10, 12, 10),
            BackColor = Color.FromArgb(244, 242, 236)
        };
        var ok = new Button { Text = "保存", AutoSize = true, DialogResult = DialogResult.OK, Margin = new Padding(8, 0, 0, 0) };
        var cancel = new Button { Text = "取消", AutoSize = true, DialogResult = DialogResult.Cancel };
        footer.Controls.Add(ok);
        footer.Controls.Add(cancel);

        content.Controls.Add(layout);
        Controls.Add(content);
        Controls.Add(footer);
        AcceptButton = ok;
        CancelButton = cancel;
    }

    public event Action? ResetRequested;
    public event Action<AppSettings>? PreviewChanged;

    public void ApplyTo(AppSettings settings)
    {
        var preview = CaptureSettings();
        settings.MonitorIndex = preview.MonitorIndex;
        settings.Scale = preview.Scale;
        settings.GapScale = preview.GapScale;
        settings.AlwaysOnTop = preview.AlwaysOnTop;
        settings.InteractionLocked = preview.InteractionLocked;
        settings.ShowGrassBackground = preview.ShowGrassBackground;
        settings.SoundEnabled = preview.SoundEnabled;
        settings.StartWithWindows = preview.StartWithWindows;
    }

    private AppSettings CaptureSettings() => new()
    {
        MonitorIndex = Math.Max(0, _monitor.SelectedIndex),
        Scale = _scale.Value / 100f,
        GapScale = _gapScale.Value / 100f,
        AlwaysOnTop = _topMost.Checked,
        InteractionLocked = _locked.Checked,
        ShowGrassBackground = _showGrass.Checked,
        SoundEnabled = _sound.Checked,
        StartWithWindows = _startup.Checked
    };

    private static FlowLayoutPanel BuildSliderRow(TrackBar trackBar, Label valueLabel)
    {
        trackBar.Width = 220;
        trackBar.Margin = new Padding(0);
        valueLabel.Margin = new Padding(10, 10, 0, 0);
        return new FlowLayoutPanel
        {
            AutoSize = true,
            WrapContents = false,
            Margin = new Padding(0),
            Controls = { trackBar, valueLabel }
        };
    }

    private void UpdateScaleLabel() => _scaleValue.Text = $"{_scale.Value}%";

    private void UpdateGapScaleLabel() => _gapScaleValue.Text = $"{_gapScale.Value}%";
}
