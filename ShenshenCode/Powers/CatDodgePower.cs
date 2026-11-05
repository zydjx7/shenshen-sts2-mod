using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;

namespace Shenshen.ShenshenCode.Powers;

public sealed class CatDodgePower : CustomPowerModel
{
    // Tracked Amount is 1 when ready, 2 when spent. Transformation never resets it.
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override int DisplayAmount => 0;
    public override LocString Description => new("powers", SmartDescriptionLocKey);
    protected override string SmartDescriptionLocKey => Id.Entry + (Amount == 2 ? ".usedDescription" : ".description");
    public override string CustomPackedIconPath => $"{MainFile.ResPath}/images/powers/power.png";
    public override string CustomBigIconPath => $"{MainFile.ResPath}/images/powers/big/power.png";

    internal void ConsumeDodge()
    {
        SetAmount(2);
        Flash();
    }

    public override Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, ICombatState combatState)
    {
        if (player == Owner.Player) SetAmount(1);
        return Task.CompletedTask;
    }
}
