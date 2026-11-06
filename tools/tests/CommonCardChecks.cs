using System.Reflection;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.Unlocks;
using MegaCrit.Sts2.Core.ValueProps;
using Shenshen.ShenshenCode.CardPools;
using Shenshen.ShenshenCode.Cards;
using Shenshen.ShenshenCode.Character;
using Shenshen.ShenshenCode.Powers;
using CommonTemporaryDexterityPower = Shenshen.ShenshenCode.Powers.TemporaryDexterityPower;
using Hero = Shenshen.ShenshenCode.Character.Shenshen;

public partial class Harness
{
    private async Task RunCommonCardChecks()
    {
        var pool = ModelDb.CardPool<ShenshenCardPool>();
        var solo = pool.GetUnlockedCards(UnlockState.all, CardMultiplayerConstraint.SingleplayerOnly)
            .Where(c => c.Rarity == CardRarity.Common).OfType<ShenshenCard>().ToArray();
        var multiplayer = pool.GetUnlockedCards(UnlockState.all, CardMultiplayerConstraint.MultiplayerOnly)
            .Where(c => c.Rarity == CardRarity.Common).ToArray();
        Check(solo.Length == 20 && solo.Count(c => c.Form == ShenshenForm.Human) == 10 &&
            solo.Count(c => c.Form == ShenshenForm.Cat) == 10,
            "the native solo card pool exposes exactly ten girl and ten cat commons");
        Check(solo.Count(c => c.Type == CardType.Attack) == 10 && solo.Count(c => c.Type == CardType.Skill) == 10 &&
            solo.All(c => c.MultiplayerConstraint == CardMultiplayerConstraint.None),
            "the twenty solo commons contain ten attacks and ten skills without mode restrictions");
        Check(multiplayer.Length == 21 && multiplayer.OfType<LightBlessing>().Count() == 1 && !solo.OfType<LightBlessing>().Any(),
            "the teammate-only blessing remains the twenty-first multiplayer common");
        Check(ModelDb.Card<IDontLikeYou>().Rarity == CardRarity.Uncommon &&
            ModelDb.Card<FluffedUp>().Rarity == CardRarity.Uncommon && ModelDb.Card<LightMirror>().Rarity == CardRarity.Uncommon,
            "the three implemented enhancement cards use the confirmed uncommon rarity");

        var fixture = await CreateCommonFixture();
        void Values<T>(ShenshenForm form, CardType type, int cost, bool exhaust,
            params (string Name, decimal Base, decimal Upgraded)[] values) where T : CardModel
        {
            var card = fixture.Combat.CreateCard<T>(fixture.Player);
            Check(card is ShenshenCard owned && owned.Form == form && card.Type == type &&
                card.Rarity == CardRarity.Common && card.EnergyCost.Canonical == cost &&
                card.Keywords.Contains(CardKeyword.Exhaust) == exhaust &&
                values.All(v => card.DynamicVars[v.Name].BaseValue == v.Base),
                typeof(T).Name + " matches the confirmed base values, form, cost and exhaust rule");
            card.UpgradeInternal();
            card.FinalizeUpgradeInternal();
            Check(card.IsUpgraded && card.EnergyCost.Canonical == cost &&
                card.Keywords.Contains(CardKeyword.Exhaust) == exhaust &&
                values.All(v => card.DynamicVars[v.Name].BaseValue == v.Upgraded),
                typeof(T).Name + " upgrades the approved values without changing cost or exhaust");
        }
        Values<ShatteredMirror>(ShenshenForm.Human, CardType.Skill, 1, false, ("Block", 6, 9));
        Values<ThornPrayer>(ShenshenForm.Human, CardType.Skill, 1, true, ("Block", 5, 5), ("ThornsPower", 1, 2));
        Values<LightfootBlessing>(ShenshenForm.Human, CardType.Skill, 0, true, ("DexterityPower", 2, 3), ("Cards", 1, 1));
        Values<LightCurtain>(ShenshenForm.Human, CardType.Skill, 1, false, ("Block", 8, 11), ("Cards", 1, 1));
        Values<LingeringLight>(ShenshenForm.Human, CardType.Skill, 1, false, ("Block", 7, 10));
        Values<TwinSparks>(ShenshenForm.Human, CardType.Attack, 1, false, ("Damage", 5, 6), ("Repeat", 2, 2));
        Values<ChasingFlame>(ShenshenForm.Human, CardType.Attack, 1, false, ("Damage", 8, 11), ("MarkedDamage", 12, 16));
        Values<DazzlingLight>(ShenshenForm.Human, CardType.Attack, 1, false, ("Damage", 7, 9), ("WeakPower", 1, 2));
        Values<Halo>(ShenshenForm.Human, CardType.Attack, 1, false, ("Damage", 6, 8), ("Block", 5, 7));
        Values<RefractedVolley>(ShenshenForm.Human, CardType.Attack, 1, false, ("Damage", 3, 4), ("Repeat", 2, 2), ("Cards", 1, 1));
        Values<ArchedBack>(ShenshenForm.Cat, CardType.Skill, 1, false, ("Block", 6, 8), ("ThornsPower", 2, 3));
        Values<FurballCurl>(ShenshenForm.Cat, CardType.Skill, 2, false, ("Block", 16, 20), ("ThornsPower", 4, 6));
        Values<CuriousPeek>(ShenshenForm.Cat, CardType.Skill, 1, false, ("Cards", 3, 3), ("ShiftCards", 1, 2));
        Values<Catnap>(ShenshenForm.Cat, CardType.Skill, 1, true, ("Cards", 1, 1), ("Energy", 2, 3));
        Values<Grooming>(ShenshenForm.Cat, CardType.Skill, 0, true, ("Cards", 1, 1), ("Block", 3, 6));
        Values<ShedFur>(ShenshenForm.Cat, CardType.Attack, 1, false, ("CalculationBase", 4, 7));
        Values<SweepingClaws>(ShenshenForm.Cat, CardType.Attack, 1, false, ("Damage", 4, 5), ("Repeat", 2, 2));
        Values<BunnyKick>(ShenshenForm.Cat, CardType.Attack, 2, false, ("Damage", 4, 5), ("Repeat", 4, 4));
        Values<BiteTheOpening>(ShenshenForm.Cat, CardType.Attack, 1, false, ("Damage", 7, 9), ("VulnerablePower", 1, 2));
        Values<BackwardGlance>(ShenshenForm.Cat, CardType.Attack, 1, false, ("Damage", 8, 11));

        CheckCommonCardDescriptions(fixture, solo);
        await CheckCommonAttacks();
        await CheckCommonTemporaryBenefits();
        await CheckCommonRecovery();
        await CheckCommonDrawAndEnergy();
        await CheckShatteredMirrorReception();
    }

