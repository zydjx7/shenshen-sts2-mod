using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Shenshen.ShenshenCode.Character;

namespace Shenshen.ShenshenCode.Cards;

public sealed class PositronCannon : RareCard
{
    public PositronCannon() : base(2, CardType.Attack, TargetType.AnyEnemy, ShenshenForm.Human) { }
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain, CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(18m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var target = play.Target!;
        var before = target.Block;
        await CreatureCmd.LoseBlock(context, target, before, Owner.Creature);
        await Attack(context, play, damage: DynamicVars.Damage.BaseValue + Math.Min(24, 2 * (before - target.Block)));
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(6);
}
