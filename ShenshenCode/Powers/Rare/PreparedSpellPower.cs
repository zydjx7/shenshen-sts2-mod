using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Shenshen.ShenshenCode.Powers;

public sealed class PreparedSpellPower : RarePower
{
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;
    internal CardModel? Spell { get; private set; }
    internal static async Task Prepare(PlayerChoiceContext context, CardModel source, CardModel spell)
    {
        var power = await PowerCmd.Apply<PreparedSpellPower>(context, source.Owner.Creature, 1, source.Owner.Creature, source);
        if (power == null) return;
        power.Spell = spell;
        await CardPileCmd.Add(spell, PileType.Play);
    }
    internal async Task Release(PlayerChoiceContext context, Creature attacker)
    {
        var spell = Spell;
        await PowerCmd.Remove(this);
        if (spell?.Pile?.Type == PileType.Play)
            await CardCmd.AutoPlay(context, spell, spell.TargetType == TargetType.AnyEnemy ? attacker : null);
    }
}
