using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Shenshen.ShenshenCode.Character;
using Shenshen.ShenshenCode.Mechanics;
using Shenshen.ShenshenCode.Powers;

namespace Shenshen.ShenshenCode.Cards;

public sealed class Kneading : ShenshenCard
{
    // Only the currently resolving invocation, never persisted between plays.
    private CardPlay? _resolvingPlay;
    public Kneading() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }
    public override ShenshenForm? Form => ShenshenForm.Cat;
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(3m, ValueProp.Move), new RepeatVar(3), new BlockVar(2m, ValueProp.Move)];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(play.Target);
        _resolvingPlay = play;
        try
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue).WithHitCount(DynamicVars.Repeat.IntValue)
                .FromCard(this, play).Targeting(play.Target).Execute(context);
        }
        finally { _resolvingPlay = null; }
    }
    public override async Task AfterDamageGiven(PlayerChoiceContext context, Creature? dealer, DamageResult result,
        ValueProp props, Creature target, CardModel? cardSource)
    {
        if (cardSource == this && dealer == Owner.Creature && props.IsPoweredAttack() && _resolvingPlay != null)
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, _resolvingPlay);
    }
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(1m);
}
