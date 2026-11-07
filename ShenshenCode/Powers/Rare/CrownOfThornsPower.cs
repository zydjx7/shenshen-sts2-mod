using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Shenshen.ShenshenCode.Mechanics;

namespace Shenshen.ShenshenCode.Powers;

public sealed class CrownOfThornsPower : RarePower
{
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;
    private int usedTurn = -1;
    public override async Task AfterBlockGained(Creature creature, decimal amount, ValueProp props, CardModel? cardSource)
    {
        if (creature != Owner || amount <= 0 || cardSource?.Owner.Creature != Owner || usedTurn == Owner.Player!.PlayerCombatState!.TurnNumber) return;
        usedTurn = Owner.Player.PlayerCombatState.TurnNumber;
        int extra = Math.Min(4, (Owner.GetPower<ThornsPower>()?.Amount ?? 0) / Amount);
        if (extra > 0) await RareCombat.FixedMirror(new BlockingPlayerChoiceContext(), Owner, extra);
    }
}