    private async Task CheckCommonAttacks()
    {
        var f = await CreateCommonFixture();
        async Task Single<T>(int damage, bool upgraded = false) where T : CardModel
        {
            int hp = f.Enemy.CurrentHp;
            await CardCmd.AutoPlay(f.Context, CommonCard<T>(f, upgraded), f.Enemy);
            Check(f.Enemy.CurrentHp == hp - damage, typeof(T).Name + (upgraded ? "+" : "") + " executes its real single-target attack");
        }
        async Task Area<T>(int damage, bool upgraded = false) where T : CardModel
        {
            int first = f.Enemy.CurrentHp;
            int second = f.OtherEnemy.CurrentHp;
            await CardCmd.AutoPlay(f.Context, CommonCard<T>(f, upgraded), null);
            Check(f.Enemy.CurrentHp == first - damage && f.OtherEnemy.CurrentHp == second - damage,
                typeof(T).Name + (upgraded ? "+" : "") + " executes equal damage against both real enemies");
        }

        await Single<TwinSparks>(10);
        await Single<TwinSparks>(12, true);
        await Single<ChasingFlame>(8);
        await Single<ChasingFlame>(11, true);
        await LightMarkPower.ApplyMark(f.Context, f.Enemy, f.Teammate.Creature, 1, 2, f.Combat.CreateCard<LightMark>(f.Teammate));
        await Single<ChasingFlame>(8);
        Check(MarkOn(f.Enemy, f.Player.Creature) == null, "a teammate's mark does not satisfy Chasing Flame's own-mark condition");
        await LightMarkPower.ApplyMark(f.Context, f.Enemy, f.Player.Creature, 1, 2, f.Combat.CreateCard<LightMark>(f.Player));
        await Single<ChasingFlame>(13);
        await Single<ChasingFlame>(17, true);
        await PowerCmd.Apply<StrengthPower>(f.Context, f.Player.Creature, 1, f.Player.Creature, null);
        await Single<TwinSparks>(14);
        await PowerCmd.Remove(f.Player.Creature.GetPower<StrengthPower>());
        foreach (var mark in f.Enemy.GetPowerInstances(ModelDb.Power<LightMarkPower>().Id).ToArray())
            await PowerCmd.Remove(mark);

        await Single<DazzlingLight>(7);
        Check(f.Enemy.GetPower<WeakPower>()?.Amount == 1, "Dazzling Light applies one base Weak");
        await PowerCmd.Remove(f.Enemy.GetPower<WeakPower>());
        await Single<DazzlingLight>(9, true);
        Check(f.Enemy.GetPower<WeakPower>()?.Amount == 2, "Dazzling Light upgrade applies two Weak");
        await PowerCmd.Remove(f.Enemy.GetPower<WeakPower>());
        await Area<Halo>(6);
        Check(f.Player.Creature.Block == 0, "Halo does not grant its conditional block without thorns");
        await PowerCmd.Apply<ThornsPower>(f.Context, f.Player.Creature, 1, f.Player.Creature, null);
        await Area<Halo>(6);
        Check(f.Player.Creature.Block == 5, "base Halo recognizes thorns and grants five block");
        await Area<Halo>(8, true);
        Check(f.Player.Creature.Block == 12, "upgraded Halo adds seven conditional block");
        await LoseCommonBlock(f);
        var undrawn = await CommonDrawCard<LittleFireball>(f);
        await Single<RefractedVolley>(6);
        Check(undrawn.Pile?.Type == PileType.Draw, "Refracted Volley does not draw when played without existing block");
        await CreatureCmd.GainBlock(f.Player.Creature, 1, ValueProp.Unpowered, null);
        await Single<RefractedVolley>(8, true);
        Check(undrawn.Pile?.Type == PileType.Hand, "Refracted Volley draws after the attack when block existed before play");

        f.Player.Creature.GetPower<FormPower>()!.SetAmount(2);
        await PowerCmd.Remove(f.Player.Creature.GetPower<ThornsPower>());
        await Area<SweepingClaws>(8);
        await Area<SweepingClaws>(10, true);
        await Single<BunnyKick>(16);
        await Single<BunnyKick>(20, true);
        await PowerCmd.Apply<StrengthPower>(f.Context, f.Player.Creature, 2, f.Player.Creature, null);
        await PowerCmd.Apply<ThornsPower>(f.Context, f.Player.Creature, 3, f.Player.Creature, null);
        await Area<ShedFur>(9);
        await Area<ShedFur>(12, true);
        Check(f.Player.Creature.GetPower<ThornsPower>()?.Amount == 3, "Shed Fur leaves the thorns resource intact");
        await LightMarkPower.ApplyMark(f.Context, f.Enemy, f.Player.Creature, 1, 2, f.Combat.CreateCard<LightMark>(f.Player));
        int markedHp = f.Enemy.CurrentHp;
        int otherHp = f.OtherEnemy.CurrentHp;
        await CardCmd.AutoPlay(f.Context, CommonCard<ShedFur>(f), null);
        Check(f.Enemy.CurrentHp == markedHp - 10 && f.OtherEnemy.CurrentHp == otherHp - 9,
            "Shed Fur adds strength once per enemy and triggers a mark only once per enemy");
        await PowerCmd.Remove(MarkOn(f.Enemy, f.Player.Creature));
        await PowerCmd.Remove(f.Player.Creature.GetPower<StrengthPower>());
        await Single<BiteTheOpening>(7);
        Check(f.Enemy.GetPower<VulnerablePower>()?.Amount == 1, "Bite the Opening applies one base Vulnerable");
        await PowerCmd.Remove(f.Enemy.GetPower<VulnerablePower>());
        await Single<BiteTheOpening>(9, true);
        Check(f.Enemy.GetPower<VulnerablePower>()?.Amount == 2, "Bite the Opening upgrade applies two Vulnerable");
    }

