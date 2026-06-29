namespace DesktopGarden;

internal sealed class ImageCache : IDisposable
{
    private readonly Dictionary<string, Bitmap> _images = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<(string Path, int Width, int Height), Bitmap> _scaled = [];

    public Bitmap Get(string path)
    {
        if (_images.TryGetValue(path, out var cached))
        {
            return cached;
        }

        using var source = new Bitmap(path);
        var bitmap = new Bitmap(source.Width, source.Height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.DrawImageUnscaled(source, 0, 0);
        }

        _images[path] = bitmap;
        return bitmap;
    }

    public Bitmap GetScaled(string path, Size size)
    {
        var width = Math.Max(1, size.Width);
        var height = Math.Max(1, size.Height);
        var key = (path, width, height);
        if (_scaled.TryGetValue(key, out var cached)) return cached;

        var source = Get(path);
        var bitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
            graphics.DrawImage(source, new Rectangle(0, 0, width, height));
        }
        _scaled[key] = bitmap;
        return bitmap;
    }

    public void Dispose()
    {
        foreach (var image in _scaled.Values)
        {
            image.Dispose();
        }
        _scaled.Clear();
        foreach (var image in _images.Values)
        {
            image.Dispose();
        }

        _images.Clear();
    }
}
