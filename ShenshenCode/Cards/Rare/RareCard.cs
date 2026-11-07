using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Shenshen.ShenshenCode.Character;

namespace Shenshen.ShenshenCode.Cards;

public abstract class RareCard(int cost, CardType type, TargetType target, ShenshenForm? form,
    CardRarity rarity = CardRarity.Rare) : ShenshenCard(cost, type, rarity, target)
{
    public override ShenshenForm? Form => form;

    protected async Task<AttackCommand> Attack(PlayerChoiceContext context, CardPlay play,
        int hits = 1, decimal? damage = null)
    {
        var attack = DamageCmd.Attack(damage ?? DynamicVars.Damage.BaseValue)
            .WithHitCount(hits).FromCard(this, play);
        if (TargetType == TargetType.AllEnemies)
            attack.TargetingAllOpponents(CombatState!);
        else
            attack.Targeting(play.Target ?? throw new InvalidOperationException("Attack needs a target."));
        return await attack.Execute(context);
    }

    protected Task Gain<T>(PlayerChoiceContext context, decimal amount) where T : PowerModel =>
        PowerCmd.Apply<T>(context, Owner.Creature, amount, Owner.Creature, this);
}
