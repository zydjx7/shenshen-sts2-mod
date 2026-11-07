using MegaCrit.Sts2.Core.Entities.Powers;

namespace Shenshen.ShenshenCode.Powers;

public sealed class StoppedTimePower : RarePower
{
    public override PowerType Type => PowerType.Debuff;
    internal bool WaitingForRoll { get; set; }
}
