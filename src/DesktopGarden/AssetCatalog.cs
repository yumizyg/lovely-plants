using System.Text.RegularExpressions;
using DesktopGarden.Core;

namespace DesktopGarden;

internal sealed partial class AssetCatalog
{
    private static readonly IReadOnlyDictionary<string, string> PlantNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["Alocasia"] = "海芋",
        ["Anthurium"] = "红掌",
        ["BostonFern"] = "波士顿蕨",
        ["BroadleafSapling"] = "阔叶幼苗",
        ["Cactus"] = "仙人掌",
        ["Coreopsis"] = "金鸡菊",
        ["Daisy"] = "雏菊",
        ["Fittonia"] = "网纹草",
        ["Hydrangea"] = "绣球花",
        ["Lavender"] = "薰衣草",
        ["Marguerite"] = "木茼蒿",
        ["Monstera"] = "龟背竹",
        ["PinkOxalis"] = "粉花酢浆草",
        ["PurpleOxalis"] = "紫叶酢浆草",
        ["RedBerryHolly"] = "红果冬青",
        ["RedRose"] = "红玫瑰",
        ["Sunflower"] = "向日葵",
        ["ZZPlant"] = "金钱树"
    };

    private readonly Dictionary<string, PlantDefinition> _plants;
    private readonly Dictionary<string, string> _pots;
    private readonly Dictionary<string, string> _expressions;

    public AssetCatalog(string root)
    {
        Root = root;
        _plants = LoadPlants(System.IO.Path.Combine(root, "Plants"));
        _pots = LoadFiles(System.IO.Path.Combine(root, "Pots"), NaturalNumber);
        _expressions = LoadFiles(System.IO.Path.Combine(root, "Expressions"), TrailingNumber);

        if (_plants.Count == 0 || _pots.Count == 0 || _expressions.Count == 0)
        {
            throw new InvalidOperationException("应用素材不完整，请重新安装 Lovely Plants。");
        }
    }

    public string Root { get; }
    public IReadOnlyList<PlantDefinition> Plants => _plants.Values.OrderBy(item => item.Id, StringComparer.OrdinalIgnoreCase).ToList();
    public IReadOnlyList<string> PlantIds => Plants.Select(item => item.Id).ToList();
    public IReadOnlyList<string> PotIds => _pots.Keys.OrderBy(NaturalNumber).ToList();
    public IReadOnlyList<string> ExpressionIds => _expressions.Keys.OrderBy(TrailingNumber).ToList();

    public string PlantPath(string id, int stage)
    {
        if (!_plants.TryGetValue(id, out var definition))
        {
            definition = Plants[0];
        }

        return definition.StagePaths.TryGetValue(stage, out var path) ? path : definition.StagePaths[1];
    }

    public string PotPath(string id) => _pots.TryGetValue(id, out var path) ? path : _pots.Values.First();

    public string ExpressionPath(string id) => _expressions.TryGetValue(id, out var path) ? path : _expressions.Values.First();

    public string GrassPath => System.IO.Path.Combine(Root, "glass.png");

    public string PlantName(string id) => string.IsNullOrWhiteSpace(id) ? "未种植" : PlantNames.TryGetValue(id, out var name) ? name : id;

    public string PotName(string id) => $"花盆 {Math.Max(0, PotIds.ToList().IndexOf(id)) + 1}";

    public string ExpressionName(string id) => $"表情 {Math.Max(0, ExpressionIds.ToList().IndexOf(id)) + 1}";

    public void RepairState(GardenState state)
    {
        foreach (var pot in state.Pots)
        {
            if (!string.IsNullOrWhiteSpace(pot.PlantId) && !_plants.ContainsKey(pot.PlantId)) pot.PlantId = PlantIds[0];
            if (!_pots.ContainsKey(pot.PotId)) pot.PotId = PotIds[0];
            if (!_expressions.ContainsKey(pot.ExpressionId)) pot.ExpressionId = ExpressionIds[0];
        }
    }

    private static Dictionary<string, PlantDefinition> LoadPlants(string directory)
    {
        return Directory.EnumerateFiles(directory, "*.png")
            .Select(path => (Path: path, Match: PlantFileName().Match(System.IO.Path.GetFileNameWithoutExtension(path))))
            .Where(item => item.Match.Success)
            .GroupBy(item => item.Match.Groups[1].Value, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() == 3)
            .ToDictionary(
                group => group.Key,
                group => new PlantDefinition(group.Key, group.ToDictionary(item => int.Parse(item.Match.Groups[2].Value), item => item.Path)),
                StringComparer.OrdinalIgnoreCase);
    }

    private static Dictionary<string, string> LoadFiles(string directory, Func<string, int> order)
    {
        return Directory.EnumerateFiles(directory, "*.png")
            .OrderBy(path => order(System.IO.Path.GetFileNameWithoutExtension(path)))
            .ToDictionary(path => System.IO.Path.GetFileNameWithoutExtension(path)!, path => path, StringComparer.OrdinalIgnoreCase);
    }

    private static int NaturalNumber(string value) => int.TryParse(value, out var number) ? number : int.MaxValue;

    private static int TrailingNumber(string value)
    {
        var match = TrailingDigits().Match(value);
        return match.Success && int.TryParse(match.Groups[1].Value, out var number) ? number : int.MaxValue;
    }

    [GeneratedRegex("^(.+?)([123])$")]
    private static partial Regex PlantFileName();

    [GeneratedRegex("(\\d+)$")]
    private static partial Regex TrailingDigits();
}
