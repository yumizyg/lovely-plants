using DesktopGarden.Core;

namespace DesktopGarden;

internal sealed class PotInspectorForm : Form
{
    private readonly AssetCatalog _catalog;
    private readonly PictureBox _preview = new();
    private readonly Label _title = new();
    private readonly Label _stage = new();
    private readonly Label _plantValue = new();
    private readonly Label _potValue = new();
    private readonly Label _expressionValue = new();
    private readonly Label _scaleValue = new();
    private readonly FluentProgressBar _progress = new();
    private readonly FluentSlider _scale = new() { Minimum = 60, Maximum = 300 };
    private Image? _previewImage;

    public PotInspectorForm(PotInstance pot, AssetCatalog catalog, Rectangle anchor)
    {
        Pot = pot;
        _catalog = catalog;
        Text = "植物检查器";
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;
        ShowIcon = false;
        KeyPreview = true;
        BackColor = FluentTheme.Surface;
        ForeColor = FluentTheme.Text;
        Font = FluentTheme.Body;
        ClientSize = new Size(372, 510);
        Padding = new Padding(18);

        BuildLayout();
        RefreshState();
        PositionNear(anchor);
        KeyDown += (_, eventArgs) =>
        {
            if (eventArgs.KeyCode == Keys.Escape) Close();
        };
    }

