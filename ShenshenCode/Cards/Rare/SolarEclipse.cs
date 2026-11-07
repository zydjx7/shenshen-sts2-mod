using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using Shenshen.ShenshenCode.Character;
using Shenshen.ShenshenCode.Powers;

namespace Shenshen.ShenshenCode.Cards;

public sealed class SolarEclipse : RareCard
{
    public SolarEclipse() : base(1, CardType.Skill, TargetType.AllEnemies, ShenshenForm.Human) { }
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain, CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Loss", 4m)];

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var loss = Math.Max(0, DynamicVars["Loss"].IntValue + (Owner.Creature.GetPower<DexterityPower>()?.Amount ?? 0));
        await PowerCmd.Apply<PiercingWailPower>(context, CombatState!.HittableEnemies, loss, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars["Loss"].UpgradeValueBy(2);
}
