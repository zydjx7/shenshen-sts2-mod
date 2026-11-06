using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Shenshen.ShenshenCode.Character;
using Shenshen.ShenshenCode.Mechanics;
using Shenshen.ShenshenCode.Powers;

namespace Shenshen.ShenshenCode.Cards;

public sealed class TransformationMagic : ShenshenCard
{
    public TransformationMagic() : base(1, CardType.Skill, CardRarity.Basic, TargetType.Self) { }
    public override ShenshenForm? Form => null;
    public override bool CanBeGeneratedInCombat => false;
    // Canonical/library cards have no owner. Outside an active battle the run
    // starts in girl form, so its printed face always offers the cat form.
    public bool TransformsToCat => !IsMutable || !CombatManager.Instance.IsInProgress ||
        Owner?.Creature.CombatState == null || FormState.GetForm(Owner) != ShenshenForm.Cat;
    public override bool GainsBlock => IsUpgraded && !TransformsToCat;
    public const string CatPortraitPath = MainFile.ResPath + "/images/card_portraits/transformation_cat.tres";
    public const string GirlPortraitPath = MainFile.ResPath + "/images/card_portraits/transformation_girl.tres";
    public override string CustomPortraitPath => TransformsToCat ? CatPortraitPath : GirlPortraitPath;
    public override IEnumerable<string> AllPortraitPaths => [CatPortraitPath, GirlPortraitPath];
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain];
    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            yield return TransformsToCat ? HoverTipFactory.FromPower<StrengthPower>()
                : HoverTipFactory.FromPower<DexterityPower>();
            if (TransformsToCat && IsUpgraded) yield return HoverTipFactory.FromPower<ThornsPower>();
            yield return HoverTipFactory.FromPower<FormPower>();
        }
    }
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<StrengthPower>(1m),
        new PowerVar<DexterityPower>(1m),
        new PowerVar<ThornsPower>(0m),
        new BlockVar(0m, ValueProp.Unpowered)
    ];
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        FormState.Switch(choiceContext, Owner, IsUpgraded);

    protected override void AddExtraArgsToDescription(LocString description)
    {
        description.Add("ToCat", TransformsToCat);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["ThornsPower"].UpgradeValueBy(1m);
        DynamicVars.Block.UpgradeValueBy(4m);
    }

    // Retain the same combat card instance, including cost modifiers and any upgrade state.
    protected override CardLocation GetResultLocationForCardPlay() =>
        new(Owner, PileType.Hand, CardPilePosition.Bottom);
}
