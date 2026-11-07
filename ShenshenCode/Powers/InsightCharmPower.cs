using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Shenshen.ShenshenCode.Cards;
using Shenshen.ShenshenCode.Character;

namespace Shenshen.ShenshenCode.Powers;

public sealed class InsightCharmPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override string CustomPackedIconPath => $"{MainFile.ResPath}/images/powers/power.png";
    public override string CustomBigIconPath => $"{MainFile.ResPath}/images/powers/big/power.png";
    private bool Eligible(CardModel card) => card.Owner.Creature == Owner && card.Type == CardType.Skill &&
        card is ShenshenCard owned && owned.Form == ShenshenForm.Human;
    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = Eligible(card) ? Math.Max(0, originalCost - Amount) : originalCost;
        return modifiedCost != originalCost;
    }
    // Energy has already been spent when this hook runs. Removing here lets a new InsightCharm
    // played as the discounted skill establish its own separate next-skill benefit.
    public override async Task BeforeCardPlayed(CardPlay play)
    {
        if (Eligible(play.Card)) await PowerCmd.Remove(this);
    }
    public override async Task AfterSideTurnEnd(PlayerChoiceContext context, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side == CombatSide.Player && participants.Contains(Owner)) await PowerCmd.Remove(this);
    }
    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext context, ICombatState state)
    {
        if (player == Owner.Player) await PowerCmd.Remove(this);
    }
}
