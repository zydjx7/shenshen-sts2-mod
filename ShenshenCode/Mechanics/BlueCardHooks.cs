using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Shenshen.ShenshenCode.Powers;

namespace Shenshen.ShenshenCode.Mechanics;

internal static class BlueCardHooks
{
    internal static async Task AfterReflection(PlayerChoiceContext context, Creature owner)
    {
        await MirrorAfterglowPower.AfterReflection(context, owner);
        await ReturningClawMemoryPower.RecordReflection(context, owner);
    }

    // Keep the native Thorns trigger; change only this character's thorn damage
    // while the temporary multiplier is active, then notify its growth power.
    [HarmonyPatch(typeof(ThornsPower), nameof(ThornsPower.BeforeDamageReceived))]
    private static class ThornsDamagePatch
    {
        private static bool Prefix(ThornsPower __instance, PlayerChoiceContext choiceContext,
            Creature target, ValueProp props, Creature? dealer, CardModel? cardSource, ref Task __result)
        {
            if (__instance.Owner.Player?.Character is not Character.Shenshen) return true;
            __result = Resolve(__instance, choiceContext, target, props, dealer, cardSource);
            return false;
        }

        private static async Task Resolve(ThornsPower power, PlayerChoiceContext context,
            Creature target, ValueProp props, Creature? dealer, CardModel? cardSource)
        {
            if (target != power.Owner || dealer == null ||
                (!props.IsPoweredAttack() && cardSource is not Omnislice)) return;
            var multiplier = power.Owner.GetPower<DandelionPuffPower>() == null ? 1 : 2;
            var results = await CreatureCmd.Damage(context, dealer, power.Amount * multiplier,
                ValueProp.Unpowered | ValueProp.SkipHurtAnim, power.Owner, null, null);
            if (results.Any(result => result.TotalDamage > 0))
                await ThornyPurrPower.AfterThornsDamage(context, power.Owner);
        }
    }

    [HarmonyPatch(typeof(ReflectPower), nameof(ReflectPower.AfterDamageReceived))]
    private static class FullMirrorDamagePatch
    {
        private static bool Prefix(ReflectPower __instance, PlayerChoiceContext choiceContext,
            Creature target, DamageResult result, ValueProp props, Creature? dealer, ref Task __result)
        {
            if (__instance.Owner.Player?.Character is not Character.Shenshen) return true;
            __result = Resolve(__instance, choiceContext, target, result, props, dealer);
            return false;
        }

        private static async Task Resolve(ReflectPower power, PlayerChoiceContext context,
            Creature target, DamageResult result, ValueProp props, Creature? dealer)
        {
            if (target != power.Owner || result.BlockedDamage <= 0 ||
                !props.IsPoweredAttack() || dealer == null) return;
            var reflected = await CreatureCmd.Damage(context, dealer, result.BlockedDamage,
                ValueProp.Unpowered, power.Owner);
            if (reflected.Any(hit => hit.TotalDamage > 0))
                await AfterReflection(context, power.Owner);
        }
    }

    // The enumerable overload settles an entire attack segment, including all
    // AoE targets. One notification here avoids counting an AoE once per enemy.
    [HarmonyPatch(typeof(CreatureCmd), nameof(CreatureCmd.Damage),
        [typeof(PlayerChoiceContext), typeof(IEnumerable<Creature>), typeof(decimal), typeof(ValueProp),
            typeof(Creature), typeof(CardModel), typeof(CardPlay)])]
    private static class AttackSegmentPatch
    {
        private static void Postfix(PlayerChoiceContext choiceContext, ValueProp props,
            Creature? dealer, ref Task<IEnumerable<DamageResult>> __result)
        {
            if (dealer?.Player?.Character is Character.Shenshen && props.IsPoweredAttack())
                __result = AfterSegment(__result, choiceContext, dealer);
        }

        private static async Task<IEnumerable<DamageResult>> AfterSegment(
            Task<IEnumerable<DamageResult>> previous, PlayerChoiceContext context, Creature dealer)
        {
            var results = await previous;
            if (dealer.IsAlive && results.Any(hit => hit.TotalDamage > 0))
                await WardingPrayerPower.AfterAttackHit(context, dealer);
            return results;
        }
    }
}
