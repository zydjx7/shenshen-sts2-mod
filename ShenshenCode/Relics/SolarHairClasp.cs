using BaseLib.Abstracts;
using BaseLib.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Shenshen.ShenshenCode.Cards;
using Shenshen.ShenshenCode.RelicPools;

namespace Shenshen.ShenshenCode.Relics;

[Pool(typeof(ShenshenRelicPool))]
public sealed class SolarHairClasp : CustomRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Starter;
    public override string PackedIconPath => MainFile.ResPath + "/images/relics/solar_hair_clasp.png";
    protected override string PackedIconOutlinePath => MainFile.ResPath + "/images/relics/solar_hair_clasp_outline.png";
    protected override string BigIconPath => MainFile.ResPath + "/images/relics/big/solar_hair_clasp.png";

    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext,
        ICombatState combatState)
    {
        var state = Owner.PlayerCombatState;
        if (player != Owner || state == null || state.TurnNumber > 1) return;
        // Use the owned card's combat instance, preserving upgrades and cost changes.
        var transformation = state.AllCards.OfType<TransformationMagic>().FirstOrDefault();
        if (transformation == null) return;
        Flash();
        await CardPileCmd.Add(transformation, PileType.Hand);
    }

    internal static bool Protects(CardModel card) =>
        card is TransformationMagic && card.IsMutable && card.Owner.GetRelic<SolarHairClasp>() != null;

    // The installed game has no veto hook before CardCmd moves discarded cards.
    [HarmonyPatch(typeof(CardCmd), nameof(CardCmd.DiscardAndDraw))]
    private static class ProtectFromDiscardPatch
    {
        private static void Prefix(ref IEnumerable<CardModel> cardsToDiscard) =>
            cardsToDiscard = cardsToDiscard.Where(card => !Protects(card)).ToArray();
    }

    [HarmonyPatch(typeof(CardSelectCmd), nameof(CardSelectCmd.FromHandForDiscard))]
    private static class ExcludeFromDiscardSelectionPatch
    {
        private static void Prefix(Player player, ref Func<CardModel, bool>? filter)
        {
            if (player.GetRelic<SolarHairClasp>() == null) return;
            var previous = filter;
            filter = card => !Protects(card) && (previous?.Invoke(card) ?? true);
        }
    }
}
