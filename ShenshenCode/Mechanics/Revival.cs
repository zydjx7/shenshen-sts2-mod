using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using Shenshen.ShenshenCode.Cards;
using Shenshen.ShenshenCode.Powers;

namespace Shenshen.ShenshenCode.Mechanics;

internal static class Revival
{
    private sealed record Snapshot((CardModel Card, PileType Pile)[] Cards);
    private static readonly ConditionalWeakTable<PlayerCombatState, Snapshot> Fallen = new();

    // Native death removes the cards from their piles and marks them removed. Keep the original
    // references and pile order until this fight ends, including the exhaust pile.
    [HarmonyPatch(typeof(CombatManager), nameof(CombatManager.RemoveDeadPlayerCardsFromCombat))]
    private static class RememberPiles
    {
        private static void Prefix(Player player)
        {
            if (!player.Creature.IsDead || player.PlayerCombatState is not { } state ||
                !player.RunState.Players.Any(p => p.Character is Character.Shenshen)) return;
            var cards = state.AllCards.Select(c => (c, c.Pile!.Type)).ToArray();
            if (cards.Length > 0) Fallen.AddOrUpdate(state, new Snapshot(cards));
        }
    }

    internal static async Task Restore(PlayerChoiceContext context, Creature target)
    {
        if (!target.IsDead || target.Player?.PlayerCombatState is not { } state || target.CombatState is not { } combat) return;
        await CreatureCmd.Heal(target, Math.Max(1, (int)(target.MaxHp * 0.15m)));
        if (Fallen.TryGetValue(state, out var snapshot))
        {
            foreach (var (card, pile) in snapshot.Cards)
            {
                if (card.Pile != null) continue;
                if (!combat.ContainsCard(card)) combat.AddCard(card, target.Player);
                card.HasBeenRemovedFromState = false;
                await CardPileCmd.Add(card, pile == PileType.Play ? PileType.Discard : pile);
            }
            Fallen.Remove(state);
        }
        await PowerCmd.Apply<AwaitingRevivalTurnPower>(context, target, 1, target, null);
    }

    [HarmonyPatch(typeof(CardModel), nameof(CardModel.IsValidTarget))]
    private static class DeadTarget
    {
        private static void Postfix(CardModel __instance, Creature? target, ref bool __result)
        {
            if (__instance is Resurrection)
                __result = target?.Player != null && target != __instance.Owner.Creature && target.IsDead && target.CombatState == __instance.CombatState;
        }
    }

    [HarmonyPatch(typeof(CardCmd), nameof(CardCmd.AutoPlay))]
    private static class AutomaticTarget
    {
        private static void Prefix(CardModel card, ref Creature? target)
        {
            if (card is Resurrection && target == null && card.CombatState is { } combat)
                target = card.Owner.RunState.Rng.CombatTargets.NextItem(combat.PlayerCreatures.Where(card.IsValidTarget));
        }
    }

    [HarmonyPatch]
    private static class LivingAllyRestriction
    {
        private static System.Reflection.MethodBase TargetMethod() => AccessTools.Method(typeof(CardModel), nameof(CardModel.CanPlay),
            [typeof(UnplayableReason).MakeByRefType(), typeof(AbstractModel).MakeByRefType()]);
        private static void Postfix(CardModel __instance, ref UnplayableReason reason, ref bool __result)
        {
            if (__instance is not Resurrection || !reason.HasFlag(UnplayableReason.NoLivingAllies)) return;
            reason &= ~UnplayableReason.NoLivingAllies;
            __result = reason == UnplayableReason.None;
        }
    }

    private static Resurrection? aimedCard;
    [HarmonyPatch(typeof(NTargetManager), nameof(NTargetManager.StartTargeting),
        [typeof(TargetType), typeof(Control), typeof(TargetMode), typeof(Func<bool>), typeof(Func<Node, bool>)])]
    private static class Aim
    {
        private static void Prefix(Control control) => aimedCard = (control as NCard)?.Model as Resurrection;
    }

    [HarmonyPatch(typeof(NTargetManager), nameof(NTargetManager.StartTargeting),
        [typeof(TargetType), typeof(Vector2), typeof(TargetMode), typeof(Func<bool>), typeof(Func<Node, bool>)])]
    private static class OtherAim
    {
        private static void Prefix() => aimedCard = null;
    }

    [HarmonyPatch(typeof(NTargetManager), "AllowedToTargetCreature")]
    private static class Hover
    {
        private static void Postfix(Creature creature, ref bool __result)
        {
            if (aimedCard != null) __result = aimedCard.IsValidTarget(creature);
        }
    }

    [HarmonyPatch(typeof(NControllerCardPlay), "SingleCreatureTargeting")]
    private static class Controller
    {
        private static bool Prefix(NControllerCardPlay __instance, ref Task __result)
        {
            if (__instance.Holder.CardNode?.Model is not Resurrection card) return true;
            __result = SelectFallen(__instance, card);
            return false;
        }

        private static async Task SelectFallen(NControllerCardPlay play, Resurrection card)
        {
            var room = NCombatRoom.Instance;
            if (room == null || play.Holder.CardNode is not { } cardNode) { play.CancelPlayCard(); return; }
            var nodes = card.CombatState!.PlayerCreatures.Where(card.IsValidTarget)
                .Select(room.GetCreatureNode).OfType<NCreature>().ToArray();
            if (nodes.Length == 0) { play.CancelPlayCard(); return; }
            var manager = NTargetManager.Instance;
            manager.StartTargeting(TargetType.AnyAlly, cardNode, TargetMode.Controller,
                () => !GodotObject.IsInstanceValid(play), null);
            room.RestrictControllerNavigation(nodes.Select(n => n.Hitbox));
            Node? selected;
            try
            {
                nodes[0].Hitbox.GrabFocus();
                selected = await manager.SelectionFinished();
            }
            finally { room.EnableControllerNavigation(); }
            if (!GodotObject.IsInstanceValid(play)) return;
            if (selected is NCreature creature)
                AccessTools.Method(typeof(NCardPlay), "TryPlayCard").Invoke(play, [creature.Entity]);
            else play.CancelPlayCard();
        }
    }
}
