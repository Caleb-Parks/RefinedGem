using System.Reflection;
using System.Text.Json;

namespace RefinedGem.UI;

internal static class RefinedGemUiText
{
    private static readonly IReadOnlyDictionary<string, string> Strings = LoadStrings();

    public static string Get(string key) =>
        Strings.TryGetValue(key, out var value) ? value : key;

    private static IReadOnlyDictionary<string, string> LoadStrings()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("RefinedGem.locales.eng.json");
        if (stream is null)
            return new Dictionary<string, string>();

        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
    }
}
