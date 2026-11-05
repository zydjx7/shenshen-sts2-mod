using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Shenshen.ShenshenCode.Character;
using Shenshen.ShenshenCode.Mechanics;

namespace Shenshen.ShenshenCode.Cards;

public sealed class ClawStrike : ShenshenCard
{
    public ClawStrike() : base(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy) { }

    public override ShenshenForm? Form => ShenshenForm.Cat;
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(2m, ValueProp.Move), new ShiftRepeatVar()];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        var hits = FormState.HasShiftedThisTurn(Owner) ? 4 : DynamicVars.Repeat.IntValue;
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).WithHitCount(hits).FromCard(this, cardPlay)
            .Targeting(cardPlay.Target).Execute(choiceContext);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(1m);

    private sealed class ShiftRepeatVar() : RepeatVar(3)
    {
        public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode,
            Creature? target, bool runGlobalHooks)
        {
            PreviewValue = card.CombatState != null && FormState.HasShiftedThisTurn(card.Owner)
                ? 4m : BaseValue;
        }
    }
}
