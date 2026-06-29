using DesktopGarden.Core;

namespace DesktopGarden;

internal sealed class PlantInfoForm : Form
{
    private readonly Label _name = new();
    private readonly Label _stage = new();
    private readonly Label _elapsed = new();
    private readonly Label _remaining = new();
    private readonly FluentProgressBar _progress = new();

    public PlantInfoForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;
        ShowIcon = false;
        BackColor = FluentTheme.Surface;
        ClientSize = new Size(292, 118);
        Padding = new Padding(14, 12, 14, 11);
        Opacity = 0.97;

        _name.Font = FluentTheme.Title;
        _name.ForeColor = FluentTheme.Text;
        _name.AutoSize = true;
        _stage.Font = FluentTheme.Caption;
        _stage.ForeColor = FluentTheme.Accent;
        _stage.AutoSize = true;
        _elapsed.Font = FluentTheme.Caption;
        _elapsed.ForeColor = FluentTheme.TextMuted;
        _elapsed.AutoSize = true;
        _remaining.Font = FluentTheme.Caption;
        _remaining.ForeColor = FluentTheme.TextMuted;
        _remaining.AutoSize = true;

        var header = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 28, WrapContents = false, Margin = Padding.Empty };
        header.Controls.Add(_name);
        _stage.Margin = new Padding(10, 4, 0, 0);
        header.Controls.Add(_stage);
        _elapsed.Location = new Point(14, 46);
        _remaining.Location = new Point(14, 88);
        _progress.Location = new Point(14, 74);
        _progress.Width = 264;
        Controls.Add(header);
        Controls.Add(_elapsed);
        Controls.Add(_progress);
        Controls.Add(_remaining);
    }

    protected override bool ShowWithoutActivation => true;

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        Region?.Dispose();
        Region = FluentTheme.RoundedRegion(Size, 8);
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var parameters = base.CreateParams;
            parameters.ExStyle |= NativeMethods.WsExToolWindow | NativeMethods.WsExNoActivate | NativeMethods.WsExTransparent;
            parameters.ClassStyle |= 0x00020000;
            return parameters;
        }
    }

    public void UpdatePot(PotInstance pot, AssetCatalog catalog)
    {
        if (string.IsNullOrWhiteSpace(pot.PlantId))
        {
            _name.Text = "未种植";
            _stage.Text = "等待播种";
            _elapsed.Text = "这盆花还没有种下植物";
            _progress.Value = 0;
            _remaining.Text = "右键打开面板后选择植物";
            return;
        }

        _name.Text = catalog.PlantName(pot.PlantId);
        _stage.Text = StageName(pot.GrowthStage);
        _elapsed.Text = $"已陪伴 {FormatDuration(pot.ElapsedRunSeconds)}";
        _progress.Value = GrowthPolicy.GetStageProgress(pot.ElapsedRunSeconds);
        var remaining = GrowthPolicy.GetSecondsUntilNextStage(pot.ElapsedRunSeconds);
        _remaining.Text = pot.GrowthStage == 3 ? "已经长成，仍会继续陪伴你" : $"距离下一阶段 {FormatDuration(remaining)}";
    }

    public void ShowNear(Rectangle potBounds, IWin32Window owner)
    {
        var workArea = Screen.FromRectangle(potBounds).WorkingArea;
        var x = Math.Clamp(potBounds.Left + (potBounds.Width - Width) / 2, workArea.Left + 8, workArea.Right - Width - 8);
        var y = potBounds.Top - Height - 10;
        if (y < workArea.Top + 8) y = Math.Min(workArea.Bottom - Height - 8, potBounds.Bottom + 10);
        Location = new Point(x, y);
        if (!Visible) Show(owner);
    }

    private static string StageName(int stage) => stage switch
    {
        1 => "阶段 1 · 幼苗期",
        2 => "阶段 2 · 生长期",
        _ => "阶段 3 · 成熟期"
    };

    internal static string FormatDuration(double seconds)
    {
        var duration = TimeSpan.FromSeconds(Math.Max(0, seconds));
        if (duration.TotalHours >= 1) return $"{(int)duration.TotalHours}小时 {duration.Minutes}分钟";
        return $"{Math.Max(1, duration.Minutes)}分钟";
    }
}
