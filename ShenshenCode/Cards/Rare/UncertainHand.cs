using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Shenshen.ShenshenCode.Character;
using Shenshen.ShenshenCode.Powers;

namespace Shenshen.ShenshenCode.Cards;

public sealed class UncertainHand : RareCard
{
    public UncertainHand() : base(1, CardType.Power, TargetType.Self, ShenshenForm.Human) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Chance", 60m)];

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var power = await PowerCmd.Apply<UncertainHandPower>(context, Owner.Creature, DynamicVars["Chance"].BaseValue, Owner.Creature, this);
        power?.RollHand();
    }

    protected override void OnUpgrade() => DynamicVars["Chance"].UpgradeValueBy(20);
}
