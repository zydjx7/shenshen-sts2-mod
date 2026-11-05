using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
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

    internal static bool HasShiftedThisTurn(Player player) =>
        player.Creature.GetPower<FormShiftPower>()?.Amount > 0;

    internal static async Task Switch(PlayerChoiceContext context, Player player, bool upgraded = false)
    {
        var power = player.Creature.GetPower<FormPower>();
        if (power == null || player.PlayerCombatState == null) return;
        var current = GetForm(player);
        var replaced = player.PlayerCombatState.Hand.Cards
            .OfType<ShenshenCard>().Where(card => card.Form == current).ToArray();
        await CardCmd.Discard(context, replaced);
        power.SetAmount(current == ShenshenForm.Human ? 2 : 1);
        if (!HasShiftedThisTurn(player))
            await PowerCmd.Apply<FormShiftPower>(context, player.Creature, 1, player.Creature, null);
        if (current == ShenshenForm.Human)
        {
            await PowerCmd.Apply<StrengthPower>(context, player.Creature, 1, player.Creature, null);
            if (upgraded)
                await PowerCmd.Apply<ThornsPower>(context, player.Creature, 1, player.Creature, null);
        }
        else
        {
            await PowerCmd.Apply<DexterityPower>(context, player.Creature, 1, player.Creature, null);
            // Prototype choice: this immediate reward is fixed, rather than boosted by Dexterity.
            if (upgraded)
                await CreatureCmd.GainBlock(player.Creature, 4m, ValueProp.Unpowered, null);
        }
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
            // Move the owned card's real combat instance before the normal five-card draw.
            var transformation = player.PlayerCombatState!.AllCards.OfType<TransformationMagic>().FirstOrDefault();
            if (transformation != null)
                await CardPileCmd.Add(transformation, PileType.Hand);
        }
    }

    private static void RestoreMissingOwnedTransformation(Player player)
    {
        if (player.Character is not Character.Shenshen || player.Deck.Cards.OfType<TransformationMagic>().Any())
            return;

        // During FromSerializable, native deck cards do not yet have owners. CreateShared
        // registers every loaded deck card afterwards; sync already has a live RunState.
        var transformation = player.RunState is NullRunState
            ? ModelDb.Card<TransformationMagic>().ToMutable()
            : player.RunState.CreateCard<TransformationMagic>(player);
        transformation.FloorAddedToDeck = 1;
        player.Deck.AddInternal(transformation, silent: true);
        MainFile.Logger.Info("Migrated one missing owned Transformation Magic into the permanent deck.");
    }

    [HarmonyPatch(typeof(Player), nameof(Player.FromSerializable))]
    private static class MigrateLoadedDeckPatch
    {
        private static void Postfix(Player __result) => RestoreMissingOwnedTransformation(__result);
    }

    [HarmonyPatch(typeof(Player), nameof(Player.SyncWithSerializedPlayer))]
    private static class MigrateSynchronizedDeckPatch
    {
        private static void Postfix(Player __instance) => RestoreMissingOwnedTransformation(__instance);
    }
}
