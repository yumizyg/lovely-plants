using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using DesktopGarden.Core;

namespace DesktopGarden;

internal enum PotAnimationKind { Water }

internal enum FooterControlKind { WorkTimer, Pomodoro }

internal sealed record PotAnimation(PotAnimationKind Kind, DateTime StartedUtc)
{
    public double Progress(DateTime nowUtc) => Math.Clamp((nowUtc - StartedUtc).TotalMilliseconds / DurationMilliseconds, 0, 1);
    public int DurationMilliseconds => 1150;
    public bool IsFinished(DateTime nowUtc) => (nowUtc - StartedUtc).TotalMilliseconds >= DurationMilliseconds;
}

internal sealed record GardenOverlayState(string WorkTimeText, string PomodoroText, bool PomodoroExpired, bool PomodoroActive);

internal sealed record GardenRenderResult(
    Bitmap Bitmap,
    IReadOnlyList<Rectangle> Slots,
    IReadOnlyDictionary<FooterControlKind, Rectangle> FooterControls,
    bool OwnsBitmap = true) : IDisposable
{
    public void Dispose()
    {
        if (OwnsBitmap) Bitmap.Dispose();
    }
}

internal sealed class GardenRenderer(AssetCatalog catalog, ImageCache images)
{
    private const int FooterHeight = 70;
    private const int FooterTopPadding = 12;
    private const int FooterCardWidth = 246;
    private const int FooterCardHeight = 42;
    private const int FooterCardGap = 12;
    private const int FooterSidePadding = 12;

    public Size Measure(GardenState state, float effectiveScale)
    {
        var gap = Scale(LayoutCalculator.GetGap(state.Settings.GapScale), effectiveScale);
        var sizes = GetSlotSizes(state.Pots, effectiveScale);
        var potHeight = Math.Max(1, sizes.Count == 0 ? Scale(LayoutCalculator.BaseSlotHeight, effectiveScale) : sizes.Max(size => size.Height));
        var potWidth = Math.Max(1, sizes.Sum(size => size.Width) + Math.Max(0, state.Pots.Count - 1) * gap);
        return new Size(
            Math.Max(potWidth, FooterContentWidth),
            potHeight + FooterTopPadding + FooterHeight);
    }

    public GardenRenderResult Render(
        GardenState state,
        float effectiveScale,
        IReadOnlyDictionary<Guid, PotAnimation> animations,
        IReadOnlyDictionary<Guid, string> temporaryExpressions,
        GardenOverlayState overlay,
        IReadOnlyList<PotInstance>? arrangedPots = null,
        Guid? draggedPotId = null,
        float dragOffsetX = 0,
        Bitmap? target = null,
        DateTime? renderTimeUtc = null)
    {
        var pots = arrangedPots ?? state.Pots;
        var count = Math.Max(1, pots.Count);
        var gap = Scale(LayoutCalculator.GetGap(state.Settings.GapScale), effectiveScale);
        var slotSizes = GetSlotSizes(pots, effectiveScale);
        var potWidth = slotSizes.Sum(size => size.Width) + Math.Max(0, count - 1) * gap;
        var potAreaHeight = slotSizes.Count == 0 ? Scale(LayoutCalculator.BaseSlotHeight, effectiveScale) : slotSizes.Max(size => size.Height);
        var width = Math.Max(potWidth, FooterContentWidth);
        var height = potAreaHeight + FooterTopPadding + FooterHeight;
        width = Math.Max(1, width);
        height = Math.Max(1, height);
        var bitmap = target ?? new Bitmap(width, height, PixelFormat.Format32bppPArgb);
        if (bitmap.Width != width || bitmap.Height != height) throw new ArgumentException("Target bitmap has the wrong dimensions.", nameof(target));
        var slots = new List<Rectangle>(pots.Count);

        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.Transparent);
        graphics.CompositingMode = CompositingMode.SourceOver;
        graphics.CompositingQuality = CompositingQuality.HighQuality;
        graphics.InterpolationMode = InterpolationMode.HighQualityBilinear;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;

        var now = renderTimeUtc ?? DateTime.UtcNow;
        var x = Math.Max(0, (width - potWidth) / 2);
        for (var index = 0; index < pots.Count; index++)
        {
            var size = slotSizes[index];
            var slot = new Rectangle(x, potAreaHeight - size.Height, size.Width, size.Height);
            slots.Add(slot);
            var pot = pots[index];
            DrawPot(
                graphics,
                slot,
                pot,
                temporaryExpressions.GetValueOrDefault(pot.Id),
                animations.GetValueOrDefault(pot.Id),
                draggedPotId == pot.Id ? dragOffsetX : 0,
                now);
            x += size.Width + gap;
        }