    private async Task CheckCommonTemporaryBenefits()
    {
        var f = await CreateCommonFixture();
        var prayer = CommonCard<ThornPrayer>(f);
        await CardCmd.AutoPlay(f.Context, prayer, null);
        await CardCmd.AutoPlay(f.Context, CommonCard<ThornPrayer>(f, true), null);
        Check(f.Player.Creature.Block == 10 && f.Player.Creature.GetPower<ThornsPower>()?.Amount == 3 && prayer.Pile?.Type == PileType.Exhaust,
            "base and upgraded prayers grant permanent thorns and actually exhaust");
        await PowerCmd.Apply<DexterityPower>(f.Context, f.Player.Creature, 2, f.Player.Creature, null);
        var blessingDraw = await CommonDrawCard<LittleFireball>(f);
        var lightfoot = CommonCard<LightfootBlessing>(f);
        await CardCmd.AutoPlay(f.Context, lightfoot, null);
        Check(f.Player.Creature.GetPower<DexterityPower>()?.Amount == 4 && blessingDraw.Pile?.Type == PileType.Hand && lightfoot.Pile?.Type == PileType.Exhaust,
            "Lightfoot Blessing grants temporary dexterity, draws the actual top card and exhausts");
        await Hook.BeforeHandDraw(f.Combat, f.Teammate, f.Context);
        Check(f.Player.Creature.GetPower<DexterityPower>()?.Amount == 4 && f.Player.Creature.GetPower<CommonTemporaryDexterityPower>()?.Amount == 2,
            "a teammate's draw step does not expire temporary dexterity");
        CheckCommonPowerPacket(f, ModelDb.Power<CommonTemporaryDexterityPower>().Id, 2,
            "the native combat packet carries the temporary dexterity contribution separately");
        await Hook.BeforeHandDraw(f.Combat, f.Player, f.Context);
        Check(f.Player.Creature.GetPower<DexterityPower>()?.Amount == 2 && f.Player.Creature.GetPower<CommonTemporaryDexterityPower>() == null,
            "the owner's next draw step removes only temporary dexterity and preserves permanent dexterity");

        f.Player.Creature.GetPower<FormPower>()!.SetAmount(2);
        await LoseCommonBlock(f);
        await CardCmd.AutoPlay(f.Context, CommonCard<ArchedBack>(f), null);
        await CardCmd.AutoPlay(f.Context, CommonCard<FurballCurl>(f, true), null);
        Check(f.Player.Creature.Block == 30 && f.Player.Creature.GetPower<ThornsPower>()?.Amount == 11 &&
            f.Player.Creature.GetPower<TemporaryThornsPower>()?.Amount == 8,
            "Arched Back and upgraded Furball stack temporary thorns while each block gain respects dexterity");
        await Switch(f.Context, f.Player);
        Check(f.Player.Creature.GetPower<ThornsPower>()?.Amount == 11,
            "temporary thorns persist when changing back to girl form");
        await Hook.BeforeHandDraw(f.Combat, f.Teammate, f.Context);
        Check(f.Player.Creature.GetPower<ThornsPower>()?.Amount == 11,
            "a teammate's draw step does not expire temporary thorns");
        CheckCommonPowerPacket(f, ModelDb.Power<TemporaryThornsPower>().Id, 8,
            "the native combat packet carries the stacked temporary thorns contribution");
        int retaliatedHp = f.Enemy.CurrentHp;
        await CommonEnemyAttack(f, 1);
        Check(f.Enemy.CurrentHp == retaliatedHp - 11,
            "fully blocked attacks trigger both permanent and temporary thorns through the native retaliation");
        await PowerCmd.Apply<ThornsPower>(f.Context, f.Player.Creature, 1, f.Player.Creature, null);
        await Hook.BeforeHandDraw(f.Combat, f.Player, f.Context);
        Check(f.Player.Creature.GetPower<ThornsPower>()?.Amount == 4 && f.Player.Creature.GetPower<TemporaryThornsPower>() == null,
            "temporary thorns expiry preserves both prior and newly gained permanent thorns");

        await LoseCommonBlock(f);
        await PowerCmd.Remove(f.Player.Creature.GetPower<DexterityPower>());
        var curtainDraw = await CommonDrawCard<LittleFireball>(f);
        await CardCmd.AutoPlay(f.Context, CommonCard<LightCurtain>(f, true), null);
        Check(f.Player.Creature.Block == 11 && curtainDraw.Pile?.Type == PileType.Hand,
            "upgraded Light Curtain combines eleven block with a real draw");
        f.Player.Creature.GetPower<FormPower>()!.SetAmount(2);
        await LoseCommonBlock(f);
        var groomingDraw = await CommonDrawCard<ClawStrike>(f);
        await CardCmd.AutoPlay(f.Context, CommonCard<Grooming>(f, true), null);
        Check(f.Player.Creature.Block == 6 && groomingDraw.Pile?.Type == PileType.Hand,
            "upgraded Grooming draws and grants six block when thorns exist");
        await PowerCmd.Remove(f.Player.Creature.GetPower<ThornsPower>());
        await LoseCommonBlock(f);
        await CardCmd.AutoPlay(f.Context, CommonCard<Grooming>(f), null);
        Check(f.Player.Creature.Block == 0, "Grooming grants no conditional block without thorns");
    }

