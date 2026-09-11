using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using RefinedGem.Services;

namespace RefinedGem.Content;

public sealed class RefinedGemRelic : RelicModel
{
    private const string RelicIconPath = "res://assets/refined_gem_relic.png";
    private const string RelicOutlinePath = "res://assets/refined_gem_relic_outline.png";

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override string PackedIconPath => RelicIconPath;

    protected override string PackedIconOutlinePath => RelicOutlinePath;

    protected override string BigIconPath => RelicIconPath;

    public override bool IsAllowedAtNeow(Player player) => true;

    public override CardCreationOptions ModifyCardRewardCreationOptions(Player player, CardCreationOptions options) =>
        RefinedPoolService.ApplyCardCreationOptions(player, options);

    public override CardCreationOptions ModifyCardRewardCreationOptionsLate(Player player, CardCreationOptions options) =>
        RefinedPoolService.ApplyCardCreationOptions(player, options);

    public override IEnumerable<CardModel> ModifyMerchantCardPool(Player player, IEnumerable<CardModel> cards) =>
        RefinedPoolService.GetMerchantCardsForRun(player, cards);
}
