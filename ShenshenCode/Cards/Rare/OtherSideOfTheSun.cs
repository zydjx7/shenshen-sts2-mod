using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Shenshen.ShenshenCode.Character;
using Shenshen.ShenshenCode.Mechanics;
using Shenshen.ShenshenCode.Powers;

namespace Shenshen.ShenshenCode.Cards;

public sealed class OtherSideOfTheSun : RareCard
{
    public OtherSideOfTheSun() : base(1, CardType.Skill, TargetType.Self, null) { }
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain, CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(10m, ValueProp.Move), new DynamicVar("StrengthGain", 3m)];
    internal bool GirlFace => !IsMutable || CombatState == null || FormState.GetForm(Owner) == ShenshenForm.Human;
    protected override void AddExtraArgsToDescription(LocString description) => description.Add("Girl", GirlFace);
    public override bool GainsBlock => GirlFace;
    private const string GirlPortraitPath = MainFile.ResPath + "/images/card_portraits/rare/other_side_sun_girl.png";
    private const string CatPortraitPath = MainFile.ResPath + "/images/card_portraits/rare/other_side_sun_cat.png";
    public override string CustomPortraitPath => GirlFace ? GirlPortraitPath : CatPortraitPath;
    public override IEnumerable<string> AllPortraitPaths => [GirlPortraitPath, CatPortraitPath];

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        if (GirlFace)
        {
            await ShatteredMirrorPower.Grant(context, this, play);
            await Gain<NextCatStrengthPower>(context, DynamicVars["StrengthGain"].BaseValue);
        }
        else
        {
            await RareCombat.TemporaryStrength(context, this, DynamicVars["StrengthGain"].IntValue);
            await Gain<NextGirlMirrorPower>(context, DynamicVars.Block.IntValue);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(2);
        DynamicVars["StrengthGain"].UpgradeValueBy(1);
    }
}
