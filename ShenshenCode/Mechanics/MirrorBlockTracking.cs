using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Shenshen.ShenshenCode.Powers;

namespace Shenshen.ShenshenCode.Mechanics;

internal static class MirrorBlockTracking
{
    private sealed class DamageFrame
    {
        internal readonly Dictionary<Creature, int> Consumed = new();
    }

    // Only the in-flight damage result is transient. Remaining mirror block is
    // always kept in Power.Amount, so native multiplayer state carries it.
    private static readonly AsyncLocal<DamageFrame?> Current = new();

    internal static int TakeConsumedBlock(Creature creature) =>
        Current.Value?.Consumed.Remove(creature, out var consumed) == true ? consumed : 0;

    [HarmonyPatch(typeof(CreatureCmd), nameof(CreatureCmd.Damage),
        [typeof(PlayerChoiceContext), typeof(IEnumerable<Creature>), typeof(decimal), typeof(ValueProp),
            typeof(Creature), typeof(CardModel), typeof(CardPlay)])]
    private static class DamageScopePatch
    {
        private static void Prefix(out DamageFrame? __state)
        {
            __state = Current.Value;
            Current.Value = new DamageFrame();
        }

        private static void Postfix(DamageFrame? __state) => Current.Value = __state;
        private static Exception? Finalizer(Exception? __exception, DamageFrame? __state)
        {
            Current.Value = __state;
            return __exception;
        }
    }

    [HarmonyPatch(typeof(Creature), nameof(Creature.Block), MethodType.Setter)]
    private static class BlockLossPatch
    {
        private static void Prefix(Creature __instance, out int __state) => __state = __instance.Block;

        private static void Postfix(Creature __instance, int __state)
        {
            var lost = __state - __instance.Block;
            var mirror = __instance.GetPower<ShatteredMirrorPower>();
            if (lost > 0 && mirror?.Amount > 0)
                mirror.SetAmount(Math.Max(0, mirror.Amount - lost));
        }
    }

    [HarmonyPatch(typeof(Creature), nameof(Creature.DamageBlockInternal))]
    private static class DamageBlockPatch
    {
        private static void Prefix(Creature __instance, out int __state) =>
            __state = __instance.GetPower<ShatteredMirrorPower>()?.Amount ?? 0;

        private static void Postfix(Creature __instance, int __state, decimal __result)
        {
            var consumed = Math.Min(__state, (int)__result);
            if (consumed > 0 && Current.Value is { } frame)
                frame.Consumed[__instance] = consumed;
        }
    }
}
