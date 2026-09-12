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
/// Constrained refined rewards first; on failure broaden to any refined card, then Uniform odds,
/// then last-resort vanilla so tiny pools cannot soft-lock high-count events.
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
        if (!RefinedPoolService.ShouldInterceptCreateForReward(player, options))
            return true;

        var vanillaSnapshot = RefinedPoolService.CloneCardCreationOptions(options);

        try
        {
            __result = CardFactoryCreateForRewardOriginal.Invoke(player, cardCount, options);
            return false;
        }
        catch (InvalidOperationException)
        {
            // Drop event/relic rarity-type-cost filters; keep refined ID allowlist.
            try
            {
                var broadened = RefinedPoolService.CreateBroadenedRefinedOptions(player, vanillaSnapshot);
                __result = CardFactoryCreateForRewardOriginal.Invoke(player, cardCount, broadened);
                return false;
            }
            catch (InvalidOperationException)
            {
                if (vanillaSnapshot.RarityOdds != CardRarityOddsType.Uniform)
                {
                    try
                    {
                        var uniform = RefinedPoolService
                            .CreateBroadenedRefinedOptions(player, vanillaSnapshot)
                            .WithRarityOdds(CardRarityOddsType.Uniform);
                        __result = CardFactoryCreateForRewardOriginal.Invoke(player, cardCount, uniform);
                        return false;
                    }
                    catch (InvalidOperationException)
                    {
                        // Fall through to vanilla last resort.
                    }
                }

                using (RefinedPoolService.EnterModificationBypass())
                {
                    __result = CardFactoryCreateForRewardOriginal.Invoke(player, cardCount, vanillaSnapshot);
                    return false;
                }
            }
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
