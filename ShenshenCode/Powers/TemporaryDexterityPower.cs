using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Shenshen.ShenshenCode.Powers;

public sealed class TemporaryDexterityPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override string CustomPackedIconPath => $"{MainFile.ResPath}/images/powers/power.png";
    public override string CustomBigIconPath => $"{MainFile.ResPath}/images/powers/big/power.png";

    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext,
        ICombatState combatState)
    {
        if (player != Owner.Player) return;

        var temporaryAmount = Amount;
        await PowerCmd.Remove(this);
        // Only undo this marker's contribution, preserving permanent Dexterity
        // and any enemy-applied reductions received while it was active.
        await PowerCmd.Apply<DexterityPower>(choiceContext, Owner, -temporaryAmount, null, null);
    }
}
