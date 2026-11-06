using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Shenshen.ShenshenCode.Character;
using Shenshen.ShenshenCode.Powers;

namespace Shenshen.ShenshenCode.Cards;

public sealed class CuriousPeek : ShenshenCard
{
    public CuriousPeek() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self) { }
    public override ShenshenForm? Form => ShenshenForm.Cat;
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<CuriousPeekPower>()];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(3), new DynamicVar("ShiftCards", 1m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, Owner);
        await PowerCmd.Apply<CuriousPeekPower>(choiceContext, Owner.Creature,
            DynamicVars["ShiftCards"].IntValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars["ShiftCards"].UpgradeValueBy(1m);
}
