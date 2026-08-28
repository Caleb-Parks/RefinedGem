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
        types: [typeof(RelicModel), typeof(string), typeof(string)],
        modifiers: null)!;

    public static EventOption Create(AncientEventModel ancient)
    {
        var relic = ModelDb.Relic<RefinedGemRelic>().ToMutable();
        if (ancient.Owner is not null)
            relic.Owner = ancient.Owner;

        return (EventOption)RelicOptionMethod.Invoke(
            ancient,
            [relic, "INITIAL", "NEOW.pages.DONE.POSITIVE.description"])!;
    }
}
