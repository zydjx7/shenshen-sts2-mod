using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Shenshen.ShenshenCode.Cards;
using Shenshen.ShenshenCode.Character;

namespace Shenshen.ShenshenCode.Powers;

public sealed class MirrorAfterglowPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override string CustomPackedIconPath => $"{MainFile.ResPath}/images/powers/power.png";
    public override string CustomBigIconPath => $"{MainFile.ResPath}/images/powers/big/power.png";
    // Called only by the full/limited reflection damage bridge, never by ordinary unpowered damage.
    public static async Task AfterReflection(PlayerChoiceContext context, Creature owner)
    {
        var power = owner.GetPower<MirrorAfterglowPower>();
        if (power == null || owner.GetPower<MirrorAfterglowUsedPower>() != null) return;
        await PowerCmd.Apply<MirrorAfterglowUsedPower>(context, owner, 1, owner, null);
        power.Flash();
        await PowerCmd.Apply<CatnapPower>(context, owner, power.Amount, owner, null);
    }
}
