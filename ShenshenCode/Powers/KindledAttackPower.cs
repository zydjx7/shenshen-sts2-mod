using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Shenshen.ShenshenCode.Cards;

namespace Shenshen.ShenshenCode.Powers;

public sealed class KindledAttackPower : CustomPowerModel
{
    private const int PileStride = 100000;
    private sealed class MovingTarget { internal CardModel? Card; }
    protected override object InitInternalData() => new MovingTarget();
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;
    public override int DisplayAmount => Bonus;
    public int Bonus => Amount % 10;
    public override LocString Description
    {
        get
        {
            var description = new LocString("powers", Id.Entry + ".description");
            description.Add("Bonus", Bonus);
            return description;
        }
    }
    public override string CustomPackedIconPath => $"{MainFile.ResPath}/images/powers/power.png";
    public override string CustomBigIconPath => $"{MainFile.ResPath}/images/powers/big/power.png";

    // Stable state is the native combat pile and exact slot, plus this application’s 2/3 bonus.
    // InternalData is only a bridge while a pile mutation temporarily detaches that original card.
    private static int Encode(int pile, int slot, int bonus) => ((pile + 1) * PileStride + slot) * 10 + bonus;

    public static async Task ApplyToCard(PlayerChoiceContext context, Player player, CardModel card, int bonus, CardModel source)
    {
        var piles = player.PlayerCombatState!.AllPiles;
        for (var pile = 0; pile < piles.Count; pile++)
        {
            var slot = piles[pile].Cards.ToList().IndexOf(card);
            if (slot >= 0)
            {
                await PowerCmd.Apply<KindledAttackPower>(context, player.Creature, Encode(pile, slot, bonus), player.Creature, source);
                return;
            }
        }
    }

    internal CardModel? ResolveTarget()
    {
        var moving = GetInternalData<MovingTarget>().Card;
        if (moving != null) return moving;
        var location = Amount / 10;
        var pile = location / PileStride - 1;
        var slot = location % PileStride;
        var piles = Owner.Player?.PlayerCombatState?.AllPiles;
        return piles != null && pile >= 0 && pile < piles.Count && slot < piles[pile].Cards.Count
            ? piles[pile].Cards[slot] : null;
    }

    internal void CaptureBeforePileChange()
    {
        var moving = GetInternalData<MovingTarget>();
        moving.Card ??= ResolveTarget();
    }

    internal void RefreshAfterPileChange()
    {
        var moving = GetInternalData<MovingTarget>();
        var card = moving.Card;
        var piles = Owner.Player?.PlayerCombatState?.AllPiles;
        if (card == null || piles == null) return;
        var bonus = Bonus;
        for (var pile = 0; pile < piles.Count; pile++)
        {
            var slot = piles[pile].Cards.ToList().IndexOf(card);
            if (slot < 0) continue;
            SetAmount(Encode(pile, slot, bonus));
            moving.Card = null;
            return;
        }
        // The selected instance may sit outside piles between native remove/add calls.
        SetAmount(bonus);
        if (card.Owner != Owner.Player) moving.Card = null;
    }

    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? play) =>
        dealer == Owner && cardSource != null && cardSource == ResolveTarget() &&
        (props.IsPoweredAttack() || cardSource is MoltenLightPierce && play != null) ? Bonus : 0m;

    public override async Task AfterSideTurnEnd(PlayerChoiceContext context, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side == CombatSide.Player && participants.Contains(Owner)) await PowerCmd.Remove(this);
    }
    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext context, ICombatState state)
    {
        if (player == Owner.Player) await PowerCmd.Remove(this);
    }
    public override Task AfterRemoved(Creature oldOwner)
    {
        GetInternalData<MovingTarget>().Card = null;
        return Task.CompletedTask;
    }
}
