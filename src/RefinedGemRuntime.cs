using RefinedGem.Data;
using STS2RitsuLib;
using STS2RitsuLib.Data;
using STS2RitsuLib.Utils.Persistence;

namespace RefinedGem;

public static class RefinedGemRuntime
{
    public static ModDataStoreCache<RefinedGemSettings> SettingsCache { get; private set; } = null!;

    internal static void RegisterDataStores()
    {
        using (RitsuLibFramework.BeginModDataRegistration(RefinedGemEntry.ModId))
        {
            var store = RitsuLibFramework.GetDataStore(RefinedGemEntry.ModId);

            store.Register(
                key: "refined_pool",
                fileName: "refined_pool.json",
                scope: SaveScope.Profile,
                defaultFactory: () => new RefinedPoolProfile(),
                autoCreateIfMissing: true);

            store.Register(
                key: "settings",
                fileName: "settings.json",
                scope: SaveScope.Global,
                defaultFactory: () => new RefinedGemSettings(),
                autoCreateIfMissing: true);

            SettingsCache = store.CreateCache<RefinedGemSettings>("settings");
        }
    }
}
