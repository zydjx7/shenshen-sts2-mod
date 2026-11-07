using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Shenshen.ShenshenCode.Character;
using Shenshen.ShenshenCode.Powers;

namespace Shenshen.ShenshenCode.Cards;

public sealed class SpreadingSparks : RareCard
{
    public SpreadingSparks() : base(2, CardType.Power, TargetType.Self, ShenshenForm.Human) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Bonus", 1m)];

    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        Gain<SpreadingSparksPower>(context, DynamicVars["Bonus"].IntValue);

    protected override void OnUpgrade() => DynamicVars["Bonus"].UpgradeValueBy(1);
}
