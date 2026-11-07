using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models;
using Shenshen.ShenshenCode.Mechanics;

namespace Shenshen.ShenshenCode.Powers;

public sealed class RoseInBloomPower : RarePower
{
    public override Task AfterPowerAmountChanged(PlayerChoiceContext context, PowerModel power, decimal amount, Creature? applier, CardModel? source) =>
        power is ThornsPower && power.Owner == Owner && amount > 0 && !TemporaryThornsPower.GrantInProgress.Value
            ? RareCombat.CheckBloom(context, Owner) : Task.CompletedTask;
}
