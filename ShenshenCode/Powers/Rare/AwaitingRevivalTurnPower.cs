using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Shenshen.ShenshenCode.Powers;

public sealed class AwaitingRevivalTurnPower : RarePower
{
    public override bool ShouldPlay(CardModel card, MegaCrit.Sts2.Core.Entities.Cards.AutoPlayType type) =>
        card.Owner.Creature != Owner;
    public override Task BeforeHandDraw(Player player, PlayerChoiceContext context, ICombatState state) =>
        player == Owner.Player ? PowerCmd.Remove(this) : Task.CompletedTask;
}
