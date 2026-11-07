using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Shenshen.ShenshenCode.Powers;

public sealed class PhylacteryCraftPower : RarePower
{
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;
    public override decimal ModifyHandDraw(Player player, decimal count) =>
        player == Owner.Player ? Math.Max(0, count - 1) : count;
    public override async Task AfterPlayerTurnStartLate(PlayerChoiceContext context, Player player)
    {
        if (player != Owner.Player) return;
        if (Amount == 1) await PowerCmd.Apply<PhylacteryReadyPower>(context, Owner, 1, Owner, null);
        await PowerCmd.Decrement(this);
    }
}
