using System.Drawing.Drawing2D;

namespace DesktopGarden;

internal static class FluentTheme
{
    internal static readonly Color Surface = Color.FromArgb(247, 248, 246);
    internal static readonly Color SurfaceRaised = Color.FromArgb(255, 255, 253);
    internal static readonly Color Text = Color.FromArgb(32, 36, 32);
    internal static readonly Color TextMuted = Color.FromArgb(103, 111, 104);
    internal static readonly Color Border = Color.FromArgb(220, 229, 221);
    internal static readonly Color Accent = Color.FromArgb(111, 143, 114);
    internal static readonly Color AccentSoft = Color.FromArgb(229, 237, 229);
    internal static readonly Color Coral = Color.FromArgb(227, 130, 112);
    internal static readonly Color Danger = Color.FromArgb(180, 76, 63);
    internal static readonly Font Body = new("Microsoft YaHei UI", 9f);
    internal static readonly Font Caption = new("Microsoft YaHei UI", 8f);
    internal static readonly Font Title = new("Microsoft YaHei UI", 11f, FontStyle.Bold);

    internal static Button Button(string text, bool accent = false)
    {
        var button = new FluentButton
        {
            Text = text,
            Size = new Size(TextRenderer.MeasureText(text, Body).Width + 24, 32),
            Accent = accent,
            ForeColor = accent ? Color.White : Text,
            Font = Body,
            Cursor = Cursors.Hand,
            Margin = new Padding(4, 0, 0, 0)
        };
        return button;
    }

    internal static Region RoundedRegion(Size size, int radius)
    {
        using var path = new GraphicsPath();
        var diameter = radius * 2;
        path.AddArc(0, 0, diameter, diameter, 180, 90);
        path.AddArc(size.Width - diameter - 1, 0, diameter, diameter, 270, 90);
        path.AddArc(size.Width - diameter - 1, size.Height - diameter - 1, diameter, diameter, 0, 90);
        path.AddArc(0, size.Height - diameter - 1, diameter, diameter, 90, 90);
        path.CloseFigure();
        return new Region(path);
    }
}

internal sealed class FluentProgressBar : Control
{
    private double _value;

    public double Value
    {
        get => _value;
        set { _value = Math.Clamp(value, 0, 1); Invalidate(); }
    }

    public FluentProgressBar()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
        Height = 6;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var track = new SolidBrush(Color.FromArgb(229, 234, 229));
        using var fill = new SolidBrush(FluentTheme.Accent);
        using var trackPath = RoundedPath(ClientRectangle, Height / 2f);
        e.Graphics.FillPath(track, trackPath);
        var fillWidth = (int)Math.Round(Width * Value);
        if (fillWidth > 0)
        {
            using var fillPath = RoundedPath(new Rectangle(0, 0, Math.Max(Height, fillWidth), Height), Height / 2f);
            e.Graphics.FillPath(fill, fillPath);
        }
    }

    private static GraphicsPath RoundedPath(Rectangle bounds, float radius)
    {
        var path = new GraphicsPath();
        var diameter = radius * 2;
        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}

internal sealed class FluentButton : Button
{
    private bool _hovered;
    private bool _pressed;

    internal bool Accent { get; init; }

    public FluentButton()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        UseVisualStyleBackColor = false;
    }

    protected override void OnMouseEnter(EventArgs e) { _hovered = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hovered = false; _pressed = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseDown(MouseEventArgs e) { _pressed = true; Invalidate(); base.OnMouseDown(e); }
    protected override void OnMouseUp(MouseEventArgs e) { _pressed = false; Invalidate(); base.OnMouseUp(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = ButtonPath(bounds, 6);
        var baseColor = Accent ? FluentTheme.Accent : FluentTheme.SurfaceRaised;
        var background = _pressed
            ? (Accent ? Color.FromArgb(81, 116, 86) : Color.FromArgb(218, 229, 219))
            : _hovered
                ? (Accent ? Color.FromArgb(96, 130, 100) : FluentTheme.AccentSoft)
                : baseColor;
        using var brush = new SolidBrush(background);
        using var pen = new Pen(Accent ? FluentTheme.Accent : FluentTheme.Border);
        e.Graphics.FillPath(brush, path);
        e.Graphics.DrawPath(pen, path);
        TextRenderer.DrawText(e.Graphics, Text, Font, bounds, ForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
    }

    private static GraphicsPath ButtonPath(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
        var diameter = radius * 2;
        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}

internal sealed class FluentSlider : Control
{
    private int _value = 100;
    private bool _dragging;

    internal int Minimum { get; init; } = 60;
    internal int Maximum { get; init; } = 140;
    internal int Value
    {
        get => _value;
        set
        {
            var next = Math.Clamp(value, Minimum, Maximum);
            if (_value == next) return;
            _value = next;
            Invalidate();
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    internal event EventHandler? ValueChanged;

    public FluentSlider()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
        Cursor = Cursors.Hand;
        Height = 28;
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        _dragging = true;
        Capture = true;
        SetFromX(e.X);
        base.OnMouseDown(e);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (_dragging) SetFromX(e.X);
        base.OnMouseMove(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        _dragging = false;
        Capture = false;
        base.OnMouseUp(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var track = new Rectangle(6, Height / 2 - 2, Math.Max(1, Width - 12), 4);
        var ratio = (Value - Minimum) / (double)(Maximum - Minimum);
        var thumbX = track.Left + (int)Math.Round(track.Width * ratio);
        using var trackBrush = new SolidBrush(Color.FromArgb(222, 230, 223));
        using var fillBrush = new SolidBrush(FluentTheme.Accent);
        using var thumbBrush = new SolidBrush(FluentTheme.SurfaceRaised);
        using var thumbPen = new Pen(FluentTheme.Accent, 2);
        e.Graphics.FillRectangle(trackBrush, track);
        e.Graphics.FillRectangle(fillBrush, new Rectangle(track.Left, track.Top, Math.Max(1, thumbX - track.Left), track.Height));
        e.Graphics.FillEllipse(thumbBrush, thumbX - 6, Height / 2 - 6, 12, 12);
        e.Graphics.DrawEllipse(thumbPen, thumbX - 6, Height / 2 - 6, 12, 12);
    }

    private void SetFromX(int x)
    {
        var ratio = Math.Clamp((x - 6d) / Math.Max(1, Width - 12), 0, 1);
        Value = (int)Math.Round((Minimum + ratio * (Maximum - Minimum)) / 5d) * 5;
    }
}
