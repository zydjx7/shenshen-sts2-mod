using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Shenshen.ShenshenCode.Character;
using Shenshen.ShenshenCode.Powers;

namespace Shenshen.ShenshenCode.Cards;

public sealed class SevenfoldMirror : RareCard
{
    public SevenfoldMirror() : base(3, CardType.Power, TargetType.Self, ShenshenForm.Human) { }


    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var shield = Owner.Creature.GetPower<SevenfoldMirrorPower>();
        if (shield == null) await Gain<SevenfoldMirrorPower>(context, 7);
        else shield.SetAmount(7);
    }

    protected override void OnUpgrade() => AddKeyword(CardKeyword.Retain);
}
