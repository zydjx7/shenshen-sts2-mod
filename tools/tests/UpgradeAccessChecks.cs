using System.Reflection;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.Unlocks;
using Shenshen.ShenshenCode.Cards;
using Shenshen.ShenshenCode.Powers;
using Hero = Shenshen.ShenshenCode.Character.Shenshen;

public partial class Harness
{
    private async Task RunUpgradeAccessChecks()
    {
        var (player, run, combat, enemy, context) = await CreateUpgradeFixture();
        var basic = combat.CreateCard<FluffedUp>(player);
        var upgraded = combat.CreateCard<FluffedUp>(player);
        upgraded.UpgradeInternal();
        upgraded.FinalizeUpgradeInternal();
        Check(basic.EnergyCost.Canonical == 1 && upgraded.EnergyCost.Canonical == 1 &&
            basic.DynamicVars["LaterThorns"].BaseValue == 0 && upgraded.DynamicVars["LaterThorns"].BaseValue == 1 &&
            basic.DynamicVars["DebuffThorns"].BaseValue == 0 && upgraded.DynamicVars["DebuffThorns"].BaseValue == 1,
            "fluff upgrade unlocks later-hit and debuff growth while both card versions remain one energy");
        var basicText = basic.GetDescriptionForPile(PileType.Hand);
        var previewText = basic.GetDescriptionForUpgradePreview();
        var upgradedText = upgraded.GetDescriptionForPile(PileType.Hand);
        Check(!basicText.Contains("{IfUpgraded") && !previewText.Contains("{IfUpgraded") &&
            !upgradedText.Contains("{IfUpgraded") && !basicText.Contains('0') &&
            !basicText.Contains("later enemy attack") && upgradedText.Contains("later enemy attack"),
            "fluff base, upgraded and upgrade-preview text formats without zero-value upgrade clauses");
        player.PlayerCombatState!.Energy = 3;
        await CardPileCmd.AddGeneratedCardToCombat(basic, PileType.Hand, player);
        await PlayPaid(basic, context);
        var fluff = player.Creature.GetPower<FluffedUpPower>()!;
        var basicPowerText = fluff.Description.GetFormattedText();
        Check(player.PlayerCombatState.Energy == 2 && fluff.Amount == 1 && !fluff.HasReceivedFirstReward && !fluff.IsGrowthEnabled,
            "actually playing basic fluff spends one energy and begins with only its unused first reward");
        CheckFluffPacket(run, player, 1, "base pending fluff flags survive the native combat packet");
        await PowerCmd.Apply<WeakPower>(context, player.Creature, 3, enemy, null);
        Check(player.Creature.GetPower<ThornsPower>() == null && fluff.Amount == 1,
            "basic fluff ignores enemy debuffs without consuming its first HP-loss reward");
        await DamageCmd.Attack(1).FromMonster(enemy.Monster!).WithNoAttackerAnim().Execute(context);
        Check(player.Creature.GetPower<ThornsPower>()?.Amount == 3 && fluff.Amount == 2 && fluff.HasReceivedFirstReward && !fluff.IsGrowthEnabled,
            "basic fluff grants exactly three thorns on its first enemy HP loss and records that reward");
        CheckFluffPacket(run, player, 2, "base spent fluff flags survive the native combat packet");
        await DamageCmd.Attack(1).FromMonster(enemy.Monster!).WithNoAttackerAnim().WithHitCount(2).Execute(context);
        await PowerCmd.Apply<VulnerablePower>(context, player.Creature, 2, enemy, null);
        Check(player.Creature.GetPower<ThornsPower>()?.Amount == 3 && fluff.Amount == 2,
            "later attack segments and later enemy debuffs do not grow basic fluff");
        await CardPileCmd.AddGeneratedCardToCombat(upgraded, PileType.Hand, player);
        await PlayPaid(upgraded, context);
        Check(player.PlayerCombatState.Energy == 1 && fluff.Amount == 4 && fluff.HasReceivedFirstReward && fluff.IsGrowthEnabled &&
            player.Creature.GetPower<ThornsPower>()?.Amount == 3,
            "playing upgraded fluff after a spent base copy costs one energy and unlocks growth without a second first reward");
        await DamageCmd.Attack(1).FromMonster(enemy.Monster!).WithNoAttackerAnim().Execute(context);
        Check(player.Creature.GetPower<ThornsPower>()?.Amount == 4 && fluff.Amount == 4,
            "the next enemy HP loss after upgrading grants one thorns rather than three");
        await PowerCmd.Apply<WeakPower>(context, player.Creature, 2, enemy, null);
        Check(player.Creature.GetPower<ThornsPower>()?.Amount == 5 && fluff.Amount == 4,
            "upgrading also unlocks one thorns per successful enemy debuff application");
        await CardCmd.AutoPlay(context, combat.CreateCard<FluffedUp>(player), null);
        Check(fluff.Amount == 4 && fluff.IsGrowthEnabled && fluff.HasReceivedFirstReward,
            "a later basic fluff copy neither downgrades an upgraded power nor resets its spent first reward");
        CheckFluffPacket(run, player, 4, "upgraded spent fluff flags survive the native combat packet");
        Check(basicPowerText.Contains("first enemy attack") && fluff.Description.GetFormattedText().Contains("Each later hit"),
            "live basic and upgraded fluff power descriptions expose their different growth rules");

        (player, run, combat, enemy, context) = await CreateUpgradeFixture();
        await CardCmd.AutoPlay(context, combat.CreateCard<FluffedUp>(player), null);
        var earlyUpgrade = combat.CreateCard<FluffedUp>(player);
        earlyUpgrade.UpgradeInternal();
        earlyUpgrade.FinalizeUpgradeInternal();
        await CardCmd.AutoPlay(context, earlyUpgrade, null);
        fluff = player.Creature.GetPower<FluffedUpPower>()!;
        Check(fluff.Amount == 3 && fluff.IsGrowthEnabled && !fluff.HasReceivedFirstReward,
            "upgrading an unused base fluff retains its pending combat-wide first reward");
        await CardCmd.AutoPlay(context, combat.CreateCard<FluffedUp>(player), null);
        Check(fluff.Amount == 3 && fluff.IsGrowthEnabled && !fluff.HasReceivedFirstReward,
            "a basic copy also cannot downgrade an upgraded power before the first HP loss");
        await PowerCmd.Apply<WeakPower>(context, player.Creature, 3, enemy, null);
        Check(player.Creature.GetPower<ThornsPower>()?.Amount == 1 && fluff.Amount == 3,
            "an upgraded pre-hit debuff awards one thorns without spending the pending three-thorns reward");
        CheckFluffPacket(run, player, 3, "upgraded pending fluff flags survive the native combat packet");
        await DamageCmd.Attack(1).FromMonster(enemy.Monster!).WithNoAttackerAnim().Execute(context);
        Check(player.Creature.GetPower<ThornsPower>()?.Amount == 4 && fluff.Amount == 4,
            "first HP loss after an early upgrade still awards the full three thorns");

        var ownedFluff = run.CreateCard<FluffedUp>(player);
        CardCmd.Upgrade(ownedFluff, CardPreviewStyle.None);
        await CardPileCmd.Add(ownedFluff, PileType.Deck);
        var save = SaveManager.FromJson<SerializableRun>(SaveManager.ToJson(RunManager.Instance.ToSave(null)));
        Check(save.Success && save.SaveData != null, "upgraded fluff inventory uses the real SerializableRun JSON format");
        var loaded = Player.FromSerializable(save.SaveData!.Players.Single(p => p.NetId == player.NetId));
        Check(loaded.Deck.Cards.Count == 20 && loaded.Deck.Cards.OfType<FluffedUp>().Single().IsUpgraded &&
            loaded.Deck.Cards.OfType<FluffedUp>().Single().EnergyCost.Canonical == 1,
            "real inventory save reload preserves an acquired upgraded fluff and its unchanged one-energy cost");
        await RunOrdinaryTransformationSmithChecks();
    }

