namespace RefinedGem.Services;

/// <summary>
/// Stable card ids (<c>CardModel.Id.Entry</c>) that cannot be added to the refined pool.
/// </summary>
internal static class RefinedPoolBlacklist
{
    private static readonly HashSet<string> Entries = new(StringComparer.Ordinal)
    {
        "OUTRAGE",
        "LARGESSE",
    };

    internal static bool IsBlacklisted(string? cardId) =>
        cardId is not null && Entries.Contains(cardId);
}
