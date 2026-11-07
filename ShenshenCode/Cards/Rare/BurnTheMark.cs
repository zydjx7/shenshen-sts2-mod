using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Shenshen.ShenshenCode.Character;
using Shenshen.ShenshenCode.Powers;

namespace Shenshen.ShenshenCode.Cards;

public sealed class BurnTheMark : RareCard
{
    public BurnTheMark() : base(1, CardType.Attack, TargetType.AnyEnemy, ShenshenForm.Human) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(7m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var target = play.Target!;
        var mark = target.GetPowerInstances<LightMarkPower>().FirstOrDefault(m => m.Applier == Owner.Creature);
        var spent = Math.Min(3, mark?.RemainingTurns ?? 0);
        if (mark != null)
        {
            if (spent == mark.RemainingTurns) await PowerCmd.Remove(mark);
            else mark.SetAmount(mark.Amount - 10 * spent);
        }
        await Attack(context, play, 1 + spent);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2);
}
