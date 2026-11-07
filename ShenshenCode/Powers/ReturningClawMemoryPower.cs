using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Shenshen.ShenshenCode.Cards;
using Shenshen.ShenshenCode.Character;
using Shenshen.ShenshenCode.Mechanics;

namespace Shenshen.ShenshenCode.Powers;

public sealed class ReturningClawMemoryPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override string CustomPackedIconPath => $"{MainFile.ResPath}/images/powers/power.png";
    public override string CustomBigIconPath => $"{MainFile.ResPath}/images/powers/big/power.png";
    // Tracked bit flags: 1 baseline; 2 current enemy turn; 4 most recently ended enemy turn.
    protected override bool IsVisibleInternal => false;
    public bool ReflectedLastEnemyTurn => (Amount & 4) != 0;
    public static async Task RecordReflection(PlayerChoiceContext context, Creature owner)
    {
        if (owner.CombatState?.CurrentSide != CombatSide.Enemy) return;
        var memory = owner.GetPower<ReturningClawMemoryPower>();
        if (memory == null)
            memory = await PowerCmd.Apply<ReturningClawMemoryPower>(context, owner, 1, owner, null);
        memory?.SetAmount(memory.Amount | 2);
    }
    public override Task BeforeSideTurnStart(PlayerChoiceContext context, CombatSide side, IReadOnlyList<Creature> participants, ICombatState state)
    {
        if (side == CombatSide.Enemy) SetAmount(Amount & ~2);
        return Task.CompletedTask;
    }
    public override Task AfterSideTurnEnd(PlayerChoiceContext context, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side == CombatSide.Enemy) SetAmount(1 | ((Amount & 2) != 0 ? 4 : 0));
        return Task.CompletedTask;
    }
}
