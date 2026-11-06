using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Shenshen.ShenshenCode.Character;
using Shenshen.ShenshenCode.Powers;

namespace Shenshen.ShenshenCode.Cards;

public sealed class Catnap : ShenshenCard
{
    public Catnap() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self) { }
    public override ShenshenForm? Form => ShenshenForm.Cat;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<CatnapPower>()];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1), new EnergyVar(2)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, Owner);
        await PowerCmd.Apply<CatnapPower>(choiceContext, Owner.Creature,
            DynamicVars.Energy.IntValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars.Energy.UpgradeValueBy(1m);
}
