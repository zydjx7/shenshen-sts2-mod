using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using Shenshen.ShenshenCode.Cards;
using Shenshen.ShenshenCode.Character;
using Shenshen.ShenshenCode.Powers;
using Shenshen.ShenshenCode.Visuals;

namespace Shenshen.ShenshenCode.Mechanics;

internal static class FormState
{
    // Amount belongs to the game's tracked power state and multiplayer checksum.
    internal static ShenshenForm GetForm(Player player) =>
        player.Creature.GetPower<FormPower>()?.Amount == 2 ? ShenshenForm.Cat : ShenshenForm.Human;

    internal static async Task Switch(PlayerChoiceContext context, Player player)
    {
        var current = GetForm(player);
        var replaced = player.PlayerCombatState!.Hand.Cards
            .OfType<ShenshenCard>().Where(card => card.Form == current).ToArray();
        var power = player.Creature.GetPower<FormPower>();
        if (power == null) return;
        await CardCmd.Discard(context, replaced);
        power.SetAmount(current == ShenshenForm.Human ? 2 : 1);
        ShenshenVisuals.SetForm(player.Creature.GetCreatureNode()?.Visuals, GetForm(player));
        await CardPileCmd.Draw(context, replaced.Length, player);
        MainFile.Logger.Info($"Form switched to {GetForm(player)}; replaced {replaced.Length} owned cards.");
    }

    [HarmonyPatch(typeof(Hook), nameof(Hook.BeforeHandDraw))]
    private static class OpeningHandPatch
    {
        private static void Postfix(ICombatState combatState, Player player,
            PlayerChoiceContext playerChoiceContext, ref Task __result)
        {
            if (player.Character is Character.Shenshen)
                __result = PrepareOpeningHand(__result, combatState, player, playerChoiceContext);
        }

        private static async Task PrepareOpeningHand(Task previous, ICombatState state,
            Player player, PlayerChoiceContext context)
        {
            await previous;
            if (player.Creature.GetPower<FormPower>() != null) return;
            await PowerCmd.Apply<FormPower>(context, player.Creature, 1, player.Creature, null);
            await PowerCmd.Apply<CatDodgePower>(context, player.Creature, 1, player.Creature, null);
            // This is the single extra combat-only transformation card, never a replacement deck.
            var transformation = state.CreateCard<TransformationMagic>(player);
            await CardPileCmd.AddGeneratedCardToCombat(transformation, PileType.Hand, player);
        }
    }
}
