using Godot;
using MegaCrit.Sts2.Core.Models;
using RefinedGem.Services;

namespace RefinedGem.Content;

public sealed class RefinedCardPool : CardPoolModel
{
    public override string Title => "Refined";

    public override string EnergyColorName => "colorless";

    public override string CardFrameMaterialPath => "card_frame_colorless";

    public override Color DeckEntryCardColor => new("9b9b9b");

    public override bool IsColorless => true;

    protected override CardModel[] GenerateAllCards() =>
        RefinedPoolService.GetCardsForCardPoolModel().ToArray();

    public static void InvalidateCachedCards()
    {
        // Canonical pools are what CardCreationOptions.GetPossibleCards reads. Invalidating only a
        // ToMutable() clone left a stale AllCards cache (e.g. local-only ids) so a remote player's
        // disjoint refined filter produced an empty reward pool and aborted the shared rewards UI.
        if (ModelDb.CardPool<RefinedCardPool>() is RefinedCardPool canonical)
            canonical.InvalidateCache();
    }

    private void InvalidateCache() => InvalidateCardCache();
}