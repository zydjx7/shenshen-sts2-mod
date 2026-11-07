using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Shenshen.ShenshenCode.Character;
using Shenshen.ShenshenCode.Mechanics;
using Shenshen.ShenshenCode.Powers;

namespace Shenshen.ShenshenCode.Cards;

public sealed class CatchTheLight : ShenshenCard
{
    public CatchTheLight() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    public override ShenshenForm? Form => ShenshenForm.Cat;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Reduction", 1m)];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        var draw = PileType.Draw.GetPile(Owner);
        bool Available(CardModel card) => card is ShenshenCard owned && owned.Form == ShenshenForm.Cat && card.Type == CardType.Attack;
        if (!draw.Cards.Any(Available)) return;
        var selected = (await CardSelectCmd.FromCombatPile(context, draw, Owner, new CardSelectorPrefs(SelectionScreenPrompt, 1), Available)).FirstOrDefault();
        if (selected == null) return;
        var result = await CardPileCmd.Add(selected, PileType.Hand);
        if (result.success && selected.Pile?.Type == PileType.Hand)
            selected.EnergyCost.AddThisTurn(-DynamicVars["Reduction"].IntValue, reduceOnly: true);
    }
    protected override void OnUpgrade() => DynamicVars["Reduction"].UpgradeValueBy(1m);
}
