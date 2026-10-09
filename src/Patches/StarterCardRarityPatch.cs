using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using RefinedGem.Services;

namespace RefinedGem.Patches;

/// <summary>
/// Starter cards stay Basic on deck copies. During refined generation, canonical pool
/// entries report as Common so rewards, shops, and transforms can roll them.
/// </summary>
[HarmonyPatch(typeof(CardModel), nameof(CardModel.Rarity), MethodType.Getter)]
internal static class CardModelRarityStarterAsCommonPatch
{
    [HarmonyPostfix]
    private static void Postfix(CardModel __instance, ref CardRarity __result)
    {
        if (__result != CardRarity.Basic)
            return;

        if (RefinedPoolService.ShouldReportStarterAsCommon(__instance))
            __result = CardRarity.Common;
    }
}
