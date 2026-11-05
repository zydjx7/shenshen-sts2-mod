using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Shenshen.ShenshenCode.Character;
using Shenshen.ShenshenCode.Mechanics;

namespace Shenshen.ShenshenCode.Cards;

public sealed class TransformationMagic : ShenshenCard
{
    public TransformationMagic() : base(1, CardType.Skill, CardRarity.Basic, TargetType.Self) { }
    public override ShenshenForm? Form => null;
    public override bool CanBeGeneratedInCombat => false;
    public override bool GainsBlock => IsUpgraded;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain];
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<StrengthPower>(),
        HoverTipFactory.FromPower<DexterityPower>(),
        HoverTipFactory.FromPower<ThornsPower>()
    ];
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<StrengthPower>(1m),
        new PowerVar<DexterityPower>(1m),
        new PowerVar<ThornsPower>(0m),
        new BlockVar(0m, ValueProp.Unpowered)
    ];
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        FormState.Switch(choiceContext, Owner, IsUpgraded);

    protected override void OnUpgrade()
    {
        DynamicVars["ThornsPower"].UpgradeValueBy(1m);
        DynamicVars.Block.UpgradeValueBy(4m);
    }

    // Retain the same combat card instance, including cost modifiers and any upgrade state.
    protected override CardLocation GetResultLocationForCardPlay() =>
        new(Owner, PileType.Hand, CardPilePosition.Bottom);
}
