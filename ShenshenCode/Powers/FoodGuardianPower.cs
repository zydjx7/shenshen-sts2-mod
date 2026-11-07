using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Shenshen.ShenshenCode.Cards;
using Shenshen.ShenshenCode.Character;
using Shenshen.ShenshenCode.Mechanics;

namespace Shenshen.ShenshenCode.Powers;

public sealed class FoodGuardianPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override string CustomPackedIconPath => $"{MainFile.ResPath}/images/powers/power.png";
    public override string CustomBigIconPath => $"{MainFile.ResPath}/images/powers/big/power.png";
    public override int DisplayAmount => Amount / 2;
    public override LocString Description
    {
        get
        {
            var text = new LocString("powers", Id.Entry + ".description");
            text.Add("Thorns", DisplayAmount);
            return text;
        }
    }
    public override async Task AfterBlockGained(Creature creature, decimal amount, ValueProp props, CardModel? cardSource)
    {
        if (creature != Owner || amount <= 0 || (Amount & 1) != 0) return;
        SetAmount(Amount | 1);
        Flash();
        var context = new ThrowingPlayerChoiceContext();
        var before = Owner.GetPower<ThornsPower>()?.Amount ?? 0;
        await PowerCmd.Apply<ThornsPower>(context, Owner, DisplayAmount, Owner, null);
        var gained = (Owner.GetPower<ThornsPower>()?.Amount ?? 0) - before;
        if (gained > 0) await PowerCmd.Apply<TemporaryThornsPower>(context, Owner, gained, Owner, null);
    }
    public override Task BeforeSideTurnStart(PlayerChoiceContext context, CombatSide side,
        IReadOnlyList<Creature> participants, ICombatState state)
    {
        if (side == CombatSide.Player && participants.Contains(Owner)) SetAmount(Amount & ~1);
        return Task.CompletedTask;
    }
}
