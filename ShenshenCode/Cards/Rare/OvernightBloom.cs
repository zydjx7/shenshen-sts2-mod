using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using Shenshen.ShenshenCode.Character;
using Shenshen.ShenshenCode.Mechanics;
using Shenshen.ShenshenCode.Powers;

namespace Shenshen.ShenshenCode.Cards;

public sealed class OvernightBloom : RareCard
{
    public OvernightBloom() : base(2, CardType.Skill, TargetType.Self, ShenshenForm.Human) { }
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Thorns", 3m)];

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await Gain<ThornsPower>(context, DynamicVars["Thorns"].BaseValue);
        for (int i = 0; i < 2; i++)
            foreach (var enemy in CombatState!.HittableEnemies.ToArray())
                await RareCombat.ThornHit(context, Owner.Creature, enemy);
    }

    protected override void OnUpgrade() => DynamicVars["Thorns"].UpgradeValueBy(1);
}