    private async Task RunOrdinaryTransformationSmithChecks()
    {
        RunManager.Instance.CleanUp(false);
        var player = Player.CreateForNewRun<Hero>(UnlockState.all, 1);
        var run = RunState.CreateForTest([player], seed: "SHENSHENORDINARYSMITH");
        RunManager.Instance.SetUpTest(run, new MegaCrit.Sts2.Core.Multiplayer.NetSingleplayerGameService());
        MegaCrit.Sts2.Core.Context.LocalContext.NetId = 1;
        var permanentTransform = player.Deck.Cards.OfType<TransformationMagic>().Single();
        Check(HasStartingComposition(player) && permanentTransform.Rarity == CardRarity.Basic && !permanentTransform.IsUpgraded,
            "the unique transformation is a real unupgraded Basic card in the nineteen-card permanent inventory");
        var prefs = new CardSelectorPrefs(CardSelectorPrefs.UpgradeSelectionPrompt, 1)
        {
            Cancelable = true,
            RequireManualConfirmation = true
        };
        var selector = new TestCardSelector();
        using (CardSelectCmd.UseSelector(selector))
        {
            selector.PrepareToSelect(new[] { permanentTransform });
            var candidates = (await CardSelectCmd.FromDeckForUpgrade(player, prefs)).ToArray();
            Check(candidates.Length == 1 && ReferenceEquals(candidates[0], permanentTransform) && !permanentTransform.IsUpgraded,
                "the native deck-upgrade selector includes the actual permanent transformation without a proxy or automatic upgrade");
        }
        var synchronizer = RunManager.Instance.RestSiteSynchronizer;
        synchronizer.BeginRestSite();
        var options = synchronizer.GetLocalOptions().ToList();
        int smithIndex = options.FindIndex(option => option is SmithRestSiteOption);
        Check(options.Count == 2 && options.Any(option => option is HealRestSiteOption) && smithIndex >= 0,
            "a normal single-player camp has only its native rest and smith options, with no special transformation button");
        using (CardSelectCmd.UseSelector(selector))
        {
            selector.PrepareToSelect(Array.Empty<CardModel>());
            Check(!await synchronizer.ChooseLocalOption(smithIndex) && synchronizer.GetLocalOptions().Count == 2 && !permanentTransform.IsUpgraded,
                "canceling ordinary smithing leaves both camp actions available and the permanent transformation unchanged");
            selector.PrepareToSelect(new[] { permanentTransform });
            Check(await synchronizer.ChooseLocalOption(smithIndex) && permanentTransform.IsUpgraded &&
                synchronizer.GetLocalOptions().Count == 0 && HasStartingComposition(player) &&
                ReferenceEquals(player.Deck.Cards.OfType<TransformationMagic>().Single(), permanentTransform),
                "choosing the real transformation through ordinary smithing upgrades that permanent instance and consumes the camp action");
        }
        bool secondActionRejected = false;
        try
        {
            await synchronizer.ChooseLocalOption(smithIndex);
        }
        catch (InvalidOperationException)
        {
            secondActionRejected = true;
        }
        Check(secondActionRejected && !permanentTransform.IsUpgradable && permanentTransform.EnergyCost.Canonical == 1,
            "the completed camp rejects a second action and the upgraded transformation remains one energy");
        var save = SaveManager.FromJson<SerializableRun>(SaveManager.ToJson(RunManager.Instance.ToSave(null)));
        Check(save.Success && save.SaveData != null, "ordinary transformation smithing persists through the real run-save JSON format");
        var savedPlayer = save.SaveData!.Players.Single(p => p.NetId == player.NetId);
        var loaded = Player.FromSerializable(savedPlayer);
        Check(HasStartingComposition(loaded) && loaded.Deck.Cards.OfType<TransformationMagic>().Single().IsUpgraded,
            "native inventory reload restores the upgraded owned transformation without a custom progress field");
        var writer = new PacketWriter();
        savedPlayer.Serialize(writer);
        var reader = new PacketReader();
        reader.Reset(writer.Buffer);
        var packetPlayer = new SerializablePlayer();
        packetPlayer.Deserialize(reader);
        var packetLoaded = Player.FromSerializable(packetPlayer);
        Check(packetLoaded.Deck.Cards.OfType<TransformationMagic>().Single().IsUpgraded && HasStartingComposition(packetLoaded),
            "native player packet serialization retains the real upgraded transformation and all nineteen permanent cards");
        Check(Player.FromSerializable(savedPlayer.Anonymized()).Deck.Cards.OfType<TransformationMagic>().Single().IsUpgraded,
            "native anonymization retains the owned transformation's ordinary card upgrade");

        // Recreate an old eighteen-card payload by removing only this real card
        // at the matching native deck index; the production load patch migrates it.
        var legacyPlayerSave = player.ToSerializable();
        int transformationIndex = player.Deck.Cards.ToList().IndexOf(permanentTransform);
        legacyPlayerSave.Deck.RemoveAt(transformationIndex);
        var migrated = Player.FromSerializable(legacyPlayerSave);
        Check(HasStartingComposition(migrated) && !migrated.Deck.Cards.OfType<TransformationMagic>().Single().IsUpgraded,
            "loading a legacy eighteen-card inventory migrates exactly one owned base transformation");
        var syncTarget = Player.CreateForNewRun<Hero>(UnlockState.all, 1);
        _ = RunState.CreateForTest([syncTarget], seed: "SHENSHENORDINARYSMITH");
        syncTarget.SyncWithSerializedPlayer(packetPlayer);
        Check(HasStartingComposition(syncTarget) && syncTarget.Deck.Cards.OfType<TransformationMagic>().Single().IsUpgraded,
            "native player synchronization inherits the ordinary permanent-card upgrade");
        syncTarget.SyncWithSerializedPlayer(legacyPlayerSave);
        Check(HasStartingComposition(syncTarget) && !syncTarget.Deck.Cards.OfType<TransformationMagic>().Single().IsUpgraded,
            "synchronizing a legacy inventory migrates one base transformation without leaving duplicates");
        var otherCharacter = Player.CreateForNewRun<MegaCrit.Sts2.Core.Models.Characters.Ironclad>(UnlockState.all, 2);
        _ = RunState.CreateForTest([otherCharacter], seed: "SHENSHENOTHERHERO");
        Check(Player.FromSerializable(otherCharacter.ToSerializable()).Deck.Cards.OfType<TransformationMagic>().Count() == 0,
            "legacy migration does not add transformation cards to another character");

        RunManager.Instance.CleanUp(false);
        var reloadedRun = RunState.CreateForTest([loaded], seed: "SHENSHENUPGRADEDOPENING");
        RunManager.Instance.SetUpTest(reloadedRun, new MegaCrit.Sts2.Core.Multiplayer.NetSingleplayerGameService());
        MegaCrit.Sts2.Core.Context.LocalContext.NetId = 1;
        var combat = new CombatState(runState: reloadedRun);
        combat.AddPlayer(loaded);
        var enemy = combat.CreateCreature(ModelDb.Monster<MegaCrit.Sts2.Core.Models.Monsters.TestSubject>().ToMutable(), CombatSide.Enemy, null);
        combat.AddCreature(enemy);
        CombatManager.Instance.SetUpCombat(combat);
        var turn = typeof(CombatManager).GetField("_turnState", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(CombatManager.Instance)!;
        turn.GetType().GetProperty("IsInProgress")!.SetValue(turn, true);
        turn.GetType().GetProperty("IsStarting")!.SetValue(turn, false);
        var context = new ThrowingPlayerChoiceContext();
        var ownedCombatTransform = loaded.PlayerCombatState!.AllCards.OfType<TransformationMagic>().Single();
        await Hook.BeforeHandDraw(combat, loaded, context);
        await CardPileCmd.Draw(context, 5, loaded, fromHandDraw: true);
        var openingTransform = loaded.PlayerCombatState.Hand.Cards.OfType<TransformationMagic>().Single();
        Check(ReferenceEquals(ownedCombatTransform, openingTransform) && openingTransform.IsUpgraded &&
            loaded.PlayerCombatState.Hand.Cards.Count == 6 && loaded.PlayerCombatState.AllCards.Count() == 19 && HasStartingComposition(loaded),
            "the next opening moves the actual upgraded combat instance into hand before five draws without generating another card");
        await Hook.BeforeHandDraw(combat, loaded, context);
        Check(loaded.PlayerCombatState.AllCards.OfType<TransformationMagic>().Count() == 1,
            "later opening hooks never duplicate the owned transformation");
        await CardCmd.AutoPlay(context, openingTransform, null);
        Check(loaded.Creature.GetPower<StrengthPower>()?.Amount == 1 && loaded.Creature.GetPower<ThornsPower>()?.Amount == 1,
            "the normally smithed opening transformation actually executes its upgraded cat-entry bonuses");
    }

    private static async Task<(Player Player, RunState Run, CombatState Combat, Creature Enemy, ThrowingPlayerChoiceContext Context)> CreateUpgradeFixture()
    {
        RunManager.Instance.CleanUp(false);
        var player = Player.CreateForNewRun<Hero>(UnlockState.all, 1);
        var run = RunState.CreateForTest([player], seed: "SHENSHENUPGRADE");
        RunManager.Instance.SetUpTest(run, new MegaCrit.Sts2.Core.Multiplayer.NetSingleplayerGameService());
        MegaCrit.Sts2.Core.Context.LocalContext.NetId = 1;
        var combat = new CombatState(runState: run);
        combat.AddPlayer(player);
        var enemy = combat.CreateCreature(ModelDb.Monster<MegaCrit.Sts2.Core.Models.Monsters.TestSubject>().ToMutable(), CombatSide.Enemy, null);
        combat.AddCreature(enemy);
        CombatManager.Instance.SetUpCombat(combat);
        var turn = typeof(CombatManager).GetField("_turnState", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(CombatManager.Instance)!;
        turn.GetType().GetProperty("IsInProgress")!.SetValue(turn, true);
        turn.GetType().GetProperty("IsStarting")!.SetValue(turn, false);
        var context = new ThrowingPlayerChoiceContext();
        await CreatureCmd.SetMaxAndCurrentHp(enemy, 2000);
        await PowerCmd.Apply<FormPower>(context, player.Creature, 2, player.Creature, null);
        return (player, run, combat, enemy, context);
    }

    private void CheckFluffPacket(RunState run, Player player, int expectedAmount, string name)
    {
        var snapshot = NetFullCombatState.FromRun(run, null);
        var writer = new PacketWriter();
        snapshot.Serialize(writer);
        var reader = new PacketReader();
        reader.Reset(writer.Buffer);
        var restored = new NetFullCombatState();
        restored.Deserialize(reader);
        var restoredPower = restored.Creatures.Single(c => c.playerId == player.NetId).powers
            .Single(p => p.id == ModelDb.Power<FluffedUpPower>().Id);
        var live = player.Creature.GetPower<FluffedUpPower>()!;
        Check(restoredPower.amount == expectedAmount && restoredPower.amount == live.Amount &&
            live.IsGrowthEnabled == (expectedAmount is 3 or 4) && live.HasReceivedFirstReward == (expectedAmount is 2 or 4), name);
    }

}
