namespace DesktopGarden.Core;

public static class GrowthPolicy
{
    public const double StageTwoSeconds = 8 * 60 * 60;
    public const double StageThreeSeconds = 40 * 60 * 60;

    public static int GetStage(double elapsedRunSeconds) => elapsedRunSeconds switch
    {
        >= StageThreeSeconds => 3,
        >= StageTwoSeconds => 2,
        _ => 1
    };

    public static double GetStageProgress(double elapsedRunSeconds)
    {
        var elapsed = Math.Max(0, elapsedRunSeconds);
        return GetStage(elapsed) switch
        {
            1 => Math.Clamp(elapsed / StageTwoSeconds, 0, 1),
            2 => Math.Clamp((elapsed - StageTwoSeconds) / (StageThreeSeconds - StageTwoSeconds), 0, 1),
            _ => 1
        };
    }

    public static double GetSecondsUntilNextStage(double elapsedRunSeconds)
    {
        var elapsed = Math.Max(0, elapsedRunSeconds);
        return GetStage(elapsed) switch
        {
            1 => Math.Max(0, StageTwoSeconds - elapsed),
            2 => Math.Max(0, StageThreeSeconds - elapsed),
            _ => 0
        };
    }

    public static void Accumulate(GardenState state, TimeSpan elapsed)
    {
        if (elapsed <= TimeSpan.Zero)
        {
            return;
        }

        foreach (var pot in state.Pots)
        {
            if (string.IsNullOrWhiteSpace(pot.PlantId))
            {
                continue;
            }
            pot.ElapsedRunSeconds += elapsed.TotalSeconds;
        }
    }
}