        var footerControls = DrawFooterOverlayAndCapture(graphics, new Size(width, height), overlay);
        return new GardenRenderResult(bitmap, slots, footerControls, target is null);
    }

    public IReadOnlyDictionary<FooterControlKind, Rectangle> GetFooterControls(Size size) => ComputeFooterControls(size);

    public void DrawFooterOverlay(Graphics graphics, Size canvasSize, GardenOverlayState overlay)
    {
        var footerBounds = FooterArea(canvasSize);
        using var clearBrush = new SolidBrush(Color.Transparent);
        graphics.CompositingMode = CompositingMode.SourceCopy;
        graphics.FillRectangle(clearBrush, footerBounds);
        graphics.CompositingMode = CompositingMode.SourceOver;
        DrawFooterOverlayInternal(graphics, canvasSize, overlay);
    }

    private IReadOnlyDictionary<FooterControlKind, Rectangle> DrawFooterOverlayAndCapture(Graphics graphics, Size canvasSize, GardenOverlayState overlay)
        => DrawFooterOverlayInternal(graphics, canvasSize, overlay);

    private Dictionary<FooterControlKind, Rectangle> DrawFooterOverlayInternal(Graphics graphics, Size canvasSize, GardenOverlayState overlay)
    {
        graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        var controls = ComputeFooterControls(canvasSize);
        DrawTimerCard(graphics, controls[FooterControlKind.WorkTimer], "工", "工作时间", overlay.WorkTimeText, FluentTheme.Accent, FluentTheme.Text);
        var pomodoroColor = overlay.PomodoroExpired ? FluentTheme.Danger : FluentTheme.Coral;
        var valueColor = overlay.PomodoroExpired ? FluentTheme.Danger : FluentTheme.Text;
        DrawTimerCard(graphics, controls[FooterControlKind.Pomodoro], "番", "番茄钟", overlay.PomodoroText, pomodoroColor, valueColor, overlay.PomodoroActive);
        return controls;
    }

    private static Dictionary<FooterControlKind, Rectangle> ComputeFooterControls(Size canvasSize)
    {
        var totalWidth = FooterContentWidth - FooterSidePadding * 2;
        var startX = (canvasSize.Width - totalWidth) / 2;
        var y = canvasSize.Height - FooterHeight + 14;
        return new Dictionary<FooterControlKind, Rectangle>
        {
            [FooterControlKind.WorkTimer] = new Rectangle(startX, y, FooterCardWidth, FooterCardHeight),
            [FooterControlKind.Pomodoro] = new Rectangle(startX + FooterCardWidth + FooterCardGap, y, FooterCardWidth, FooterCardHeight)
        };
    }

    private static Rectangle FooterArea(Size canvasSize) => new(0, Math.Max(0, canvasSize.Height - FooterHeight), canvasSize.Width, FooterHeight);

    private static int FooterContentWidth => FooterCardWidth * 2 + FooterCardGap + FooterSidePadding * 2;

    private static void DrawTimerCard(Graphics graphics, Rectangle bounds, string iconText, string title, string value, Color accent, Color valueColor, bool emphasize = false)
    {
        using var path = RoundedPath(bounds, 12);
        using var shadowBrush = new SolidBrush(Color.FromArgb(28, 0, 0, 0));
        using var cardBrush = new SolidBrush(Color.FromArgb(238, 251, 251, 248));
        using var borderPen = new Pen(Color.FromArgb(220, 229, 221));
        graphics.FillPath(shadowBrush, RoundedPath(new Rectangle(bounds.X, bounds.Y + 2, bounds.Width, bounds.Height), 12));
        graphics.FillPath(cardBrush, path);
        graphics.DrawPath(borderPen, path);

        var iconBounds = new Rectangle(bounds.Left + 10, bounds.Top + 9, 24, 24);
        using var iconBrush = new SolidBrush(Color.FromArgb(emphasize ? 244 : 235, accent));
        using var iconTextBrush = new SolidBrush(Color.White);
        graphics.FillEllipse(iconBrush, iconBounds);
        using var iconFont = new Font("Microsoft YaHei UI", 9f, FontStyle.Bold);
        var center = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        graphics.DrawString(iconText, iconFont, iconTextBrush, iconBounds, center);

        using var titleBrush = new SolidBrush(FluentTheme.TextMuted);
        using var valueBrush = new SolidBrush(valueColor);
        using var titleFont = new Font("Microsoft YaHei UI", 7.5f);
        using var valueFont = new Font("Consolas", 12f, FontStyle.Bold);
        var titleRect = new RectangleF(bounds.Left + 42, bounds.Top + 12, 66, 18);
        var valueRect = new RectangleF(bounds.Left + 110, bounds.Top + 8, bounds.Width - 118, 24);
        var valueFormat = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center };
        graphics.DrawString(title, titleFont, titleBrush, titleRect);
        graphics.DrawString(value, valueFont, valueBrush, valueRect, valueFormat);
    }

    private static GraphicsPath RoundedPath(Rectangle bounds, int radius)
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

    private void DrawPot(Graphics graphics, Rectangle slot, PotInstance instance, string? temporaryExpression, PotAnimation? animation, float dragOffsetX, DateTime now)
    {
        var wholePot = graphics.Save();
        if (Math.Abs(dragOffsetX) > 0.1f) graphics.TranslateTransform(dragOffsetX, 0);

        var phaseOffset = (Math.Abs(instance.Id.GetHashCode()) % 1000) / 1000d * Math.PI * 2;
        var seconds = (now - DateTime.UnixEpoch).TotalSeconds;
        var plantWave = Math.Sin(seconds * Math.PI * 2 / 4.0 + phaseOffset);
        var expressionWave = Math.Sin(seconds * Math.PI * 2 / 2.0 + phaseOffset * 0.7);

        if (!string.IsNullOrWhiteSpace(instance.PlantId))
        {
            var plant = images.Get(catalog.PlantPath(instance.PlantId, instance.GrowthStage));
            var plantRect = FitInside(plant.Size, new Rectangle(slot.Left, slot.Top, slot.Width, (int)(slot.Height * 0.66f)), alignBottom: true);
            plantRect.Offset(0, -(int)Math.Round(slot.Height * 0.055f));
            var scaledPlant = images.GetScaled(catalog.PlantPath(instance.PlantId, instance.GrowthStage), plantRect.Size);
            var plantState = graphics.Save();
            var plantCenterX = plantRect.Left + plantRect.Width / 2f;
            var plantBottom = plantRect.Bottom;
            graphics.TranslateTransform(plantCenterX, plantBottom);
            graphics.RotateTransform((float)(plantWave * 2.35));
            graphics.TranslateTransform(-plantCenterX, -plantBottom + (float)(plantWave * 4.6));
            graphics.DrawImageUnscaled(scaledPlant, plantRect.Location);
            graphics.Restore(plantState);
        }

        var pot = images.Get(catalog.PotPath(instance.PotId));
        var potArea = new Rectangle(slot.Left, slot.Top + (int)(slot.Height * 0.51f), slot.Width, (int)(slot.Height * 0.49f));
        var potRect = FitPotByHeight(pot.Size, potArea);
        graphics.DrawImageUnscaled(images.GetScaled(catalog.PotPath(instance.PotId), potRect.Size), potRect.Location);

        var expression = images.Get(catalog.ExpressionPath(temporaryExpression ?? instance.ExpressionId));
        var faceArea = new Rectangle(
            potRect.Left + (int)(potRect.Width * 0.14f),
            potRect.Top + (int)(potRect.Height * 0.2f),
            (int)(potRect.Width * 0.72f),
            (int)(potRect.Height * 0.52f));
        var expressionRect = FitInside(expression.Size, faceArea, alignBottom: false);
        expressionRect = ExpandAroundCenter(expressionRect, potRect, 2f, 0.92f, 0.72f);
        expressionRect.Offset(0, (int)Math.Round(expressionWave * 3.6));
        graphics.DrawImageUnscaled(images.GetScaled(catalog.ExpressionPath(temporaryExpression ?? instance.ExpressionId), expressionRect.Size), expressionRect.Location);
        graphics.Restore(wholePot);

        if (animation?.Kind == PotAnimationKind.Water)
        {
            DrawWater(graphics, slot, animation.Progress(now));
        }
    }

    private static void DrawWater(Graphics graphics, Rectangle slot, double progress)
    {
        var fade = 1d - progress;
        using var dropBrush = new SolidBrush(Color.FromArgb((int)(235 * fade), 94, 184, 238));
        using var streakPen = new Pen(Color.FromArgb((int)(200 * fade), 128, 212, 255), Math.Max(2f, slot.Width / 85f))
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };
        using var splashBrush = new SolidBrush(Color.FromArgb((int)(170 * fade), 94, 184, 238));

        for (var index = 0; index < 11; index++)
        {
            var phase = (progress * 1.18 + index * 0.09) % 1;
            var x = slot.Left + slot.Width * (0.14f + index * 0.07f);
            var startY = slot.Top + slot.Height * (0.06f + (float)(index % 3) * 0.02f);
            var length = slot.Height * (0.13f + (float)((index % 4) * 0.012f));
            var y = startY + slot.Height * (float)phase * 0.46f;
            graphics.DrawLine(streakPen, x, y, x - slot.Width * 0.012f, y + length);
            var radius = Math.Max(3, slot.Width / 45);
            graphics.FillEllipse(dropBrush, x - radius / 2f, y + length - radius * 0.35f, radius, radius * 1.35f);
        }

        var splashY = slot.Top + slot.Height * 0.54f;
        for (var index = 0; index < 5; index++)
        {
            var spread = (float)(index - 2) * slot.Width * 0.06f;
            var height = slot.Height * (0.015f + (float)(1 - Math.Abs(index - 2) / 3d) * 0.028f) * (float)fade;
            graphics.FillEllipse(
                splashBrush,
                slot.Left + slot.Width * 0.5f + spread - slot.Width * 0.028f,
                splashY - height,
                slot.Width * 0.056f,
                Math.Max(3f, height * 2.2f));
        }
    }

    private static Rectangle FitInside(Size image, Rectangle bounds, bool alignBottom)
    {
        var scale = Math.Min(bounds.Width / (float)image.Width, bounds.Height / (float)image.Height);
        var width = Math.Max(1, (int)Math.Round(image.Width * scale));
        var height = Math.Max(1, (int)Math.Round(image.Height * scale));
        var x = bounds.Left + (bounds.Width - width) / 2;
        var y = alignBottom ? bounds.Bottom - height : bounds.Top + (bounds.Height - height) / 2;
        return new Rectangle(x, y, width, height);
    }

    private static Rectangle FitPotByHeight(Size image, Rectangle bounds)
    {
        var targetHeight = Math.Max(1, bounds.Height);
        var scale = targetHeight / (float)image.Height;
        var width = Math.Max(1, (int)Math.Round(image.Width * scale));
        var x = bounds.Left + (bounds.Width - width) / 2;
        var y = bounds.Bottom - targetHeight;
        return new Rectangle(x, y, width, targetHeight);
    }

    private static Rectangle ExpandAroundCenter(Rectangle source, Rectangle potRect, float factor, float maxWidthRatio, float maxHeightRatio)
    {
        var centerX = source.Left + source.Width / 2f;
        var centerY = source.Top + source.Height / 2f;
        var width = Math.Min((int)Math.Round(source.Width * factor), Math.Max(1, (int)Math.Round(potRect.Width * maxWidthRatio)));
        var height = Math.Min((int)Math.Round(source.Height * factor), Math.Max(1, (int)Math.Round(potRect.Height * maxHeightRatio)));
        var x = (int)Math.Round(centerX - width / 2f);
        var y = (int)Math.Round(centerY - height / 2f);
        return new Rectangle(x, y, Math.Max(1, width), Math.Max(1, height));
    }

    private static int Scale(float value, float scale) => Math.Max(1, (int)Math.Round(value * scale));

    private List<Size> GetSlotSizes(IReadOnlyList<PotInstance> pots, float effectiveScale) => pots
        .Select(pot =>
        {
            var slotHeight = Scale(LayoutCalculator.BaseSlotHeight * pot.Scale, effectiveScale);
            var baseSlotWidth = Scale(LayoutCalculator.BaseSlotWidth * pot.Scale, effectiveScale);
            var potTargetHeight = Math.Max(1, (int)Math.Round(slotHeight * 0.49f));
            var potImage = images.Get(catalog.PotPath(pot.PotId));
            var scaledPotWidth = (int)Math.Ceiling(potImage.Width * (potTargetHeight / (float)potImage.Height));
            var slotWidth = Math.Max(baseSlotWidth, scaledPotWidth + Math.Max(10, slotHeight / 25));
            return new Size(slotWidth, slotHeight);
        })
        .ToList();
}
