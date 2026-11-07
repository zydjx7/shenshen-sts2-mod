using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.Unlocks;
using MegaCrit.Sts2.Core.ValueProps;
using Shenshen.ShenshenCode.CardPools;
using Shenshen.ShenshenCode.Cards;
using Shenshen.ShenshenCode.Character;
using Shenshen.ShenshenCode.Powers;

public partial class Harness
{
    private async Task RunRareCardChecks()
    {
        var pool = ModelDb.CardPool<ShenshenCardPool>();
        var solo = pool.GetUnlockedCards(UnlockState.all, CardMultiplayerConstraint.SingleplayerOnly).OfType<RareCard>().ToArray();
        var all = pool.GetUnlockedCards(UnlockState.all, CardMultiplayerConstraint.MultiplayerOnly).OfType<RareCard>().ToArray();
        Check(solo.Count(c => c.Rarity == CardRarity.Rare) == 39 && !solo.OfType<Resurrection>().Any() &&
            all.Length == 42 && all.Count(c => c.Rarity == CardRarity.Ancient) == 2, "39 solo rares, one co-op rare and two Ancients in native pools");
        var f = await CreateCommonFixture();
        CheckCommonCardDescriptions(f, all);
        foreach (var canonical in all)
        {
            var card = f.Combat.CreateCard(canonical, f.Player);
            Check(card.IsUpgradable && card.Portrait != null, canonical.Id.Entry + " has an upgrade and a usable placeholder portrait");
        }
        var tooth = (ArchaicTooth)ModelDb.Relic<ArchaicTooth>().ToMutable();
        var original = f.Player.Deck.Cards.OfType<LightMark>().Single();
        CardCmd.Upgrade(original);
        Check(tooth.SetupForPlayer(f.Player) && tooth.AncientCard?.Id == ModelDb.Card<DualLightSigil>().Id &&
            CardModel.FromSerializable(tooth.AncientCard).IsUpgraded, "the native tooth picks the upgraded owned Light Mark");
        var tome = (DustyTome)ModelDb.Relic<DustyTome>().ToMutable();
        tome.SetupForPlayer(f.Player);
        Check(tome.AncientCard == ModelDb.Card<OrbitingStarChart>().Id, "the tome excludes the tooth replacement and offers Star Chart");

        await TestRareResources();
        await TestRareThorns();
        await TestRareDefense();
        await TestRareEnemyActions();
        await TestRareCardInteractions();
        await TestRareControlTurns();
        await TestRareResurrection();
        await TestRareCardPlays(all);
    }

    private static async Task<T> PlayRare<T>(CommonFixture f, Creature? target = null, bool upgraded = false) where T : CardModel
    {
        var card = CommonCard<T>(f, upgraded);
        await CardCmd.AutoPlay(f.Context, card, target, skipCardPileVisuals: true);
        return card;
    }

    private async Task TestRareResources()
    {
        var f = await CreateCommonFixture();
        var owner = f.Player.Creature;
        var state = f.Player.PlayerCombatState!;
        var chart = CommonCard<OrbitingStarChart>(f);
        state.Energy = 6;
        await CardPileCmd.AddGeneratedCardToCombat(chart, PileType.Hand, f.Player);
        await PlayPaid(chart, f.Context);
        Check(owner.GetPower<DexterityPower>() == null, "Star Chart does not pay itself");
        var shift = state.AllCards.OfType<TransformationMagic>().Single();
        await CardPileCmd.Add(shift, PileType.Hand);
        await PlayPaid(shift, f.Context);
        Check(owner.GetPower<ThornsPower>()?.Amount == 1, "paid transformation grants Thorns in the new cat form");
        var pounce = CommonCard<SwiftPounce>(f);
        await CardPileCmd.AddGeneratedCardToCombat(pounce, PileType.Hand, f.Player);
        int hp = f.Enemy.CurrentHp;
        var (energy, stars) = await pounce.SpendResources();
        await pounce.OnPlayWrapper(f.Context, f.Enemy, false, new ResourceInfo { EnergySpent = energy, EnergyValue = energy, StarsSpent = stars, StarValue = stars }, true);
        Check(energy == 3 && hp - f.Enemy.CurrentHp == 21 && owner.GetPower<ThornsPower>()?.Amount == 4 && state.Energy == 0,
            "X costs pay once, scale each hit with Strength and grant exactly the spent energy in Thorns");
        await PlayRare<SwiftPounce>(f, f.Enemy);
        Check(owner.GetPower<ThornsPower>()?.Amount == 4, "free autoplay grants no Star Chart resources");
        await PlayRare<RuneLogic>(f, upgraded: true);
        var fire = state.AllCards.OfType<LittleFireball>().First();
        var shield = state.AllCards.OfType<LightShield>().First();
        Check(fire.EnergyCost.GetWithModifiers(CostModifiers.All) == 0 && shield.EnergyCost.GetWithModifiers(CostModifiers.All) == 1, "Rune Logic only discounts Little Fireball");
    }

