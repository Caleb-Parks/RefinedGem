using System.Reflection;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using RefinedGem.Data;
using HarmonyLib;
using RefinedGem.Patches;
using STS2RitsuLib;
using STS2RitsuLib.Content;
using STS2RitsuLib.Interop;
using STS2RitsuLib.Settings;
using STS2RitsuLib.Utils.Persistence;

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
            ["eng"],
            [],
            [],
            assembly);

        RefinedGemRuntime.RegisterDataStores();

        RitsuLibFramework.CreateContentPack(ModId)
            .CardLibraryCompendiumSharedPoolFilter<Content.RefinedCardPool>(
                "refined_pool",
                "res://assets/refined_pool_filter_icon.png",
                [
                    new CardLibraryCompendiumPlacementRule
                    {
                        VanillaFilterAnchorUniqueName = CardLibraryCompendiumVanillaFilterNames.ColorlessPool,
                        Relation = CardLibraryCompendiumFilterInsertRelation.After,
                    },
                ])
            .Apply();

        RegisterSettings();

        new Harmony(ModId).PatchAll(assembly);
    }

    private static void RegisterSettings()
    {
        RitsuLibFramework.RegisterModSettings(ModId, page => page
            .WithTitle(ModSettingsText.Literal("Refined Gem"))
            .WithModDisplayName(ModSettingsText.Literal("Refined Gem"))
            .AddSection("general", section => section
                .WithTitle(ModSettingsText.Literal("General"))
                .AddToggle(
                    "add_to_neow_pool",
                    ModSettingsText.Literal("Add Refined Gem to Neow"),
                    new ModSettingsValueBinding<RefinedGemSettings, bool>(
                        ModId,
                        "settings",
                        SaveScope.Global,
                        settings => settings.AddToNeowPool,
                        (settings, value) => settings.AddToNeowPool = value),
                    description: ModSettingsText.Literal(
                        "When enabled, Refined Gem can appear among Neow's relic options."))));
    }
}
