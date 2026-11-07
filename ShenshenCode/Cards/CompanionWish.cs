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
using Shenshen.ShenshenCode.Powers;

namespace Shenshen.ShenshenCode.Cards;

public sealed class CompanionWish : ShenshenCard
{
    public CompanionWish() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    public override ShenshenForm? Form => ShenshenForm.Human;
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(7m, ValueProp.Move)];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play);
        var discard = PileType.Discard.GetPile(Owner);
        bool Eligible(CardModel card) => card is ShenshenCard owned && owned.Form == ShenshenForm.Cat;
        if (!discard.Cards.Any(Eligible)) return;
        var selected = await CardSelectCmd.FromCombatPile(context, discard, Owner, new CardSelectorPrefs(SelectionScreenPrompt, 1), Eligible);
        await CardPileCmd.Add(selected, PileType.Hand);
    }
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3m);
}
