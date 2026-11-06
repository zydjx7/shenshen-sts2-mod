using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Shenshen.ShenshenCode.Character;
using Shenshen.ShenshenCode.Powers;

namespace Shenshen.ShenshenCode.Cards;

public sealed class ShatteredMirror : ShenshenCard
{
    public ShatteredMirror() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self) { }
    public override ShenshenForm? Form => ShenshenForm.Human;
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(6m, ValueProp.Move)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<ShatteredMirrorPower>()];

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        ShatteredMirrorPower.Grant(choiceContext, this, cardPlay);

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3m);
}
