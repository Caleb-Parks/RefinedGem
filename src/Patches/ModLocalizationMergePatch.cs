using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using RefinedGem.Services;

namespace RefinedGem.Patches;

[HarmonyPatch(typeof(LocManager), nameof(LocManager.Initialize))]
[HarmonyPriority(Priority.First)]
internal static class ModLocalizationMergePatch
{
    private static void Postfix()
    {
        ModLocalizationMerger.MergeIntoGameTables(typeof(RefinedGemEntry).Assembly);
    }
}
