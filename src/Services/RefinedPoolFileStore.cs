using System.Text.Json;
using Godot;
using RefinedGem.Data;
using STS2RitsuLib.Utils.Persistence;

namespace RefinedGem.Services;

internal static class RefinedPoolFileStore
{
    private const string FileName = "refined_pool.json";

    private static readonly object Lock = new();
    private static readonly JsonSerializerOptions WriteOptions = new() { WriteIndented = true };

    private static List<string> _cardIds = [];
    private static DateTime _loadedWriteTimeUtc;
    private static string? _filePath;
    private static bool _initialized;

    internal static IReadOnlyList<string> GetCardIds()
    {
        EnsureLoaded();
        return _cardIds;
    }

    internal static bool Contains(string cardId)
    {
        EnsureLoaded();
        return _cardIds.Contains(cardId, StringComparer.Ordinal);
    }

    internal static void ToggleCardId(string cardId)
    {
        lock (Lock)
        {
            EnsureLoaded(forceReload: false);
            if (!_cardIds.Remove(cardId))
                _cardIds.Add(cardId);

            SaveInternal();
        }
    }

    private static void EnsureLoaded(bool forceReload = false)
    {
        lock (Lock)
        {
            var path = GetFilePath();

            if (!File.Exists(path))
            {
                if (!_initialized)
                {
                    TryMigrateLegacyProfile(path);
                    if (!File.Exists(path))
                        WriteFile(path, []);
                }

                _cardIds = File.Exists(path) ? ParseFile(path) : [];
                _loadedWriteTimeUtc = File.Exists(path) ? File.GetLastWriteTimeUtc(path) : DateTime.MinValue;
                _initialized = true;
                return;
            }

            var writeTime = File.GetLastWriteTimeUtc(path);
            if (_initialized && !forceReload && writeTime == _loadedWriteTimeUtc)
                return;

            _cardIds = ParseFile(path);
            _loadedWriteTimeUtc = writeTime;
            _initialized = true;
        }
    }

    private static string GetFilePath()
    {
        if (_filePath is not null)
            return _filePath;

        var assemblyPath = typeof(RefinedGemEntry).Assembly.Location;
        var modDir = Path.GetDirectoryName(assemblyPath)
            ?? throw new InvalidOperationException("Could not resolve mod directory from assembly location.");

        _filePath = Path.Combine(modDir, FileName);
        return _filePath;
    }

    private static void SaveInternal()
    {
        var path = GetFilePath();
        var deduped = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var id in _cardIds)
        {
            if (seen.Add(id))
                deduped.Add(id);
        }

        _cardIds = deduped;
        WriteFile(path, _cardIds);
        _loadedWriteTimeUtc = File.GetLastWriteTimeUtc(path);
        _initialized = true;
    }

    private static void WriteFile(string path, IReadOnlyList<string> cardIds)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(cardIds, WriteOptions));
    }

    private static List<string> ParseFile(string path)
    {
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            return document.RootElement.ValueKind switch
            {
                JsonValueKind.Array => ParseArray(document.RootElement),
                JsonValueKind.Object => ParseLegacyProfile(document.RootElement),
                _ => [],
            };
        }
        catch (Exception ex)
        {
            RefinedGemEntry.Logger.Warn($"Failed to parse {FileName}; treating pool as empty. {ex.Message}");
            return [];
        }
    }

    private static List<string> ParseArray(JsonElement element)
    {
        var ids = new List<string>();
        foreach (var item in element.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String)
                continue;

            var value = item.GetString();
            if (!string.IsNullOrWhiteSpace(value))
                ids.Add(value);
        }

        return ids;
    }

    private static List<string> ParseLegacyProfile(JsonElement element)
    {
        if (!element.TryGetProperty(nameof(RefinedPoolProfile.CardIds), out var cardIds)
            && !element.TryGetProperty("cardIds", out cardIds))
            return [];

        return ParseArray(cardIds);
    }

    private static void TryMigrateLegacyProfile(string targetPath)
    {
        try
        {
            var legacyPath = ResolveLegacyProfilePath();
            if (legacyPath is null || !File.Exists(legacyPath))
                return;

            var migrated = ParseFile(legacyPath);
            if (migrated.Count == 0)
                return;

            WriteFile(targetPath, migrated);
            RefinedGemEntry.Logger.Info($"Migrated {migrated.Count} card(s) from profile-scoped pool data to {FileName}.");
        }
        catch (Exception ex)
        {
            RefinedGemEntry.Logger.Warn($"Could not migrate legacy profile pool data: {ex.Message}");
        }
    }

    private static string? ResolveLegacyProfilePath()
    {
        var godotPath = ProfileManager.Instance.GetFilePath(
            FileName,
            SaveScope.Profile,
            RefinedGemEntry.ModId);

        if (string.IsNullOrWhiteSpace(godotPath))
            return null;

        return godotPath.StartsWith("user://", StringComparison.Ordinal)
            ? ProjectSettings.GlobalizePath(godotPath)
            : godotPath;
    }
}
