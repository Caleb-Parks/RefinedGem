using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
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
