using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Shenshen.ShenshenCode.Cards;
using Shenshen.ShenshenCode.Character;
using Shenshen.ShenshenCode.Mechanics;

namespace Shenshen.ShenshenCode.Powers;

public sealed class GuardTheBellyPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override string CustomPackedIconPath => $"{MainFile.ResPath}/images/powers/power.png";
    public override string CustomBigIconPath => $"{MainFile.ResPath}/images/powers/big/power.png";
    public override async Task AfterDamageReceived(PlayerChoiceContext context, Creature target, DamageResult result,
        ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner || dealer?.Side != CombatSide.Enemy || !props.IsPoweredAttack()) return;
        var recovery = Math.Min(Amount, result.BlockedDamage);
        Flash();
        await PowerCmd.Remove(this);
        if (recovery > 0 && Owner.IsAlive)
            await CreatureCmd.GainBlock(Owner, recovery, ValueProp.Unpowered, null);
    }
    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext context, ICombatState state)
    {
        if (player == Owner.Player) await PowerCmd.Remove(this);
    }
}
