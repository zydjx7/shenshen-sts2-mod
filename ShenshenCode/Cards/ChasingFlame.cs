using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Shenshen.ShenshenCode.Character;
using Shenshen.ShenshenCode.Powers;

namespace Shenshen.ShenshenCode.Cards;

public sealed class ChasingFlame : ShenshenCard
{
    public ChasingFlame() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }
    public override ShenshenForm? Form => ShenshenForm.Human;
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<LightMarkPower>()];
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(8m, ValueProp.Move), new DamageVar("MarkedDamage", 12m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        bool marked = cardPlay.Target.GetPowerInstances(ModelDb.Power<LightMarkPower>().Id)
            .Any(power => power.Applier == Owner.Creature);
        decimal damage = marked ? DynamicVars["MarkedDamage"].BaseValue : DynamicVars.Damage.BaseValue;
        await DamageCmd.Attack(damage).FromCard(this, cardPlay).Targeting(cardPlay.Target).Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
        DynamicVars["MarkedDamage"].UpgradeValueBy(4m);
    }
}
