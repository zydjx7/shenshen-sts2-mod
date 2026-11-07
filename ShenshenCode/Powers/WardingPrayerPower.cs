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

public sealed class WardingPrayerPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override string CustomPackedIconPath => $"{MainFile.ResPath}/images/powers/power.png";
    public override string CustomBigIconPath => $"{MainFile.ResPath}/images/powers/big/power.png";
    // The damage bridge calls this once per powered attack segment after all AOE targets resolve.
    public static async Task AfterAttackHit(PlayerChoiceContext context, Creature dealer)
    {
        var power = dealer.GetPower<WardingPrayerPower>();
        var used = dealer.GetPower<WardingPrayerUsedPower>()?.Amount ?? 0;
        if (power == null || used >= 3 || dealer.IsDead) return;
        await PowerCmd.Apply<WardingPrayerUsedPower>(context, dealer, 1, dealer, null);
        power.Flash();
        await CreatureCmd.GainBlock(dealer, power.Amount, ValueProp.Unpowered, null);
    }
}
