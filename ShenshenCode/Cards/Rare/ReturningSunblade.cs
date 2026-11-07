using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Shenshen.ShenshenCode.Character;

namespace Shenshen.ShenshenCode.Cards;

public sealed class ReturningSunblade : RareCard
{
    public ReturningSunblade() : base(2, CardType.Attack, TargetType.AnyEnemy, ShenshenForm.Human) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(16m, ValueProp.Move)];

    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        Attack(context, play);

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(6);
}
