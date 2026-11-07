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

public sealed class CleansingFlame : ShenshenCard
{
    public CleansingFlame() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    public override ShenshenForm? Form => ShenshenForm.Human;
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(8m, ValueProp.Move)];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        bool Eligible(CardModel card) => card.Type == CardType.Attack;
        if (!PileType.Hand.GetPile(Owner).Cards.Any(Eligible)) return;
        var selected = await CardSelectCmd.FromHand(context, Owner, new CardSelectorPrefs(SelectionScreenPrompt, 1), Eligible, this);
        foreach (var card in selected)
        {
            var moved = await CardCmd.Exhaust(context, card);
            if (moved != null)
                await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play);
        }
    }
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(4m);
}
