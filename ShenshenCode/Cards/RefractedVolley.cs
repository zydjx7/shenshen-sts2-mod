using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Shenshen.ShenshenCode.Character;

namespace Shenshen.ShenshenCode.Cards;

public sealed class RefractedVolley : ShenshenCard
{
    public RefractedVolley() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }
    public override ShenshenForm? Form => ShenshenForm.Human;
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(3m, ValueProp.Move), new RepeatVar(2), new CardsVar(1)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        bool hadBlock = Owner.Creature.Block > 0;
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).WithHitCount(DynamicVars.Repeat.IntValue)
            .FromCard(this, cardPlay).Targeting(cardPlay.Target).Execute(choiceContext);
        if (hadBlock) await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, Owner);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(1m);
}
