using HarmonyLib;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using RefinedGem.Content;

namespace RefinedGem.Patches;

[HarmonyPatch(typeof(Neow), nameof(Neow.AllPossibleOptions), MethodType.Getter)]
internal static class NeowAllPossibleOptionsPatch
{
    [HarmonyPostfix]
    private static void Postfix(Neow __instance, ref IEnumerable<EventOption> __result)
    {
        if (__result.Any(ReferencesRefinedGem))
            return;

        var options = __result.ToList();
        options.Add(RefinedGemNeowOption.Create(__instance));
        __result = options;
    }

    private static bool ReferencesRefinedGem(EventOption option) =>
        option.Relic?.CanonicalInstance is RefinedGemRelic;
}
