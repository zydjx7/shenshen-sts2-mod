using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Shenshen.ShenshenCode.Character;
using Shenshen.ShenshenCode.Powers;

namespace Shenshen.ShenshenCode.Cards;

public sealed class FluffedUp : ShenshenCard
{
    // Additional copies never reset the combat-wide first reward.
    public FluffedUp() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }
    public override ShenshenForm? Form => ShenshenForm.Cat;
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<ThornsPower>()];
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<ThornsPower>(3m),
        new DynamicVar("LaterThorns", 0m),
        new DynamicVar("DebuffThorns", 0m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        var existing = Owner.Creature.GetPower<FluffedUpPower>();
        if (existing == null)
            await PowerCmd.Apply<FluffedUpPower>(choiceContext, Owner.Creature, IsUpgraded ? 3 : 1,
                Owner.Creature, this);
        else if (IsUpgraded)
            existing.EnableGrowth();
    }

    protected override void OnUpgrade()
    {
        DynamicVars["LaterThorns"].UpgradeValueBy(1m);
        DynamicVars["DebuffThorns"].UpgradeValueBy(1m);
    }
}
