using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Shenshen.ShenshenCode.Powers;

public sealed class FluffedUpPower : CustomPowerModel
{
    // Amount stores the combat-wide first-HP-loss flag, not a multiplier:
    // 1 = first reward available, 2 = already received. No turn/form hook resets it.
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override int DisplayAmount => 0;
    public override string CustomPackedIconPath => $"{MainFile.ResPath}/images/powers/power.png";
    public override string CustomBigIconPath => $"{MainFile.ResPath}/images/powers/big/power.png";

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target,
        DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner || result.UnblockedDamage <= 0 || !props.IsPoweredAttack() ||
            dealer?.Side != CombatSide.Enemy) return;

        var reward = Amount == 1 ? 3 : 1;
        SetAmount(2);
        Flash();
        // Thorns' BeforeDamageReceived for this hit has already finished. The new
        // amount can retaliate only on subsequent attack segments.
        await PowerCmd.Apply<ThornsPower>(choiceContext, Owner, reward, Owner, null);
    }

    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext,
        PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (power.Owner != Owner || applier?.Side != CombatSide.Enemy || amount == 0 ||
            power.GetTypeForAmount(amount) != PowerType.Debuff) return;

        // This hook receives the actual post-immunity change once per power type,
        // not once per layer; it never consumes the first-HP-loss reward.
        Flash();
        await PowerCmd.Apply<ThornsPower>(choiceContext, Owner, 1, Owner, null);
    }
}
