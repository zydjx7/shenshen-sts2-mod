using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Shenshen.ShenshenCode.Mechanics;

namespace Shenshen.ShenshenCode.Powers;

public sealed class ShatteredMirrorPower : CustomPowerModel
{
    // Amount is the remaining, tracked portion of block supplied by mirror cards.
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    protected override bool IsVisibleInternal => Amount > 0;
    public override string CustomPackedIconPath => $"{MainFile.ResPath}/images/powers/power.png";
    public override string CustomBigIconPath => $"{MainFile.ResPath}/images/powers/big/power.png";

    public static async Task Grant(PlayerChoiceContext context, CardModel card, CardPlay play)
    {
        var creature = card.Owner.Creature;
        var before = creature.Block;
        var mirrorBefore = creature.GetPower<ShatteredMirrorPower>()?.Amount ?? 0;
        var gained = await CreatureCmd.GainBlock(creature, card.DynamicVars.Block, play);
        // Include nested fixed mirror gains (Thorn Crown) in both deltas. A quota from
        // Mirror Vestments covers part of the original gain without adding extra Block.
        var alreadyConverted = Math.Max(0,
            (creature.GetPower<ShatteredMirrorPower>()?.Amount ?? 0) - mirrorBefore);
        var remaining = Math.Clamp(creature.Block - before - alreadyConverted, 0, (int)gained);
        if (remaining > 0)
            await PowerCmd.Apply<ShatteredMirrorPower>(context, creature, remaining, creature, card);
    }

    public override async Task AfterDamageReceived(PlayerChoiceContext context, Creature target,
        DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner) return;
        var reflected = MirrorBlockTracking.TakeConsumedBlock(Owner);
        // The native full-turn mirror already returns this same blocked damage.
        if (reflected > 0 && props.IsPoweredAttack() && dealer?.Side == CombatSide.Enemy &&
            dealer.IsAlive && Owner.GetPower<ReflectPower>() == null)
        {
            var results = await CreatureCmd.Damage(context, RareCombat.CounterTarget(Owner, dealer), reflected, ValueProp.Unpowered, Owner);
            await RareCombat.AfterRetaliation(context, Owner, results, thorns: false);
        }

        if (Amount <= 0) await PowerCmd.Remove(this);
    }

    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext context, ICombatState combatState)
    {
        if (player == Owner.Player && (Amount == 0 || Owner.GetPower<LightInGlassPower>() == null))
            await PowerCmd.Remove(this);
    }
}
