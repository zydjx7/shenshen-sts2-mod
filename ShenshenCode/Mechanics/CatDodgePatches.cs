using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Shenshen.ShenshenCode.Character;
using Shenshen.ShenshenCode.Powers;

namespace Shenshen.ShenshenCode.Mechanics;

internal static class CatDodgePatches
{
    private sealed class AttackFrame(AttackCommand command)
    {
        internal readonly AttackCommand Command = command;
        internal decimal HitCount;
    }

    // Each async attack captures its own frame. Restore the caller immediately;
    // nested/counter attacks and concurrent async flows must not inherit its target.
    private static readonly AsyncLocal<AttackFrame?> Current = new();

    [HarmonyPatch(typeof(AttackCommand), nameof(AttackCommand.Execute))]
    private static class AttackScopePatch
    {
        private static void Prefix(AttackCommand __instance, out AttackFrame? __state)
        {
            __state = Current.Value;
            Current.Value = new AttackFrame(__instance);
        }

        private static void Postfix(AttackFrame? __state) => Current.Value = __state;

        private static Exception? Finalizer(Exception? __exception, AttackFrame? __state)
        {
            Current.Value = __state;
            return __exception;
        }
    }

    [HarmonyPatch(typeof(Hook), nameof(Hook.ModifyAttackHitCount))]
    private static class HitCountPatch
    {
        private static void Postfix(AttackCommand attackCommand, decimal __result)
        {
            if (Current.Value is { } frame && frame.Command == attackCommand)
                frame.HitCount = __result;
        }
    }

    [HarmonyPatch(typeof(CreatureCmd), nameof(CreatureCmd.Damage),
        [typeof(PlayerChoiceContext), typeof(IEnumerable<Creature>), typeof(decimal), typeof(ValueProp),
            typeof(Creature), typeof(CardModel), typeof(CardPlay)])]
    private static class DamagePatch
    {
        private static bool Prefix(PlayerChoiceContext choiceContext, ref IEnumerable<Creature>? targets,
            decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay,
            ref Task<IEnumerable<DamageResult>> __result)
        {
            var frame = Current.Value;
            if (targets == null || frame == null || dealer?.Side != CombatSide.Enemy || dealer.IsDead ||
                frame.Command.Attacker != dealer || props != frame.Command.DamageProps || RareControl.IsResolving(dealer)) return true;
            if (RareControl.Handles(dealer, targets))
            {
                __result = RareControl.Resolve(choiceContext, targets, dealer, amount, props, cardSource, cardPlay,
                    Math.Max(1, (int)frame.HitCount), frame.Command.Results.Count());
                return false;
            }
            if (frame.HitCount <= 1 || frame.Command.Results.Any()) return true;

            var remaining = new List<Creature>();
            foreach (var target in targets)
            {
                if (!target.IsAlive || !RareControl.Dodge(target)) remaining.Add(target);
            }
            // Do not turn damage into zero: removing the target bypasses the entire
            // reception pipeline, including Thorns, block loss, and Reflect.
            targets = remaining;
            return true;
        }
    }
}
