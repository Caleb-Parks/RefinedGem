using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using RefinedGem.Content;
using RefinedGem.Data;
using STS2RitsuLib;
using STS2RitsuLib.Data;
using STS2RitsuLib.Utils.Persistence;

namespace RefinedGem.Services;

public static class RefinedPoolService
{
    public const int MinimumRewardCards = 3;

    private static ModDataStore Store => RitsuLibFramework.GetDataStore(RefinedGemEntry.ModId);

    public static bool ShouldUseRefinedPool(Player player) =>
        player.GetRelic<RefinedGemRelic>() is not null && GetActiveCardCount() > 0;

    public static int GetActiveCardCount() => GetProfile().CardIds.Count;

    public static CardCreationOptions ApplyCardCreationOptions(Player player, CardCreationOptions options)
    {
        if (!ShouldUseRefinedPool(player))
            return options;

        var eligibleCards = GetCardsForRun(player).ToList();
        if (eligibleCards.Count < MinimumRewardCards)
            return options;

        var allowed = eligibleCards
            .Select(card => card.CanonicalInstance)
            .ToHashSet();

        return options
            .WithCardPools([ModelDb.CardPool<RefinedCardPool>()])
            .WithFilter(card => allowed.Contains(card.CanonicalInstance))
            .WithRarityOdds(CardRarityOddsType.Uniform);
    }

    public static IEnumerable<CardModel> GetMerchantCardsForRun(Player player, IEnumerable<CardModel> vanillaCards)
    {
        var vanillaList = vanillaCards.ToList();

        if (!ShouldUseRefinedPool(player))
            return vanillaList;

        var eligible = GetCardsForRun(player).ToList();
        if (eligible.Count < MinimumRewardCards)
            return vanillaList;

        if (IsColorlessMerchantPool(vanillaList))
            return vanillaList;

        if (!HasMerchantTypeCoverage(eligible))
            return vanillaList;

        return eligible;
    }

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
        var constraint = player.RunState.CardMultiplayerConstraint;
        return GetCanonicalCardsForProfile()
            .Where(card => IsEligibleForRun(card, constraint));
    }

    private static bool IsColorlessMerchantPool(IReadOnlyList<CardModel> cards) =>
        cards.Count > 0 && cards.All(card => card.Pool.IsColorless);

    private static bool HasMerchantTypeCoverage(IReadOnlyList<CardModel> cards) =>
        cards.Any(card => card.Type == CardType.Attack)
        && cards.Any(card => card.Type == CardType.Skill)
        && cards.Any(card => card.Type == CardType.Power);

    private static bool IsEligibleForRun(CardModel card, CardMultiplayerConstraint runConstraint) =>
        card.MultiplayerConstraint switch
        {
            CardMultiplayerConstraint.None => true,
            CardMultiplayerConstraint.MultiplayerOnly =>
                runConstraint is CardMultiplayerConstraint.None or CardMultiplayerConstraint.MultiplayerOnly,
            CardMultiplayerConstraint.SingleplayerOnly =>
                runConstraint is CardMultiplayerConstraint.None or CardMultiplayerConstraint.SingleplayerOnly,
            _ => true,
        };

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
