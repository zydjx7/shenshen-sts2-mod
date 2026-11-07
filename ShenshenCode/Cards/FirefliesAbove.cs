using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Shenshen.ShenshenCode.Character;
using Shenshen.ShenshenCode.Powers;

namespace Shenshen.ShenshenCode.Cards;

public sealed class FirefliesAbove : ShenshenCard
{
    public FirefliesAbove() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies) { }
    public override ShenshenForm? Form => ShenshenForm.Human;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(6m, ValueProp.Move), new DynamicVar("BurnDamage", 1m), new DynamicVar("Turns", 2m)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<LightMarkPower>()];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var targets = CombatState!.GetOpponentsOf(Owner.Creature).Where(enemy => enemy.IsAlive).ToArray();
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this, play).TargetingAllOpponents(CombatState).Execute(context);
        foreach (var target in targets.Where(enemy => enemy.IsAlive))
            await LightMarkPower.ApplyMark(context, target, Owner.Creature, DynamicVars["BurnDamage"].IntValue, DynamicVars["Turns"].IntValue, this);
    }
    protected override void OnUpgrade()
    {
        DynamicVars["BurnDamage"].UpgradeValueBy(1m);
        DynamicVars["Turns"].UpgradeValueBy(1m);
    }
}
