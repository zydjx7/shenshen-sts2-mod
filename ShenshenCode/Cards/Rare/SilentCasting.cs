using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Shenshen.ShenshenCode.Character;
using Shenshen.ShenshenCode.Powers;

namespace Shenshen.ShenshenCode.Cards;

public sealed class SilentCasting : RareCard
{
    public SilentCasting() : base(1, CardType.Skill, TargetType.Self, ShenshenForm.Human) { }
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        bool Eligible(MegaCrit.Sts2.Core.Models.CardModel card) => card != this && card is ShenshenCard { Form: ShenshenForm.Human }
            && !card.EnergyCost.CostsX && card.EnergyCost.Canonical <= 2
            && (card.Type == CardType.Attack || card.Type == CardType.Skill && card.GainsBlock);
        if (!PileType.Hand.GetPile(Owner).Cards.Any(Eligible)) return;
        var selected = (await CardSelectCmd.FromHand(context, Owner, new CardSelectorPrefs(SelectionScreenPrompt, 1), Eligible, this)).FirstOrDefault();
        if (selected != null) await PreparedSpellPower.Prepare(context, this, selected);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
