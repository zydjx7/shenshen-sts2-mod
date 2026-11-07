using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Shenshen.ShenshenCode.Character;
using Shenshen.ShenshenCode.Powers;

namespace Shenshen.ShenshenCode.Cards;

public sealed class DawnReflection : RareCard
{
    public DawnReflection() : base(2, CardType.Skill, TargetType.Self, ShenshenForm.Human) { }
    public override bool GainsBlock => true;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(12m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await ShatteredMirrorPower.Grant(context, this, play);
        await Gain<DawnReflectionPower>(context, 1);
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(4);
}
