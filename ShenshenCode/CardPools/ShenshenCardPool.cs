using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Unlocks;

namespace Shenshen.ShenshenCode.CardPools;

public sealed class ShenshenCardPool : CustomCardPoolModel
{
    public override string Title => "Shenshen";
    public override bool IsColorless => false;
    public override Color DeckEntryCardColor => new("ffffff");
    public override Color ShaderColor => new("ffffff");
    public override string BigEnergyIconPath => $"{MainFile.ResPath}/images/charui/big_energy.png";
    public override string TextEnergyIconPath => $"{MainFile.ResPath}/images/charui/text_energy.png";

    protected override IEnumerable<CardModel> FilterThroughEpochs(UnlockState unlockState,
        IEnumerable<CardModel> cards) => base.FilterThroughEpochs(unlockState, cards)
        .Where(card => card.Rarity != CardRarity.Token);
}
