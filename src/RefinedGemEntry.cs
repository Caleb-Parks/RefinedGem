using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models.Events;
using RefinedGem.Content;
using STS2RitsuLib;
using STS2RitsuLib.Content;
using STS2RitsuLib.Interop;
using STS2RitsuLib.Scaffolding.Ancients.Options;

namespace RefinedGem;

[ModInitializer(nameof(Initialize))]
public static class RefinedGemEntry
{
    public const string ModId = "RefinedGem";

    public static Logger Logger { get; private set; } = null!;

    public static void Initialize()
    {
        var assembly = Assembly.GetExecutingAssembly();
        Logger = RitsuLibFramework.CreateLogger(ModId);
        ModTypeDiscoveryHub.RegisterModAssembly(ModId, assembly);

        RitsuLibFramework.CreateModLocalization(
            ModId,
            ModId,
            resourceFolders: ["locales"],
            resourceAssembly: assembly);

        RefinedGemRuntime.RegisterDataStores();

        RitsuLibFramework.CreateContentPack(ModId)
            .CardLibraryCompendiumSharedPoolFilter<RefinedCardPool>(
                "refined_pool",
                "res://assets/refined_gem_relic.png",
                [
                    new CardLibraryCompendiumPlacementRule
                    {
                        VanillaFilterAnchorUniqueName = CardLibraryCompendiumVanillaFilterNames.ColorlessPool,
                        Relation = CardLibraryCompendiumFilterInsertRelation.After,
                    },
                ])
            .AncientOption<Neow>(ModAncientOptionRule.Single(
                ancient => RefinedGemNeowOption.Create(ancient),
                _ => true))
            .Apply();

        new Harmony(ModId).PatchAll(assembly);
    }
}
