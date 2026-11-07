using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Shenshen.ShenshenCode.Powers;

public sealed class SheepPower : RarePower
{
    public override PowerType Type => PowerType.Debuff;
    public override Task AfterDamageReceived(PlayerChoiceContext context, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? source) =>
        target == Owner && result.UnblockedDamage > 0 ? PowerCmd.Remove(this) : Task.CompletedTask;
}
