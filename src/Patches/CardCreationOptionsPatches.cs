using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Runs;
using RefinedGem.Services;

namespace RefinedGem.Patches;

[HarmonyPatch(typeof(CardCreationOptions), nameof(CardCreationOptions.ForRoom))]
internal static class CardCreationOptionsForRoomPatch
{
    [HarmonyPostfix]
    private static void Postfix(Player player, ref CardCreationOptions __result) =>
        __result = RefinedPoolService.ApplyCardCreationOptions(player, __result);
}

/// <summary>
/// Prefer vanilla room rarity odds for refined rewards; if generation fails because the pool
/// cannot satisfy the rolled rarities, retry once with Uniform among the refined pool.
/// </summary>
[HarmonyPatch(typeof(CardFactory), nameof(CardFactory.CreateForReward), typeof(Player), typeof(int), typeof(CardCreationOptions))]
internal static class CardFactoryCreateForRewardRarityFallbackPatch
{
    [HarmonyPrefix]
    private static bool Prefix(
        Player player,
        int cardCount,
        CardCreationOptions options,
        ref IEnumerable<CardCreationResult> __result)
    {
        if (!RefinedPoolService.ShouldUseRefinedPool(player))
            return true;

        try
        {
            __result = CardFactoryCreateForRewardOriginal.Invoke(player, cardCount, options);
            return false;
        }
        catch (InvalidOperationException)
        {
            if (options.RarityOdds == CardRarityOddsType.Uniform)
                throw;

            __result = CardFactoryCreateForRewardOriginal.Invoke(
                player,
                cardCount,
                options.WithRarityOdds(CardRarityOddsType.Uniform));
            return false;
        }
    }
}

[HarmonyPatch(typeof(CardFactory), nameof(CardFactory.CreateForReward), typeof(Player), typeof(int), typeof(CardCreationOptions))]
internal static class CardFactoryCreateForRewardOriginal
{
    [HarmonyReversePatch]
    internal static IEnumerable<CardCreationResult> Invoke(
        Player player,
        int cardCount,
        CardCreationOptions options) =>
        throw new NotImplementedException("Harmony reverse patch stub.");
}
