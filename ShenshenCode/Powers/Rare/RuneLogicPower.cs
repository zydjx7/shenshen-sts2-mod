using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using Shenshen.ShenshenCode.Cards;

namespace Shenshen.ShenshenCode.Powers;

public sealed class RuneLogicPower : RarePower
{
    public override bool TryModifyEnergyCostInCombatLate(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        if (card is LittleFireball && card.Owner.Creature == Owner) { modifiedCost = 0; return true; }
        modifiedCost = originalCost;
        return false;
    }
}
