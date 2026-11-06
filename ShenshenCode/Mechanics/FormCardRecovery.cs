using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using Shenshen.ShenshenCode.Cards;
using Shenshen.ShenshenCode.Character;

namespace Shenshen.ShenshenCode.Mechanics;

public static class FormCardRecovery
{
    public static async Task PutOnDrawTop(PlayerChoiceContext context, Player player,
        ShenshenForm form, LocString prompt)
    {
        var discard = PileType.Discard.GetPile(player);
        bool Available(CardModel card) => card is ShenshenCard owned && owned.Form == form;
        if (!discard.Cards.Any(Available)) return;

        var selected = await CardSelectCmd.FromCombatPile(context, discard, player,
            new CardSelectorPrefs(prompt, 0, 1), Available);
        // Move the selected owned instance, including its upgrades and cost state.
        await CardPileCmd.Add(selected, PileType.Draw, CardPilePosition.Top);
    }
}
