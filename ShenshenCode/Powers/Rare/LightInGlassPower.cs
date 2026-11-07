using MegaCrit.Sts2.Core.Entities.Creatures;

namespace Shenshen.ShenshenCode.Powers;

public sealed class LightInGlassPower : RarePower
{
    public override bool ShouldClearBlock(Creature creature) =>
        creature != Owner || (Owner.GetPower<ShatteredMirrorPower>()?.Amount ?? 0) <= 0;
    public override Task AfterPreventingBlockClear(MegaCrit.Sts2.Core.Models.AbstractModel preventer, Creature creature)
    {
        if (preventer != this || creature != Owner || Owner.GetPower<ShatteredMirrorPower>() is not { } mirror)
            return Task.CompletedTask;
        int keep = Math.Min(Amount, Math.Min(mirror.Amount, Owner.Block));
        Owner.LoseBlockInternal(Owner.Block - keep);
        mirror.SetAmount(keep);
        return Task.CompletedTask;
    }
}
