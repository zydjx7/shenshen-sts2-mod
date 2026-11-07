using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using Shenshen.ShenshenCode.Powers;

namespace Shenshen.ShenshenCode.Mechanics;

internal static class KindledAttackTracking
{
    [HarmonyPatch]
    private static class PileMutationPatch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            // Native ordinary Shuffle moves cards through AddInternal/RemoveInternal;
            // the direct in-place randomizer is RandomizeOrderInternal on this game version.
            foreach (var name in new[]
            {
                nameof(CardPile.AddInternal), nameof(CardPile.RemoveInternal),
                nameof(CardPile.RandomizeOrderInternal),
                nameof(CardPile.MoveToTopInternal), nameof(CardPile.MoveToBottomInternal)
            })
                yield return AccessTools.DeclaredMethod(typeof(CardPile), name)
                    ?? throw new MissingMethodException(typeof(CardPile).FullName, name);
        }

        private static void Prefix(object[] __args, out KindledAttackPower[] __state)
        {
            var combat = __args.OfType<ICombatState>().FirstOrDefault();
            var card = __args.OfType<CardModel>().FirstOrDefault();
            IEnumerable<Player> players = combat?.Players ??
                (card?.Owner == null ? Array.Empty<Player>() : new[] { card.Owner });
            __state = players.SelectMany(player => player.Creature.GetPowerInstances<KindledAttackPower>()).ToArray();
            foreach (var power in __state) power.CaptureBeforePileChange();
        }

        private static void Postfix(KindledAttackPower[] __state)
        {
            foreach (var power in __state) power.RefreshAfterPileChange();
        }

        private static Exception? Finalizer(Exception? __exception, KindledAttackPower[]? __state)
        {
            if (__exception != null && __state != null)
                foreach (var power in __state) power.RefreshAfterPileChange();
            return __exception;
        }
    }
}
