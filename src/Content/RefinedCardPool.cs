using Godot;
using MegaCrit.Sts2.Core.Models;
using RefinedGem.Services;
using STS2RitsuLib.Interop.AutoRegistration;

namespace RefinedGem.Content;

[RegisterSharedCardPool]
public sealed class RefinedCardPool : CardPoolModel
{
    public override string Title => "Refined";

    public override string EnergyColorName => "colorless";

    public override string CardFrameMaterialPath => "card_frame_colorless";

    public override Color DeckEntryCardColor => new("9b9b9b");

    public override bool IsColorless => true;

    protected override CardModel[] GenerateAllCards() =>
        RefinedPoolService.GetCanonicalCardsForProfile().ToArray();

    public static void InvalidateCachedCards()
    {
        if (ModelDb.CardPool<RefinedCardPool>().ToMutable() is RefinedCardPool mutable)
            mutable.InvalidateCache();
    }

    private void InvalidateCache() => InvalidateCardCache();
}
