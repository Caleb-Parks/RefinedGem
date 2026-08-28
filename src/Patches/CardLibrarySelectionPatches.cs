using Godot;
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
        Callable.From(() => CardLibrarySelectionController.Attach(__instance)).CallDeferred();
}

[HarmonyPatch(typeof(NCardLibrary), "OnSubmenuOpened")]
internal static class CardLibraryOpenedPatch
{
    [HarmonyPostfix]
    private static void Postfix(NCardLibrary __instance) =>
        Callable.From(() => CardLibrarySelectionController.Attach(__instance)).CallDeferred();
}

[HarmonyPatch(typeof(NCardLibrary), "UpdateCardPoolFilter")]
internal static class CardLibraryPoolFilterPatch
{
    [HarmonyPostfix]
    private static void Postfix(NCardLibrary __instance, NCardPoolFilter filter) =>
        CardLibrarySelectionController.NotifyPoolFilterChanged(__instance, filter);
}

[HarmonyPatch(typeof(NCardLibrary), "ShowCardDetail")]
internal static class CardLibraryShowCardDetailPatch
{
    [HarmonyPrefix]
    private static bool Prefix(NCardHolder holder)
    {
        if (holder.CardModel is not CardModel card)
            return true;

        if (CardLibrarySelectionController.EditModeEnabled)
        {
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

        if (!CardLibrarySelectionController.IsRefinedPoolViewActive)
            return true;

        if (!CardLibrarySelectionController.TryRemoveCard(card))
            return true;

        RitsuToastService.ShowInfo(
            "Refined Gem",
            RefinedGemUiText.Get("refined_gem.ui.card_removed"));

        return false;
    }
}

