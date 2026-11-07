using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Shenshen.ShenshenCode.Cards;
using Shenshen.ShenshenCode.Character;
using Shenshen.ShenshenCode.Powers;

namespace Shenshen.ShenshenCode.Mechanics;

internal static class RareCombat
{
    private sealed class History
    {
        internal int LastGirlTurn = -1;
        internal int LastShiftTurn = -1;
        internal readonly Dictionary<Creature, int> Retaliation = new();
    }

    private static readonly ConditionalWeakTable<PlayerCombatState, History> Histories = new();
    internal static int LastShift(Player player) => Histories.GetOrCreateValue(player.PlayerCombatState!).LastShiftTurn;
    internal static int GrudgeDamage(Creature owner, Creature target) =>
        Histories.GetOrCreateValue(owner.Player!.PlayerCombatState!).Retaliation.GetValueOrDefault(target);

    internal static async Task OnShift(PlayerChoiceContext context, Player player, bool first)
    {
        var state = player.PlayerCombatState!;
        var history = Histories.GetOrCreateValue(state);
        var turn = state.TurnNumber;
        var form = FormState.GetForm(player);
        history.LastShiftTurn = turn;
        foreach (var card in state.AllCards.OfType<Sunfall>())
            card.EnergyCost.AddUntilPlayed(-1, reduceOnly: true);
        if (form == ShenshenForm.Human && history.LastGirlTurn != turn)
        {
            history.LastGirlTurn = turn;
            foreach (var blade in state.DiscardPile.Cards.OfType<ReturningSunblade>().ToArray())
            {
                await CardPileCmd.Add(blade, PileType.Hand);
                if (blade.Pile?.Type == PileType.Hand) blade.EnergyCost.SetThisTurn(0);
            }
        }
        if (first && player.Creature.GetPower<DoubleChantPower>() is { } chant)
            await PowerCmd.Apply<NextDoubleAttackPower>(context, player.Creature, chant.Amount, player.Creature, null);
        if (form == ShenshenForm.Cat && player.Creature.GetPower<NextCatStrengthPower>() is { } cat)
        {
            await TemporaryStrength(context, player.Creature, cat.Amount, null);
            await PowerCmd.Remove(cat);
        }
        if (form == ShenshenForm.Human && player.Creature.GetPower<NextGirlMirrorPower>() is { } girl)
        {
            await FixedMirror(context, player.Creature, girl.Amount);
            await PowerCmd.Remove(girl);
        }
    }

    internal static Task TemporaryStrength(PlayerChoiceContext context, CardModel source, int amount) =>
        TemporaryStrength(context, source.Owner.Creature, amount, source);

    private static async Task TemporaryStrength(PlayerChoiceContext context, Creature owner, int amount, CardModel? source)
    {
        var before = owner.GetPower<StrengthPower>()?.Amount ?? 0;
        await PowerCmd.Apply<StrengthPower>(context, owner, amount, owner, source);
        var gained = (owner.GetPower<StrengthPower>()?.Amount ?? 0) - before;
        if (gained > 0) await PowerCmd.Apply<BigStretchPower>(context, owner, gained, owner, source);
    }

    internal static async Task FixedMirror(PlayerChoiceContext context, Creature owner, int amount)
    {
        var gained = await CreatureCmd.GainBlock(owner, amount, ValueProp.Unpowered, null);
        if (gained > 0) await PowerCmd.Apply<ShatteredMirrorPower>(context, owner, gained, owner, null);
    }

    internal static Creature CounterTarget(Creature owner, Creature attacker) =>
        owner.CombatState!.HittableEnemies.FirstOrDefault(enemy =>
            enemy.GetPowerInstances<FixedGazePower>().Any(p => p.Applier == owner)) ?? attacker;

