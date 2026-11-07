using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Shenshen.ShenshenCode.Mechanics;

namespace Shenshen.ShenshenCode.Powers;

public sealed class TemporalWardPower : RarePower
{
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;
    internal int CastTurn { get; private set; }
    internal int Debt { get; set; }
    public override int DisplayAmount => Debt;
    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        CastTurn = Owner.Player!.PlayerCombatState!.TurnNumber;
        return Task.CompletedTask;
    }
    public override async Task AfterSideTurnEndLate(PlayerChoiceContext context, CombatSide side, IEnumerable<Creature> participants)
    {
        if (!participants.Contains(Owner) || Owner.Player!.PlayerCombatState!.TurnNumber <= CastTurn) return;
        await PowerCmd.Remove(this);
        int blocked = Math.Min(Owner.Block, Debt);
        if (blocked > 0) await CreatureCmd.LoseBlock(context, Owner, blocked, null);
        if (Debt > blocked) await RareDefense.PayDebt(context, Owner, Debt - blocked);
    }
}
