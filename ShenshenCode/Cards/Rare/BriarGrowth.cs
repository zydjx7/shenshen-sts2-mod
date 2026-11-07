using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using Shenshen.ShenshenCode.Character;
using Shenshen.ShenshenCode.Powers;

namespace Shenshen.ShenshenCode.Cards;

public sealed class BriarGrowth : RareCard
{
    public BriarGrowth() : base(1, CardType.Power, TargetType.Self, ShenshenForm.Cat) { }
    public override string CustomPortraitPath =>
        $"{MainFile.ResPath}/images/card_portraits/rare/briar_growth.png";
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<ThornsPower>(2m)];

    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        Gain<BriarGrowthPower>(context, DynamicVars["ThornsPower"].BaseValue);

    protected override void OnUpgrade() => DynamicVars["ThornsPower"].UpgradeValueBy(1);
}
