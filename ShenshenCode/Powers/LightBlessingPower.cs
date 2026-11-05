using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Shenshen.ShenshenCode.Mechanics;

namespace Shenshen.ShenshenCode.Powers;

public sealed class LightBlessingPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override string CustomPackedIconPath => $"{MainFile.ResPath}/images/powers/power.png";
    public override string CustomBigIconPath => $"{MainFile.ResPath}/images/powers/big/power.png";

    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext,
        ICombatState combatState)
    {
        if (player != Owner.Player) return;

        // Prototype boundaries: shuffle only when the current draw cycle is empty,
        // take at most the available cards and hand capacity, then consume all layers.
        await CardPileCmd.ShuffleIfNecessary(choiceContext, player);
        var draw = PileType.Draw.GetPile(player);
        bool Available(MegaCrit.Sts2.Core.Models.CardModel card) =>
            player.Character is not Character.Shenshen || FormDeckPatches.IsAvailable(card, player);
        var count = Math.Min(Amount, Math.Min(draw.Cards.Count(Available),
            CardPile.MaxCardsInHand - PileType.Hand.GetPile(player).Cards.Count));
        if (count > 0)
        {
            var selected = await CardSelectCmd.FromCombatPile(choiceContext, draw, player,
                new CardSelectorPrefs(SelectionScreenPrompt, count), Available);
            // Moving selected instances is extra entry into hand, not a normal draw.
            await CardPileCmd.Add(selected, PileType.Hand);
        }
        await PowerCmd.Remove(this);
    }
}
