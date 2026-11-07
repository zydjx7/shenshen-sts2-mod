using System.Reflection;
using System.Runtime.Loader;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.Unlocks;
using Shenshen.ShenshenCode.Cards;
using Shenshen.ShenshenCode.Powers;
using Hero = Shenshen.ShenshenCode.Character.Shenshen;
using Environment = System.Environment;

public partial class Harness : Node
{
    private int _checks;
    public override async void _Ready()
    {
        try
        {
            var repository = Environment.GetEnvironmentVariable("SHENSHEN_REPO_ROOT")
                ?? throw new InvalidOperationException("Run tools/test.ps1 to set the isolated harness paths.");
            var game = Environment.GetEnvironmentVariable("SHENSHEN_GAME_PATH")
                ?? throw new InvalidOperationException("Missing game path.");
            AssemblyLoadContext.Default.Resolving += (_, name) =>
            {
                var path = Path.Combine(game, "data_sts2_windows_x86_64", name.Name + ".dll");
                return File.Exists(path) ? AssemblyLoadContext.Default.LoadFromAssemblyPath(path) : null;
            };
            foreach (var pack in new[] { Path.Combine(game, "SlayTheSpire2.pck"),
                Path.Combine(repository, "dist", "BaseLib", "BaseLib.pck"), Path.Combine(repository, "dist", "Shenshen", "Shenshen.pck") })
                if (!ProjectSettings.LoadResourcePack(pack)) throw new InvalidOperationException("Cannot load resource pack: " + pack);
            await RunGameTests();
            GD.Print($"SHENSHEN_HEADLESS_PASS checks={_checks}");
            GetTree().Quit(0);
        }
        catch (Exception e)
        {
            GD.PrintErr("SHENSHEN_HEADLESS_FAIL " + e);
            GetTree().Quit(1);
        }
    }

