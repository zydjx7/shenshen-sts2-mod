using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using Shenshen.ShenshenCode.Powers;

namespace Shenshen.ShenshenCode.Mechanics;

[HarmonyPatch]
internal static class RareIntentLabels
{
    private static IEnumerable<MethodBase> TargetMethods() =>
        new[] { typeof(AbstractIntent), typeof(SingleAttackIntent), typeof(MultiAttackIntent), typeof(StatusIntent) }
        .Select(type => AccessTools.DeclaredMethod(type, nameof(AbstractIntent.GetIntentLabel)));

    private static void Postfix(AbstractIntent __instance, IEnumerable<Creature> __0, Creature __1, ref LocString __result)
    {
        var monster = __1;
        var local = LocalContext.GetMe(monster.CombatState)?.Creature;
        if (local == null) return;
        string? label = null;
        var charm = monster.GetPowerInstances<CharmSpellPower>().FirstOrDefault(p => p.Applier == local);
        if (monster.GetPower<SheepPower>() != null) label = new LocString("powers", "SHENSHEN-SHEEP_POWER.title").GetFormattedText();
        else if (monster.GetPower<StoppedTimePower>() is { WaitingForRoll: false }) label = "…";
        else if (__instance is AttackIntent intent && __0.Contains(local))
        {
            int hits = Math.Max(1, intent.Repeats);
            bool blind = monster.GetPowerInstances<SunBlindPower>().Any(p => p.Applier == local);
            bool dance = monster.GetPower<OttoDancePower>() != null;
            if (!blind && !dance && charm == null) return;
            int count = Math.Max(hits, charm?.Amount ?? 0);
            int total = hits * intent.GetSingleDamage(new[] { local }, monster);
            var amounts = Enumerable.Range(0, count).Select(i => blind || dance && i % 2 == 1
                ? 0 : total / count + (i < total % count ? 1 : 0)).ToArray();
            label = amounts.All(n => n == amounts[0]) && count > 1 ? $"{amounts[0]}×{count}" : string.Join("·", amounts);
        }
        else if (charm != null && monster.Monster?.IntendsToAttack == false) label = $"0×{charm.Amount}";
        if (label == null) return;
        __result = new LocString("cards", "SHENSHEN-RARE_INTENT.label");
        __result.Add("Text", label);
    }
}
