using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Shenshen.ShenshenCode.Powers;

public sealed class BriarGrowthPower : RarePower
{
    private int round = -1;
    private int used;
    internal async Task Grow(PlayerChoiceContext context)
    {
        if (round != Owner.CombatState!.RoundNumber) { round = Owner.CombatState.RoundNumber; used = 0; }
        if (used >= Amount) return;
        used++;
        await TemporaryThornsPower.Grant(context, Owner, 1, null);
    }
}
