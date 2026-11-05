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
        private static void Prefix(ref IEnumerable<Creature>? targets, ValueProp props, Creature? dealer)
        {
            var frame = Current.Value;
            if (targets == null || frame == null || frame.HitCount <= 1 ||
                frame.Command.Results.Any() || dealer?.Side != CombatSide.Enemy || dealer.IsDead ||
                frame.Command.Attacker != dealer || props != frame.Command.DamageProps) return;

            var remaining = new List<Creature>();
            foreach (var target in targets)
            {
                var dodge = target.GetPower<CatDodgePower>();
                if (target.IsAlive && target.Player is { } player && dodge?.Amount == 1 &&
                    FormState.GetForm(player) == ShenshenForm.Cat)
                {
                    dodge.ConsumeDodge();
                    MainFile.Logger.Info("Cat dodge avoided the first segment of an enemy multi-hit attack.");
                }
                else remaining.Add(target);
            }
            // Do not turn damage into zero: removing the target bypasses the entire
            // reception pipeline, including Thorns, block loss, and Reflect.
            targets = remaining;
        }
    }
}
