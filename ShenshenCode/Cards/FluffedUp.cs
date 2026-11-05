using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Shenshen.ShenshenCode.Character;
using Shenshen.ShenshenCode.Powers;

namespace Shenshen.ShenshenCode.Cards;

public sealed class FluffedUp : ShenshenCard
{
    // Common rarity, non-stacking copies, and an unchanged upgrade are prototype
    // choices. Replaying a copy must not reset this combat's first-HP-loss flag.
    public FluffedUp() : base(1, CardType.Power, CardRarity.Common, TargetType.Self) { }
    public override ShenshenForm? Form => ShenshenForm.Cat;
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<ThornsPower>()];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        if (Owner.Creature.GetPower<FluffedUpPower>() == null)
            await PowerCmd.Apply<FluffedUpPower>(choiceContext, Owner.Creature, 1, Owner.Creature, this);
    }
}
