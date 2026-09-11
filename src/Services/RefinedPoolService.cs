using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using RefinedGem.Content;

namespace RefinedGem.Services;

public static class RefinedPoolService
{
    public const int MinimumRewardCards = 3;

    private static readonly ConditionalWeakTable<Player, HashSet<string>> MerchantExcludedCardIds = new();

    public static bool ShouldUseRefinedPool(Player player) =>
        player.GetRelic<RefinedGemRelic>() is not null && GetActiveCardCount() > 0;

    public static int GetActiveCardCount() => GetCanonicalCardsForProfile().Count;

    public static CardCreationOptions ApplyCardCreationOptions(Player player, CardCreationOptions options)
    {
        if (!ShouldUseRefinedPool(player))
            return options;

        var eligibleCards = GetDistinctCardsForRun(player);
        if (eligibleCards.Count < MinimumRewardCards)
            return options;

        var allowed = eligibleCards
            .Select(GetStableCardId)
            .ToHashSet(StringComparer.Ordinal);

        return options
            .WithCardPools([ModelDb.CardPool<RefinedCardPool>()])
            .WithFilter(card => allowed.Contains(GetStableCardId(card)))
            .WithRarityOdds(CardRarityOddsType.Uniform);
    }

    private static void TrackMerchantSelectedCardId(Player player, string cardId)
    {
        if (!MerchantExcludedCardIds.TryGetValue(player, out var excluded))
            return;

        excluded.Add(cardId);
    }

    public static void BeginMerchantPopulation(Player player)
    {
        if (!MerchantExcludedCardIds.TryGetValue(player, out var excluded))
        {
            MerchantExcludedCardIds.Add(player, []);
            return;
        }

        excluded.Clear();
    }

    public static void TrackMerchantSelectedCard(Player player, CardModel card) =>
        TrackMerchantSelectedCardId(player, GetStableCardId(card.CanonicalInstance));

    public static IEnumerable<CardModel> GetMerchantCardsForRun(Player player, IEnumerable<CardModel> vanillaCards)
    {
        var vanillaList = vanillaCards.ToList();

        if (!ShouldUseRefinedPool(player))
            return vanillaList;

        if (IsColorlessMerchantPool(vanillaList))
            return vanillaList;

        var fullEligible = GetDistinctCardsForRun(player);
        var eligible = fullEligible;
        var excludedCount = 0;
        if (MerchantExcludedCardIds.TryGetValue(player, out var excluded))
        {
            excludedCount = excluded.Count;
            eligible = fullEligible.Where(card => !excluded.Contains(GetStableCardId(card))).ToList();
        }

        if (eligible.Count == 0)
            return vanillaList;

        if (fullEligible.Count < MinimumRewardCards && excludedCount == 0)
            return vanillaList;

        // Gate on the full pool, not the post-exclusion remainder. Slot stocking removes
        // attacks/skills before the Power entry runs; remaining skill count must not force vanilla.
        if (!HasMerchantTypeCoverage(fullEligible))
            return vanillaList;

        return eligible;
    }

    public static bool ContainsCard(CardModel card) =>
        RefinedPoolFileStore.Contains(GetStableCardId(card));

    public static bool ToggleCard(CardModel card)
    {
        RefinedPoolFileStore.ToggleCardId(GetStableCardId(card));
        InvalidatePoolCache();
        return true;
    }

    public static IReadOnlyList<CardModel> GetCanonicalCardsForProfile()
    {
        var cards = new List<CardModel>();
        var seenIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var id in RefinedPoolFileStore.GetCardIds())
        {
            if (!seenIds.Add(id))
                continue;

            if (TryResolveCard(id, out var card))
                cards.Add(card.CanonicalInstance);
        }

        return cards;
    }

    public static IEnumerable<CardModel> GetCardsForRun(Player player)
    {
        var constraint = player.RunState.CardMultiplayerConstraint;
        return GetCanonicalCardsForProfile()
            .Where(card => IsEligibleForRun(card, constraint));
    }

    public static IReadOnlyList<CardModel> GetDistinctCardsForRun(Player player) =>
        GetCardsForRun(player).ToList();

    private static bool IsColorlessMerchantPool(IReadOnlyList<CardModel> cards) =>
        cards.Count > 0 && cards.All(card => card.Pool.IsColorless);

    private static bool HasMerchantTypeCoverage(IReadOnlyList<CardModel> cards)
    {
        // MerchantInventory stocks Attack, Attack, Skill, Skill, Power and CreateForMerchant
        // excludes Basic cards, so coverage must use non-Basic counts for those slot totals.
        var usable = cards.Where(card => card.Rarity != CardRarity.Basic).ToList();
        return usable.Count(card => card.Type == CardType.Attack) >= 2
            && usable.Count(card => card.Type == CardType.Skill) >= 2
            && usable.Count(card => card.Type == CardType.Power) >= 1;
    }

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
