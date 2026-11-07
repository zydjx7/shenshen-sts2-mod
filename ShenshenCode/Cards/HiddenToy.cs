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

public sealed class HiddenToy : ShenshenCard
{
    public HiddenToy() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    public override ShenshenForm? Form => ShenshenForm.Cat;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(1)];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        var selected = (await CardSelectCmd.FromHand(context, Owner, new CardSelectorPrefs(SelectionScreenPrompt, 1),
            card => card != this && card is ShenshenCard owned && owned.Form == ShenshenForm.Cat, this)).FirstOrDefault();
        if (selected == null) return;
        var result = await CardPileCmd.Add(selected, PileType.Draw, CardPilePosition.Bottom);
        if (result.success && selected.Pile?.Type == PileType.Draw)
            await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
    }
    protected override void OnUpgrade() => DynamicVars.Energy.UpgradeValueBy(1m);
}
