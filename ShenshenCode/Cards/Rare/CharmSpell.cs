using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using Shenshen.ShenshenCode.Character;
using Shenshen.ShenshenCode.Powers;

namespace Shenshen.ShenshenCode.Cards;

public sealed class CharmSpell : RareCard
{
    public CharmSpell() : base(2, CardType.Skill, TargetType.AnyEnemy, ShenshenForm.Human) { }
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Hits", 3m)];

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var target = play.Target!;
        if (target.Monster?.IntendsToAttack == true)
            await PowerCmd.Apply<WeakPower>(context, target, 1, Owner.Creature, this);
        await PowerCmd.Apply<CharmSpellPower>(context, target, DynamicVars["Hits"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars["Hits"].UpgradeValueBy(1);
}
