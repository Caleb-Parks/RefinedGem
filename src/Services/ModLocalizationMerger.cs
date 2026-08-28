using System.Reflection;
using System.Text.Json;
using MegaCrit.Sts2.Core.Localization;

namespace RefinedGem.Services;

internal static class ModLocalizationMerger
{
    private static readonly (string Table, string Resource)[] TableResources =
    [
        ("relics", "RefinedGem.locales.eng.relics.json"),
        ("static_hover_tips", "RefinedGem.locales.eng.static_hover_tips.json"),
    ];

    internal static IReadOnlyDictionary<string, int> MergeIntoGameTables(Assembly assembly)
    {
        var merged = new Dictionary<string, int>();
        foreach (var (table, resource) in TableResources)
        {
            merged[table] = MergeTable(assembly, table, resource);
        }

        return merged;
    }

    private static int MergeTable(Assembly assembly, string tableName, string resourceName)
    {
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
            return 0;

        var entries = JsonSerializer.Deserialize<Dictionary<string, string>>(stream);
        if (entries is null || entries.Count == 0)
            return 0;

        LocManager.Instance.GetTable(tableName).MergeWith(entries);
        return entries.Count;
    }
}
