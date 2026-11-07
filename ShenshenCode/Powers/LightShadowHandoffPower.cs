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

public sealed class LightShadowHandoffPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;
    public override string CustomPackedIconPath => $"{MainFile.ResPath}/images/powers/power.png";
    public override string CustomBigIconPath => $"{MainFile.ResPath}/images/powers/big/power.png";
    public static async Task AfterShift(PlayerChoiceContext context, Player player)
    {
        foreach (var power in player.Creature.GetPowerInstances<LightShadowHandoffPower>().ToArray())
        {
            var amount = power.Amount;
            power.Flash();
            await PowerCmd.Remove(power);
            await CreatureCmd.GainBlock(player.Creature, amount, ValueProp.Move, null);
        }
    }
    public override async Task AfterSideTurnEnd(PlayerChoiceContext context, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side == CombatSide.Player && participants.Contains(Owner)) await PowerCmd.Remove(this);
    }
    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext context, ICombatState state)
    {
        if (player == Owner.Player) await PowerCmd.Remove(this);
    }
}
