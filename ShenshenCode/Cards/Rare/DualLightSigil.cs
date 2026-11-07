using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Shenshen.ShenshenCode.Character;
using Shenshen.ShenshenCode.Powers;

namespace Shenshen.ShenshenCode.Cards;

public sealed class DualLightSigil : RareCard
{
    public DualLightSigil() : base(1, CardType.Attack, TargetType.AllEnemies, ShenshenForm.Human, CardRarity.Ancient) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(8m, ValueProp.Move), new DynamicVar("Turns", 2m), new DynamicVar("Burn", 2m)];

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await Attack(context, play);
        foreach (var enemy in CombatState!.HittableEnemies.ToArray())
            await LightMarkPower.ApplyMark(context, enemy, Owner.Creature, DynamicVars["Burn"].IntValue, DynamicVars["Turns"].IntValue, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4);
        DynamicVars["Turns"].UpgradeValueBy(1);
        DynamicVars["Burn"].UpgradeValueBy(1);
    }
}
