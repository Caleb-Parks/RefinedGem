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

[HarmonyPatch(typeof(NCardLibrary), "ShowCardDetail")]
internal static class CardLibraryShowCardDetailPatch
{
    [HarmonyPrefix]
    private static bool Prefix(NCardHolder holder)
    {
        if (holder.CardModel is not CardModel card)
            return true;

        if (!CardLibrarySelectionController.EditModeEnabled)
            return true;

        var wasInPool = RefinedPoolService.ContainsCard(card);
        if (!CardLibrarySelectionController.TryToggleCard(card, holder))
            return false;

        RitsuToastService.ShowInfo(
            "Refined Gem",
            wasInPool
                ? RefinedGemUiText.Get("refined_gem.ui.card_removed")
                : RefinedGemUiText.Get("refined_gem.ui.card_added"));

        return false;
    }
}

[HarmonyPatch(typeof(NCardLibrary), "DisplayCardsAfterShortDelay")]
internal static class CardLibraryDisplayCardsPatch
{
    [HarmonyPostfix]
    private static void Postfix() =>
        Callable.From(CardLibrarySelectionController.RefreshAllPoolHighlights).CallDeferred();
}

[HarmonyPatch(typeof(NGridCardHolder), "SetCard")]
internal static class GridCardHolderSetCardPatch
{
    [HarmonyPostfix]
    private static void Postfix(NGridCardHolder __instance)
    {
        var card = __instance.CardModel;
        CardLibrarySelectionController.ApplyPoolHighlight(
            __instance,
            card is not null && RefinedPoolService.ContainsCard(card));
    }
}
