using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Shenshen.ShenshenCode.Character;
using Shenshen.ShenshenCode.Powers;

namespace Shenshen.ShenshenCode.Cards;

public sealed class MadeReal : RareCard
{
    public MadeReal() : base(2, CardType.Power, TargetType.Self, ShenshenForm.Human) { }
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        Owner.Creature.GetPower<MadeRealPower>() == null
            ? Gain<MadeRealPower>(context, 1)
            : Task.CompletedTask;

    protected override void OnUpgrade() => AddKeyword(CardKeyword.Retain);
}
