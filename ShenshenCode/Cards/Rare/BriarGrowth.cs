using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Shenshen.ShenshenCode.Character;
using Shenshen.ShenshenCode.Powers;

namespace Shenshen.ShenshenCode.Cards;

public sealed class BriarGrowth : RareCard
{
    public BriarGrowth() : base(1, CardType.Power, TargetType.Self, ShenshenForm.Cat) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Limit", 3m)];

    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        Gain<BriarGrowthPower>(context, DynamicVars["Limit"].BaseValue);

    protected override void OnUpgrade() => DynamicVars["Limit"].UpgradeValueBy(1);
}
