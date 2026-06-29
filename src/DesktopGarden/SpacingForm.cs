namespace DesktopGarden;

internal sealed class SpacingForm : Form
{
    private readonly FluentSlider _slider = new() { Minimum = 60, Maximum = 200 };
    private readonly Label _value = new() { AutoSize = true, Font = FluentTheme.Caption, ForeColor = FluentTheme.TextMuted };

    public SpacingForm(float gapScale)
    {
        Text = "调整间距";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowIcon = false;
        BackColor = FluentTheme.Surface;
        ClientSize = new Size(360, 160);
        Font = FluentTheme.Body;

        var title = new Label
        {
            Text = "全体花盆间距",
            Font = FluentTheme.Title,
            ForeColor = FluentTheme.Text,
            Location = new Point(20, 20),
            AutoSize = true
        };
        var hint = new Label
        {
            Text = "调整所有花盆之间的水平间隔。",
            Font = FluentTheme.Caption,
            ForeColor = FluentTheme.TextMuted,
            Location = new Point(20, 48),
            AutoSize = true
        };
        _slider.Location = new Point(20, 82);
        _slider.Width = 250;
        _slider.Value = (int)Math.Round(Math.Clamp(gapScale, 0.6f, 2f) * 100);
        _slider.ValueChanged += (_, _) => UpdateValue();
        _value.Location = new Point(284, 88);
        var ok = new Button { Text = "确定", DialogResult = DialogResult.OK, AutoSize = true, Location = new Point(186, 118) };
        var cancel = new Button { Text = "取消", DialogResult = DialogResult.Cancel, AutoSize = true, Location = new Point(268, 118) };

        Controls.AddRange([title, hint, _slider, _value, ok, cancel]);
        AcceptButton = ok;
        CancelButton = cancel;
        UpdateValue();
    }

    public float SelectedGapScale => _slider.Value / 100f;

    private void UpdateValue() => _value.Text = $"{_slider.Value}%";
}
