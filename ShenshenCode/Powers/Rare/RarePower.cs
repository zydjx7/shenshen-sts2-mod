using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace Shenshen.ShenshenCode.Powers;

public abstract class RarePower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override string CustomPackedIconPath => $"{MainFile.ResPath}/images/powers/power.png";
    public override string CustomBigIconPath => $"{MainFile.ResPath}/images/powers/big/power.png";
}
