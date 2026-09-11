using HarmonyLib;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using RefinedGem.Services;

namespace RefinedGem.Patches;

/// <summary>
/// Prefer the refined pool for default transforms (relics, events, combat, preview).
/// Vanilla Common/Uncommon/Rare filtering is applied first; uniform fallback lives in
/// <see cref="RefinedPoolService.TryGetTransformationOptions"/>.
/// </summary>
[HarmonyPatch(typeof(CardFactory), nameof(CardFactory.GetDefaultTransformationOptions))]
internal static class CardFactoryGetDefaultTransformationOptionsPatch
{
    [HarmonyPrefix]
    private static bool Prefix(CardModel original, bool isInCombat, ref IEnumerable<CardModel> __result)
    {
        if (!RefinedPoolService.TryGetTransformationOptions(original, isInCombat, out var options))
            return true;

        __result = options;
        return false;
    }
}

[HarmonyPatch(typeof(CardFactory), "GetFilteredTransformationOptions")]
internal static class CardFactoryGetFilteredTransformationOptionsOriginal
{
    [HarmonyReversePatch]
    internal static CardModel[] Invoke(
        CardModel original,
        IEnumerable<CardModel> originalOptions,
        bool isInCombat) =>
        throw new NotImplementedException("Harmony reverse patch stub.");
}

[HarmonyPatch(typeof(CardFactory), "FilterForPlayerCount")]
internal static class CardFactoryFilterForPlayerCountOriginal
{
    [HarmonyReversePatch]
    internal static IEnumerable<CardModel> Invoke(IRunState runState, IEnumerable<CardModel> options) =>
        throw new NotImplementedException("Harmony reverse patch stub.");
}
