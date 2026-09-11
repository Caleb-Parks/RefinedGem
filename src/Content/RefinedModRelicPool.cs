using MegaCrit.Sts2.Core.Models;

namespace RefinedGem.Content;

public sealed class RefinedModRelicPool : RelicPoolModel
{
    public override string EnergyColorName => "colorless";

    protected override IEnumerable<RelicModel> GenerateAllRelics() => [];
}
