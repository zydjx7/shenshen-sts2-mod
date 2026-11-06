using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Shenshen.ShenshenCode.Character;

namespace Shenshen.ShenshenCode.Cards;

public sealed class ShedFur : ShenshenCard
{
    public ShedFur() : base(1, CardType.Attack, CardRarity.Common, TargetType.AllEnemies) { }
    public override ShenshenForm? Form => ShenshenForm.Cat;
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<ThornsPower>()];
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        MakeCalculatedDamage(4, (card, _) => card.CombatState == null ? 0m :
            Math.Max(0, card.Owner.Creature.GetPower<ThornsPower>()?.Amount ?? 0));

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await DamageCmd.Attack(DynamicVars.CalculatedDamage).FromCard(this, cardPlay)
            .TargetingAllOpponents(CombatState!).Execute(choiceContext);
    }

    protected override void OnUpgrade() => DynamicVars.CalculationBase.UpgradeValueBy(3m);
}