    public PotInstance Pot { get; }
    public event Action<PotInstance>? WaterRequested;
    public event Action<PotInstance>? PlantChangeRequested;
    public event Action<PotInstance>? PotChangeRequested;
    public event Action<PotInstance>? ExpressionChangeRequested;
    public event Action<PotInstance, float>? ScaleChanged;
    public event Action<PotInstance>? RemoveRequested;

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
        Region = FluentTheme.RoundedRegion(Size, 8);
    }

    public void RefreshState()
    {
        var hasPlant = !string.IsNullOrWhiteSpace(Pot.PlantId);
        _title.Text = _catalog.PlantName(Pot.PlantId);
        _stage.Text = hasPlant
            ? $"阶段 {Pot.GrowthStage}  ·  已陪伴 {PlantInfoForm.FormatDuration(Pot.ElapsedRunSeconds)}"
            : "等待播种";
        _progress.Value = hasPlant ? GrowthPolicy.GetStageProgress(Pot.ElapsedRunSeconds) : 0;
        _plantValue.Text = _catalog.PlantName(Pot.PlantId);
        _potValue.Text = _catalog.PotName(Pot.PotId);
        _expressionValue.Text = _catalog.ExpressionName(Pot.ExpressionId);
        _scale.Value = (int)Math.Round(Math.Clamp(Pot.Scale, 0.6f, 3f) * 100);
        UpdateScaleLabel();
        SetPreview(hasPlant ? _catalog.PlantPath(Pot.PlantId, Pot.GrowthStage) : _catalog.PotPath(Pot.PotId));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _previewImage?.Dispose();
        base.Dispose(disposing);
    }

    private void BuildLayout()
    {
        var close = FluentTheme.Button("关闭");
        close.Click += (_, _) => Close();
        var headerActions = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 34, FlowDirection = FlowDirection.RightToLeft, WrapContents = false };
        headerActions.Controls.Add(close);
        Controls.Add(headerActions);

        _preview.Size = new Size(68, 68);
        _preview.SizeMode = PictureBoxSizeMode.Zoom;
        _preview.Location = new Point(18, 56);
        _title.Font = FluentTheme.Title;
        _title.ForeColor = FluentTheme.Text;
        _title.AutoSize = true;
        _title.Location = new Point(100, 60);
        _stage.Font = FluentTheme.Caption;
        _stage.ForeColor = FluentTheme.TextMuted;
        _stage.AutoSize = true;
        _stage.Location = new Point(100, 88);
        _progress.Location = new Point(100, 113);
        _progress.Width = 235;
        Controls.Add(_preview);
        Controls.Add(_title);
        Controls.Add(_stage);
        Controls.Add(_progress);

        var care = new FlowLayoutPanel { Location = new Point(18, 140), Size = new Size(336, 36), WrapContents = false };
        var water = FluentTheme.Button("浇水", true);
        water.Click += (_, _) => WaterRequested?.Invoke(Pot);
        care.Controls.Add(water);
        Controls.Add(care);

        var divider = new Panel { Location = new Point(18, 188), Size = new Size(336, 1), BackColor = FluentTheme.Border };
        Controls.Add(divider);
        AddAssetRow("植物", _plantValue, "更换", 205, () => PlantChangeRequested?.Invoke(Pot));
        var warning = new Label
        {
            Text = "更换植物后，当前成长时间会归零。",
            Font = FluentTheme.Caption,
            ForeColor = FluentTheme.Coral,
            AutoSize = true,
            Location = new Point(92, 249)
        };
        Controls.Add(warning);
        AddAssetRow("花盆", _potValue, "更换", 279, () => PotChangeRequested?.Invoke(Pot));
        AddAssetRow("表情", _expressionValue, "更换", 337, () => ExpressionChangeRequested?.Invoke(Pot));

        var scaleLabel = LabelFor("缩放");
        scaleLabel.Location = new Point(18, 403);
        Controls.Add(scaleLabel);
        _scale.Location = new Point(86, 394);
        _scale.Width = 210;
        _scale.ValueChanged += (_, _) =>
        {
            UpdateScaleLabel();
            ScaleChanged?.Invoke(Pot, _scale.Value / 100f);
        };
        _scaleValue.Font = FluentTheme.Caption;
        _scaleValue.ForeColor = FluentTheme.TextMuted;
        _scaleValue.AutoSize = true;
        _scaleValue.Location = new Point(305, 405);
        Controls.Add(_scale);
        Controls.Add(_scaleValue);

        var footerDivider = new Panel { Location = new Point(18, 452), Size = new Size(336, 1), BackColor = FluentTheme.Border };
        var remove = FluentTheme.Button("移除花盆");
        remove.ForeColor = FluentTheme.Danger;
        remove.Location = new Point(18, 468);
        remove.Click += (_, _) => RemoveRequested?.Invoke(Pot);
        Controls.Add(footerDivider);
        Controls.Add(remove);
    }

    private void AddAssetRow(string label, Label value, string actionText, int y, Action action)
    {
        var key = LabelFor(label);
        key.Location = new Point(18, y + 8);
        value.Font = FluentTheme.Body;
        value.ForeColor = FluentTheme.Text;
        value.AutoEllipsis = true;
        value.Size = new Size(160, 28);
        value.Location = new Point(92, y + 7);
        var actionButton = FluentTheme.Button(actionText);
        actionButton.Location = new Point(282, y);
        actionButton.Click += (_, _) => action();
        Controls.Add(key);
        Controls.Add(value);
        Controls.Add(actionButton);
        var separator = new Panel { Location = new Point(92, y + 50), Size = new Size(262, 1), BackColor = FluentTheme.Border };
        Controls.Add(separator);
    }

    private static Label LabelFor(string text) => new()
    {
        Text = text,
        Font = FluentTheme.Caption,
        ForeColor = FluentTheme.TextMuted,
        AutoSize = true
    };

    private void UpdateScaleLabel() => _scaleValue.Text = $"{_scale.Value}%";

    private void SetPreview(string path)
    {
        _previewImage?.Dispose();
        using var source = new Bitmap(path);
        var bounds = AlphaBounds(source);
        _previewImage = bounds.IsEmpty ? new Bitmap(source) : source.Clone(bounds, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
        _preview.Image = _previewImage;
    }

    private static Rectangle AlphaBounds(Bitmap bitmap)
    {
        var left = bitmap.Width;
        var top = bitmap.Height;
        var right = -1;
        var bottom = -1;
        for (var y = 0; y < bitmap.Height; y += 2)
        {
            for (var x = 0; x < bitmap.Width; x += 2)
            {
                if (bitmap.GetPixel(x, y).A <= 8) continue;
                left = Math.Min(left, x);
                top = Math.Min(top, y);
                right = Math.Max(right, x);
                bottom = Math.Max(bottom, y);
            }
        }

        if (right < left || bottom < top) return Rectangle.Empty;
        return Rectangle.FromLTRB(Math.Max(0, left - 2), Math.Max(0, top - 2), Math.Min(bitmap.Width, right + 3), Math.Min(bitmap.Height, bottom + 3));
    }

    private void PositionNear(Rectangle anchor)
    {
        var workArea = Screen.FromRectangle(anchor).WorkingArea;
        var x = anchor.Right + 12;
        if (x + Width > workArea.Right - 8) x = anchor.Left - Width - 12;
        x = Math.Clamp(x, workArea.Left + 8, workArea.Right - Width - 8);
        var y = Math.Clamp(anchor.Bottom - Height, workArea.Top + 8, workArea.Bottom - Height - 8);
        Location = new Point(x, y);
    }
}
