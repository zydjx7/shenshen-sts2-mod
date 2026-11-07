using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Shenshen.ShenshenCode.Character;
using Shenshen.ShenshenCode.Mechanics;

namespace Shenshen.ShenshenCode.Cards;

public sealed class Resurrection : RareCard
{
    public Resurrection() : base(2, CardType.Skill, TargetType.AnyAlly, ShenshenForm.Human) { }
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain, CardKeyword.Exhaust];
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;
    protected override bool IsPlayable => base.IsPlayable && CombatState?.PlayerCreatures.Any(c => c != Owner.Creature && c.IsDead) == true;

    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        Revival.Restore(context, play.Target!);

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
