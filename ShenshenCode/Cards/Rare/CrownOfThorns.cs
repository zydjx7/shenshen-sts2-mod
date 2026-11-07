using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using Shenshen.ShenshenCode.Character;
using Shenshen.ShenshenCode.Powers;

namespace Shenshen.ShenshenCode.Cards;

public sealed class CrownOfThorns : RareCard
{
    public CrownOfThorns() : base(2, CardType.Power, TargetType.Self, ShenshenForm.Human, CardRarity.Uncommon) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Ratio", 3m)];

    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        Gain<CrownOfThornsPower>(context, DynamicVars["Ratio"].BaseValue);

    protected override void OnUpgrade() => DynamicVars["Ratio"].UpgradeValueBy(-1);
}
