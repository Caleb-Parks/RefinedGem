using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.Saves;
using RefinedGem.Data;

namespace RefinedGem.Services;

internal static class RefinedPoolFileStore
{
    private const string FileName = "refined_pool.json";
    private const string PoolFilesFolderName = "Pool Files";

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
            if (RefinedPoolBlacklist.IsBlacklisted(cardId))
            {
                if (_cardIds.Remove(cardId))
                    SaveInternal();
                return;
            }

            if (!_cardIds.Remove(cardId))
                _cardIds.Add(cardId);

            SaveInternal();
        }
    }

    internal static void SetCardIdsIncluded(IEnumerable<string> cardIds, bool included)
    {
        lock (Lock)
        {
            EnsureLoaded(forceReload: false);
            if (ApplyCardIdsIncluded(cardIds, included))
                SaveInternal();
        }
    }

    /// <summary>
    /// Adds ids that are not already present. Returns how many new ids were stored.
    /// </summary>
    internal static int AppendCardIds(IEnumerable<string> cardIds)
    {
        lock (Lock)
        {
            EnsureLoaded(forceReload: false);
            var before = _cardIds.Count;
            if (ApplyCardIdsIncluded(cardIds, included: true))
                SaveInternal();
            return _cardIds.Count - before;
        }
    }

    internal static void Clear()
    {
        lock (Lock)
        {
            EnsureLoaded(forceReload: false);
            _cardIds = [];
            SaveInternal();
        }
    }

    /// <summary>
    /// Writes a copy of the current pool. Does not modify the live pool file.
    /// </summary>
    internal static int ExportTo(string path)
    {
        lock (Lock)
        {
            EnsureLoaded(forceReload: false);
            WriteFile(path, _cardIds);
            return _cardIds.Count;
        }
    }

    internal static string GetPoolFilesDirectory()
    {
        var modDir = Path.GetDirectoryName(GetFilePath())
            ?? throw new InvalidOperationException("Could not resolve mod directory from assembly location.");
        var directory = Path.Combine(modDir, PoolFilesFolderName);
        Directory.CreateDirectory(directory);
        return directory;
    }

    /// <summary>
    /// Reads a pool file shaped like refined_pool.json. Invalid JSON is a failure, not an empty pool.
    /// </summary>
    internal static bool TryReadCardIds(string path, out IReadOnlyList<string> cardIds)
    {
        cardIds = [];
        try
        {
            if (!TryParseCardIds(File.ReadAllText(path), out var parsed))
            {
                RefinedGemEntry.Logger.Warn($"Pool file '{path}' is not a card id list.");
                return false;
            }

            cardIds = parsed;
            return true;
        }
        catch (Exception ex)
        {
            RefinedGemEntry.Logger.Warn($"Failed to read pool file '{path}': {ex.Message}");
            return false;
        }
    }

    private static bool ApplyCardIdsIncluded(IEnumerable<string> cardIds, bool included)
    {
        var changed = false;
        foreach (var cardId in cardIds)
        {
            if (string.IsNullOrWhiteSpace(cardId))
                continue;

            if (included && RefinedPoolBlacklist.IsBlacklisted(cardId))
                continue;

            if (included)
            {
                if (_cardIds.Contains(cardId, StringComparer.Ordinal))
                    continue;

                _cardIds.Add(cardId);
                changed = true;
            }
            else if (_cardIds.Remove(cardId))
            {
                changed = true;
            }
        }

        return changed;
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
                StripBlacklistedIds(path);
                return;
            }

            var writeTime = File.GetLastWriteTimeUtc(path);
            if (_initialized && !forceReload && writeTime == _loadedWriteTimeUtc)
                return;

            _cardIds = ParseFile(path);
            _loadedWriteTimeUtc = writeTime;
            _initialized = true;
            StripBlacklistedIds(path);
        }
    }

    private static void StripBlacklistedIds(string path)
    {
        if (_cardIds.RemoveAll(id => RefinedPoolBlacklist.IsBlacklisted(id)) <= 0)
            return;

        if (File.Exists(path))
            SaveInternal();
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
            return TryParseCardIds(File.ReadAllText(path), out var cardIds) ? cardIds : [];
        }
        catch (Exception ex)
        {
            RefinedGemEntry.Logger.Warn($"Failed to parse {FileName}; treating pool as empty. {ex.Message}");
            return [];
        }
    }

    private static bool TryParseCardIds(string json, out List<string> cardIds)
    {
        cardIds = [];
        using var document = JsonDocument.Parse(json);
        switch (document.RootElement.ValueKind)
        {
            case JsonValueKind.Array:
                cardIds = ParseArray(document.RootElement);
                return true;
            case JsonValueKind.Object:
                cardIds = ParseLegacyProfile(document.RootElement);
                return true;
            default:
                return false;
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
        try
        {
            var saveManager = SaveManager.Instance;
            if (saveManager is null)
                return null;

            var relativePath = Path.Combine("mod_data", RefinedGemEntry.ModId, FileName);
            var path = saveManager.GetProfileScopedPath(relativePath);
            if (string.IsNullOrWhiteSpace(path))
                return null;

            return path.StartsWith("user://", StringComparison.Ordinal)
                ? ProjectSettings.GlobalizePath(path)
                : path;
        }
        catch (Exception ex)
        {
            RefinedGemEntry.Logger.Warn($"Could not resolve legacy profile pool path: {ex.Message}");
            return null;
        }
    }
}
