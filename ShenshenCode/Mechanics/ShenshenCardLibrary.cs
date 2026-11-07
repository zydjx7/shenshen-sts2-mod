using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using MegaCrit.Sts2.Core.Saves;
using Shenshen.ShenshenCode.Cards;

namespace Shenshen.ShenshenCode.Mechanics;

/// <summary>Make Shenshen's encyclopedia useful before a player's first run.</summary>
[HarmonyPatch(typeof(NCardLibraryGrid), nameof(NCardLibraryGrid.RefreshVisibility))]
internal static class ShenshenCardLibrary
{
    private static void Prefix(out CardModel[] __state)
    {
        __state = ModelDb.AllCards.OfType<ShenshenCard>()
            .Where(card => card.ShouldShowInCardLibrary).Cast<CardModel>().ToArray();
        var progress = SaveManager.Instance.Progress;
        // Native search and the inspection screen also consult this set.
        foreach (var card in __state)
            progress.MarkCardAsSeen(card.Id);
    }

    private static void Postfix(CardModel[] __state, HashSet<CardModel> ____unlockedCards)
    {
        // This is the library grid's local visibility set, not reward eligibility.
        ____unlockedCards.UnionWith(__state);
    }
}
