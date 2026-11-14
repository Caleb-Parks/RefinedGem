using System.Reflection;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;

namespace RefinedGem.Content;

internal static class RefinedGemNeowOption
{
    private static readonly MethodInfo RelicOptionMethod = typeof(AncientEventModel).GetMethod(
        "RelicOption",
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
        binder: null,
        types: [typeof(RelicModel), typeof(string)],
        modifiers: null)!;

    public static EventOption Create(AncientEventModel ancient)
    {
        var relic = ModelDb.Relic<RefinedGemRelic>().ToMutable();
        if (ancient.Owner is not null)
            relic.Owner = ancient.Owner;

        // STS2 0.113+: RelicOption(relic, pageName) uses the relic title/description.
        return (EventOption)RelicOptionMethod.Invoke(ancient, [relic, "INITIAL"])!;
    }
}
