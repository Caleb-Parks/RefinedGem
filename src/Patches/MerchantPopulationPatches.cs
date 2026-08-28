using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
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
