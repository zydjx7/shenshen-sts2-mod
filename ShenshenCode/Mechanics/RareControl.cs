using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Shenshen.ShenshenCode.Character;
using Shenshen.ShenshenCode.Powers;

namespace Shenshen.ShenshenCode.Mechanics;

internal static class RareControl
{
    private sealed record Segment(Creature Dealer, Creature Target, int Original, int Count, int Index, bool Zero);
    private static readonly AsyncLocal<Segment?> Inside = new();
    internal static bool IsResolving(Creature dealer) => Inside.Value?.Dealer == dealer;

    internal static bool Handles(Creature dealer, IEnumerable<Creature> targets) =>
        Inside.Value?.Dealer != dealer && (dealer.GetPower<OttoDancePower>() != null ||
        dealer.GetPower<CharmSpellPower>() != null || dealer.GetPower<SunBlindPower>() != null ||
        targets.Any(t => t.GetPower<PreparedSpellPower>() != null));

    internal static async Task<IEnumerable<DamageResult>> Resolve(PlayerChoiceContext context, IEnumerable<Creature> targets,
        Creature dealer, decimal amount, ValueProp props, CardModel? card, CardPlay? play, int original, int index)
    {
        var results = new List<DamageResult>();
        foreach (var target in targets.ToArray())
        {
            foreach (var spell in target.GetPowerInstances<PreparedSpellPower>().ToArray())
                if (dealer.IsAlive) await spell.Release(context, dealer);
            if (dealer.IsDead || !target.IsAlive) continue;
            var charm = dealer.GetPowerInstances<CharmSpellPower>().FirstOrDefault(p => p.Applier == target);
            if (charm != null) charm.Attacked = true;
            var count = Math.Max(original, charm?.Amount ?? 0);
            var blind = dealer.GetPowerInstances<SunBlindPower>().Any(p => p.Applier == target);
            for (int part = index * count / original; part < (index + 1) * count / original && dealer.IsAlive && target.IsAlive; part++)
            {
                if (blind) { CountDodge(target); continue; }
                if (part == 0 && count > 1 && Dodge(target)) continue;
                bool zero = dealer.GetPower<OttoDancePower>() != null && part % 2 == 1;
                var previous = Inside.Value;
                Inside.Value = new Segment(dealer, target, original, count, part, zero);
                try
                {
                    results.AddRange(await CreatureCmd.Damage(context, target, amount, props, dealer, card, play));
                }
                finally { Inside.Value = previous; }
            }
        }
        return results;
    }

    internal static bool Dodge(Creature target)
    {
        if (target.Player is not { } player || FormState.GetForm(player) != ShenshenForm.Cat ||
            target.GetPower<CatDodgePower>() is not { Amount: 1 } dodge) return false;
        dodge.ConsumeDodge();
        CountDodge(target);
        return true;
    }

    private static void CountDodge(Creature target)
    {
        foreach (var landing in target.GetPowerInstances<FourPawsLandingPower>()) landing.CountHit();
    }

    [HarmonyPatch(typeof(Hook), nameof(Hook.ModifyDamage))]
    private static class SplitAfterModifiers
    {
        private static void Postfix(Creature? dealer, Creature? target, ValueProp props, ref decimal __result)
        {
            if (Inside.Value is not { } s || s.Dealer != dealer || s.Target != target || !props.IsPoweredAttack()) return;
            if (s.Zero) { __result = 0; return; }
            if (s.Count == s.Original) return;
            int total = (int)__result * s.Original;
            __result = total / s.Count + (s.Index < total % s.Count ? 1 : 0);
        }
    }

    [HarmonyPatch(typeof(MonsterModel), nameof(MonsterModel.PerformMove))]
    private static class AlterMove
    {
        private static bool Prefix(MonsterModel __instance, ref Task __result)
        {
            var monster = __instance.Creature;
            if (monster.GetPower<StoppedTimePower>() is { } stopped)
            {
                if (!stopped.WaitingForRoll)
                {
                    stopped.WaitingForRoll = true;
                    __result = Task.CompletedTask;
                }
                else if (stopped.Amount > 1) __result = PowerCmd.Decrement(stopped);
                else __result = Resume(__instance, stopped);
                return false;
            }
            if (monster.GetPower<SheepPower>() is not { } sheep) return true;
            __instance.MoveStateMachine?.OnMovePerformed(__instance.NextMove);
            __result = PowerCmd.Remove(sheep);
            return false;
        }

        private static async Task Resume(MonsterModel monster, StoppedTimePower stopped)
        {
            await PowerCmd.Remove(stopped);
            await monster.PerformMove();
        }

        private static void Postfix(MonsterModel __instance, ref Task __result)
        {
            if (__instance.Creature.GetPower<CharmSpellPower>() != null &&
                __instance.Creature.GetPower<StoppedTimePower>()?.WaitingForRoll != true)
                __result = FinishCharm(__result, __instance.Creature);
        }

        private static async Task FinishCharm(Task move, Creature monster)
        {
            await move;
            foreach (var charm in monster.GetPowerInstances<CharmSpellPower>().ToArray())
            {
                var target = charm.Applier;
                var count = charm.Amount;
                var attacked = charm.Attacked;
                await PowerCmd.Remove(charm);
                if (attacked || !monster.IsAlive || target?.IsAlive != true) continue;
                var context = new BlockingPlayerChoiceContext();
                for (int i = 0; i < count && monster.IsAlive && target.IsAlive; i++)
                {
                    if (monster.GetPowerInstances<SunBlindPower>().Any(p => p.Applier == target)) { CountDodge(target); continue; }
                    if (i == 0 && count > 1 && Dodge(target)) continue;
                    var previous = Inside.Value;
                    Inside.Value = new Segment(monster, target, count, count, i, true);
                    try { await CreatureCmd.Damage(context, target, 0, ValueProp.Move, monster); }
                    finally { Inside.Value = previous; }
                }
            }
        }
    }

    [HarmonyPatch(typeof(MonsterModel), nameof(MonsterModel.RollMove))]
    private static class KeepPostponedIntent
    {
        private static bool Prefix(MonsterModel __instance) =>
            __instance.Creature.GetPower<StoppedTimePower>()?.WaitingForRoll != true;
    }

    [HarmonyPatch(typeof(CreatureCmd), nameof(CreatureCmd.Damage),
        [typeof(PlayerChoiceContext), typeof(IEnumerable<Creature>), typeof(decimal), typeof(ValueProp), typeof(Creature), typeof(CardModel), typeof(CardPlay)])]
    private static class PierceBlock
    {
        private static void Prefix(CardModel? cardSource, Creature? dealer, ref ValueProp props)
        {
            if (cardSource != null && props.IsPoweredAttack() && dealer?.GetPower<UncertainHandPower>()?.Pierces(cardSource) == true)
                props |= ValueProp.Unblockable;
        }
    }

    [HarmonyPatch(typeof(CardModel), nameof(CardModel.GetDescriptionForPile), [typeof(PileType), typeof(Creature)])]
    private static class ShowPiercing
    {
        private static void Postfix(CardModel __instance, PileType pileType, ref string __result)
        {
            if (pileType == PileType.Hand && __instance.IsMutable && __instance.Owner?.Creature.GetPower<UncertainHandPower>()?.Pierces(__instance) == true)
                __result += "\n" + new LocString("cards", "SHENSHEN-UNCERTAIN_HAND.piercing").GetFormattedText();
        }
    }
}
