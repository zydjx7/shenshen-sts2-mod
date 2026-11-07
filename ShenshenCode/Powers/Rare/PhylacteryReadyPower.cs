using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace Shenshen.ShenshenCode.Powers;

public sealed class PhylacteryReadyPower : RarePower
{
    public override bool ShouldDieLate(Creature creature) => creature != Owner;
    public override async Task AfterPreventingDeath(Creature creature)
    {
        if (creature != Owner) return;
        await PowerCmd.Decrement(this);
        await CreatureCmd.SetCurrentHp(Owner, 1);
        await PowerCmd.Apply<PhylacteryGuardPower>(new BlockingPlayerChoiceContext(), Owner, 1, Owner, null);
        await CreatureCmd.GainBlock(Owner, 12, ValueProp.Unpowered, null);
    }
}
