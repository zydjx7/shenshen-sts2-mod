using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Shenshen.ShenshenCode.Character;
using Shenshen.ShenshenCode.Mechanics;
using Shenshen.ShenshenCode.Powers;

namespace Shenshen.ShenshenCode.Cards;

public sealed class RoseInBloom : RareCard
{
    public RoseInBloom() : base(1, CardType.Power, TargetType.Self, ShenshenForm.Human) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Burst", 24m)];

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await Gain<RoseInBloomPower>(context, DynamicVars["Burst"].BaseValue);
        await RareCombat.CheckBloom(context, Owner.Creature);
    }

    protected override void OnUpgrade() => DynamicVars["Burst"].UpgradeValueBy(8);
}
