using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Shenshen.ShenshenCode.Character;
using Shenshen.ShenshenCode.Powers;

namespace Shenshen.ShenshenCode.Cards;

public sealed class DayNightCycle : RareCard
{
    public DayNightCycle() : base(2, CardType.Power, TargetType.Self, ShenshenForm.Human, CardRarity.Uncommon) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Uses", 2m)];

    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        Gain<DayNightCyclePower>(context, DynamicVars["Uses"].BaseValue);

    protected override void OnUpgrade() => DynamicVars["Uses"].UpgradeValueBy(1);
}
