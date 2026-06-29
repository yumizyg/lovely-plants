using DesktopGarden.Core;

namespace DesktopGarden;

internal static class PreviewExporter
{
    public static void Export(string path)
    {
        var catalog = new AssetCatalog(System.IO.Path.Combine(AppContext.BaseDirectory, "Assets"));
        using var images = new ImageCache();
        var state = GardenStateFactory.CreateDefault(catalog.PlantIds, catalog.PotIds, catalog.ExpressionIds);
        var renderer = new GardenRenderer(catalog, images);
        using var result = renderer.Render(
            state,
            0.72f,
            new Dictionary<Guid, PotAnimation>(),
            new Dictionary<Guid, string>(),
            new GardenOverlayState("00:32:18", "00:25:00", false, true));
        var directory = System.IO.Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
        result.Bitmap.Save(path, System.Drawing.Imaging.ImageFormat.Png);
    }

    public static void ExportUi(string path)
    {
        var catalog = new AssetCatalog(System.IO.Path.Combine(AppContext.BaseDirectory, "Assets"));
        var state = GardenStateFactory.CreateDefault(catalog.PlantIds, catalog.PotIds, catalog.ExpressionIds);
        var pot = state.Pots[0];
        pot.ElapsedRunSeconds = 2 * 60 * 60 + 18 * 60;

        using var info = new PlantInfoForm();
        info.UpdatePot(pot, catalog);
        info.Location = new Point(-20000, -20000);
        info.Show();
        Application.DoEvents();
        using var infoBitmap = new Bitmap(info.Width, info.Height);
        info.DrawToBitmap(infoBitmap, new Rectangle(Point.Empty, info.Size));
        info.Hide();

        using var inspector = new PotInspectorForm(pot, catalog, new Rectangle(0, 0, 250, 500));
        inspector.Location = new Point(-20000, -20000);
        inspector.Show();
        Application.DoEvents();
        using var inspectorBitmap = new Bitmap(inspector.Width, inspector.Height);
        inspector.DrawToBitmap(inspectorBitmap, new Rectangle(Point.Empty, inspector.Size));
        inspector.Hide();

        using var gardenImages = new ImageCache();
        var renderer = new GardenRenderer(catalog, gardenImages);
        using var garden = renderer.Render(
            state,
            0.72f,
            new Dictionary<Guid, PotAnimation>(),
            new Dictionary<Guid, string>(),
            new GardenOverlayState("01:18:42", "00:09:58", false, true),
            renderTimeUtc: DateTime.UnixEpoch.AddSeconds(1));

        var canvas = new Bitmap(1120, 720, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
        using var graphics = Graphics.FromImage(canvas);
        graphics.Clear(Color.FromArgb(236, 239, 236));
        graphics.DrawImageUnscaled(infoBitmap, 40, 36);
        graphics.DrawImageUnscaled(inspectorBitmap, canvas.Width - inspectorBitmap.Width - 40, 36);
        graphics.DrawImageUnscaled(garden.Bitmap, 40, canvas.Height - garden.Bitmap.Height - 30);
        canvas.Save(path, System.Drawing.Imaging.ImageFormat.Png);
        canvas.Dispose();
    }
}
