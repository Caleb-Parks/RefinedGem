using RefinedGem.Data;
using STS2RitsuLib;
using STS2RitsuLib.Utils.Persistence;

namespace RefinedGem;

public static class RefinedGemRuntime
{
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
        }
    }
}
