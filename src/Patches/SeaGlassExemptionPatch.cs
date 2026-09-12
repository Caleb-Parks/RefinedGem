using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models.Relics;
using RefinedGem.Services;

namespace RefinedGem.Patches;

/// <summary>
/// Sea Glass uses the owner's character pool, so pool heuristics cannot distinguish it from
/// Touch Core / Future of Potions. Wrap its pickup rewards so Refined Gem leaves them vanilla.
/// </summary>
[HarmonyPatch(typeof(SeaGlass), nameof(SeaGlass.AfterObtained))]
internal static class SeaGlassAfterObtainedExemptionPatch
{
    [HarmonyPrefix]
    private static bool Prefix(SeaGlass __instance, ref Task __result)
    {
        __result = InvokeWithBypass(__instance);
        return false;
    }

    private static async Task InvokeWithBypass(SeaGlass instance)
    {
        using (RefinedPoolService.EnterModificationBypass())
            await SeaGlassAfterObtainedOriginal.Invoke(instance);
    }
}

[HarmonyPatch(typeof(SeaGlass), nameof(SeaGlass.AfterObtained))]
internal static class SeaGlassAfterObtainedOriginal
{
    [HarmonyReversePatch]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static Task Invoke(SeaGlass instance) =>
        throw new NotImplementedException("Harmony reverse patch stub.");
}
