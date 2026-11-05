using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization;

namespace Shenshen.ShenshenCode.Powers;

public sealed class FormPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override int DisplayAmount => 0;
    public override LocString Title => new("powers", Id.Entry + (Amount == 2 ? ".catTitle" : ".title"));
    public override LocString Description => new("powers", Id.Entry + (Amount == 2 ? ".catDescription" : ".description"));
    protected override string SmartDescriptionLocKey => Id.Entry + (Amount == 2 ? ".catDescription" : ".description");
    public override string CustomPackedIconPath => $"{MainFile.ResPath}/images/powers/power.png";
    public override string CustomBigIconPath => $"{MainFile.ResPath}/images/powers/big/power.png";
}
