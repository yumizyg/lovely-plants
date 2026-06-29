using DesktopGarden.Core;

namespace DesktopGarden.Tests;

public sealed class LayoutCalculatorTests
{
    [Fact]
    public void CapacityAlwaysAllowsAtLeastOnePot()
    {
        Assert.Equal(1, LayoutCalculator.GetCapacity(100, 1));
    }

    [Fact]
    public void FitScaleShrinksCrowdedGarden()
    {
        var scale = LayoutCalculator.FitScale(1366, 8, 1f);
        Assert.InRange(scale, 0.5f, 0.7f);
    }

    [Fact]
    public void LargerGapReducesCapacity()
    {
        var compact = LayoutCalculator.GetCapacity(1920, 1f, 0.6f);
        var roomy = LayoutCalculator.GetCapacity(1920, 1f, 2f);
        Assert.True(roomy < compact);
    }

    [Fact]
    public void NormalizeMakesSortOrderContiguous()
    {
        var state = new GardenState
        {
            Pots =
            [
                new PotInstance { SortOrder = 9 },
                new PotInstance { SortOrder = 2 }
            ]
        };

        GardenStateFactory.Normalize(state);

        Assert.Equal([0, 1], state.Pots.Select(pot => pot.SortOrder));
        Assert.All(state.Pots, pot => Assert.Equal(1f, pot.Scale));
    }
}
