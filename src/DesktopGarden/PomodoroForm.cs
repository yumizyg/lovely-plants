namespace DesktopGarden;

internal sealed class PomodoroForm : Form
{
    private readonly NumericUpDown _minutes = new() { Minimum = 1, Maximum = 180, Value = 25 };
    private readonly NumericUpDown _seconds = new() { Minimum = 0, Maximum = 59, Value = 0 };

    public PomodoroForm()
    {
        Text = "番茄钟";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        TopMost = true;
        ClientSize = new Size(300, 166);
        BackColor = Color.FromArgb(248, 247, 243);
        Font = new Font("Microsoft YaHei UI", 9f);

        var title = new Label
        {
            Text = "设置倒计时",
            Font = new Font("Microsoft YaHei UI", 11f, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(18, 18)
        };

        var panel = new FlowLayoutPanel
        {
            Location = new Point(18, 56),
            Size = new Size(250, 34),
            WrapContents = false
        };
        _minutes.Width = 70;
        _seconds.Width = 70;
        panel.Controls.Add(_minutes);
        panel.Controls.Add(new Label { Text = "分钟", AutoSize = true, Margin = new Padding(8, 7, 16, 0) });
        panel.Controls.Add(_seconds);
        panel.Controls.Add(new Label { Text = "秒", AutoSize = true, Margin = new Padding(8, 7, 0, 0) });

        var hint = new Label
        {
            Text = "倒计时结束后会变红，并在花盆上方提示一次。",
            ForeColor = FluentTheme.TextMuted,
            AutoSize = true,
            Location = new Point(18, 102)
        };

        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 52,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(12, 10, 12, 10)
        };
        var ok = new Button { Text = "开始", AutoSize = true, DialogResult = DialogResult.OK, Margin = new Padding(8, 0, 0, 0) };
        var cancel = new Button { Text = "取消", AutoSize = true, DialogResult = DialogResult.Cancel };
        footer.Controls.Add(ok);
        footer.Controls.Add(cancel);

        Controls.Add(title);
        Controls.Add(panel);
        Controls.Add(hint);
        Controls.Add(footer);
        AcceptButton = ok;
        CancelButton = cancel;
    }

    public TimeSpan SelectedDuration => TimeSpan.FromMinutes((double)_minutes.Value) + TimeSpan.FromSeconds((double)_seconds.Value);

    public void SetDuration(TimeSpan? duration)
    {
        if (duration is null || duration.Value <= TimeSpan.Zero) return;
        var value = duration.Value;
        var minutes = Math.Clamp((int)value.TotalMinutes, 1, 180);
        var seconds = Math.Clamp(value.Seconds, 0, 59);
        _minutes.Value = minutes;
        _seconds.Value = seconds;
    }
}
