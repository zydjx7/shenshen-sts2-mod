using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Shenshen.ShenshenCode.Character;
using Shenshen.ShenshenCode.Powers;

namespace Shenshen.ShenshenCode.Cards;

public sealed class OttoDance : RareCard
{
    public OttoDance() : base(1, CardType.Skill, TargetType.AnyEnemy, ShenshenForm.Human) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Turns", 2m)];

    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        PowerCmd.Apply<OttoDancePower>(context, play.Target!, DynamicVars["Turns"].IntValue, Owner.Creature, this);

    protected override void OnUpgrade() => DynamicVars["Turns"].UpgradeValueBy(1);
}
