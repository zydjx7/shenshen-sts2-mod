using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using Shenshen.ShenshenCode.Character;
using Shenshen.ShenshenCode.Powers;

namespace Shenshen.ShenshenCode.Cards;

public sealed class TailtipMark : RareCard
{
    public TailtipMark() : base(2, CardType.Power, TargetType.Self, ShenshenForm.Cat) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Burn", 1m)];

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await Gain<ThornsPower>(context, 2);
        await Gain<TailtipMarkPower>(context, DynamicVars["Burn"].BaseValue);
    }

    protected override void OnUpgrade() => DynamicVars["Burn"].UpgradeValueBy(1);
}
