using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Shenshen.ShenshenCode.Character;

namespace Shenshen.ShenshenCode.Cards;

public sealed class IDontLikeYou : ShenshenCard
{
    public IDontLikeYou() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }

    public override ShenshenForm? Form => ShenshenForm.Human;
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<StrengthPower>()];
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(5m, ValueProp.Move),
        new RepeatVar(2),
        new PowerVar<StrengthPower>(3m),
        new DynamicVar("EnemyStrength", 1m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).WithHitCount(DynamicVars.Repeat.IntValue)
            .FromCard(this, cardPlay).Targeting(cardPlay.Target).Execute(choiceContext);
        await PowerCmd.Apply<StrengthPower>(choiceContext, Owner.Creature, DynamicVars.Strength.BaseValue,
            Owner.Creature, this);
        await PowerCmd.Apply<StrengthPower>(choiceContext, cardPlay.Target, DynamicVars["EnemyStrength"].BaseValue,
            Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(1m);
        DynamicVars.Strength.UpgradeValueBy(1m);
    }
}
