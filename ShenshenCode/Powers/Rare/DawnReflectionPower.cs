using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace Shenshen.ShenshenCode.Powers;

public sealed class DawnReflectionPower : RarePower
{
    private int reflected;
    public override int DisplayAmount => reflected;
    internal void Record(int damage) => reflected += damage * Amount;
    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext context, ICombatState state)
    {
        if (player != Owner.Player) return;
        var damage = reflected;
        await PowerCmd.Remove(this);
        if (damage > 0) await CreatureCmd.Damage(context, state.HittableEnemies.ToArray(), damage, ValueProp.Unpowered, Owner);
    }
}
