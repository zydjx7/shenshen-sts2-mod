using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Shenshen.ShenshenCode.Character;

namespace Shenshen.ShenshenCode.Cards;

public sealed class AtomicFission : RareCard
{
    public AtomicFission() : base(3, CardType.Attack, TargetType.AnyEnemy, ShenshenForm.Human) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(24m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var strike = await Attack(context, play);
        int explosions = strike.Results.SelectMany(r => r).Count(r => r.WasTargetKilled);
        while (explosions-- > 0 && CombatState!.HittableEnemies.Any())
        {
            var hits = await CreatureCmd.Damage(context, CombatState.HittableEnemies.ToArray(), 12,
                ValueProp.Unpowered, Owner.Creature, this, null);
            explosions += hits.Count(r => r.WasTargetKilled);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(6);
}
