using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Shenshen.ShenshenCode.Character;
using Shenshen.ShenshenCode.Powers;

namespace Shenshen.ShenshenCode.Cards;

public sealed class OrbitingStarChart : RareCard
{
    public OrbitingStarChart() : base(2, CardType.Power, TargetType.Self, ShenshenForm.Human, CardRarity.Ancient) { }


    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        Gain<OrbitingStarChartPower>(context, 1);

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
