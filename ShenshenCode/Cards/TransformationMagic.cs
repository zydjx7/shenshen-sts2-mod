using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Shenshen.ShenshenCode.Character;
using Shenshen.ShenshenCode.Mechanics;

namespace Shenshen.ShenshenCode.Cards;

public sealed class TransformationMagic : ShenshenCard
{
    public TransformationMagic() : base(1, CardType.Skill, CardRarity.Token, TargetType.Self) { }
    public override ShenshenForm? Form => null;
    public override bool CanBeGeneratedInCombat => false;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain];
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        FormState.Switch(choiceContext, Owner);

    // Retain the same combat card instance, including cost modifiers and any upgrade state.
    protected override CardLocation GetResultLocationForCardPlay() =>
        new(Owner, PileType.Hand, CardPilePosition.Bottom);
}
