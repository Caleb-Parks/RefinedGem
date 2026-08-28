using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using RefinedGem.Services;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace RefinedGem.Content;

[RegisterRelic(typeof(RefinedModRelicPool))]
public sealed class RefinedGemRelic : ModRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override string CustomIconPath => "res://assets/refined_gem_relic.png";

    public override string CustomIconOutlinePath => "res://assets/refined_gem_relic_outline.png";

    public override bool IsAllowedAtNeow(Player player) =>
        RefinedGemRuntime.SettingsCache.Value.AddToNeowPool;

    public override CardCreationOptions ModifyCardRewardCreationOptions(Player player, CardCreationOptions options) =>
        ApplyRefinedPool(player, options);

    public override CardCreationOptions ModifyCardRewardCreationOptionsLate(Player player, CardCreationOptions options) =>
        ApplyRefinedPool(player, options);

    public override IEnumerable<CardModel> ModifyMerchantCardPool(Player player, IEnumerable<CardModel> cards)
    {
        if (!RefinedPoolService.ShouldUseRefinedPool(player))
            return cards;

        var allowed = RefinedPoolService.GetCardsForRun(player)
            .Select(card => card.CanonicalInstance)
            .ToHashSet();
        return cards.Where(card => allowed.Contains(card.CanonicalInstance));
    }

    private static CardCreationOptions ApplyRefinedPool(Player player, CardCreationOptions options)
    {
        if (!RefinedPoolService.ShouldUseRefinedPool(player))
            return options;

        return options.WithCardPools([ModelDb.CardPool<RefinedCardPool>()]);
    }
}
