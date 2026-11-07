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

public sealed class MoltenLightPierce : ShenshenCard
{
    public MoltenLightPierce() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }
    public override ShenshenForm? Form => ShenshenForm.Human;
    // Prototype: fixed unpowered, unblockable damage; native enemy damage caps still apply.
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(18m, ValueProp.Unpowered | ValueProp.Unblockable)];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(play.Target);
        await CreatureCmd.TriggerAnim(Owner.Creature, "Attack", Owner.Character.AttackAnimDelay);
        var marks = play.Target.GetPowerInstances<LightMarkPower>().Where(mark => mark.Applier == Owner.Creature).ToArray();
        // Unpowered preserves fixed damage, while these explicit attack interactions
        // keep the hit from bypassing native Thorns or losing this character's hit synergies.
        if (play.Target.GetPower<ThornsPower>() is { } thorns)
            await thorns.BeforeDamageReceived(context, play.Target, DynamicVars.Damage.BaseValue,
                ValueProp.Move | ValueProp.Unblockable, Owner.Creature, this);
        if (!Owner.Creature.IsAlive || !play.Target.IsAlive) return;
        var results = await CreatureCmd.Damage(context, play.Target, DynamicVars.Damage.BaseValue,
            ValueProp.Unpowered | ValueProp.Unblockable, Owner.Creature, this, play);
        var hit = results.FirstOrDefault(result => result.Receiver == play.Target && result.TotalDamage > 0);
        if (hit == null) return;
        await WardingPrayerPower.AfterAttackHit(context, Owner.Creature);
        foreach (var mark in marks.Where(mark => play.Target.GetPowerInstances<LightMarkPower>().Contains(mark)))
            await mark.AfterDamageReceived(context, play.Target, hit, ValueProp.Move, Owner.Creature, this);
    }
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(6m);
}
