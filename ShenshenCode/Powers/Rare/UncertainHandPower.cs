using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace Shenshen.ShenshenCode.Powers;

public sealed class UncertainHandPower : RarePower
{
    private HashSet<CardModel> piercing = new();
    protected override void AfterCloned()
    {
        base.AfterCloned();
        piercing = new HashSet<CardModel>(piercing);
    }
    internal bool Pierces(CardModel card) => piercing.Contains(card);
    internal void RollHand()
    {
        foreach (var card in Owner.Player!.PlayerCombatState!.Hand.Cards) Roll(card);
    }
    private void Roll(CardModel card)
    {
        if (card.Type == CardType.Attack && Owner.Player!.RunState.Rng.CombatCardSelection.NextInt(100) < Amount)
            piercing.Add(card);
    }
    public override Task AfterCardChangedPiles(CardModel card, PileType oldPileType, MegaCrit.Sts2.Core.Models.AbstractModel? clonedBy)
    {
        if (card.Owner.Creature != Owner) return Task.CompletedTask;
        if (card.Pile?.Type == PileType.Hand && oldPileType != PileType.Hand) { piercing.Remove(card); Roll(card); }
        else if (card.Pile?.Type != PileType.Play) piercing.Remove(card);
        return Task.CompletedTask;
    }
}