    private async Task RunGameTests()
    {
        TestMode.IsOn = true;
        // The engine's test-mode initializer skips disk discovery and save migration.
        await MegaCrit.Sts2.Core.Modding.ModManager.Initialize(new MegaCrit.Sts2.Core.Modding.ModManagerFileIo(), null, null);
        RegisterLoadedMod("BaseLib", typeof(BaseLib.BaseLibMain).Assembly);
        RegisterLoadedMod("Shenshen", typeof(Hero).Assembly);
        BaseLib.BaseLibMain.Initialize();
        Shenshen.ShenshenCode.MainFile.Initialize();
        MegaCrit.Sts2.Core.Modding.AssemblyInfo.Init();
        ModelDb.Init();
        foreach (var type in typeof(Hero).Assembly.GetTypes().Where(t => !t.IsAbstract && typeof(AbstractModel).IsAssignableFrom(t)))
            ModelDb.Inject(type);
        MegaCrit.Sts2.Core.Multiplayer.Serialization.ModelIdSerializationCache.Init();
        ModelDb.InitIds();
        MegaCrit.Sts2.Core.Saves.SaveManager.Instance.InitSettingsDataForTest();
        MegaCrit.Sts2.Core.Saves.SaveManager.Instance.SettingsSave.Language = "eng";
        MegaCrit.Sts2.Core.Localization.LocManager.Initialize();
        var player = Player.CreateForNewRun<Hero>(UnlockState.all, 1);
        var run = RunState.CreateForTest([player], seed: "SHENSHEN");
        RunManager.Instance.SetUpTest(run, new MegaCrit.Sts2.Core.Multiplayer.NetSingleplayerGameService());
        MegaCrit.Sts2.Core.Context.LocalContext.NetId = 1;
        var combat = new CombatState(runState: run);
        combat.AddPlayer(player);
        var enemy = combat.CreateCreature(ModelDb.Monster<MegaCrit.Sts2.Core.Models.Monsters.TestSubject>().ToMutable(),
            CombatSide.Enemy, null);
        combat.AddCreature(enemy);
        CombatManager.Instance.SetUpCombat(combat);
        var turnState = typeof(CombatManager).GetField("_turnState", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(CombatManager.Instance)!;
        turnState.GetType().GetProperty("IsInProgress")!.SetValue(turnState, true);
        turnState.GetType().GetProperty("IsStarting")!.SetValue(turnState, false);
        var context = new ThrowingPlayerChoiceContext();
        await PowerCmd.Apply<FormPower>(context, player.Creature, 1, player.Creature, null);
        var state = player.PlayerCombatState!;
        var all = state.AllCards.ToArray();
        Check(player.Deck.Cards.Count == 19 && all.Length == 19, "both owned nine-card starting decks and one permanent transformation");
        var ownedTransformation = all.OfType<TransformationMagic>().Single();
        await CardPileCmd.Add(ownedTransformation, PileType.Hand);
        var upgraded = all.OfType<LittleFireball>().First();
        upgraded.UpgradeInternal();
        upgraded.EnergyCost.SetThisCombat(0);
        var drawn = (await CardPileCmd.Draw(context, 5, player)).ToArray();
        Check(drawn.Length == 5 && drawn.All(c => c is ShenshenCard { Form: Shenshen.ShenshenCode.Character.ShenshenForm.Human }), "girl draw filters cats");
        var inactiveOrder = state.DrawPile.Cards.Where(c => c is ShenshenCard { Form: Shenshen.ShenshenCode.Character.ShenshenForm.Cat }).ToArray();
        await CardPileCmd.Shuffle(context, player);
        Check(inactiveOrder.SequenceEqual(state.DrawPile.Cards.Where(c => c is ShenshenCard { Form: Shenshen.ShenshenCode.Character.ShenshenForm.Cat })), "inactive draw order survives shuffle");
        await Switch(context, player);
        Check(state.Hand.Cards.Count == 6 && state.Hand.Cards.Contains(ownedTransformation) &&
            state.Hand.Cards.Where(c => c is not TransformationMagic).All(c => c is ShenshenCard { Form: Shenshen.ShenshenCode.Character.ShenshenForm.Cat }),
            "switch draws same count cats and retains the actual owned transformation");
        for (int i = 0; i < 8; i++) await Switch(context, player);
        Check(all.ToHashSet().SetEquals(state.AllCards) && state.AllCards.Count() == 19, "repeated switches preserve every exact card reference");
        Check(upgraded.IsUpgraded && upgraded.EnergyCost.GetWithModifiers(CostModifiers.Local) == 0, "upgrade and cost state persist");
        // The real shared transformation remains retained. Shared state cards
        // provide eligible draws beyond the ten-card hand cap.
        var handLimitFillers = new[] { combat.CreateCard<Wound>(player), combat.CreateCard<Wound>(player) };
        foreach (var filler in handLimitFillers)
            await CardPileCmd.AddGeneratedCardToCombat(filler, PileType.Draw, player);
        await CardPileCmd.Draw(context, 100, player);
        Check(state.Hand.Cards.Count == 10 && state.DrawPile.Cards.Any(c => c is Wound ||
            c is ShenshenCard { Form: Shenshen.ShenshenCode.Character.ShenshenForm.Cat }), "hand cap leaves eligible cards undrawn");
        Check(!(await CardPileCmd.Draw(context, 1, player)).Any(), "full hand draws none");
        var reward = new CardReward(CardCreationOptions.ForRoom(player, RoomType.Monster), 3, player);
        reward.Populate();
        Check(reward.Cards.Count() == 3 && reward.Cards.All(c => c is ShenshenCard && c is not LightBlessing), "standard three-card solo reward and multiplayer filtering");
        var merchant = MerchantInventory.CreateForNormalMerchant(player);
        Check(merchant.CharacterCardEntries.Count == 5 && merchant.CharacterCardEntries.All(e => e.IsStocked &&
            e.CreationResult!.Card is ShenshenCard && e.CreationResult.Card.Rarity != CardRarity.Token && e.CreationResult.Card is not LightBlessing),
            "standard solo merchant fills all five character slots without tokens or multiplayer cards");

        await CardCmd.Discard(context, state.Hand.Cards.Where(c => c is not TransformationMagic).ToArray());
        var injury = combat.CreateCard<Injury>(player);
        var wound = combat.CreateCard<Wound>(player);
        await CardPileCmd.AddGeneratedCardToCombat(injury, PileType.Hand, player);
        await CardPileCmd.AddGeneratedCardToCombat(wound, PileType.Hand, player);
        await CardPileCmd.Draw(context, 3, player);
        state.Energy = 2;
        await CreatureCmd.GainBlock(player.Creature, 7, ValueProp.Unpowered, null);
        int hp = player.Creature.CurrentHp;
        await Switch(context, player);
        Check(state.Hand.Cards.Contains(injury) && state.Hand.Cards.Contains(wound), "curse and status exact instances stay in hand on transform");
        Check(state.Energy == 2 && player.Creature.Block == 7 && player.Creature.CurrentHp == hp, "transform preserves energy block and HP");
        var wrong = state.AllCards.OfType<ClawStrike>().First();
        await CardPileCmd.Add(wrong, PileType.Hand);
        Check(!wrong.CanPlay(out var reason, out _) && reason.HasFlag(UnplayableReason.BlockedByCardLogic), "directly selected inactive card cannot be manually played");
        foreach (var filler in handLimitFillers)
            await CardPileCmd.Add(filler, PileType.Exhaust);
        foreach (var card in state.AllCards.Where(c => c is ShenshenCard { Form: Shenshen.ShenshenCode.Character.ShenshenForm.Human }).ToArray())
            await CardPileCmd.Add(card, PileType.Exhaust);
        await CardPileCmd.Add(state.Hand.Cards.ToArray(), PileType.Exhaust);
        var dormant = state.DrawPile.Cards.Concat(state.DiscardPile.Cards).ToArray();
        Check(!(await CardPileCmd.Draw(context, 5, player)).Any() && dormant.SequenceEqual(state.DrawPile.Cards.Concat(state.DiscardPile.Cards)), "empty active deck never draws or shuffles dormant cards");

        await PowerCmd.Remove(player.Creature.GetPower<CatDodgePower>());
        player.Creature.GetPower<FormPower>()!.SetAmount(1);
        await Switch(context, player);
        await CreatureCmd.SetMaxAndCurrentHp(enemy, 1000);
        await CreatureCmd.LoseBlock(context, player.Creature, player.Creature.Block, player.Creature);
        int before = player.Creature.CurrentHp;
        await DamageCmd.Attack(1).FromMonster(enemy.Monster!).WithNoAttackerAnim().Execute(context);
        Check(player.Creature.CurrentHp == before - 1 && player.Creature.GetPower<CatDodgePower>()!.Amount == 1, "single enemy hit is not dodged and does not consume readiness");
        await PowerCmd.Apply<ThornsPower>(context, player.Creature, 6, player.Creature, null);
        await PowerCmd.Apply<ReflectPower>(context, player.Creature, 1, player.Creature, null);
        await CreatureCmd.GainBlock(player.Creature, 10, ValueProp.Unpowered, null);
        before = player.Creature.CurrentHp;
        int enemyHp = enemy.CurrentHp;
        var attack = await DamageCmd.Attack(10).FromMonster(enemy.Monster!).WithNoAttackerAnim().WithHitCount(2)
            .BeforeDamage(async () => await Task.Yield()).Execute(context);
        Check(player.Creature.CurrentHp == before && player.Creature.Block == 0 &&
            attack.Results.Count() == 2 && attack.Results.First().Count == 0 && attack.Results.Last().Count == 1,
            "multi-hit dodge survives async await and removes only the first reception");
        Check(enemy.CurrentHp == enemyHp - 16, "dodged segment triggers no thorns or reflect; blocked second hit triggers both");
        await Switch(context, player);
        await Switch(context, player);
        Check(player.Creature.GetPower<CatDodgePower>()!.Amount == 2, "same-turn transforms never refresh dodge");
        await Hook.BeforeHandDraw(combat, player, context);
        Check(player.Creature.GetPower<CatDodgePower>() == null,
            "next own hand draw removes the shift-turn dodge instead of refreshing it");

        player.Creature.GetPower<FormPower>()!.SetAmount(1);
        await PowerCmd.Apply<FluffedUpPower>(context, player.Creature, 3, player.Creature, null);
        int thorns = player.Creature.GetPower<ThornsPower>()!.Amount;
        await PowerCmd.Apply<WeakPower>(context, player.Creature, 3, enemy, null);
        Check(player.Creature.GetPower<ThornsPower>()!.Amount == thorns + 1 && player.Creature.GetPower<FluffedUpPower>()!.Amount == 3,
            "enemy multi-layer debuff awards one thorns without consuming first HP-loss reward");
        enemyHp = enemy.CurrentHp;
        await DamageCmd.Attack(2).FromMonster(enemy.Monster!).WithNoAttackerAnim().Execute(context);
        Check(player.Creature.GetPower<ThornsPower>()!.Amount == thorns + 4 && enemy.CurrentHp == enemyHp - (thorns + 1),
            "first enemy HP loss awards three thorns only after this hit retaliation");
        await Switch(context, player);
        await Hook.BeforeHandDraw(combat, player, context);
        await DamageCmd.Attack(2).FromMonster(enemy.Monster!).WithNoAttackerAnim().Execute(context);
        Check(player.Creature.GetPower<ThornsPower>()!.Amount == thorns + 5,
            "first HP-loss bonus never refreshes across form and turn hooks");
        await CreatureCmd.Damage(context, player.Creature, 1, ValueProp.Unpowered, player.Creature);
        await CreatureCmd.Damage(context, player.Creature, 1, ValueProp.Unpowered, null, null, null);
        Check(player.Creature.GetPower<ThornsPower>()!.Amount == thorns + 5, "self damage and poison-style HP loss do not trigger fluff");
        await PowerCmd.Apply<ArtifactPower>(context, player.Creature, 1, player.Creature, null);
        await PowerCmd.Apply<VulnerablePower>(context, player.Creature, 2, enemy, null);
        Check(player.Creature.GetPower<ThornsPower>()!.Amount == thorns + 5, "immune enemy debuff grants no thorns");

        var snapshot = NetFullCombatState.FromRun(run, null);
        var writer = new PacketWriter();
        snapshot.Serialize(writer);
        var reader = new PacketReader();
        reader.Reset(writer.Buffer);
        var restored = new NetFullCombatState();
        restored.Deserialize(reader);
        Check(restored.Creatures.Single(c => c.playerId == player.NetId).powers.Any(p => p.id == ModelDb.Power<FormPower>().Id && p.amount == 2),
            "full-combat packet roundtrip preserves form power");
        Check(restored.Players.Single().piles.Sum(p => p.cards.Count) == state.AllCards.Count(), "full-combat packet includes every owned combat card");

        RunManager.Instance.CleanUp(false);
        var heroine = Player.CreateForNewRun<Hero>(UnlockState.all, 1);
        var teammate = Player.CreateForNewRun<MegaCrit.Sts2.Core.Models.Characters.Ironclad>(UnlockState.all, 2);
        var duo = RunState.CreateForTest([heroine, teammate], seed: "SHENSHENDUO");
        RunManager.Instance.SetUpTest(duo, new MegaCrit.Sts2.Core.Multiplayer.NetSingleplayerGameService());
        MegaCrit.Sts2.Core.Context.LocalContext.NetId = 1;
        var duoCombat = new CombatState(runState: duo);
        duoCombat.AddPlayer(heroine);
        duoCombat.AddPlayer(teammate);
        var duoEnemy = duoCombat.CreateCreature(ModelDb.Monster<MegaCrit.Sts2.Core.Models.Monsters.TestSubject>().ToMutable(), CombatSide.Enemy, null);
        duoCombat.AddCreature(duoEnemy);
        CombatManager.Instance.SetUpCombat(duoCombat);
        var duoTurn = typeof(CombatManager).GetField("_turnState", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(CombatManager.Instance)!;
        duoTurn.GetType().GetProperty("IsInProgress")!.SetValue(duoTurn, true);
        duoTurn.GetType().GetProperty("IsStarting")!.SetValue(duoTurn, false);
        await Hook.BeforeHandDraw(duoCombat, heroine, context);
        var transform = heroine.PlayerCombatState!.Hand.Cards.OfType<TransformationMagic>().Single();
        Check(heroine.Creature.GetPower<CatDodgePower>() == null,
            "the human opening does not grant a cat-entry dodge");
        await CardPileCmd.Draw(context, 5, heroine, fromHandDraw: true);
        Check(heroine.PlayerCombatState.Hand.Cards.Count == 6 && transform.Keywords.Contains(CardKeyword.Retain) && transform.EnergyCost.Canonical == 1,
            "opening adds exactly one extra retained one-energy transformation before five normal draws");
        await Hook.BeforeHandDraw(duoCombat, heroine, context);
        Check(heroine.PlayerCombatState.AllCards.OfType<TransformationMagic>().Count() == 1, "later turn hook never generates duplicate transformation");
        await CardCmd.AutoPlay(context, transform, null);
        Check(heroine.PlayerCombatState.Hand.Cards.Contains(transform) && heroine.Creature.GetPower<FormPower>()!.Amount == 2 &&
            heroine.PlayerCombatState.Hand.Cards.OfType<ShenshenCard>().All(c => c.Form != Shenshen.ShenshenCode.Character.ShenshenForm.Human),
            "actual transformation card execution returns the same instance and exchanges the hand");
        var catForStock = duoCombat.CreateCard<ClawStrike>(teammate);
        await CardPileCmd.AddGeneratedCardToCombat(catForStock, PileType.Draw, teammate, CardPilePosition.Top);
        Check(ReferenceEquals((await CardPileCmd.Draw(context, 1, teammate)).Single(), catForStock), "other characters keep vanilla drawing even for a cat card");
        Check(CardCreationOptions.ForRoom(heroine, RoomType.Monster).GetPossibleCards(heroine).Any(c => c is LightBlessing),
            "two-player reward candidates include teammate blessing");
        // Keep the older direct card checks independent of the transformation bonuses.
        await PowerCmd.Remove(heroine.Creature.GetPower<StrengthPower>());
        await PowerCmd.Remove(heroine.Creature.GetPower<FormShiftPower>());
        await CreatureCmd.SetMaxAndCurrentHp(duoEnemy, 1000);
        int targetHp = duoEnemy.CurrentHp;
        await CardCmd.AutoPlay(context, duoCombat.CreateCard<ClawStrike>(heroine), duoEnemy);
        Check(duoEnemy.CurrentHp == targetHp - 6, "unshifted claw card executes three two-damage hits");
        await CardCmd.AutoPlay(context, duoCombat.CreateCard<MiDodge>(heroine), null);
        Check(heroine.Creature.Block == 7, "cat defense card grants exactly seven block");
        heroine.Creature.GetPower<FormPower>()!.SetAmount(1);
        targetHp = duoEnemy.CurrentHp;
        await CardCmd.AutoPlay(context, duoCombat.CreateCard<LittleFireball>(heroine), duoEnemy);
        Check(duoEnemy.CurrentHp == targetHp - 6, "fireball card executes six damage");
        await CardCmd.AutoPlay(context, duoCombat.CreateCard<LightShield>(heroine), null);
        Check(heroine.Creature.Block == 12, "girl defense card grants five additional block");
        targetHp = duoEnemy.CurrentHp;
        await CardCmd.AutoPlay(context, duoCombat.CreateCard<IDontLikeYou>(heroine), duoEnemy);
        Check(duoEnemy.CurrentHp == targetHp - 10 && heroine.Creature.GetPower<StrengthPower>()!.Amount == 3 && duoEnemy.GetPower<StrengthPower>()!.Amount == 1,
            "I don't like you executes original double-hit and both strength effects");
        await PowerCmd.Remove(heroine.Creature.GetPower<StrengthPower>()!);
        await PowerCmd.Remove(duoEnemy.GetPower<StrengthPower>()!);
        var upgradedDislike = duoCombat.CreateCard<IDontLikeYou>(heroine);
        upgradedDislike.UpgradeInternal();
        upgradedDislike.FinalizeUpgradeInternal();
        targetHp = duoEnemy.CurrentHp;
        await CardCmd.AutoPlay(context, upgradedDislike, duoEnemy);
        Check(duoEnemy.CurrentHp == targetHp - 12 && heroine.Creature.GetPower<StrengthPower>()!.Amount == 4 && duoEnemy.GetPower<StrengthPower>()!.Amount == 1,
            "upgraded I don't like you uses six-times-two and four own strength");
        await CreatureCmd.LoseBlock(context, heroine.Creature, heroine.Creature.Block, heroine.Creature);
        await CardCmd.AutoPlay(context, duoCombat.CreateCard<LightMirror>(heroine), null);
        Check(heroine.Creature.Block == 15 && heroine.Creature.GetPower<ReflectPower>() != null, "light mirror card grants fifteen block and reflection");
        await CardCmd.AutoPlay(context, duoCombat.CreateCard<LightBlessing>(heroine), heroine.Creature);
        Check(heroine.Creature.GetPower<LightBlessingPower>() == null, "blessing effect rejects a forced self target");
        await CardCmd.AutoPlay(context, duoCombat.CreateCard<LightBlessing>(heroine), teammate.Creature);
        Check(teammate.Creature.GetPower<LightBlessingPower>()?.Amount == 1, "blessing applies one selection layer to another teammate");
        await CardCmd.AutoPlay(context, duoCombat.CreateCard<LightBlessing>(heroine), teammate.Creature);
        Check(teammate.Creature.GetPower<LightBlessingPower>()?.Amount == 2 && heroine.Creature.GetPower<LightBlessingPower>() == null,
            "repeated blessing stacks only on the other teammate");
        heroine.Creature.GetPower<FormPower>()!.SetAmount(2);
        var duoSnapshot = NetFullCombatState.FromRun(duo, null);
        writer.Reset();
        duoSnapshot.Serialize(writer);
        reader.Reset(writer.Buffer);
        var duoRestored = new NetFullCombatState();
        duoRestored.Deserialize(reader);
        Check(duoRestored.Players.Count == 2 && duoRestored.Players.All(p => p.piles.Count == 5), "two-player full-combat packet roundtrip preserves both players and five tracked piles each");
        Check(duoRestored.Creatures.Single(c => c.playerId == heroine.NetId).powers.Any(p => p.id == ModelDb.Power<FormPower>().Id && p.amount == 2),
            "two-player packet roundtrip preserves heroine cat form independently");
        var permanentUpgrade = heroine.Deck.Cards.OfType<LittleFireball>().First();
        permanentUpgrade.UpgradeInternal();
        permanentUpgrade.FinalizeUpgradeInternal();
        var savedRun = RunManager.Instance.ToSave(null);
        var saveJson = MegaCrit.Sts2.Core.Saves.SaveManager.ToJson(savedRun);
        var parsedSave = MegaCrit.Sts2.Core.Saves.SaveManager.FromJson<SerializableRun>(saveJson);
        Check(parsedSave.Success && parsedSave.SaveData != null, "actual SerializableRun JSON save format roundtrip succeeds");
        // This combat fixture intentionally has no generated map/room sets. Reload its
        // real SerializablePlayer inventory rather than pretending to restore a complete run.
        var loadedHeroine = Player.FromSerializable(parsedSave.SaveData!.Players.Single(p => p.NetId == heroine.NetId));
        Check(loadedHeroine.Deck.Cards.Count == 19 && loadedHeroine.Deck.Cards.Count(c => c is LittleFireball or LightShield or LightMark) == 9 &&
            loadedHeroine.Deck.Cards.Count(c => c is ClawStrike or MiDodge or CatTailSweep) == 9 &&
            loadedHeroine.Deck.Cards.OfType<TransformationMagic>().Count() == 1 &&
            loadedHeroine.Deck.Cards.OfType<LittleFireball>().Count(c => c.IsUpgraded) == 1,
            "permanent save reload preserves both nine-card owned decks, the shared transformation and the original upgrade");
        File.WriteAllText(Path.Combine(Environment.GetEnvironmentVariable("SHENSHEN_REPO_ROOT")!, "work", "headless-tests", "roundtrip.save"), saveJson);
        await RunRareCardChecks();
        await RunDeckDesignChecks();
        await RunUpgradeAccessChecks();
        await RunCommonCardChecks();
        await RunFormFaceDodgeChecks();
        UiResourceChecks.Run(Check);
        await UiResourceChecks.RunLayout(this, Check);
        AnimationChecks.Run(this, Check);
    }

    private static Task Switch(PlayerChoiceContext context, Player player, bool upgraded = false) =>
        (Task)typeof(Hero).Assembly.GetType("Shenshen.ShenshenCode.Mechanics.FormState")!
            .GetMethod("Switch", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, [context, player, upgraded])!;

    private static void RegisterLoadedMod(string id, Assembly assembly)
    {
        var mods = (List<MegaCrit.Sts2.Core.Modding.Mod>)MegaCrit.Sts2.Core.Modding.ModManager.Mods;
        mods.Add(new MegaCrit.Sts2.Core.Modding.Mod
        {
            path = Path.Combine(Environment.GetEnvironmentVariable("SHENSHEN_REPO_ROOT")!, "dist", id),
            manifest = new() { id = id, affectsGameplay = true },
            state = MegaCrit.Sts2.Core.Modding.ModLoadState.Loaded
        });
        MegaCrit.Sts2.Core.Modding.ModManager.AssociateAssemblyWithMod(id, assembly);
    }

    private void Check(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException(name);
        _checks++;
        GD.Print("PASS " + name);
    }
}
