using System.Text.Json.Serialization;

namespace DesktopGarden.Core;

public sealed class GardenState
{
    public int SchemaVersion { get; set; } = 2;
    public List<PotInstance> Pots { get; set; } = [];
    public AppSettings Settings { get; set; } = new();
}

public sealed class PotInstance
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string PlantId { get; set; } = string.Empty;
    public string PotId { get; set; } = string.Empty;
    public string ExpressionId { get; set; } = string.Empty;
    public double ElapsedRunSeconds { get; set; }
    public int SortOrder { get; set; }
    public float Scale { get; set; } = 1f;

    [JsonIgnore]
    public int GrowthStage => GrowthPolicy.GetStage(ElapsedRunSeconds);
}

public sealed class AppSettings
{
    public int MonitorIndex { get; set; }
    public float Scale { get; set; } = 0.70f;
    public float GapScale { get; set; } = 1f;
    public bool ShowGrassBackground { get; set; } = true;
    public bool AlwaysOnTop { get; set; } = true;
    public bool InteractionLocked { get; set; }
    public bool SoundEnabled { get; set; }
    public bool StartWithWindows { get; set; }
    public bool GardenVisible { get; set; } = true;
    public int GardenOffsetX { get; set; }
    public int GardenOffsetY { get; set; }
}

public sealed record PlantDefinition(string Id, IReadOnlyDictionary<int, string> StagePaths);
