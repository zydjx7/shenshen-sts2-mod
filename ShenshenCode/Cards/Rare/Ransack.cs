using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Shenshen.ShenshenCode.Character;

namespace Shenshen.ShenshenCode.Cards;

public sealed class Ransack : RareCard
{
    public Ransack() : base(1, CardType.Skill, TargetType.Self, ShenshenForm.Cat) { }
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var discard = PileType.Discard.GetPile(Owner);
        if (!discard.Cards.Any(card => card is ShenshenCard { Form: ShenshenForm.Cat })) return;
        var chosen = await CardSelectCmd.FromCombatPile(context, discard, Owner, new CardSelectorPrefs(SelectionScreenPrompt, 0, 3),
            card => card is ShenshenCard { Form: ShenshenForm.Cat });
        await CardPileCmd.Add(chosen, PileType.Hand);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
