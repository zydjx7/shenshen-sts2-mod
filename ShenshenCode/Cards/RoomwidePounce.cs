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
using Shenshen.ShenshenCode.Mechanics;
using Shenshen.ShenshenCode.Powers;

namespace Shenshen.ShenshenCode.Cards;

public sealed class RoomwidePounce : ShenshenCard
{
    public RoomwidePounce() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies) { }
    public override ShenshenForm? Form => ShenshenForm.Cat;
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(3m, ValueProp.Move), new RepeatVar(3), new BlockVar(3m, ValueProp.Move)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<LightMarkPower>()];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var marked = CombatState!.HittableEnemies.Count(enemy => enemy.GetPowerInstances(ModelDb.Power<LightMarkPower>().Id).Any(power => power.Applier == Owner.Creature));
        if (marked > 0) await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block.BaseValue * marked, ValueProp.Move, play);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).WithHitCount(DynamicVars.Repeat.IntValue).FromCard(this, play).TargetingAllOpponents(CombatState!).Execute(context);
    }
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(1m);
        DynamicVars.Block.UpgradeValueBy(1m);
    }
}
