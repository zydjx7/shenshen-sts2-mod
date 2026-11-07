using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
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

public sealed class GuardTheBelly : ShenshenCard
{
    public GuardTheBelly() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    public override ShenshenForm? Form => ShenshenForm.Cat;
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(9m, ValueProp.Move), new DynamicVar("Recovery", 6m)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<GuardTheBellyPower>(DynamicVars["Recovery"].IntValue)];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play);
        await PowerCmd.Apply<GuardTheBellyPower>(context, Owner.Creature, DynamicVars["Recovery"].IntValue, Owner.Creature, this);
    }
    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);
        DynamicVars["Recovery"].UpgradeValueBy(2m);
    }
}
