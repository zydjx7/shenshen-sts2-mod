using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Shenshen.ShenshenCode.Powers;

public sealed class TemporaryThornsPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override string CustomPackedIconPath => $"{MainFile.ResPath}/images/powers/power.png";
    public override string CustomBigIconPath => $"{MainFile.ResPath}/images/powers/big/power.png";

    public static async Task Grant(PlayerChoiceContext context, Creature target, int amount, CardModel source)
    {
        var previous = target.GetPower<ThornsPower>()?.Amount ?? 0;
        await PowerCmd.Apply<ThornsPower>(context, target, amount, target, source);
        var gained = (target.GetPower<ThornsPower>()?.Amount ?? 0) - previous;
        if (gained > 0)
            await PowerCmd.Apply<TemporaryThornsPower>(context, target, gained, target, source);
    }

    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext,
        ICombatState combatState)
    {
        if (player != Owner.Player) return;

        var thorns = Owner.GetPower<ThornsPower>();
        var amount = Math.Min(Amount, thorns?.Amount ?? 0);
        await PowerCmd.Remove(this);
        if (thorns != null && amount > 0)
            await PowerCmd.ModifyAmount(choiceContext, thorns, -amount, Owner, null);
    }
}