    private async Task TestRareThorns()
    {
        var f = await CreateCommonFixture();
        var owner = f.Player.Creature;
        await PlayRare<MadeReal>(f, upgraded: true);
        await PlayRare<BriarGrowth>(f);
        await Switch(f.Context, f.Player);
        Check(owner.GetPower<TemporaryThornsPower>()?.Amount == 3, "first shift creates three temporary Thorns");
        await CreatureCmd.Damage(f.Context, owner, 0, ValueProp.Move, f.Enemy);
        Check(owner.GetPower<ThornsPower>()?.Amount == 4 && owner.GetPower<TemporaryThornsPower>()?.Amount == 2,
            "a true zero-damage hit grows one temporary Thorn and makes two permanent");
        int before = f.Enemy.CurrentHp;
        await PlayRare<Grudge>(f, f.Enemy);
        Check(before - f.Enemy.CurrentHp == 16, "Grudge remembers actual thorn HP loss on this target");
    }

    private async Task TestRareDefense()
    {
        var f = await CreateCommonFixture();
        var owner = f.Player.Creature;
        await PlayRare<LichPhylactery>(f, upgraded: true);
        Check(Hook.ModifyHandDraw(f.Combat, f.Player, 5, out _) == 4, "crafting costs one normal draw");
        await Hook.AfterPlayerTurnStart(f.Combat, f.Context, f.Player);
        await CreatureCmd.SetCurrentHp(owner, 6);
        for (int i = 0; i < 3; i++) await CreatureCmd.Damage(f.Context, owner, 7, ValueProp.Move, f.Enemy);
        Check(owner.CurrentHp == 1 && owner.Block == 0 && owner.GetPower<PhylacteryGuardPower>() != null, "phylactery survives all three incoming hits");
        await CreatureCmd.Heal(owner, 10);
        Check(owner.CurrentHp == 1, "phylactery fixes HP at one throughout its protection");
        await Hook.AfterPlayerTurnStart(f.Combat, f.Context, f.Player);
        Check(owner.GetPower<PhylacteryGuardPower>() == null, "phylactery ends when the next turn can act");
        await CreatureCmd.SetCurrentHp(owner, 5);
        await PlayRare<TemporalWard>(f);
        await CreatureCmd.Damage(f.Context, owner, 12, ValueProp.Move, f.Enemy);
        f.Combat.RoundNumber++;
        f.Player.PlayerCombatState!.IncrementTurnNumber();
        await CreatureCmd.Heal(owner, 3);
        await CreatureCmd.GainBlock(owner, 7, ValueProp.Unpowered, null);
        await Hook.AfterSideTurnEnd(f.Combat, CombatSide.Player, [owner]);
        Check(owner.CurrentHp == 3 && owner.Block == 0, "12 delayed, repaid by 3 healing and 7 Block, leaves 2 HP loss");
    }

    private async Task TestRareEnemyActions()
    {
        var f = await CreateCommonFixture();
        var owner = f.Player.Creature;
        await PowerCmd.Apply<ThornsPower>(f.Context, owner, 3, owner, null);
        await PlayRare<OttoDance>(f, f.Enemy);
        int before = owner.CurrentHp;
        int enemyBefore = f.Enemy.CurrentHp;
        await CommonEnemyAttack(f, 6, 4);
        Check(before - owner.CurrentHp == 12 && enemyBefore - f.Enemy.CurrentHp == 12, "dance zeroes hits two and four while all four still hit Thorns");
        await PowerCmd.Apply<SunBlindPower>(f.Context, f.Enemy, 1, owner, null);
        before = owner.CurrentHp;
        enemyBefore = f.Enemy.CurrentHp;
        int allyBefore = f.Teammate.Creature.CurrentHp;
        await CommonEnemyAttack(f, 6, 4);
        Check(before == owner.CurrentHp && enemyBefore == f.Enemy.CurrentHp && allyBefore - f.Teammate.Creature.CurrentHp == 12,
            "blind misses only its caster, dance still applies to teammates");
    }

    private async Task TestRareResurrection()
    {
        var f = await CreateCommonFixture();
        var ally = f.Teammate;
        var exhausted = ally.PlayerCombatState!.AllCards.First();
        await CardPileCmd.Add(exhausted, PileType.Exhaust);
        var before = ally.PlayerCombatState.AllCards.ToHashSet();
        await CreatureCmd.SetCurrentHp(ally.Creature, 0);
        var revive = CommonCard<Resurrection>(f);
        await CardPileCmd.AddGeneratedCardToCombat(revive, PileType.Hand, f.Player);
        f.Player.PlayerCombatState!.Energy = 2;
        Check(revive.CanPlayTargeting(ally.Creature) && !revive.IsValidTarget(f.Player.Creature), "dead teammate is a valid multiplayer target");
        await CardCmd.AutoPlay(f.Context, revive, ally.Creature, skipCardPileVisuals: true);
        Check(ally.Creature.CurrentHp == 150 && exhausted.Pile?.Type == PileType.Exhaust &&
            before.SetEquals(ally.PlayerCombatState.AllCards) && ally.Creature.GetPower<AwaitingRevivalTurnPower>() != null,
            "resurrection restores 15 percent, original instances and exhausted cards, and waits until next turn");
    }

    private async Task TestRareCardPlays(IEnumerable<RareCard> all)
    {
        foreach (var canonical in all.Where(c => c is not Resurrection))
        {
            var f = await CreateCommonFixture(canonical.Form ?? ShenshenForm.Human);
            var card = f.Combat.CreateCard(canonical, f.Player);
            Creature? target = card.TargetType == TargetType.AnyEnemy ? f.Enemy : null;
            await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, f.Player);
            await CardCmd.AutoPlay(f.Context, card, target, skipCardPileVisuals: true);
            Check(card.Pile?.Type != PileType.Hand, canonical.Id.Entry + " resolves its normal combat play");
        }
    }
}
