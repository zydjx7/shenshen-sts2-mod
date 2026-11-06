using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Shenshen.ShenshenCode.Powers;

public sealed class CuriousPeekPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override string CustomPackedIconPath => $"{MainFile.ResPath}/images/powers/power.png";
    public override string CustomBigIconPath => $"{MainFile.ResPath}/images/powers/big/power.png";

    // Called only after FormState has completed the ordinary hand replacement.
    public static async Task AfterShift(PlayerChoiceContext context, Player player)
    {
        var power = player.Creature.GetPower<CuriousPeekPower>();
        if (power == null) return;

        var count = power.Amount;
        power.Flash();
        await PowerCmd.Remove(power);
        await CardPileCmd.Draw(context, count, player);
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side == CombatSide.Player && participants.Contains(Owner))
            await PowerCmd.Remove(this);
    }

    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext,
        ICombatState combatState)
    {
        if (player == Owner.Player) await PowerCmd.Remove(this);
    }
}
