using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Shenshen.ShenshenCode.Powers;

public sealed class SunBlindPower : RarePower
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerInstanceType InstanceType => PowerInstanceType.InstancedPerApplier;
    public override Task BeforeHandDraw(Player player, PlayerChoiceContext context, ICombatState state) =>
        player == Applier?.Player ? PowerCmd.Remove(this) : Task.CompletedTask;
}
