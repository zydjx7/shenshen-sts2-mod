using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Shenshen.ShenshenCode.Character;
using Shenshen.ShenshenCode.Powers;

namespace Shenshen.ShenshenCode.Cards;

public sealed class LichPhylactery : RareCard
{
    public LichPhylactery() : base(2, CardType.Skill, TargetType.Self, ShenshenForm.Human) { }
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Turns", 2m)];

    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        Gain<PhylacteryCraftPower>(context, DynamicVars["Turns"].BaseValue);

    protected override void OnUpgrade() => DynamicVars["Turns"].UpgradeValueBy(-1);
}
