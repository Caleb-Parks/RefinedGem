using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using RefinedGem.Services;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace RefinedGem.Content;

[RegisterRelic(typeof(RefinedModRelicPool), FullPublicEntry = "REFINED_GEM")]
public sealed class RefinedGemRelic : ModRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override string CustomIconPath => "res://assets/refined_gem_relic.png";

    public override string CustomIconOutlinePath => "res://assets/refined_gem_relic_outline.png";

    public override bool IsAllowedAtNeow(Player player) => true;

    public override CardCreationOptions ModifyCardRewardCreationOptions(Player player, CardCreationOptions options) =>
        RefinedPoolService.ApplyCardCreationOptions(player, options);

    public override CardCreationOptions ModifyCardRewardCreationOptionsLate(Player player, CardCreationOptions options) =>
        RefinedPoolService.ApplyCardCreationOptions(player, options);

    public override IEnumerable<CardModel> ModifyMerchantCardPool(Player player, IEnumerable<CardModel> cards) =>
        RefinedPoolService.GetMerchantCardsForRun(player, cards);
}
