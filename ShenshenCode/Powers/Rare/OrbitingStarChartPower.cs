using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models;
using Shenshen.ShenshenCode.Character;
using Shenshen.ShenshenCode.Mechanics;

namespace Shenshen.ShenshenCode.Powers;

public sealed class OrbitingStarChartPower : RarePower
{
    private Dictionary<CardModel, int> payments = new();
    protected override void AfterCloned()
    {
        base.AfterCloned();
        payments = new Dictionary<CardModel, int>(payments);
    }
    public override Task AfterEnergySpent(CardModel card, int amount)
    {
        if (card.Owner.Creature == Owner && amount > 0) payments[card] = amount * Amount;
        return Task.CompletedTask;
    }
    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay play)
    {
        if (!play.IsLastInSeries || play.IsAutoPlay || !payments.Remove(play.Card, out int paid)) return;
        if (FormState.GetForm(Owner.Player!) == ShenshenForm.Cat)
            await PowerCmd.Apply<ThornsPower>(context, Owner, paid, Owner, null);
        else
            await PowerCmd.Apply<DexterityPower>(context, Owner, paid, Owner, null);
    }
}
