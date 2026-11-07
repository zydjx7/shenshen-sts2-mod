using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using Shenshen.ShenshenCode.Cards;
using Shenshen.ShenshenCode.Mechanics;

namespace Shenshen.ShenshenCode.Powers;

public sealed class LightShadowTracePower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override string CustomPackedIconPath => $"{MainFile.ResPath}/images/powers/power.png";
    public override string CustomBigIconPath => $"{MainFile.ResPath}/images/powers/big/power.png";

    public override async Task BeforeFlush(PlayerChoiceContext context, Player player)
    {
        if (player != Owner.Player || !FormState.HasShiftedThisTurn(player) || Amount <= 0) return;
        var form = FormState.GetForm(player);
        bool Eligible(CardModel card) => !card.ShouldRetainThisTurn &&
            (card is not ShenshenCard owned || owned.Form == null || owned.Form == form);
        var available = player.PlayerCombatState!.Hand.Cards.Count(Eligible);
        if (available == 0) return;

        var limit = Math.Min(Amount, available);
        var prompt = new LocString("powers", Id.Entry + ".selectionScreenPrompt");
        prompt.Add("Cards", limit);
        var selected = await CardSelectCmd.FromHand(context, player,
            new CardSelectorPrefs(prompt, 0, limit), Eligible, this);
        foreach (var card in selected.Distinct().Take(limit))
            if (card.Pile?.Type == PileType.Hand && card.Owner == player && Eligible(card))
                CardCmd.ApplySingleTurnRetain(card);
    }
}
