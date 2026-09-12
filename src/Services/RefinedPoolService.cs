using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using RefinedGem.Content;
using RefinedGem.Patches;

namespace RefinedGem.Services;

public static class RefinedPoolService
{
    public const int MinimumRewardCards = 3;

    private static readonly ConditionalWeakTable<Player, HashSet<string>> MerchantExcludedCardIds = new();
    private static readonly AsyncLocal<int> ModificationBypassDepth = new();

    public static bool ShouldUseRefinedPool(Player player) =>
        player.GetRelic<RefinedGemRelic>() is not null && GetCardIdsForPlayer(player).Count > 0;

    public static bool IsModificationBypassed => ModificationBypassDepth.Value > 0;

    public static int GetActiveCardCount() => GetCanonicalCardsForProfile().Count;

    /// <summary>
    /// Temporarily skip refined pool rewriting (Sea Glass exemption, last-resort vanilla fallback).
    /// </summary>
    public static ModificationBypassScope EnterModificationBypass()
    {
        ModificationBypassDepth.Value++;
        return new ModificationBypassScope();
    }

    public readonly struct ModificationBypassScope : IDisposable
    {
        public void Dispose()
        {
            if (ModificationBypassDepth.Value > 0)
                ModificationBypassDepth.Value--;
        }
    }

    public static CardCreationOptions CloneCardCreationOptions(CardCreationOptions options)
    {
        var clone = new CardCreationOptions(
            options.CardPools.ToList(),
            options.Source,
            options.RarityOdds,
            options.CardPoolFilter);

        if (options.Flags != 0)
            clone.WithFlags(options.Flags);

        if (options.RngOverride != null)
            clone.WithRngOverride(options.RngOverride);

        return clone;
    }

    public static CardCreationOptions ApplyCardCreationOptions(Player player, CardCreationOptions options)
    {
        if (!ShouldRewriteCardCreationOptions(player, options))
            return options;

        var eligibleCards = GetDistinctCardsForRun(player);
        var allowed = eligibleCards
            .Select(GetStableCardId)
            .ToHashSet(StringComparer.Ordinal);
        var prior = options.CardPoolFilter;

        // Clone so WithCardPools/WithFilter do not mutate the caller's vanilla options.
        return CloneCardCreationOptions(options)
            .WithCardPools([ModelDb.CardPool<RefinedCardPool>()])
            .WithFilter(card =>
                allowed.Contains(GetStableCardId(card))
                && (prior == null || prior(card)));
    }

    /// <summary>
    /// Refined pool with per-player ID allowlist only (drops any prior rarity/type/cost filter).
    /// </summary>
    public static CardCreationOptions CreateBroadenedRefinedOptions(Player player, CardCreationOptions snapshot)
    {
        var eligibleCards = GetDistinctCardsForRun(player);
        var allowed = eligibleCards
            .Select(GetStableCardId)
            .ToHashSet(StringComparer.Ordinal);

        return CloneCardCreationOptions(snapshot)
            .WithCardPools([ModelDb.CardPool<RefinedCardPool>()])
            .WithFilter(card => allowed.Contains(GetStableCardId(card)));
    }

    /// <summary>
    /// True when CreateForReward should run the constrained→broaden→Uniform→vanilla ladder.
    /// Already-refined options (e.g. from ForRoom) still intercept so rarity fallback works.
    /// </summary>
    public static bool ShouldInterceptCreateForReward(Player player, CardCreationOptions options)
    {
        if (!ShouldUseRefinedPool(player) || IsModificationBypassed)
            return false;

        if (IsRefinedPoolOptions(options))
            return true;

        return ShouldRewriteCardCreationOptions(player, options);
    }

    private static bool ShouldRewriteCardCreationOptions(Player player, CardCreationOptions options)
    {
        if (!ShouldUseRefinedPool(player) || IsModificationBypassed)
            return false;

        // Colorless / off-character sources stay vanilla (Brain Leech, Philosophers, Kaleidoscope, etc.).
        // RefinedCardPool is labeled colorless, so only apply this before rewriting.
        if (ContainsColorlessPool(options) || IsOffCharacterOnly(player, options))
            return false;

        var usableRewardCards = GetDistinctCardsForRun(player).Count(card =>
            card.Rarity is not CardRarity.Basic and not CardRarity.Ancient);
        return usableRewardCards >= MinimumRewardCards;
    }

    private static bool IsRefinedPoolOptions(CardCreationOptions options)
    {
        var refined = ModelDb.CardPool<RefinedCardPool>();
        return options.CardPools.Count > 0
            && options.CardPools.All(pool => ReferenceEquals(pool, refined));
    }

    private static bool ContainsColorlessPool(CardCreationOptions options) =>
        options.CardPools.Any(pool => pool.IsColorless);

    private static bool IsOffCharacterOnly(Player player, CardCreationOptions options)
    {
        if (options.CardPools.Count == 0)
            return false;

        var ownerPool = player.Character.CardPool;
        var characterPools = ModelDb.AllCharacterCardPools.ToHashSet();
        return options.CardPools.All(pool =>
            characterPools.Contains(pool) && !ReferenceEquals(pool, ownerPool));
    }

