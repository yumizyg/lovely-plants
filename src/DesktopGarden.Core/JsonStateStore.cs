using System.Text.Json;

namespace DesktopGarden.Core;

public sealed class JsonStateStore(string path)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public string Path { get; } = path;

    public GardenState? Load()
    {
        if (!File.Exists(Path))
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(Path);
            var state = JsonSerializer.Deserialize<GardenState>(json, JsonOptions);
            if (state is null)
            {
                throw new JsonException("State file was empty.");
            }

            GardenStateFactory.Normalize(state);
            return state;
        }
        catch (Exception exception) when (exception is JsonException or IOException or UnauthorizedAccessException)
        {
            BackupCorruptFile();
            return null;
        }
    }

    public void Save(GardenState state)
    {
        GardenStateFactory.Normalize(state);
        var directory = System.IO.Path.GetDirectoryName(Path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var temporaryPath = Path + ".tmp";
        var json = JsonSerializer.Serialize(state, JsonOptions);
        File.WriteAllText(temporaryPath, json);
        File.Move(temporaryPath, Path, true);
    }

    private void BackupCorruptFile()
    {
        var backupPath = $"{Path}.corrupt-{DateTime.UtcNow:yyyyMMddHHmmss}";
        try
        {
            File.Move(Path, backupPath, false);
        }
        catch (IOException)
        {
            File.Copy(Path, backupPath + "-copy", true);
            File.Delete(Path);
        }
    }
}

