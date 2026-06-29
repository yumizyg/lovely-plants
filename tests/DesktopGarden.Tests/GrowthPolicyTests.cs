using DesktopGarden.Core;

namespace DesktopGarden.Tests;

public sealed class GrowthPolicyTests
{
    [Theory]
    [InlineData(0, 1)]
    [InlineData(28799, 1)]
    [InlineData(28800, 2)]
    [InlineData(143999, 2)]
    [InlineData(144000, 3)]
    public void StageBoundariesAreStable(double seconds, int expectedStage)
    {
        Assert.Equal(expectedStage, GrowthPolicy.GetStage(seconds));
    }

    [Fact]
    public void AccumulateOnlyAddsExplicitRuntime()
    {
        var state = new GardenState
        {
            Pots = [new PotInstance { ElapsedRunSeconds = 10 }]
        };

        GrowthPolicy.Accumulate(state, TimeSpan.FromSeconds(5));

        Assert.Equal(15, state.Pots[0].ElapsedRunSeconds);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(14400, 0.5)]
    [InlineData(28800, 0)]
    [InlineData(86400, 0.5)]
    [InlineData(144000, 1)]
    public void StageProgressUsesCurrentStageWindow(double seconds, double expected)
    {
        Assert.Equal(expected, GrowthPolicy.GetStageProgress(seconds), 3);
    }
}