    private async Task CheckCommonRecovery()
    {
        var f = await CreateCommonFixture();
        var cat = f.Combat.CreateCard<ClawStrike>(f.Player);
        cat.UpgradeInternal();
        cat.FinalizeUpgradeInternal();
        cat.EnergyCost.SetThisCombat(0);
        var catDecoy = f.Combat.CreateCard<MiDodge>(f.Player);
        var girl = f.Combat.CreateCard<LightShield>(f.Player);
        girl.UpgradeInternal();
        girl.FinalizeUpgradeInternal();
        girl.EnergyCost.SetThisCombat(0);
        foreach (var card in new CardModel[] { cat, catDecoy, girl })
            await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Discard, f.Player);
        var selector = new TestCardSelector();
        using (CardSelectCmd.UseSelector(selector))
        {
            selector.PrepareToSelect(new[] { cat });
            var lingering = CommonCard<LingeringLight>(f);
            await CardPileCmd.AddGeneratedCardToCombat(lingering, PileType.Hand, f.Player);
            var owned = f.Player.PlayerCombatState!.AllCards.ToHashSet();
            await CardCmd.AutoPlay(f.Context, lingering, null);
            Check(f.Player.Creature.Block == 7 && ReferenceEquals(f.Player.PlayerCombatState.DrawPile.Cards.OfType<ClawStrike>().First(), cat) &&
                catDecoy.Pile?.Type == PileType.Discard && girl.Pile?.Type == PileType.Discard &&
                owned.SetEquals(f.Player.PlayerCombatState.AllCards),
                "Lingering Light selects one actual cat discard without copying cards or touching other discard instances");
            var heldGirl = f.Combat.CreateCard<LittleFireball>(f.Player);
            await CardPileCmd.AddGeneratedCardToCombat(heldGirl, PileType.Hand, f.Player);
            await Switch(f.Context, f.Player);
            Check(cat.Pile?.Type == PileType.Hand && cat.IsUpgraded && cat.EnergyCost.GetWithModifiers(CostModifiers.Local) == 0,
                "the next real transformation draws the recovered cat instance with its upgrade and cost intact");

            selector.PrepareToSelect(new[] { girl });
            var backward = CommonCard<BackwardGlance>(f, true);
            await CardPileCmd.AddGeneratedCardToCombat(backward, PileType.Hand, f.Player);
            owned = f.Player.PlayerCombatState.AllCards.ToHashSet();
            int hp = f.Enemy.CurrentHp;
            await CardCmd.AutoPlay(f.Context, backward, f.Enemy);
            Check(f.Enemy.CurrentHp == hp - 12 && ReferenceEquals(f.Player.PlayerCombatState.DrawPile.Cards.First(c => c is ShenshenCard { Form: ShenshenForm.Human }), girl) &&
                owned.SetEquals(f.Player.PlayerCombatState.AllCards),
                "upgraded Backward Glance deals eleven plus existing strength and recovers the selected actual girl card");
            await Switch(f.Context, f.Player);
            Check(girl.Pile?.Type == PileType.Hand && girl.IsUpgraded && girl.EnergyCost.GetWithModifiers(CostModifiers.Local) == 0,
                "returning to girl draws the same recovered upgraded girl card");

            selector.PrepareToSelect(Array.Empty<CardModel>());
            int block = f.Player.Creature.Block;
            await CardCmd.AutoPlay(f.Context, CommonCard<LingeringLight>(f, true), null);
            Check(f.Player.Creature.Block == block + 11 && cat.Pile?.Type == PileType.Discard,
                "declining optional recovery still grants upgraded Lingering Light block with existing dexterity");
        }
        await CardPileCmd.Add(f.Player.PlayerCombatState!.DiscardPile.Cards.OfType<ShenshenCard>()
            .Where(c => c.Form == ShenshenForm.Cat).ToArray(), PileType.Exhaust);
        int before = f.Player.Creature.Block;
        await CardCmd.AutoPlay(f.Context, CommonCard<LingeringLight>(f), null);
        Check(f.Player.Creature.Block == before + 8,
            "an empty cat discard pool needs no selection and still grants base recovery-card block");
    }

    private async Task CheckCommonDrawAndEnergy()
    {
        foreach (bool upgraded in new[] { false, true })
        {
            var f = await CreateCommonFixture(ShenshenForm.Cat);
            for (int i = 0; i < 3; i++) await CommonDrawCard<ClawStrike>(f);
            for (int i = 0; i < 5; i++) await CommonDrawCard<LittleFireball>(f);
            var curious = CommonCard<CuriousPeek>(f, upgraded);
            await CardPileCmd.AddGeneratedCardToCombat(curious, PileType.Hand, f.Player);
            await CardCmd.AutoPlay(f.Context, curious, null);
            Check(f.Player.PlayerCombatState!.Hand.Cards.Count == 3 &&
                f.Player.PlayerCombatState.Hand.Cards.All(c => c is ClawStrike),
                "Curious Peek draws exactly three actual current-form cards before transformation");
            CheckCommonPowerPacket(f, ModelDb.Power<CuriousPeekPower>().Id, upgraded ? 2 : 1,
                "the native combat packet carries pending Curious Peek draws");
            var owned = f.Player.PlayerCombatState.AllCards.ToHashSet();
            await Switch(f.Context, f.Player);
            Check(f.Player.PlayerCombatState.Hand.Cards.Count == (upgraded ? 5 : 4) &&
                f.Player.PlayerCombatState.Hand.Cards.All(c => c is LittleFireball) &&
                f.Player.Creature.GetPower<CuriousPeekPower>() == null && owned.SetEquals(f.Player.PlayerCombatState.AllCards),
                "Curious Peek" + (upgraded ? "+" : "") + " draws its bonus from the new form only after normal replacement and consumes the reward");
        }

        var unused = await CreateCommonFixture(ShenshenForm.Cat);
        await CardCmd.AutoPlay(unused.Context, CommonCard<CuriousPeek>(unused), null);
        await Hook.AfterSideTurnEnd(unused.Combat, CombatSide.Player, new[] { unused.Teammate.Creature });
        Check(unused.Player.Creature.GetPower<CuriousPeekPower>()?.Amount == 1,
            "another participant ending a turn does not discard the owner's unused peek reward");
        await Hook.AfterSideTurnEnd(unused.Combat, CombatSide.Player, new[] { unused.Player.Creature });
        Check(unused.Player.Creature.GetPower<CuriousPeekPower>() == null,
            "Curious Peek's unused reward expires at the end of its own turn");

        foreach (bool upgraded in new[] { false, true })
        {
            var f = await CreateCommonFixture(ShenshenForm.Cat);
            var draw = await CommonDrawCard<ClawStrike>(f);
            var nap = CommonCard<Catnap>(f, upgraded);
            await CardPileCmd.AddGeneratedCardToCombat(nap, PileType.Hand, f.Player);
            f.Player.PlayerCombatState!.Energy = 3;
            await PlayPaid(nap, f.Context);
            Check(f.Player.PlayerCombatState.Energy == 2 && draw.Pile?.Type == PileType.Hand && nap.Pile?.Type == PileType.Exhaust,
                "Catnap pays one energy now, draws and actually exhausts without immediate energy refund");
            CheckCommonPowerPacket(f, ModelDb.Power<CatnapPower>().Id, upgraded ? 3 : 2,
                "the native combat packet carries pending Catnap energy");
            await Hook.BeforeHandDraw(f.Combat, f.Teammate, f.Context);
            Check(f.Player.PlayerCombatState.Energy == 2 && f.Player.Creature.GetPower<CatnapPower>() != null,
                "a teammate's draw step neither grants nor consumes pending Catnap energy");
            f.Player.PlayerCombatState.Energy = 3;
            await Hook.BeforeHandDraw(f.Combat, f.Player, f.Context);
            Check(f.Player.PlayerCombatState.Energy == (upgraded ? 6 : 5) && f.Player.Creature.GetPower<CatnapPower>() == null,
                "Catnap" + (upgraded ? "+" : "") + " grants its delayed energy once at the next own draw step");
            await Hook.BeforeHandDraw(f.Combat, f.Player, f.Context);
            Check(f.Player.PlayerCombatState.Energy == (upgraded ? 6 : 5), "a later draw hook cannot repeat consumed Catnap energy");
        }
    }

    private async Task CheckShatteredMirrorReception()
    {
        var f = await CreateCommonFixture();
        await CardCmd.AutoPlay(f.Context, CommonCard<ShatteredMirror>(f), null);
        await CreatureCmd.GainBlock(f.Player.Creature, 8, ValueProp.Unpowered, null);
        int hp = f.Enemy.CurrentHp;
        await CommonEnemyAttack(f, 4);
        Check(f.Enemy.CurrentHp == hp - 4 && f.Player.Creature.Block == 10,
            "Shattered Mirror reflects the first four consumed mirror block while ordinary block remains");
        await CommonEnemyAttack(f, 4);
        Check(f.Enemy.CurrentHp == hp - 6 && f.Player.Creature.Block == 6,
            "the same six-point mirror allowance can span attack segments but ordinary block cannot enlarge it");
        await CommonEnemyAttack(f, 4);
        Check(f.Enemy.CurrentHp == hp - 6 && f.Player.Creature.Block == 2,
            "later ordinary-block reception reflects nothing after the mirror's own block is exhausted");

        await LoseCommonBlock(f);
        await CardCmd.AutoPlay(f.Context, CommonCard<ShatteredMirror>(f), null);
        await LoseCommonBlock(f);
        await CreatureCmd.GainBlock(f.Player.Creature, 8, ValueProp.Unpowered, null);
        hp = f.Enemy.CurrentHp;
        await CommonEnemyAttack(f, 4);
        Check(f.Enemy.CurrentHp == hp,
            "mirror block removed outside an attack cannot leave a refund allowance for newly gained ordinary block");

        await LoseCommonBlock(f);
        await PowerCmd.Apply<DexterityPower>(f.Context, f.Player.Creature, 2, f.Player.Creature, null);
        await CardCmd.AutoPlay(f.Context, CommonCard<ShatteredMirror>(f, true), null);
        Check(f.Player.Creature.Block == 11, "upgraded Shattered Mirror gains nine plus the actual two dexterity");
        await CreatureCmd.GainBlock(f.Player.Creature, 8, ValueProp.Unpowered, null);
        hp = f.Enemy.CurrentHp;
        await CommonEnemyAttack(f, 20);
        Check(f.Enemy.CurrentHp == hp - 11 && f.Player.Creature.Block == 0,
            "upgraded mirror reflects its eleven actual block but excludes the other eight block");

        await PowerCmd.Remove(f.Player.Creature.GetPower<DexterityPower>());
        f.Player.Creature.GetPower<FormPower>()!.SetAmount(2);
        await PowerCmd.Apply<CatDodgePower>(f.Context, f.Player.Creature, 1, f.Player.Creature, null);
        await CardCmd.AutoPlay(f.Context, CommonCard<ShatteredMirror>(f), null);
        hp = f.Enemy.CurrentHp;
        await CommonEnemyAttack(f, 4, 2);
        Check(f.Enemy.CurrentHp == hp - 4 && f.Player.Creature.Block == 2,
            "cat dodge leaves mirror block untouched and only the received multi-hit segment reflects");
        await CommonEnemyAttack(f, 2);
        Check(f.Enemy.CurrentHp == hp - 6 && f.Player.Creature.Block == 0,
            "the mirror allowance left after cat dodge can be spent on a later real attack");

        f.Player.Creature.GetPower<FormPower>()!.SetAmount(1);
        await CardCmd.AutoPlay(f.Context, CommonCard<ShatteredMirror>(f), null);
        await CardCmd.AutoPlay(f.Context, f.Combat.CreateCard<LightMirror>(f.Player), null);
        hp = f.Enemy.CurrentHp;
        await CommonEnemyAttack(f, 10);
        Check(f.Enemy.CurrentHp == hp - 10 && f.Player.Creature.Block == 11,
            "Light Mirror and Shattered Mirror never reflect the same consumed block twice");
        await PowerCmd.Remove(f.Player.Creature.GetPower<ReflectPower>());
        await LoseCommonBlock(f);
        await CardCmd.AutoPlay(f.Context, CommonCard<ShatteredMirror>(f), null);
        await Hook.BeforeHandDraw(f.Combat, f.Teammate, f.Context);
        Check(f.Player.Creature.GetPower<ShatteredMirrorPower>()?.Amount == 6,
            "a teammate's draw hook preserves Shattered Mirror's tracked allowance");
        CheckCommonPowerPacket(f, ModelDb.Power<ShatteredMirrorPower>().Id, 6,
            "the native combat packet roundtrip preserves the finite mirror allowance");
        await Hook.BeforeHandDraw(f.Combat, f.Player, f.Context);
        hp = f.Enemy.CurrentHp;
        await CommonEnemyAttack(f, 1);
        Check(f.Enemy.CurrentHp == hp && f.Player.Creature.GetPower<ShatteredMirrorPower>() == null,
            "Shattered Mirror expires at the next own draw hook even when old block is still present");
    }

    private static T CommonCard<T>(CommonFixture fixture, bool upgraded = false) where T : CardModel
    {
        var card = fixture.Combat.CreateCard<T>(fixture.Player);
        if (upgraded)
        {
            card.UpgradeInternal();
            card.FinalizeUpgradeInternal();
        }
        return card;
    }

    private void CheckCommonCardDescriptions(CommonFixture fixture, IEnumerable<CardModel> commons)
    {
        var localization = LocManager.Instance;
        try
        {
            foreach (var language in new[] { "eng", "zhs" })
            {
                localization.SetLanguage(language);
                var table = localization.GetTable("cards");
                // The installed game's keyword extension class is internal.
                var exhaustText = (string)typeof(CardModel).Assembly
                    .GetType("MegaCrit.Sts2.Core.Entities.Cards.CardKeywordExtensions")!
                    .GetMethod("GetCardText", BindingFlags.Public | BindingFlags.Static)!
                    .Invoke(null, new object[] { CardKeyword.Exhaust })!;
                foreach (var canonical in commons)
                {
                    var card = fixture.Combat.CreateCard(canonical, fixture.Player);
                    var titleKey = card.Id.Entry + ".title";
                    var descriptionKey = card.Id.Entry + ".description";
                    bool localEntries = table.IsLocalKey(titleKey) && table.IsLocalKey(descriptionKey);
                    card.UpdateDynamicVarPreview(CardPreviewMode.Normal, fixture.Enemy, card.DynamicVars);
                    var title = card.Title;
                    var basic = card.GetDescriptionForPile(PileType.Hand, fixture.Enemy);
                    var preview = card.GetDescriptionForUpgradePreview();
                    card.UpgradeInternal();
                    card.FinalizeUpgradeInternal();
                    card.UpdateDynamicVarPreview(CardPreviewMode.Normal, fixture.Enemy, card.DynamicVars);
                    var upgraded = card.GetDescriptionForPile(PileType.Hand, fixture.Enemy);
                    var formatted = new List<string> { title, card.Title, basic, preview, upgraded };
                    if (card is LingeringLight or BackwardGlance)
                    {
                        var promptKey = card.Id.Entry + ".selectionScreenPrompt";
                        localEntries &= table.IsLocalKey(promptKey);
                        formatted.Add(new LocString("cards", promptKey).GetFormattedText());
                    }
                    bool readable = formatted.All(text => !string.IsNullOrWhiteSpace(text) &&
                        !text.Contains('{') && !text.Contains('}') && !text.Contains(card.Id.Entry));
                    bool exhaustVisible = !card.Keywords.Contains(CardKeyword.Exhaust) ||
                        new[] { basic, preview, upgraded }.All(text => text.Contains(exhaustText));
                    Check(localEntries && readable && exhaustVisible,
                        language + " " + card.GetType().Name + " has local titles, readable base/preview/upgraded text and native keyword text");
                }
            }
        }
        finally
        {
            localization.SetLanguage("eng");
        }
    }

    private static async Task<T> CommonDrawCard<T>(CommonFixture fixture) where T : CardModel
    {
        var card = fixture.Combat.CreateCard<T>(fixture.Player);
        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Draw, fixture.Player, CardPilePosition.Top);
        return card;
    }

    private static Task LoseCommonBlock(CommonFixture fixture) => CreatureCmd.LoseBlock(fixture.Context,
        fixture.Player.Creature, fixture.Player.Creature.Block, fixture.Player.Creature);

    private static async Task CommonEnemyAttack(CommonFixture fixture, int damage, int hits = 1)
    {
        await DamageCmd.Attack(damage).FromMonster(fixture.Enemy.Monster!).WithNoAttackerAnim().WithHitCount(hits).Execute(fixture.Context);
    }

    private static async Task<CommonFixture> CreateCommonFixture(ShenshenForm form = ShenshenForm.Human)
    {
        RunManager.Instance.CleanUp(false);
        var player = Player.CreateForNewRun<Hero>(UnlockState.all, 1);
        var teammate = Player.CreateForNewRun<MegaCrit.Sts2.Core.Models.Characters.Ironclad>(UnlockState.all, 2);
        var run = RunState.CreateForTest([player, teammate], seed: "SHENSHENCOMMONS");
        RunManager.Instance.SetUpTest(run, new MegaCrit.Sts2.Core.Multiplayer.NetSingleplayerGameService());
        MegaCrit.Sts2.Core.Context.LocalContext.NetId = 1;
        var combat = new CombatState(runState: run);
        combat.AddPlayer(player);
        combat.AddPlayer(teammate);
        var enemy = combat.CreateCreature(ModelDb.Monster<TestSubject>().ToMutable(), CombatSide.Enemy, null);
        var otherEnemy = combat.CreateCreature(ModelDb.Monster<TestSubject>().ToMutable(), CombatSide.Enemy, null);
        combat.AddCreature(enemy);
        combat.AddCreature(otherEnemy);
        CombatManager.Instance.SetUpCombat(combat);
        var turn = typeof(CombatManager).GetField("_turnState", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(CombatManager.Instance)!;
        turn.GetType().GetProperty("IsInProgress")!.SetValue(turn, true);
        turn.GetType().GetProperty("IsStarting")!.SetValue(turn, false);
        var context = new ThrowingPlayerChoiceContext();
        await CreatureCmd.SetMaxAndCurrentHp(player.Creature, 1000);
        await CreatureCmd.SetMaxAndCurrentHp(teammate.Creature, 1000);
        await CreatureCmd.SetMaxAndCurrentHp(enemy, 10000);
        await CreatureCmd.SetMaxAndCurrentHp(otherEnemy, 10000);
        await PowerCmd.Apply<FormPower>(context, player.Creature, form == ShenshenForm.Human ? 1 : 2, player.Creature, null);
        await CardPileCmd.Add(player.PlayerCombatState!.AllCards.ToArray(), PileType.Exhaust);
        return new(player, teammate, run, combat, enemy, otherEnemy, context);
    }

    private void CheckCommonPowerPacket(CommonFixture fixture, ModelId id, int amount, string name)
    {
        var snapshot = NetFullCombatState.FromRun(fixture.Run, null);
        var writer = new PacketWriter();
        snapshot.Serialize(writer);
        var reader = new PacketReader();
        reader.Reset(writer.Buffer);
        var restored = new NetFullCombatState();
        restored.Deserialize(reader);
        Check(restored.Creatures.Single(c => c.playerId == fixture.Player.NetId).powers.Any(p => p.id == id && p.amount == amount), name);
    }

    private sealed record CommonFixture(Player Player, Player Teammate, RunState Run, CombatState Combat,
        Creature Enemy, Creature OtherEnemy, ThrowingPlayerChoiceContext Context);
}
