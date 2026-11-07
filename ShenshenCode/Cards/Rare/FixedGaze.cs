using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Shenshen.ShenshenCode.Character;
using Shenshen.ShenshenCode.Mechanics;
using Shenshen.ShenshenCode.Powers;

namespace Shenshen.ShenshenCode.Cards;

public sealed class FixedGaze : RareCard
{
    public FixedGaze() : base(1, CardType.Skill, TargetType.AnyEnemy, ShenshenForm.Cat) { }
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(8m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await ShatteredMirrorPower.Grant(context, this, play);
        await RareCombat.Focus(context, this, play.Target!);
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(4);
}
