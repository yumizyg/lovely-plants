using DesktopGarden.Core;

namespace DesktopGarden.Tests;

public sealed class StateStoreTests : IDisposable
{
    private readonly string _directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"DesktopGardenTests-{Guid.NewGuid():N}");

    [Fact]
    public void SaveAndLoadRoundTripsState()
    {
        var path = System.IO.Path.Combine(_directory, "state.json");
        var store = new JsonStateStore(path);
        var state = new GardenState
        {
            Pots = [new PotInstance { PlantId = "Rose", PotId = "2", ExpressionId = "3", ElapsedRunSeconds = 123 }]
        };

        store.Save(state);
        var loaded = store.Load();

        Assert.NotNull(loaded);
        Assert.Equal("Rose", loaded.Pots[0].PlantId);
        Assert.Equal(123, loaded.Pots[0].ElapsedRunSeconds);
    }

    [Fact]
    public void CorruptStateIsBackedUp()
    {
        Directory.CreateDirectory(_directory);
        var path = System.IO.Path.Combine(_directory, "state.json");
        File.WriteAllText(path, "not-json");
        var store = new JsonStateStore(path);

        Assert.Null(store.Load());
        Assert.False(File.Exists(path));
        Assert.Single(Directory.GetFiles(_directory, "state.json.corrupt-*"));
    }

    [Fact]
    public void NewPositionAndPotScaleRoundTrip()
    {
        var path = System.IO.Path.Combine(_directory, "state.json");
        var store = new JsonStateStore(path);
        var state = new GardenState
        {
            Settings = new AppSettings { GardenOffsetX = 140, GardenOffsetY = -86 },
            Pots = [new PotInstance { PlantId = "Cactus", PotId = "1", ExpressionId = "1", Scale = 1.25f }]
        };

        store.Save(state);
        var loaded = store.Load();

        Assert.NotNull(loaded);
        Assert.Equal(140, loaded.Settings.GardenOffsetX);
        Assert.Equal(-86, loaded.Settings.GardenOffsetY);
        Assert.Equal(1.25f, loaded.Pots[0].Scale);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, true);
        }
    }
}
