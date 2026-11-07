using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using Shenshen.ShenshenCode.Cards;

namespace Shenshen.ShenshenCode.Mechanics;

// Both the tooth and the tome consult this mapping. The native tooth transfers upgrades
// and enchantments, and the tome excludes tooth replacements from its own offerings.
[HarmonyPatch(typeof(ArchaicTooth), "TranscendenceUpgrades", MethodType.Getter)]
internal static class AncientCardRewards
{
    private static void Postfix(Dictionary<ModelId, CardModel> __result) =>
        __result[ModelDb.Card<LightMark>().Id] = ModelDb.Card<DualLightSigil>();
}
