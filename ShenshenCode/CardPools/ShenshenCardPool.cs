using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Unlocks;
using Shenshen.ShenshenCode.Cards;

namespace Shenshen.ShenshenCode.CardPools;

public sealed class ShenshenCardPool : CustomCardPoolModel
{
    public override string Title => "Shenshen";
    public override bool IsColorless => false;
    public override Color DeckEntryCardColor => new("eab0b2");
    public override Color ShaderColor => new("eab0b2");
    public override string BigEnergyIconPath => $"{MainFile.ResPath}/images/charui/big_energy.png";
    public override string TextEnergyIconPath => $"{MainFile.ResPath}/images/charui/text_energy.png";

    protected override IEnumerable<CardModel> FilterThroughEpochs(UnlockState unlockState,
        IEnumerable<CardModel> cards) => base.FilterThroughEpochs(unlockState, cards)
        // Retain model IDs and behavior for old saves; exclude retired cards from
        // the unlocked pool used by rewards, merchants and card generation.
        .Where(card => card.Rarity != CardRarity.Token && card is not
            (LightShadowHandoff or ShatteredMirrorFlames or MoltenLightPierce or Kneading or Teasing or BurnTheMark));
}
