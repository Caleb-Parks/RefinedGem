using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using RefinedGem.Content;

namespace RefinedGem.Patches;

[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.AllSharedCardPools), MethodType.Getter)]
internal static class AllSharedCardPoolsPatch
{
    [HarmonyPostfix]
    private static void Postfix(ref IEnumerable<CardPoolModel> __result)
    {
        if (__result.OfType<RefinedCardPool>().Any())
            return;

        __result = __result.Append(ModelDb.CardPool<RefinedCardPool>());
    }
}

[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.AllRelicPools), MethodType.Getter)]
internal static class AllRelicPoolsPatch
{
    [HarmonyPostfix]
    private static void Postfix(ref IEnumerable<RelicPoolModel> __result)
    {
        if (__result.OfType<RefinedModRelicPool>().Any())
            return;

        __result = __result.Append(ModelDb.RelicPool<RefinedModRelicPool>());
    }
}

[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.GetEntry))]
internal static class ModelDbGetEntryPatch
{
    [HarmonyPostfix]
    private static void Postfix(Type type, ref string __result)
    {
        if (type == typeof(RefinedGemRelic))
            __result = "REFINED_GEM";
    }
}
