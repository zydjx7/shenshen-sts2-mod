using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Shenshen.ShenshenCode.Powers;

public sealed class FluffedUpPower : CustomPowerModel
{
    // Tracked Amount encodes upgrade state and the combat-wide first-reward flag:
    // 1/2 = basic pending/received, 3/4 = upgraded pending/received.
    // Old 1/2 states remain basic; no turn/form hook resets the flag.
    public bool HasReceivedFirstReward => Amount is 2 or 4;
    public bool IsGrowthEnabled => Amount is 3 or 4;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override int DisplayAmount => 0;
    public override string CustomPackedIconPath => $"{MainFile.ResPath}/images/powers/power.png";
    public override string CustomBigIconPath => $"{MainFile.ResPath}/images/powers/big/power.png";
    public override LocString Description => new("powers", SmartDescriptionLocKey);
    protected override string SmartDescriptionLocKey =>
        Id.Entry + (IsGrowthEnabled ? ".upgradedDescription" : ".description");

    public void EnableGrowth()
    {
        if (!IsGrowthEnabled) SetAmount(HasReceivedFirstReward ? 4 : 3);
    }

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target,
        DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner || result.UnblockedDamage <= 0 || !props.IsPoweredAttack() ||
            dealer?.Side != CombatSide.Enemy) return;

        if (HasReceivedFirstReward && !IsGrowthEnabled) return;

        var reward = HasReceivedFirstReward ? 1 : 3;
        SetAmount(IsGrowthEnabled ? 4 : 2);
        Flash();
        // Thorns' BeforeDamageReceived for this hit has already finished. The new
        // amount can retaliate only on subsequent attack segments.
        await PowerCmd.Apply<ThornsPower>(choiceContext, Owner, reward, Owner, null);
    }

    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext,
        PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (!IsGrowthEnabled || power.Owner != Owner || applier?.Side != CombatSide.Enemy || amount == 0 ||
            power.GetTypeForAmount(amount) != PowerType.Debuff) return;

        // This hook receives the actual post-immunity change once per power type,
        // not once per layer; it never consumes the first-HP-loss reward.
        Flash();
        await PowerCmd.Apply<ThornsPower>(choiceContext, Owner, 1, Owner, null);
    }
}