    internal static async Task Focus(PlayerChoiceContext context, CardModel card, Creature target)
    {
        foreach (var enemy in card.CombatState!.Enemies)
            foreach (var power in enemy.GetPowerInstances<FixedGazePower>().Where(p => p.Applier == card.Owner.Creature).ToArray())
                await PowerCmd.Remove(power);
        await PowerCmd.Apply<FixedGazePower>(context, target, 1, card.Owner.Creature, card);
    }

    internal static async Task ThornHit(PlayerChoiceContext context, Creature owner, Creature target)
    {
        var thorns = owner.GetPower<ThornsPower>()?.Amount ?? 0;
        if (thorns <= 0 || !target.IsAlive) return;
        var multiplier = owner.GetPower<DandelionPuffPower>() == null ? 1 : 2;
        var hits = await CreatureCmd.Damage(context, target, thorns * multiplier,
            ValueProp.Unpowered | ValueProp.SkipHurtAnim, owner);
        await AfterRetaliation(context, owner, hits, thorns: true);
    }

    internal static async Task AfterRetaliation(PlayerChoiceContext context, Creature owner,
        IEnumerable<DamageResult> hits, bool thorns)
    {
        var results = hits.ToArray();
        if (!results.Any(r => r.TotalDamage > 0) || owner.Player?.PlayerCombatState is not { } state) return;
        var history = Histories.GetOrCreateValue(state);
        foreach (var hit in results)
            history.Retaliation[hit.Receiver] = history.Retaliation.GetValueOrDefault(hit.Receiver) + hit.UnblockedDamage;
        if (!thorns)
        {
            if (owner.GetPower<DawnReflectionPower>() is { } dawn)
                dawn.Record(results.Sum(r => r.TotalDamage));
            await BlueCardHooks.AfterReflection(context, owner);
            return;
        }
        await ThornyPurrPower.AfterThornsDamage(context, owner);
        if (owner.GetPower<TailtipMarkPower>() is { } tail)
            foreach (var hit in results.Where(r => r.TotalDamage > 0 && r.Receiver.IsAlive))
                await LightMarkPower.ApplyMark(context, hit.Receiver, owner, tail.Amount, 2, null);
        if (owner.GetPower<BriarGrowthPower>() is { } briar) await briar.Grow(context);
    }

    internal static async Task CheckBloom(PlayerChoiceContext context, Creature owner)
    {
        if (owner.GetPower<RoseInBloomPower>() is not { } bloom) return;
        int permanent = (owner.GetPower<ThornsPower>()?.Amount ?? 0) - (owner.GetPower<TemporaryThornsPower>()?.Amount ?? 0)
            - owner.GetPowerInstances<HeldThornsPower>().Sum(p => p.Amount);
        if (permanent < 8) return;
        var damage = bloom.Amount;
        await PowerCmd.Remove(bloom);
        await CreatureCmd.Damage(context, owner.CombatState!.HittableEnemies.ToArray(), damage, ValueProp.Unpowered, owner);
    }

    internal static async Task ExtendThorns(PlayerChoiceContext context, CardModel source, int rounds)
    {
        var owner = source.Owner.Creature;
        var expiry = source.Owner.PlayerCombatState!.TurnNumber + rounds;
        var remaining = 4;
        if (owner.GetPower<TemporaryThornsPower>() is { } temp)
        {
            var taken = Math.Min(remaining, temp.Amount);
            remaining -= taken;
            await PowerCmd.ModifyAmount(context, temp, -taken, owner, source);
        }
        foreach (var held in owner.GetPowerInstances<HeldThornsPower>().Where(p => p.Expiry < expiry).OrderBy(p => p.Expiry).ToArray())
        {
            if (remaining == 0) break;
            var taken = Math.Min(remaining, held.Amount);
            remaining -= taken;
            await PowerCmd.ModifyAmount(context, held, -taken, owner, source);
        }
        if (remaining < 4 && await PowerCmd.Apply<HeldThornsPower>(context, owner, 4 - remaining, owner, source) is { } kept)
            kept.Expiry = expiry;
    }
}
