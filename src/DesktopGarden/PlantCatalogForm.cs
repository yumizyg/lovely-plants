using DesktopGarden.Core;

namespace DesktopGarden;

internal sealed class PlantCatalogForm : Form
{
    public PlantCatalogForm(AssetCatalog catalog)
    {
        Text = "植物图鉴";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(900, 620);
        Size = new Size(1024, 720);
        BackColor = FluentTheme.Surface;
        Font = FluentTheme.Body;

        var scroll = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            WrapContents = true,
            Padding = new Padding(18),
            BackColor = FluentTheme.Surface
        };

        foreach (var plant in catalog.Plants)
        {
            scroll.Controls.Add(CreatePlantCard(catalog, plant));
        }

        Controls.Add(scroll);
    }

    private static Control CreatePlantCard(AssetCatalog catalog, PlantDefinition plant)
    {
        var panel = new Panel
        {
            Size = new Size(300, 228),
            Margin = new Padding(10),
            BackColor = FluentTheme.SurfaceRaised
        };

        panel.Paint += (_, e) =>
        {
            using var pen = new Pen(FluentTheme.Border);
            e.Graphics.DrawRectangle(pen, 0, 0, panel.Width - 1, panel.Height - 1);
        };

        var title = new Label
        {
            Text = catalog.PlantName(plant.Id),
            Font = FluentTheme.Title,
            ForeColor = FluentTheme.Text,
            Location = new Point(14, 12),
            AutoSize = true
        };
        var subtitle = new Label
        {
            Text = plant.Id,
            Font = FluentTheme.Caption,
            ForeColor = FluentTheme.TextMuted,
            Location = new Point(14, 38),
            AutoSize = true
        };
        panel.Controls.Add(title);
        panel.Controls.Add(subtitle);

        var stageNames = new[] { "阶段 1", "阶段 2", "阶段 3" };
        for (var stage = 1; stage <= 3; stage++)
        {
            var x = 14 + (stage - 1) * 94;
            var caption = new Label
            {
                Text = stageNames[stage - 1],
                Font = FluentTheme.Caption,
                ForeColor = FluentTheme.Accent,
                Location = new Point(x + 18, 68),
                AutoSize = true
            };
            var preview = new PictureBox
            {
                Location = new Point(x, 92),
                Size = new Size(80, 108),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent,
                Image = Image.FromFile(catalog.PlantPath(plant.Id, stage))
            };
            panel.Controls.Add(caption);
            panel.Controls.Add(preview);
        }

        return panel;
    }
}
