using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using RefinedGem.Content;
using RefinedGem.Data;
using STS2RitsuLib;
using STS2RitsuLib.Data;
using STS2RitsuLib.Utils.Persistence;

namespace RefinedGem.Services;

public static class RefinedPoolService
{
    private static ModDataStore Store => RitsuLibFramework.GetDataStore(RefinedGemEntry.ModId);

    public static bool ShouldUseRefinedPool(Player player) =>
        player.GetRelic<RefinedGemRelic>() is not null && GetActiveCardCount() > 0;

    public static int GetActiveCardCount() => GetProfile().CardIds.Count;

    public static bool ContainsCard(CardModel card) =>
        GetProfile().CardIds.Contains(GetStableCardId(card));

    public static bool ToggleCard(CardModel card)
    {
        var id = GetStableCardId(card);

        Store.Modify<RefinedPoolProfile>("refined_pool", profile =>
        {
            if (!profile.CardIds.Remove(id))
                profile.CardIds.Add(id);
        });

        Store.Save("refined_pool");
        InvalidatePoolCache();
        return true;
    }

    public static IReadOnlyList<CardModel> GetCanonicalCardsForProfile()
    {
        var cards = new List<CardModel>();
        foreach (var id in GetProfile().CardIds)
        {
            if (TryResolveCard(id, out var card))
                cards.Add(card);
        }

        return cards;
    }

    public static IEnumerable<CardModel> GetCardsForRun(Player player)
    {
        var unlockState = player.UnlockState;
        var constraint = player.RunState.CardMultiplayerConstraint;
        return ModelDb.CardPool<RefinedCardPool>()
            .GetUnlockedCards(unlockState, constraint);
    }

    public static void InvalidatePoolCache() => RefinedCardPool.InvalidateCachedCards();

    private static RefinedPoolProfile GetProfile() =>
        Store.Get<RefinedPoolProfile>("refined_pool");

    private static string GetStableCardId(CardModel card) =>
        card.CanonicalInstance.Id.Entry;

    private static bool TryResolveCard(string entry, out CardModel card)
    {
        card = null!;
        foreach (var candidate in ModelDb.AllCards)
        {
            if (!string.Equals(candidate.Id.Entry, entry, StringComparison.Ordinal))
                continue;

            card = candidate;
            return true;
        }

        return false;
    }
}
