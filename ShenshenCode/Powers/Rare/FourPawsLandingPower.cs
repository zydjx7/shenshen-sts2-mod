using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Shenshen.ShenshenCode.Powers;

public sealed class FourPawsLandingPower : RarePower
{
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;
    private int hits;
    public override int DisplayAmount => hits;
    internal void CountHit() => hits = Math.Min(3, hits + 1);
    public override Task AfterDamageReceived(PlayerChoiceContext context, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? card)
    {
        if (target == Owner && dealer?.Side == CombatSide.Enemy && props.IsPoweredAttack() && result.WasFullyBlocked) CountHit();
        return Task.CompletedTask;
    }
    public override async Task AfterEnergyReset(Player player)
    {
        if (player != Owner.Player) return;
        await PlayerCmd.GainEnergy(hits, player);
        await PowerCmd.Remove(this);
    }
}
