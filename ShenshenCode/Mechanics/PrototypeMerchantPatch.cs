using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;

namespace Shenshen.ShenshenCode.Mechanics;

// The small prototype has one reward attack and one single-player reward skill.
// Permit a repeated shop card only when vanilla's duplicate exclusion empties a slot.
// This is a temporary content boundary, not a change to other characters' shops.
[HarmonyPatch(typeof(CardFactory), nameof(CardFactory.CreateForMerchant),
    [typeof(Player), typeof(IEnumerable<CardModel>), typeof(CardType)])]
internal static class PrototypeMerchantPatch
{
    private static void Prefix(Player player, ref IEnumerable<CardModel> options, CardType type)
    {
        if (player.Character is not Character.Shenshen) return;
        bool multiplayer = player.RunState.Players.Count > 1;
        bool Eligible(CardModel card) => card.Type == type &&
            card.Rarity is CardRarity.Common or CardRarity.Uncommon or CardRarity.Rare &&
            card.MultiplayerConstraint != (multiplayer
                ? CardMultiplayerConstraint.SingleplayerOnly : CardMultiplayerConstraint.MultiplayerOnly);
        var supplied = options.Where(card => card.Rarity != CardRarity.Token).ToArray();
        options = supplied.Any(Eligible) ? supplied : supplied.Concat(
            player.Character.CardPool.AllCards.Where(Eligible)).Distinct();
    }
}
