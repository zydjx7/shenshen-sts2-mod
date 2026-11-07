using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Shenshen.ShenshenCode.Powers;

public sealed class HeldThornsPower : RarePower
{
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;
    internal int Expiry { get; set; }
    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext context, ICombatState state)
    {
        if (player != Owner.Player || player.PlayerCombatState!.TurnNumber < Expiry) return;
        int amount = Amount;
        await PowerCmd.Remove(this);
        if (Owner.GetPower<ThornsPower>() is { } thorns)
            await PowerCmd.ModifyAmount(context, thorns, -Math.Min(amount, thorns.Amount), Owner, null);
    }
}
