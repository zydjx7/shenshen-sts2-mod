using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Shenshen.ShenshenCode.Character;
using Shenshen.ShenshenCode.Powers;

namespace Shenshen.ShenshenCode.Cards;

public sealed class LightBlessing : ShenshenCard
{
    public LightBlessing() : base(1, CardType.Skill, CardRarity.Common, TargetType.AnyAlly) { }

    public override ShenshenForm? Form => ShenshenForm.Human;
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<LightBlessingPower>()];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // Keep the effect-side check even though the target UI normally excludes self.
        if (cardPlay.Target?.Player is not { } teammate || teammate == Owner) return;
        await PowerCmd.Apply<LightBlessingPower>(choiceContext, teammate.Creature,
            DynamicVars.Cards.IntValue, Owner.Creature, this);
    }

    // Prototype upgrade: grant two selection layers instead of one; not confirmed design.
    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1m);
}
