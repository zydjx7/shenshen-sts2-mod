using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Shenshen.ShenshenCode.Character;
using Shenshen.ShenshenCode.Powers;

namespace Shenshen.ShenshenCode.Cards;

public sealed class MadeReal : RareCard
{
    public MadeReal() : base(2, CardType.Power, TargetType.Self, ShenshenForm.Human) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Convert", 1m)];

    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        Gain<MadeRealPower>(context, DynamicVars["Convert"].BaseValue);

    protected override void OnUpgrade() => DynamicVars["Convert"].UpgradeValueBy(1);
}
