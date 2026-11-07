using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Shenshen.ShenshenCode.Character;
using Shenshen.ShenshenCode.Mechanics;

namespace Shenshen.ShenshenCode.Cards;

public sealed class SeizeTheOpening : RareCard
{
    public SeizeTheOpening() : base(1, CardType.Attack, TargetType.AnyEnemy, ShenshenForm.Cat) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(6m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var target = play.Target!;
        if (target.Monster?.IntendsToAttack != true) await RareCombat.ThornHit(context, Owner.Creature, target);
        if (target.IsAlive) await Attack(context, play, 2);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2);
}
