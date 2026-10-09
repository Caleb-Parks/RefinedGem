using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using RefinedGem.Services;

namespace RefinedGem.Patches;

[HarmonyPatch(typeof(MerchantInventory), nameof(MerchantInventory.CreateForNormalMerchant))]
internal static class MerchantInventoryCreatePatch
{
    [HarmonyPrefix]
    private static void Prefix(Player player) =>
        RefinedPoolService.BeginMerchantPopulation(player);
}

[HarmonyPatch(typeof(MerchantCardEntry), nameof(MerchantCardEntry.Populate))]
internal static class MerchantCardEntryPopulatePatch
{
    [HarmonyPostfix]
    private static void Postfix(MerchantCardEntry __instance)
    {
        var card = __instance.CreationResult?.Card;
        var inventory = Traverse.Create(__instance).Field<MerchantInventory>("_inventory").Value;
        var player = inventory?.Player;
        if (card is null || player is null)
            return;

        RefinedPoolService.TrackMerchantSelectedCard(player, card);
    }
}

[HarmonyPatch(typeof(CardFactory), nameof(CardFactory.CreateForMerchant), typeof(Player), typeof(IEnumerable<CardModel>), typeof(CardType))]
internal static class CardFactoryCreateForMerchantByTypePatch
{
    [HarmonyPrefix]
    private static void Prefix(Player player, ref IDisposable? __state) =>
        __state = RefinedPoolService.EnterStarterAsCommon(player);

    [HarmonyFinalizer]
    private static void Finalizer(IDisposable? __state) => __state?.Dispose();
}

[HarmonyPatch(typeof(CardFactory), nameof(CardFactory.CreateForMerchant), typeof(Player), typeof(IEnumerable<CardModel>), typeof(CardRarity))]
internal static class CardFactoryCreateForMerchantByRarityPatch
{
    [HarmonyPrefix]
    private static void Prefix(Player player, ref IDisposable? __state) =>
        __state = RefinedPoolService.EnterStarterAsCommon(player);

    [HarmonyFinalizer]
    private static void Finalizer(IDisposable? __state) => __state?.Dispose();
}
