using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using RefinedGem.Content;

namespace RefinedGem;

[ModInitializer(nameof(Initialize))]
public static class RefinedGemEntry
{
    public const string ModId = "RefinedGem";

    public static Logger Logger { get; private set; } = null!;

    public static void Initialize()
    {
        var assembly = Assembly.GetExecutingAssembly();
        Logger = new Logger(ModId, LogType.Generic);
        ModHelper.AddModelToPool<RefinedModRelicPool, RefinedGemRelic>();
        new Harmony(ModId).PatchAll(assembly);
    }
}
