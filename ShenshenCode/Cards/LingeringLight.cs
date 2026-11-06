using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Shenshen.ShenshenCode.Character;
using Shenshen.ShenshenCode.Mechanics;

namespace Shenshen.ShenshenCode.Cards;

public sealed class LingeringLight : ShenshenCard
{
    public LingeringLight() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self) { }
    public override ShenshenForm? Form => ShenshenForm.Human;
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(7m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        await FormCardRecovery.PutOnDrawTop(choiceContext, Owner, ShenshenForm.Cat, SelectionScreenPrompt);
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3m);
}
