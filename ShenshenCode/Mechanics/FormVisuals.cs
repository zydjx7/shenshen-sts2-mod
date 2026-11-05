using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Combat;
using Shenshen.ShenshenCode.Character;
using Shenshen.ShenshenCode.Visuals;

namespace Shenshen.ShenshenCode.Mechanics;

// The tracked FormPower remains the source of truth; visuals do not hold gameplay state.
internal static class FormVisuals
{
    [HarmonyPatch(typeof(NCreature), nameof(NCreature._Ready))]
    private static class CreatureReadyPatch
    {
        private static void Postfix(NCreature __instance)
        {
            if (__instance.Entity.Player is { Character: Character.Shenshen } player)
                ShenshenVisuals.SetForm(__instance.Visuals, FormState.GetForm(player), animate: false);
        }
    }

    [HarmonyPatch(typeof(NCreature), "OnCombatEnded")]
    private static class CombatEndedPatch
    {
        private static void Postfix(NCreature __instance)
        {
            if (__instance.Entity.Player?.Character is Character.Shenshen && !__instance.Entity.IsDead)
                ShenshenVisuals.SetForm(__instance.Visuals, ShenshenForm.Human, animate: false);
        }
    }
}
