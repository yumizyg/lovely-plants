namespace DesktopGarden;

internal sealed class AssetPickerForm : Form
{
    private readonly ListView _list = new();
    private readonly ImageList _images = new();

    public AssetPickerForm(string title, IReadOnlyList<string> ids, Func<string, string> pathForId, string selectedId, Func<string, string>? displayName = null)
    {
        Text = title;
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(620, 430);
        Size = new Size(760, 540);
        BackColor = Color.FromArgb(248, 247, 243);
        Font = new Font("Microsoft YaHei UI", 9f);

        _images.ImageSize = new Size(96, 96);
        _images.ColorDepth = ColorDepth.Depth32Bit;
        _list.Dock = DockStyle.Fill;
        _list.View = View.LargeIcon;
        _list.LargeImageList = _images;
        _list.MultiSelect = false;
        _list.HideSelection = false;
        _list.BackColor = Color.White;

        for (var index = 0; index < ids.Count; index++)
        {
            var id = ids[index];
            using var source = new Bitmap(pathForId(id));
            _images.Images.Add(CreateThumbnail(source, _images.ImageSize));
            var item = new ListViewItem(displayName?.Invoke(id) ?? DisplayName(title, id, index), index) { Tag = id };
            _list.Items.Add(item);
            if (string.Equals(id, selectedId, StringComparison.OrdinalIgnoreCase)) item.Selected = true;
        }

        var confirm = new Button { Text = "确定", AutoSize = true, DialogResult = DialogResult.OK };
        var cancel = new Button { Text = "取消", AutoSize = true, DialogResult = DialogResult.Cancel };
        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 52,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(10),
            BackColor = Color.FromArgb(248, 247, 243)
        };
        footer.Controls.Add(confirm);
        footer.Controls.Add(cancel);
        Controls.Add(_list);
        Controls.Add(footer);
        AcceptButton = confirm;
        CancelButton = cancel;
        _list.DoubleClick += (_, _) => { if (_list.SelectedItems.Count > 0) DialogResult = DialogResult.OK; };
    }

    public string? SelectedId => _list.SelectedItems.Count == 0 ? null : _list.SelectedItems[0].Tag as string;

    protected override void Dispose(bool disposing)
    {
        if (disposing) _images.Dispose();
        base.Dispose(disposing);
    }

    private static Bitmap CreateThumbnail(Image source, Size size)
    {
        var result = new Bitmap(size.Width, size.Height);
        using var graphics = Graphics.FromImage(result);
        graphics.Clear(Color.Transparent);
        graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        var scale = Math.Min(size.Width / (float)source.Width, size.Height / (float)source.Height);
        var width = (int)(source.Width * scale);
        var height = (int)(source.Height * scale);
        graphics.DrawImage(source, (size.Width - width) / 2, (size.Height - height) / 2, width, height);
        return result;
    }

    private static string DisplayName(string title, string id, int index)
    {
        if (title.Contains("植物")) return id;
        return title.Contains("花盆") ? $"花盆 {index + 1}" : $"表情 {index + 1}";
    }
}
