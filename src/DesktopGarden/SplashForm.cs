using System.Diagnostics;
using System.Drawing.Drawing2D;

namespace DesktopGarden;

internal sealed class SplashForm : Form
{
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 16 };
    private readonly Image _background;

    public SplashForm()
    {
        var path = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "background.png");
        _background = File.Exists(path) ? Image.FromFile(path) : new Bitmap(1600, 900);

        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        ShowInTaskbar = false;
        TopMost = true;
        DoubleBuffered = true;
        ClientSize = new Size(960, 540);
        BackgroundImageLayout = ImageLayout.Stretch;

        var iconPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "lovely-plants.ico");
        if (File.Exists(iconPath))
        {
            Icon = new Icon(iconPath);
        }

        _timer.Tick += (_, _) =>
        {
            if (_clock.ElapsedMilliseconds >= 2000)
            {
                DialogResult = DialogResult.OK;
                Close();
                return;
            }

            Invalidate();
        };
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        _clock.Restart();
        _timer.Start();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        e.Graphics.DrawImage(_background, ClientRectangle);

        var elapsed = _clock.Elapsed.TotalMilliseconds;
        var textAlpha = (int)(255 * Math.Clamp(elapsed / 1200d, 0, 1));

        using var overlay = new SolidBrush(Color.FromArgb(72, 12, 24, 16));
        e.Graphics.FillRectangle(overlay, ClientRectangle);

        using var titleFont = new Font("Arial Rounded MT Bold", 58f, FontStyle.Bold, GraphicsUnit.Pixel);
        using var handleFont = new Font("Segoe UI Semibold", 20f, FontStyle.Regular, GraphicsUnit.Pixel);
        using var shadowBrush = new SolidBrush(Color.FromArgb((int)(textAlpha * 0.45), 16, 24, 18));
        using var titleBrush = new SolidBrush(Color.FromArgb(textAlpha, 255, 255, 255));
        using var handleBrush = new SolidBrush(Color.FromArgb((int)(textAlpha * 0.88), 255, 255, 255));

        const string title = "Lovely Plants";
        const string handle = "@yumizyg";

        var titleSize = e.Graphics.MeasureString(title, titleFont);
        var titleX = (ClientSize.Width - titleSize.Width) / 2f;
        var titleY = ClientSize.Height * 0.33f;
        e.Graphics.DrawString(title, titleFont, shadowBrush, titleX + 4, titleY + 5);
        e.Graphics.DrawString(title, titleFont, titleBrush, titleX, titleY);

        var handleSize = e.Graphics.MeasureString(handle, handleFont);
        var handleX = ClientSize.Width - handleSize.Width - 28f;
        var handleY = ClientSize.Height - handleSize.Height - 22f;
        e.Graphics.DrawString(handle, handleFont, shadowBrush, handleX + 2, handleY + 2);
        e.Graphics.DrawString(handle, handleFont, handleBrush, handleX, handleY);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _timer.Dispose();
            _background.Dispose();
        }

        base.Dispose(disposing);
    }
}
