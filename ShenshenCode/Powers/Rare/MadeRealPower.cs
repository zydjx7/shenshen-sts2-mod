using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Shenshen.ShenshenCode.Powers;

public sealed class MadeRealPower : RarePower
{
    public override PowerStackType StackType => PowerStackType.Single;

    // The form cards grant Strength first and use BigStretchPower solely to take it back.
    public override bool TryModifyPowerAmountReceived(PowerModel power, Creature target, decimal amount,
        Creature? applier, out decimal modifiedAmount)
    {
        var keep = target == Owner && power is BigStretchPower && amount > 0;
        modifiedAmount = keep ? 0 : amount;
        return keep;
    }
}

// Native temporary Strength grants the real stat itself. Retire only the newly
// added expiry count after its own grant, preserving any earlier potion stacks.
[HarmonyPatch(typeof(TemporaryStrengthPower), nameof(TemporaryStrengthPower.AfterPowerAmountChanged))]
internal static class MadeRealNativeStrength
{
    private static void Postfix(TemporaryStrengthPower __instance, PowerModel power, decimal amount, ref Task __result)
    {
        if (power == __instance && amount > 0 && power.Type == PowerType.Buff && power.Owner.GetPower<MadeRealPower>() != null)
            __result = KeepNewStrength(__result, power, (int)amount);
    }

    private static async Task KeepNewStrength(Task nativeGrant, PowerModel power, int gained)
    {
        await nativeGrant;
        var expires = power.Amount - gained;
        if (expires > 0) power.SetAmount(expires);
        else await PowerCmd.Remove(power);
    }
}
