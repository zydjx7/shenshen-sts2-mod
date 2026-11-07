using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Shenshen.ShenshenCode.Character;
using Shenshen.ShenshenCode.Mechanics;

namespace Shenshen.ShenshenCode.Powers;

public sealed class DayNightCyclePower : RarePower
{
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext context, Player player)
    {
        if (Owner.Player != player || RareCombat.LastShift(player) != player.PlayerCombatState!.TurnNumber - 1) return;
        if (FormState.GetForm(player) == ShenshenForm.Human) await CardPileCmd.Draw(context, 2, player);
        else await PlayerCmd.GainEnergy(1, player);
        await PowerCmd.Decrement(this);
    }
}
