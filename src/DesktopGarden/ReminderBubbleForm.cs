namespace DesktopGarden;

internal sealed class ReminderBubbleForm : Form
{
    private readonly Label _title = new();
    private readonly Label _message = new();
    private readonly Button _close = new();
    private readonly System.Windows.Forms.Timer _hideTimer = new() { Interval = 5200 };

    public ReminderBubbleForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;
        ShowIcon = false;
        BackColor = FluentTheme.Surface;
        ForeColor = FluentTheme.Text;
        Font = FluentTheme.Body;
        ClientSize = new Size(318, 126);
        Padding = new Padding(16, 14, 16, 14);
        Opacity = 0.98;

        _title.Text = "温馨提示";
        _title.Font = FluentTheme.Caption;
        _title.ForeColor = FluentTheme.Accent;
        _title.AutoSize = true;
        _title.Location = new Point(16, 14);

        _close.Text = "×";
        _close.FlatStyle = FlatStyle.Flat;
        _close.FlatAppearance.BorderSize = 0;
        _close.BackColor = Color.Transparent;
        _close.ForeColor = FluentTheme.TextMuted;
        _close.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
        _close.Size = new Size(30, 26);
        _close.Location = new Point(ClientSize.Width - 40, 8);
        _close.Click += (_, _) => Hide();

        _message.Font = FluentTheme.Body;
        _message.ForeColor = FluentTheme.Text;
        _message.Location = new Point(16, 40);
        _message.Size = new Size(286, 68);

        _hideTimer.Tick += (_, _) => Hide();
        Controls.Add(_title);
        Controls.Add(_close);
        Controls.Add(_message);
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var parameters = base.CreateParams;
            parameters.ExStyle |= NativeMethods.WsExToolWindow;
            parameters.ClassStyle |= 0x00020000;
            return parameters;
        }
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        Region?.Dispose();
        Region = FluentTheme.RoundedRegion(Size, 10);
    }

    public void ShowReminder(string message, Rectangle potBounds, IWin32Window owner, int autoHideMs = 0)
    {
        _message.Text = message;
        var workArea = Screen.FromRectangle(potBounds).WorkingArea;
        var x = Math.Clamp(potBounds.Left + (potBounds.Width - Width) / 2, workArea.Left + 8, workArea.Right - Width - 8);
        var y = potBounds.Top - Height - 16;
        if (y < workArea.Top + 8) y = Math.Min(workArea.Bottom - Height - 8, potBounds.Bottom + 12);
        Location = new Point(x, y);
        _hideTimer.Stop();
        if (!Visible) Show(owner);
        else BringToFront();
        if (autoHideMs > 0)
        {
            _hideTimer.Interval = Math.Max(500, autoHideMs);
            _hideTimer.Start();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _close.Dispose();
            _hideTimer.Dispose();
        }
        base.Dispose(disposing);
    }
}
