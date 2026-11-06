using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Shenshen.ShenshenCode.Character;
using Shenshen.ShenshenCode.Mechanics;

namespace Shenshen.ShenshenCode.Cards;

public sealed class BackwardGlance : ShenshenCard
{
    public BackwardGlance() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }
    public override ShenshenForm? Form => ShenshenForm.Cat;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(8m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this, cardPlay)
            .Targeting(cardPlay.Target).Execute(choiceContext);
        await FormCardRecovery.PutOnDrawTop(choiceContext, Owner, ShenshenForm.Human, SelectionScreenPrompt);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);
}
