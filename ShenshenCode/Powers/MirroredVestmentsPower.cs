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

public sealed class MirroredVestmentsPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override string CustomPackedIconPath => $"{MainFile.ResPath}/images/powers/power.png";
    public override string CustomBigIconPath => $"{MainFile.ResPath}/images/powers/big/power.png";
    public override async Task AfterBlockGained(Creature creature, decimal amount, ValueProp props, CardModel? cardSource)
    {
        if (creature != Owner || cardSource?.Owner.Creature != Owner || cardSource.Type != CardType.Skill ||
            amount <= 0 || Owner.GetPower<MirroredVestmentsUsedPower>() != null) return;
        var context = new BlockingPlayerChoiceContext();
        await PowerCmd.Apply<MirroredVestmentsUsedPower>(context, Owner, 1, Owner, null);
        var converted = Math.Min(Amount, (int)amount);
        if (converted <= 0) return;
        Flash();
        // This adds only a reflection quota; Creature.Block is never increased here.
        // ShatteredMirrorPower.Grant subtracts this already-converted share before filling its own quota.
        await PowerCmd.Apply<ShatteredMirrorPower>(context, Owner, converted, Owner, cardSource);
    }
}
