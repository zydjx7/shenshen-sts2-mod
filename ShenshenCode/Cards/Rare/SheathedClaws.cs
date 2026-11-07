using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using Shenshen.ShenshenCode.Character;
using Shenshen.ShenshenCode.Mechanics;
using Shenshen.ShenshenCode.Powers;

namespace Shenshen.ShenshenCode.Cards;

public sealed class SheathedClaws : RareCard
{
    public SheathedClaws() : base(1, CardType.Skill, TargetType.Self, ShenshenForm.Cat) { }
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Rounds", 2m)];

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await TemporaryThornsPower.Grant(context, Owner.Creature, 2, this);
        await RareCombat.ExtendThorns(context, this, DynamicVars["Rounds"].IntValue);
    }

    protected override void OnUpgrade() => DynamicVars["Rounds"].UpgradeValueBy(1);
}
