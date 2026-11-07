using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Shenshen.ShenshenCode.Character;
using Shenshen.ShenshenCode.Powers;

namespace Shenshen.ShenshenCode.Cards;

public sealed class LightInGlass : RareCard
{
    public LightInGlass() : base(2, CardType.Power, TargetType.Self, ShenshenForm.Human) { }
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(6m, ValueProp.Move), new DynamicVar("Limit", 12m)];

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await ShatteredMirrorPower.Grant(context, this, play);
        await Gain<LightInGlassPower>(context, DynamicVars["Limit"].BaseValue);
    }

    protected override void OnUpgrade() => DynamicVars["Limit"].UpgradeValueBy(6);
}
