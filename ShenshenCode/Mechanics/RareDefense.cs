using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using Shenshen.ShenshenCode.Powers;

namespace Shenshen.ShenshenCode.Mechanics;

internal static class RareDefense
{
    internal static Task PayDebt(PlayerChoiceContext context, Creature owner, int amount) =>
        CreatureCmd.SetCurrentHp(owner, Math.Max(0, owner.CurrentHp - amount));

    [HarmonyPatch(typeof(Hook), nameof(Hook.AfterPlayerTurnStart))]
    private static class ReleaseAfterAllStartEffects
    {
        private static void Postfix(Player player, ref Task __result) => __result = Finish(__result, player);
        private static async Task Finish(Task startEffects, Player player)
        {
            await startEffects;
            if (player.Creature.GetPower<PhylacteryGuardPower>() is { } guard) await PowerCmd.Remove(guard);
        }
    }

    // These internal entries are the actual HP mutation, after Block and ordinary modifiers.
    // Damage previews never accumulate debt. Multiple wards capture a given hit only once.
    [HarmonyPatch(typeof(Creature), nameof(Creature.LoseHpInternal))]
    private static class DelayDamage
    {
        private static void Prefix(Creature __instance, ref decimal amount)
        {
            if (__instance.GetPower<PhylacteryGuardPower>() != null) { amount = 0; return; }
            if (amount < 1 || __instance.Player?.PlayerCombatState is not { } state) return;
            var ward = __instance.GetPowerInstances<TemporalWardPower>().FirstOrDefault(p => p.CastTurn == state.TurnNumber);
            if (ward == null) return;
            ward.Debt += (int)amount;
            amount = 0;
        }
    }

    [HarmonyPatch(typeof(CreatureCmd), nameof(CreatureCmd.Heal))]
    private static class RepayBeforeHealing
    {
        private static void Prefix(Creature creature, ref decimal amount)
        {
            if (creature.GetPower<PhylacteryGuardPower>() != null) { amount = 0; return; }
            foreach (var ward in creature.GetPowerInstances<TemporalWardPower>().OrderBy(p => p.CastTurn))
            {
                var payment = Math.Min((int)amount, ward.Debt);
                amount -= payment;
                ward.Debt -= payment;
            }
        }
    }

    [HarmonyPatch(typeof(Creature), nameof(Creature.SetCurrentHpInternal))]
    private static class LockHp
    {
        private static void Prefix(Creature __instance, ref decimal amount)
        {
            if (__instance.GetPower<PhylacteryGuardPower>() != null) amount = 1;
        }
    }
}
