using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using Shenshen.ShenshenCode.CardPools;
using Shenshen.ShenshenCode.Character;
using Shenshen.ShenshenCode.Mechanics;

namespace Shenshen.ShenshenCode.Cards;

/// <summary>Form ownership is separate from rarity and card pile membership.</summary>
[Pool(typeof(ShenshenCardPool))]
public abstract class ShenshenCard : CustomCardModel
{
    protected ShenshenCard(int cost, CardType type, CardRarity rarity, TargetType target)
        : base(cost, type, rarity, target)
    {
    }

    public abstract ShenshenForm? Form { get; }

    // Direct selection effects can put a card into hand without using normal Draw.
    protected override bool IsPlayable => Form is not { } required || CombatState == null ||
        Owner.Character is not Character.Shenshen || FormState.GetForm(Owner) == required;

    public override string CustomPortraitPath => $"{MainFile.ResPath}/images/card_portraits/card.png";
}
