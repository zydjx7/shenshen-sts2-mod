using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Shenshen.ShenshenCode.Powers;

public sealed class FixedGazePower : RarePower
{
    public override PowerInstanceType InstanceType => PowerInstanceType.InstancedPerApplier;
    public override Task BeforeHandDraw(Player player, PlayerChoiceContext context, ICombatState state) =>
        Applier?.Player == player ? PowerCmd.Remove(this) : Task.CompletedTask;
}
