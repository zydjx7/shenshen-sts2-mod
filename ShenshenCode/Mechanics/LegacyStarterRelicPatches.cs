using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Runs;

namespace Shenshen.ShenshenCode.Mechanics;

/// <summary>Retires the Ironclad starter from existing Shenshen runs.</summary>
internal static class LegacyStarterRelicPatches
{
    [HarmonyPatch(typeof(NGame), nameof(NGame.LoadRun))]
    private static class LoadRunPatch
    {
        private static void Postfix(RunState runState, ref Task __result) =>
            __result = RemoveLegacyStarterAfterLoad(__result, runState);
    }

    private static async Task RemoveLegacyStarterAfterLoad(Task load, RunState runState)
    {
        // At this point the loaded run and its normal relic UI are fully established.
        await load;
        foreach (var player in runState.Players.Where(player => player.Character is Character.Shenshen))
        {
            foreach (var relic in player.Relics.OfType<BurningBlood>().ToList())
            {
                await RelicCmd.Remove(relic);
                MainFile.Logger.Info("Removed legacy Burning Blood from a loaded Shenshen run.");
            }
        }
    }

    [HarmonyPatch(typeof(BurningBlood), nameof(BurningBlood.AfterCombatVictory))]
    private static class VictoryHealPatch
    {
        private static bool Prefix(BurningBlood __instance, ref Task __result)
        {
            if (__instance.Owner.Character is not Character.Shenshen) return true;
            __result = Task.CompletedTask;
            return false;
        }
    }
}
