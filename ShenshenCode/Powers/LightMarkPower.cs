using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Shenshen.ShenshenCode.Powers;

public sealed class LightMarkPower : CustomPowerModel
{
    // Native combat packets carry Amount but not custom fields. Encode the two
    // counters here: remaining player turns * 10 + fixed burn damage (1 or 2).
    // Applier and InstancedPerApplier provide the engine's per-player ownership.
    public int BurnDamage => Amount % 10;
    public int RemainingTurns => Amount / 10;
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerInstanceType InstanceType => PowerInstanceType.InstancedPerApplier;
    public override int DisplayAmount => BurnDamage;
    public override string CustomPackedIconPath => $"{MainFile.ResPath}/images/powers/power.png";
    public override string CustomBigIconPath => $"{MainFile.ResPath}/images/powers/big/power.png";
    public override LocString Description
    {
        get
        {
            var description = new LocString("powers", Id.Entry + ".description");
            description.Add("BurnDamage", BurnDamage);
            description.Add("Turns", RemainingTurns);
            return description;
        }
    }

    public static async Task ApplyMark(PlayerChoiceContext choiceContext, Creature target,
        Creature applier, int burnDamage, int turns, CardModel? cardSource)
    {
        if (target.IsDead) return;

        var canonical = ModelDb.Power<LightMarkPower>();
        var existing = PowerCmd.FindExistingInstanceForStacking(canonical, target, applier) as LightMarkPower;
        var amount = Math.Max(turns, existing?.RemainingTurns ?? 0) * 10 +
            Math.Max(burnDamage, existing?.BurnDamage ?? 0);
        var increase = amount - (existing?.Amount ?? 0);
        if (increase > 0)
            await PowerCmd.Apply<LightMarkPower>(choiceContext, target, increase, applier, cardSource);
    }

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target,
        DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner || dealer != Applier || !props.IsPoweredAttack() ||
            result.TotalDamage <= 0 || !Owner.IsAlive || Applier == null) return;

        Flash();
        // Unpowered damage uses ordinary block, ignores attack scaling and cannot
        // re-enter this mark's powered-attack trigger.
        if (cardSource?.TargetType == MegaCrit.Sts2.Core.Entities.Cards.TargetType.AnyEnemy &&
            Applier.GetPower<SpreadingSparksPower>() is { } sparks)
            await CreatureCmd.Damage(choiceContext, Applier.CombatState!.HittableEnemies.ToArray(),
                BurnDamage + sparks.Amount, ValueProp.Unpowered, Applier);
        else
            await CreatureCmd.Damage(choiceContext, Owner, BurnDamage, ValueProp.Unpowered, Applier);
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Applier?.Player) return;

        if (RemainingTurns <= 1)
            await PowerCmd.Remove(this);
        else
            SetAmount(Amount - 10);
    }
}