    /// <summary>
    /// When Refined Gem is active, replace vanilla transform pools with the refined pool.
    /// Prefers vanilla Common/Uncommon/Rare filtering; on failure, falls back to a uniform pick
    /// among remaining refined candidates (still excluding the original and applying combat/MP filters).
    /// </summary>
    public static bool TryGetTransformationOptions(
        CardModel original,
        bool isInCombat,
        out IEnumerable<CardModel> options)
    {
        options = null!;
        if (!ShouldUseRefinedPool(original.Owner))
            return false;

        var candidates = GetDistinctCardsForRun(original.Owner);
        if (candidates.Count == 0)
            return false;

        try
        {
            options = CardFactoryGetFilteredTransformationOptionsOriginal.Invoke(
                original,
                candidates,
                isInCombat);
            return true;
        }
        catch (InvalidOperationException)
        {
            var fallback = BuildUniformTransformationOptions(original, candidates, isInCombat);
            if (fallback.Count == 0)
                throw;

            options = fallback;
            return true;
        }
    }

    private static IReadOnlyList<CardModel> BuildUniformTransformationOptions(
        CardModel original,
        IReadOnlyList<CardModel> candidates,
        bool isInCombat)
    {
        IEnumerable<CardModel> source = candidates.Where(card => card.Id != original.Id);
        if (isInCombat)
            source = source.Where(card => card.CanBeGeneratedInCombat);

        return CardFactoryFilterForPlayerCountOriginal.Invoke(original.Owner.RunState, source).ToList();
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
        IReadOnlyList<CardModel> remainingEligible = fullEligible;
        if (MerchantExcludedCardIds.TryGetValue(player, out var excluded) && excluded.Count > 0)
            remainingEligible = fullEligible.Where(card => !excluded.Contains(GetStableCardId(card))).ToList();

        // Mix per type: refined where the full pool covers that type's shop slots; vanilla otherwise.
        // Gate on the full pool so stocking Attack/Skill slots does not flip later types to vanilla.
        var mixed = new List<CardModel>();
        foreach (var type in MerchantColoredCardTypes)
        {
            if (HasCoverageForType(fullEligible, type))
                mixed.AddRange(remainingEligible.Where(card => card.Type == type));
            else
                mixed.AddRange(vanillaList.Where(card => card.Type == type));
        }

        return mixed.Count > 0 ? mixed : vanillaList;
    }

    public static bool ContainsCard(CardModel card) =>
        RefinedPoolFileStore.Contains(GetStableCardId(card));

    public static bool ToggleCard(CardModel card)
    {
        RefinedPoolFileStore.ToggleCardId(GetStableCardId(card));
        InvalidatePoolCache();
        return true;
    }

    public static void SetCardsIncluded(IEnumerable<CardModel> cards, bool included)
    {
        SetCardIdsIncluded(cards.Select(GetStableCardId), included);
    }

    public static void SetCardIdsIncluded(IEnumerable<string> cardIds, bool included)
    {
        RefinedPoolFileStore.SetCardIdsIncluded(cardIds, included);
        InvalidatePoolCache();
    }

    /// <summary>
    /// Local curated pool for Card Library editing/UI. Ignores multiplayer session snapshots.
    /// </summary>
    public static IReadOnlyList<CardModel> GetCanonicalCardsForProfile() =>
        ResolveCards(RefinedPoolFileStore.GetCardIds());

    /// <summary>
    /// Cards exposed by <see cref="RefinedCardPool"/> (no player context). Uses the union of
    /// session snapshots in multiplayer so remote-only ids can still resolve.
    /// </summary>
    public static IReadOnlyList<CardModel> GetCardsForCardPoolModel()
    {
        if (RefinedPoolSessionStore.HasEntries)
            return ResolveCards(RefinedPoolSessionStore.GetUnionCardIds());

        return GetCanonicalCardsForProfile();
    }

    public static IReadOnlyList<string> GetCardIdsForPlayer(Player player)
    {
        if (RefinedPoolSessionStore.TryGet(player.NetId, out var synced))
            return synced;

        if (LocalContext.IsMe(player))
            return RefinedPoolFileStore.GetCardIds();

        return [];
    }

    public static IEnumerable<CardModel> GetCardsForRun(Player player)
    {
        var constraint = player.RunState.CardMultiplayerConstraint;
        return ResolveCards(GetCardIdsForPlayer(player))
            .Where(card => IsEligibleForRun(card, constraint));
    }

    public static IReadOnlyList<CardModel> GetDistinctCardsForRun(Player player) =>
        GetCardsForRun(player).ToList();

    private static IReadOnlyList<CardModel> ResolveCards(IEnumerable<string> cardIds)
    {
        var cards = new List<CardModel>();
        var seenIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var id in cardIds)
        {
            if (!seenIds.Add(id))
                continue;

            if (TryResolveCard(id, out var card))
                cards.Add(card.CanonicalInstance);
        }

        return cards;
    }

    private static bool IsColorlessMerchantPool(IReadOnlyList<CardModel> cards) =>
        cards.Count > 0 && cards.All(card => card.Pool.IsColorless);

    // Matches MerchantInventory._coloredCardTypes slot totals: Attack x2, Skill x2, Power x1.
    private static readonly CardType[] MerchantColoredCardTypes =
    [
        CardType.Attack,
        CardType.Skill,
        CardType.Power,
    ];

    private static int GetMerchantSlotRequirement(CardType type) =>
        type switch
        {
            CardType.Attack => 2,
            CardType.Skill => 2,
            CardType.Power => 1,
            _ => 0,
        };

    private static bool HasCoverageForType(IReadOnlyList<CardModel> cards, CardType type)
    {
        var required = GetMerchantSlotRequirement(type);
        if (required <= 0)
            return false;

        // CreateForMerchant excludes Basic cards, so only non-Basic count toward coverage.
        return cards.Count(card => card.Type == type && card.Rarity != CardRarity.Basic) >= required;
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
