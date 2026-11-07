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

public sealed class RetrieveTheStaffPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override string CustomPackedIconPath => $"{MainFile.ResPath}/images/powers/power.png";
    public override string CustomBigIconPath => $"{MainFile.ResPath}/images/powers/big/power.png";
    public static async Task AfterShift(PlayerChoiceContext context, Player player)
    {
        var power = player.Creature.GetPower<RetrieveTheStaffPower>();
        if (power == null || FormState.GetForm(player) != ShenshenForm.Human) return;
        var maximum = power.Amount;
        var prompt = power.SelectionScreenPrompt;
        power.Flash();
        await PowerCmd.Remove(power);
        var discard = PileType.Discard.GetPile(player);
        bool Available(CardModel card) => card is ShenshenCard owned && owned.Form == ShenshenForm.Human && card.Type == CardType.Skill;
        if (!discard.Cards.Any(Available)) return;
        var selected = await CardSelectCmd.FromCombatPile(context, discard, player, new CardSelectorPrefs(prompt, 0, maximum), Available);
        await CardPileCmd.Add(selected, PileType.Hand);
    }
}
