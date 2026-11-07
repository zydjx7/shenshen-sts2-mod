using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Shenshen.ShenshenCode.Character;
using Shenshen.ShenshenCode.Powers;

namespace Shenshen.ShenshenCode.Cards;

public sealed class TemporalWard : RareCard
{
    public TemporalWard() : base(2, CardType.Skill, TargetType.Self, ShenshenForm.Human) { }
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        Gain<TemporalWardPower>(context, 1);

    protected override void OnUpgrade() => AddKeyword(CardKeyword.Retain);
}
