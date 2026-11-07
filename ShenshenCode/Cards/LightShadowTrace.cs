using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Shenshen.ShenshenCode.Character;
using Shenshen.ShenshenCode.Powers;

namespace Shenshen.ShenshenCode.Cards;

public sealed class LightShadowTrace : ShenshenCard
{
    public LightShadowTrace() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }

    public override ShenshenForm? Form => null;
    public override string CustomPortraitPath =>
        $"{MainFile.ResPath}/images/card_portraits/uncommon/light_shadow_trace.png";
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromKeyword(CardKeyword.Retain)];

    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        PowerCmd.Apply<LightShadowTracePower>(context, Owner.Creature,
            DynamicVars.Cards.BaseValue, Owner.Creature, this);

    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1m);
}
