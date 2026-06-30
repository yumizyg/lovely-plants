namespace DesktopGarden.Core;

public static class LayoutCalculator
{
    public const float VisualScaleMultiplier = 0.5f;
    public const int BaseSlotWidth = 250;
    public const int BaseSlotHeight = 500;
    public const int BaseGap = 12;
    public const int HorizontalMargin = 24;

    public static int GetCapacity(int workingAreaWidth, float scale, float gapScale = 1f)
    {
        var slot = (BaseSlotWidth + GetGap(gapScale)) * ToRenderedScale(scale);
        return Math.Max(1, (int)Math.Floor((workingAreaWidth - HorizontalMargin * 2) / slot));
    }

    public static float FitScale(int workingAreaWidth, int potCount, float preferredScale, float gapScale = 1f)
    {
        if (potCount <= 0)
        {
            return preferredScale;
        }

        var available = Math.Max(1, workingAreaWidth - HorizontalMargin * 2);
        var requiredAtOne = potCount * BaseSlotWidth + Math.Max(0, potCount - 1) * GetGap(gapScale);
        return Math.Clamp(Math.Min(preferredScale, available / (float)requiredAtOne) * VisualScaleMultiplier, 0.125f, 0.6f);
    }

    public static float GetGap(float gapScale) => BaseGap * Math.Clamp(gapScale, 0.6f, 8f);

    private static float ToRenderedScale(float preferredScale) => Math.Clamp(preferredScale, 0.5f, 1.2f) * VisualScaleMultiplier;
}
