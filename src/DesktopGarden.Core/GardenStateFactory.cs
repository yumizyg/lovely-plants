namespace DesktopGarden.Core;

public static class GardenStateFactory
{
    public static GardenState CreateDefault(
        IReadOnlyList<string> plantIds,
        IReadOnlyList<string> potIds,
        IReadOnlyList<string> expressionIds,
        int count = 3)
    {
        var state = new GardenState();
        if (plantIds.Count == 0 || potIds.Count == 0 || expressionIds.Count == 0)
        {
            return state;
        }

        for (var index = 0; index < Math.Min(count, plantIds.Count); index++)
        {
            state.Pots.Add(new PotInstance
            {
                PlantId = plantIds[index % plantIds.Count],
                PotId = potIds[index % potIds.Count],
                ExpressionId = expressionIds[index % expressionIds.Count],
                SortOrder = index,
                Scale = 0.97f
            });
        }

        return state;
    }

    public static void Normalize(GardenState state)
    {
        state.Pots = state.Pots
            .OrderBy(pot => pot.SortOrder)
            .ThenBy(pot => pot.Id)
            .ToList();

        for (var index = 0; index < state.Pots.Count; index++)
        {
            state.Pots[index].SortOrder = index;
            state.Pots[index].ElapsedRunSeconds = Math.Max(0, state.Pots[index].ElapsedRunSeconds);
            state.Pots[index].Scale = state.Pots[index].Scale <= 0 ? 1f : Math.Clamp(state.Pots[index].Scale, 0.6f, 1.4f);
            if (string.IsNullOrWhiteSpace(state.Pots[index].PlantId))
            {
                state.Pots[index].ElapsedRunSeconds = 0;
            }
        }

        state.SchemaVersion = 3;
        state.Settings.Scale = Math.Clamp(state.Settings.Scale, 0.5f, 1.2f);
        state.Settings.GapScale = Math.Clamp(state.Settings.GapScale, 0.6f, 2f);
        state.Settings.MonitorIndex = Math.Max(0, state.Settings.MonitorIndex);
    }
}
