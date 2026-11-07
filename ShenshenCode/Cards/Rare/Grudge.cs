using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Shenshen.ShenshenCode.Character;
using Shenshen.ShenshenCode.Mechanics;

namespace Shenshen.ShenshenCode.Cards;

public sealed class Grudge : RareCard
{
    public Grudge() : base(2, CardType.Attack, TargetType.AnyEnemy, ShenshenForm.Cat) { }
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(12m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var extra = RareCombat.GrudgeDamage(Owner.Creature, play.Target!);
        await Attack(context, play, damage: DynamicVars.Damage.BaseValue + extra);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
