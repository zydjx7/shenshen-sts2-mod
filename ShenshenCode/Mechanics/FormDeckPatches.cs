using HarmonyLib;
using MegaCrit.Sts2.Core.Audio.Debug;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using Shenshen.ShenshenCode.Cards;

namespace Shenshen.ShenshenCode.Mechanics;

/// <summary>
/// Both owned form decks remain in the game's tracked piles. Drawing and shuffling select
/// a form-specific subsequence, so switching never clones, replaces, or exhausts a card.
/// Shared cards participate in whichever form is active when they are drawn or shuffled.
/// API verified against the installed Windows v0.111.0 assembly.
/// </summary>
internal static class FormDeckPatches
{
    internal static bool IsAvailable(CardModel card, Player player) =>
        card is not ShenshenCard { Form: { } form } || form == FormState.GetForm(player);

    private static bool IsShenshen(Player player) => player.Character is Character.Shenshen;

    [HarmonyPatch(typeof(CardPileCmd), "DrawInternal")]
    private static class DrawPatch
    {
        private static bool Prefix(PlayerChoiceContext choiceContext, decimal count, Player player,
            bool fromHandDraw, ref Task<IEnumerable<CardModel>> __result)
        {
            if (!IsShenshen(player)) return true;
            __result = Draw(choiceContext, count, player, fromHandDraw);
            return false;
        }
    }

    [HarmonyPatch(typeof(CardPileCmd), nameof(CardPileCmd.ShuffleIfNecessary))]
    private static class ShuffleIfNecessaryPatch
    {
        private static bool Prefix(PlayerChoiceContext choiceContext, Player player, ref Task __result)
        {
            if (!IsShenshen(player)) return true;
            __result = ShuffleIfNecessary(choiceContext, player);
            return false;
        }
    }

    [HarmonyPatch(typeof(CardPileCmd), nameof(CardPileCmd.Shuffle))]
    private static class ShufflePatch
    {
        private static bool Prefix(PlayerChoiceContext choiceContext, Player player, ref Task __result)
        {
            if (!IsShenshen(player)) return true;
            __result = Shuffle(choiceContext, player);
            return false;
        }
    }

    [HarmonyPatch(typeof(CardPileCmd), nameof(CardPileCmd.AutoPlayFromDrawPile))]
    private static class AutoPlayFromDrawPilePatch
    {
        private static bool Prefix(PlayerChoiceContext choiceContext, Player player, int count,
            CardPilePosition position, bool forceExhaust, ref Task __result)
        {
            if (!IsShenshen(player)) return true;
            __result = AutoPlayFromDrawPile(choiceContext, player, count, position, forceExhaust);
            return false;
        }
    }

    private static async Task<IEnumerable<CardModel>> Draw(PlayerChoiceContext choiceContext, decimal count,
        Player player, bool fromHandDraw)
    {
        if (CombatManager.Instance.IsOverOrEnding || player.Creature.CombatState is not { } combatState)
            return Array.Empty<CardModel>();
        if (!Hook.ShouldDraw(combatState, player, fromHandDraw, out var modifier))
        {
            if (modifier != null) await Hook.AfterPreventingDraw(combatState, modifier);
            return Array.Empty<CardModel>();
        }

        var result = new List<CardModel>();
        var hand = PileType.Hand.GetPile(player);
        var drawPile = PileType.Draw.GetPile(player);
        int drawsRequested = count > 0 ? (int)Math.Ceiling(count) : 0;
        for (int i = 0; i < drawsRequested && !CombatManager.Instance.IsOverOrEnding; i++)
        {
            if (hand.Cards.Count >= CardPile.MaxCardsInHand)
            {
                ThinkCmd.Play(new LocString("combat_messages", "HAND_FULL"), player.Creature, 2.0);
                break;
            }

            await ShuffleIfNecessary(choiceContext, player);
            var card = drawPile.Cards.FirstOrDefault(c => IsAvailable(c, player));
            if (card == null)
            {
                ThinkCmd.Play(new LocString("combat_messages", "NO_DRAW"), player.Creature, 2.0);
                break;
            }

            result.Add(card);
            await CardPileCmd.Add(card, hand);
            CombatManager.Instance.History.CardDrawn(combatState, card, fromHandDraw);
            await Hook.AfterCardDrawn(combatState, choiceContext, card, fromHandDraw);
            card.InvokeDrawn();
            NDebugAudioManager.Instance?.Play("card_deal.mp3", 0.25f, PitchVariance.Small);
        }
        return result;
    }

    private static Task ShuffleIfNecessary(PlayerChoiceContext choiceContext, Player player)
    {
        var state = player.PlayerCombatState;
        return state != null && !state.DrawPile.Cards.Any(c => IsAvailable(c, player)) &&
               state.DiscardPile.Cards.Any(c => IsAvailable(c, player))
            ? Shuffle(choiceContext, player)
            : Task.CompletedTask;
    }

    private static async Task Shuffle(PlayerChoiceContext choiceContext, Player player)
    {
        if (CombatManager.Instance.IsOverOrEnding || player.PlayerCombatState is not { } state ||
            player.Creature.CombatState is not { } combatState) return;
        var cards = state.DiscardPile.Cards.Where(c => IsAvailable(c, player)).ToList();
        cards.AddRange(state.DrawPile.Cards.Where(c => IsAvailable(c, player)));
        if (cards.Count == 0) return;

        cards.StableShuffle(player.RunState.Rng.Shuffle);
        Hook.ModifyShuffleOrder(combatState, player, cards, isInitialShuffle: false);
        // Moving the existing references to the bottom leaves the inactive subsequence in order.
        await CardPileCmd.Add(cards, state.DrawPile);
        if (!CombatManager.Instance.IsOverOrEnding)
            await Hook.AfterShuffle(combatState, choiceContext, player);
    }

    private static async Task AutoPlayFromDrawPile(PlayerChoiceContext choiceContext, Player player,
        int count, CardPilePosition position, bool forceExhaust)
    {
        if (CombatManager.Instance.IsOverOrEnding) return;
        var cards = new List<CardModel>();
        var drawPile = PileType.Draw.GetPile(player);
        for (int i = 0; i < count && !CombatManager.Instance.IsOverOrEnding; i++)
        {
            await ShuffleIfNecessary(choiceContext, player);
            var available = drawPile.Cards.Where(c => IsAvailable(c, player)).ToList();
            var card = position switch
            {
                CardPilePosition.Top => available.FirstOrDefault(),
                CardPilePosition.Bottom => available.LastOrDefault(),
                CardPilePosition.Random => available.Count > 0
                    ? player.RunState.Rng.CombatCardSelection.NextItem(available) : null,
                _ => throw new ArgumentOutOfRangeException(nameof(position), position, null)
            };
            if (card == null) break;
            cards.Add(card);
            await CardPileCmd.Add(card, PileType.Play);
        }
        foreach (var card in cards)
        {
            if (card.Owner.Creature.IsDead) break;
            card.ExhaustOnNextPlay = forceExhaust;
            await CardCmd.AutoPlay(choiceContext, card, null);
        }
    }
}
