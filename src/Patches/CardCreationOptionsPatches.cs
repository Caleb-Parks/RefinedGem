using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using RefinedGem.Content;
using RefinedGem.Services;

namespace RefinedGem.Patches;

[HarmonyPatch(typeof(CardCreationOptions), nameof(CardCreationOptions.ForRoom))]
internal static class CardCreationOptionsForRoomPatch
{
    [HarmonyPostfix]
    private static void Postfix(Player player, ref CardCreationOptions __result)
    {
        if (!RefinedPoolService.ShouldUseRefinedPool(player))
            return;

        __result = __result.WithCardPools([ModelDb.CardPool<RefinedCardPool>()]);
    }
}
