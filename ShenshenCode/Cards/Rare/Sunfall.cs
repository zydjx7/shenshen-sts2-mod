using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Shenshen.ShenshenCode.Character;

namespace Shenshen.ShenshenCode.Cards;

public sealed class Sunfall : RareCard
{
    public Sunfall() : base(5, CardType.Attack, TargetType.AllEnemies, ShenshenForm.Human) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(24m, ValueProp.Move)];

    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        Attack(context, play);

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(8);
}
