namespace RefinedGem.Services;

/// <summary>
/// In-memory per-player refined pool snapshots for the current multiplayer lobby/run.
/// Does not write to refined_pool.json.
/// </summary>
internal static class RefinedPoolSessionStore
{
    private static readonly object Lock = new();
    private static readonly Dictionary<ulong, IReadOnlyList<string>> PoolsByNetId = new();
    private static bool _frozen;

    public static bool HasEntries
    {
        get
        {
            lock (Lock)
                return PoolsByNetId.Count > 0;
        }
    }

    public static bool IsFrozen
    {
        get
        {
            lock (Lock)
                return _frozen;
        }
    }

    public static void Set(ulong netId, IEnumerable<string> cardIds)
    {
        var snapshot = Canonicalize(cardIds);
        lock (Lock)
            PoolsByNetId[netId] = snapshot;
    }

    public static bool TryGet(ulong netId, out IReadOnlyList<string> cardIds)
    {
        lock (Lock)
            return PoolsByNetId.TryGetValue(netId, out cardIds!);
    }

    public static IReadOnlyList<string> GetUnionCardIds()
    {
        lock (Lock)
        {
            if (PoolsByNetId.Count == 0)
                return [];

            var seen = new HashSet<string>(StringComparer.Ordinal);
            var union = new List<string>();
            foreach (var pool in PoolsByNetId.Values)
            {
                foreach (var id in pool)
                {
                    if (seen.Add(id))
                        union.Add(id);
                }
            }

            union.Sort(StringComparer.Ordinal);
            return union;
        }
    }

    public static void Freeze()
    {
        lock (Lock)
            _frozen = true;
    }

    public static void Clear()
    {
        lock (Lock)
        {
            PoolsByNetId.Clear();
            _frozen = false;
        }
    }

    private static IReadOnlyList<string> Canonicalize(IEnumerable<string> cardIds)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var list = new List<string>();
        foreach (var id in cardIds)
        {
            if (string.IsNullOrWhiteSpace(id))
                continue;

            if (seen.Add(id))
                list.Add(id);
        }

        list.Sort(StringComparer.Ordinal);
        return list;
    }
}
