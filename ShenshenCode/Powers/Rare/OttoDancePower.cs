using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Shenshen.ShenshenCode.Powers;

public sealed class OttoDancePower : RarePower
{
    public override PowerType Type => PowerType.Debuff;
    public override Task AfterSideTurnEnd(PlayerChoiceContext context, CombatSide side, IEnumerable<Creature> participants) =>
        side == CombatSide.Enemy && participants.Contains(Owner) ? PowerCmd.Decrement(this) : Task.CompletedTask;
}
