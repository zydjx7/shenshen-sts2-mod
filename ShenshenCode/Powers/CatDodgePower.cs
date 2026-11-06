using System.Reflection;
using BaseLib.Abstracts;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using Shenshen.ShenshenCode.Character;
using Shenshen.ShenshenCode.Mechanics;

namespace Shenshen.ShenshenCode.Powers;

public sealed class CatDodgePower : CustomPowerModel
{
    private static readonly MethodInfo RemoveIcon = AccessTools.Method(typeof(NPowerContainer), "Remove", [typeof(PowerModel)]);
    private static readonly MethodInfo AddIcon = AccessTools.Method(typeof(NPowerContainer), "Add", [typeof(PowerModel)]);

    // Keep the ready/spent marker until the next own turn so repeated cat entry
    // cannot restore an already-used dodge. Amount is synchronized natively.
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override int DisplayAmount => 0;
    protected override bool IsVisibleInternal => Amount == 1 && Owner?.Player is { } player &&
        FormState.GetForm(player) == ShenshenForm.Cat;
    public override LocString Description => new("powers", SmartDescriptionLocKey);
    protected override string SmartDescriptionLocKey => Id.Entry + (Amount == 2 ? ".usedDescription" : ".description");
    public override string CustomPackedIconPath => $"{MainFile.ResPath}/images/powers/power.png";
    public override string CustomBigIconPath => $"{MainFile.ResPath}/images/powers/big/power.png";

    public static async Task GrantOnCatEntry(PlayerChoiceContext context, Player player)
    {
        if (player.Creature.GetPower<CatDodgePower>() == null)
            await PowerCmd.Apply<CatDodgePower>(context, player.Creature, 1, player.Creature, null);
    }

    public void RefreshVisibility()
    {
        var container = Owner.GetCreatureNode()?.GetNodeOrNull<NCreatureStateDisplay>("%HealthBar")
            ?.GetNodeOrNull<NPowerContainer>("%PowerContainer");
        if (container == null) return;

        // The native container only checks IsVisible when a power is added.
        // Its UI-only paths also reorder icons, without changing combat powers.
        RemoveIcon.Invoke(container, [this]);
        AddIcon.Invoke(container, [this]);
    }

    internal void ConsumeDodge()
    {
        SetAmount(2);
        Flash();
        RefreshVisibility();
    }

    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, ICombatState combatState)
    {
        if (player == Owner.Player) await PowerCmd.Remove(this);
    }
}
