using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Shenshen.ShenshenCode.Character;

namespace Shenshen.ShenshenCode.Cards;

public sealed class HuntByLight : RareCard
{
    public HuntByLight() : base(2, CardType.Attack, TargetType.AnyEnemy, ShenshenForm.Human) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(12m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await Attack(context, play);
        var draw = PileType.Draw.GetPile(Owner);
        bool Eligible(CardModel card) => card is ShenshenCard { Form: ShenshenForm.Cat, Type: CardType.Attack } && !card.EnergyCost.CostsX;
        if (!draw.Cards.Any(Eligible)) return;
        var picked = await CardSelectCmd.FromCombatPile(context, draw, Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 1), Eligible);
        foreach (var card in picked)
        {
            await CardPileCmd.Add(card, PileType.Hand);
            if (card.Pile?.Type == PileType.Hand) card.EnergyCost.SetThisTurn(0);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(4);
}
