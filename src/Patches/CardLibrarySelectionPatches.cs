using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using RefinedGem.Services;
using RefinedGem.UI;
using STS2RitsuLib.Ui.Toast;

namespace RefinedGem.Patches;

[HarmonyPatch(typeof(NCardLibrary), "_Ready")]
internal static class CardLibraryReadyPatch
{
    [HarmonyPostfix]
    private static void Postfix(NCardLibrary __instance) =>
        CardLibrarySelectionController.Attach(__instance);
}

[HarmonyPatch(typeof(NCardLibrary), "ShowCardDetail")]
internal static class CardLibraryShowCardDetailPatch
{
    [HarmonyPrefix]
    private static bool Prefix(CardModel card)
    {
        if (!CardLibrarySelectionController.EditModeEnabled)
            return true;

        var wasInPool = RefinedPoolService.ContainsCard(card);
        if (!CardLibrarySelectionController.TryToggleCard(card))
            return false;

        RitsuToastService.ShowInfo(
            "Refined Gem",
            wasInPool
                ? RefinedGemUiText.Get("refined_gem.ui.card_removed")
                : RefinedGemUiText.Get("refined_gem.ui.card_added"));

        return false;
    }
}

[HarmonyPatch(typeof(NGridCardHolder), "SetCard")]
internal static class GridCardHolderSetCardPatch
{
    [HarmonyPostfix]
    private static void Postfix(NGridCardHolder __instance, CardModel card)
    {
        if (!RefinedPoolService.ContainsCard(card))
            return;

        __instance.Modulate = new Godot.Color(0.85f, 1f, 0.95f);
    }
}
