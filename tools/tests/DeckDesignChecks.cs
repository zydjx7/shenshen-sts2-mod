using System.Reflection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.Unlocks;
using MegaCrit.Sts2.Core.ValueProps;
using Shenshen.ShenshenCode.Cards;
using Shenshen.ShenshenCode.Character;
using Shenshen.ShenshenCode.Powers;
using Hero = Shenshen.ShenshenCode.Character.Shenshen;

public partial class Harness
{
    private async Task RunDeckDesignChecks()
    {
        RunManager.Instance.CleanUp(false);
        var heroine = Player.CreateForNewRun<Hero>(UnlockState.all, 1);
        var teammate = Player.CreateForNewRun<MegaCrit.Sts2.Core.Models.Characters.Ironclad>(UnlockState.all, 2);
        var run = RunState.CreateForTest([heroine, teammate], seed: "SHENSHENDECK");
        RunManager.Instance.SetUpTest(run, new MegaCrit.Sts2.Core.Multiplayer.NetSingleplayerGameService());
        MegaCrit.Sts2.Core.Context.LocalContext.NetId = 1;
        var combat = new CombatState(runState: run);
        combat.AddPlayer(heroine);
        combat.AddPlayer(teammate);
        var enemy = combat.CreateCreature(ModelDb.Monster<MegaCrit.Sts2.Core.Models.Monsters.TestSubject>().ToMutable(), CombatSide.Enemy, null);
        var otherEnemy = combat.CreateCreature(ModelDb.Monster<MegaCrit.Sts2.Core.Models.Monsters.TestSubject>().ToMutable(), CombatSide.Enemy, null);
        combat.AddCreature(enemy);
        combat.AddCreature(otherEnemy);
        CombatManager.Instance.SetUpCombat(combat);
        var turn = typeof(CombatManager).GetField("_turnState", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(CombatManager.Instance)!;
        turn.GetType().GetProperty("IsInProgress")!.SetValue(turn, true);
        turn.GetType().GetProperty("IsStarting")!.SetValue(turn, false);
        var context = new ThrowingPlayerChoiceContext();
        await CreatureCmd.SetMaxAndCurrentHp(enemy, 2000);
        await CreatureCmd.SetMaxAndCurrentHp(otherEnemy, 2000);
        var state = heroine.PlayerCombatState!;

        Check(HasStartingComposition(heroine), "starting inventory owns exactly four attacks, four defenses and one signature per form");
        Check(heroine.Deck.Cards.Count(c => c is ShenshenCard { Form: ShenshenForm.Human }) == 9 &&
            heroine.Deck.Cards.Count(c => c is ShenshenCard { Form: ShenshenForm.Cat }) == 9 &&
            heroine.Deck.Cards.OfType<TransformationMagic>().Count() == 1,
            "both nine-card inventories and one shared transformation are permanently owned");

        void CheckUpgrade<T>(params (string Name, decimal Base, decimal Upgraded)[] values) where T : CardModel
        {
            var card = combat.CreateCard<T>(heroine);
            int cost = card.EnergyCost.Canonical;
            Check(values.All(v => card.DynamicVars[v.Name].BaseValue == v.Base), typeof(T).Name + " base values");
            card.UpgradeInternal();
            card.FinalizeUpgradeInternal();
            Check(card.IsUpgraded && card.EnergyCost.Canonical == cost &&
                values.All(v => card.DynamicVars[v.Name].BaseValue == v.Upgraded), typeof(T).Name + " upgraded values and unchanged cost");
        }
        CheckUpgrade<LittleFireball>(("Damage", 6, 9));
        CheckUpgrade<LightShield>(("Block", 5, 8));
        CheckUpgrade<ClawStrike>(("Damage", 2, 3), ("Repeat", 3, 3));
        CheckUpgrade<MiDodge>(("Block", 7, 10));
        CheckUpgrade<LightMark>(("Damage", 4, 4), ("BurnDamage", 1, 2), ("Turns", 2, 3));
        CheckUpgrade<CatTailSweep>(("Damage", 2, 3), ("Repeat", 2, 2), ("Block", 6, 8));
        CheckUpgrade<IDontLikeYou>(("Damage", 5, 6), ("Repeat", 2, 2), ("StrengthPower", 3, 4), ("EnemyStrength", 1, 1));
        CheckUpgrade<LightMirror>(("Block", 15, 20));
        CheckUpgrade<LightBlessing>(("Cards", 1, 2));

        await Hook.BeforeHandDraw(combat, heroine, context);
        var transform = state.Hand.Cards.OfType<TransformationMagic>().Single();
        Check(transform.EnergyCost.Canonical == 1 && transform.DynamicVars.Strength.BaseValue == 1 &&
            transform.DynamicVars["DexterityPower"].BaseValue == 1 && transform.DynamicVars["ThornsPower"].BaseValue == 0 &&
            transform.DynamicVars.Block.BaseValue == 0,
            "the unique owned transformation has its one-energy base bonuses");
        var baseText = transform.GetDescriptionForPile(PileType.Hand);
        var upgradePreviewText = transform.GetDescriptionForUpgradePreview();
        Check(baseText.Contains("Strength") && baseText.Contains("Dexterity") && !baseText.Contains("Thorns") &&
            !baseText.Contains("fixed") && !upgradePreviewText.Contains("{IfUpgraded"),
            "base transformation hides zero upgrade bonuses and the native upgrade-preview formatter resolves successfully");
        var owned = state.AllCards.ToArray();
        var retainedUpgrade = owned.OfType<LittleFireball>().First();
        retainedUpgrade.UpgradeInternal();
        retainedUpgrade.FinalizeUpgradeInternal();
        retainedUpgrade.EnergyCost.SetThisCombat(0);
        await CardPileCmd.Draw(context, 5, heroine, fromHandDraw: true);
        state.Energy = 4;
        await CreatureCmd.GainBlock(heroine.Creature, 5, ValueProp.Unpowered, null);
        int hp = heroine.Creature.CurrentHp;
        await PlayPaid(transform, context);
        Check(state.Energy == 3 && state.Hand.Cards.Contains(transform) && heroine.Creature.CurrentHp == hp && heroine.Creature.Block == 5,
            "manual base transformation spends one energy and preserves its instance, HP and existing block");
        Check(heroine.Creature.GetPower<StrengthPower>()?.Amount == 1 && heroine.Creature.GetPower<DexterityPower>() == null &&
            heroine.Creature.GetPower<ThornsPower>() == null && heroine.Creature.GetPower<FormPower>()?.Amount == 2 &&
            heroine.Creature.GetPower<FormShiftPower>()?.Amount == 1,
            "base entry into cat grants one strength and records this turn's shift without upgraded bonuses");
        Check(owned.ToHashSet().SetEquals(state.AllCards) && retainedUpgrade.IsUpgraded &&
            retainedUpgrade.EnergyCost.GetWithModifiers(CostModifiers.Local) == 0 &&
            state.Hand.Cards.OfType<ShenshenCard>().All(c => c.Form != ShenshenForm.Human),
            "paid transformation exchanges owned instances without losing upgrades or combat costs");
        await PlayPaid(transform, context);
        Check(state.Energy == 2 && heroine.Creature.GetPower<DexterityPower>()?.Amount == 1 &&
            heroine.Creature.GetPower<StrengthPower>()?.Amount == 1 && heroine.Creature.Block == 5,
            "base return to girl adds one dexterity, retains strength and gives no extra block");
        transform.UpgradeInternal();
        transform.FinalizeUpgradeInternal();
        Check(transform.EnergyCost.Canonical == 1 && transform.DynamicVars.Strength.BaseValue == 1 &&
            transform.DynamicVars["DexterityPower"].BaseValue == 1 && transform.DynamicVars["ThornsPower"].BaseValue == 1 &&
            transform.DynamicVars.Block.BaseValue == 4,
            "upgrading the same owned combat transformation unlocks only thorns and fixed block");
        var upgradedText = transform.GetDescriptionForPile(PileType.Hand);
        Check(upgradedText.Contains("Thorns") && upgradedText.Contains("fixed") && !upgradedText.Contains("{IfUpgraded"),
            "upgraded transformation text displays the thorns and fixed-block clauses through native formatting");
        transform.EnergyCost.SetThisCombat(0);
        await PlayPaid(transform, context);
        Check(state.Energy == 2 && heroine.Creature.GetPower<StrengthPower>()?.Amount == 2 &&
            heroine.Creature.GetPower<ThornsPower>()?.Amount == 1 && state.Hand.Cards.Contains(transform),
            "upgraded cat entry adds one strength and one thorns while retaining the same zero-cost instance");
        await PlayPaid(transform, context);
        Check(heroine.Creature.GetPower<DexterityPower>()?.Amount == 2 && heroine.Creature.Block == 9 &&
            heroine.Creature.GetPower<StrengthPower>()?.Amount == 2 && heroine.Creature.GetPower<ThornsPower>()?.Amount == 1,
            "upgraded girl entry adds one dexterity and exactly four unpowered block despite existing dexterity");

        await PowerCmd.Remove(heroine.Creature.GetPower<StrengthPower>());
        await PowerCmd.Remove(heroine.Creature.GetPower<DexterityPower>());
        await PowerCmd.Remove(heroine.Creature.GetPower<ThornsPower>());
        heroine.Creature.GetPower<FormPower>()!.SetAmount(2);
        var claw = combat.CreateCard<ClawStrike>(heroine);
        await CardPileCmd.AddGeneratedCardToCombat(claw, PileType.Hand, heroine);
        claw.UpdateDynamicVarPreview(CardPreviewMode.Normal, enemy, claw.DynamicVars);
        Check(claw.DynamicVars.Repeat.PreviewValue == 4, "claw preview shows four hits after a real shift");
        int targetHp = enemy.CurrentHp;
        await CardCmd.AutoPlay(context, claw, enemy);
        Check(enemy.CurrentHp == targetHp - 8, "shifted base claw executes four two-damage hits");
        var upgradedClaw = combat.CreateCard<ClawStrike>(heroine);
        upgradedClaw.UpgradeInternal();
        upgradedClaw.FinalizeUpgradeInternal();
        targetHp = enemy.CurrentHp;
        await CardCmd.AutoPlay(context, upgradedClaw, enemy);
        Check(enemy.CurrentHp == targetHp - 12, "shifted upgraded claw executes four three-damage hits");
        await heroine.Creature.GetPower<FormShiftPower>()!.BeforeHandDraw(teammate, context, combat);
        Check(heroine.Creature.GetPower<FormShiftPower>() != null, "another player's hand-draw hook preserves the shift bonus");
        await Hook.BeforeHandDraw(combat, heroine, context);
        claw.UpdateDynamicVarPreview(CardPreviewMode.Normal, enemy, claw.DynamicVars);
        Check(heroine.Creature.GetPower<FormShiftPower>() == null && claw.DynamicVars.Repeat.PreviewValue == 3,
            "next own hand draw removes the shift and returns the claw preview to three hits");
        targetHp = enemy.CurrentHp;
        await CardCmd.AutoPlay(context, claw, enemy);
        Check(enemy.CurrentHp == targetHp - 6, "base claw returns to three hits on the next own turn");
        targetHp = enemy.CurrentHp;
        await CardCmd.AutoPlay(context, upgradedClaw, enemy);
        Check(enemy.CurrentHp == targetHp - 9, "upgraded claw returns to three hits on the next own turn");

        await CreatureCmd.LoseBlock(context, heroine.Creature, heroine.Creature.Block, heroine.Creature);
        targetHp = enemy.CurrentHp;
        await CardCmd.AutoPlay(context, combat.CreateCard<CatTailSweep>(heroine), enemy);
        Check(enemy.CurrentHp == targetHp - 4 && heroine.Creature.Block == 6, "base tail sweep deals two two-damage hits then grants six block");
        await CreatureCmd.LoseBlock(context, heroine.Creature, heroine.Creature.Block, heroine.Creature);
        var upgradedTail = combat.CreateCard<CatTailSweep>(heroine);
        upgradedTail.UpgradeInternal();
        upgradedTail.FinalizeUpgradeInternal();
        targetHp = enemy.CurrentHp;
        await CardCmd.AutoPlay(context, upgradedTail, enemy);
        Check(enemy.CurrentHp == targetHp - 6 && heroine.Creature.Block == 8, "upgraded tail sweep deals two three-damage hits then grants eight block");

        heroine.Creature.GetPower<FormPower>()!.SetAmount(1);
        var markCard = combat.CreateCard<LightMark>(heroine);
        targetHp = enemy.CurrentHp;
        await CardCmd.AutoPlay(context, markCard, enemy);
        Check(enemy.CurrentHp == targetHp - 4 && MarkOn(enemy, heroine.Creature)?.Amount == 21,
            "base light mark deals its own four damage before applying one burn for two own turns");
        heroine.Creature.GetPower<FormPower>()!.SetAmount(2);
        targetHp = enemy.CurrentHp;
        await CardCmd.AutoPlay(context, claw, enemy);
        Check(enemy.CurrentHp == targetHp - 9, "mark burns separately after each of three claw hits and survives changing form");
        heroine.Creature.GetPower<FormPower>()!.SetAmount(1);
        var upgradedMark = combat.CreateCard<LightMark>(heroine);
        upgradedMark.UpgradeInternal();
        upgradedMark.FinalizeUpgradeInternal();
        targetHp = otherEnemy.CurrentHp;
        await CardCmd.AutoPlay(context, upgradedMark, otherEnemy);
        Check(otherEnemy.CurrentHp == targetHp - 4 && MarkOn(otherEnemy, heroine.Creature)?.Amount == 32,
            "fresh upgraded light mark also applies after its own damage, with two burn and three own turns");
        targetHp = enemy.CurrentHp;
        await CardCmd.AutoPlay(context, upgradedMark, enemy);
        Check(enemy.CurrentHp == targetHp - 5 && MarkOn(enemy, heroine.Creature)?.Amount == 32,
            "replacing a base mark triggers only its old one burn before upgrading the existing mark");
        targetHp = enemy.CurrentHp;
        await CardCmd.AutoPlay(context, combat.CreateCard<StrikeIronclad>(teammate), enemy);
        Check(enemy.CurrentHp == targetHp - 6, "a teammate's attack does not consume another player's light mark");
        targetHp = enemy.CurrentHp;
        // FromMonster fixes AttackCommand targets to its opponents. The underlying
        // real damage command lets this source-filter fixture supply a monster
        // dealer while retaining the same powered-attack damage classification.
        var enemyDamage = (await CreatureCmd.Damage(context, enemy, 2, ValueProp.Move, otherEnemy)).ToArray();
        Check(enemyDamage.Any(result => result.Props.IsPoweredAttack()) && enemy.CurrentHp == targetHp - 2,
            "monster-dealt powered attack damage cannot trigger the heroine's light mark");
        var allySource = combat.CreateCard<StrikeIronclad>(teammate);
        await LightMarkPower.ApplyMark(context, enemy, teammate.Creature, 1, 2, allySource);
        targetHp = enemy.CurrentHp;
        await CardCmd.AutoPlay(context, allySource, enemy);
        Check(enemy.CurrentHp == targetHp - 7 && enemy.GetPowerInstances(ModelDb.Power<LightMarkPower>().Id).Count() == 2,
            "two appliers own separate marks and an ally attack triggers only the ally's mark");
        await PowerCmd.Remove(MarkOn(enemy, teammate.Creature));

        await PowerCmd.Apply<StrengthPower>(context, heroine.Creature, 1, heroine.Creature, null);
        await PowerCmd.Apply<VulnerablePower>(context, enemy, 1, heroine.Creature, null);
        heroine.Creature.GetPower<FormPower>()!.SetAmount(2);
        targetHp = enemy.CurrentHp;
        await CardCmd.AutoPlay(context, claw, enemy);
        Check(enemy.CurrentHp == targetHp - 18, "each burn remains fixed at two despite attack strength and target vulnerability");
        targetHp = enemy.CurrentHp;
        await CreatureCmd.Damage(context, enemy, 2, ValueProp.Unpowered, heroine.Creature);
        Check(enemy.CurrentHp == targetHp - 2, "unpowered damage bypasses attack scaling and never recursively triggers a mark");
        await PowerCmd.Remove(heroine.Creature.GetPower<StrengthPower>());
        await PowerCmd.Remove(enemy.GetPower<VulnerablePower>());
        await CreatureCmd.GainBlock(enemy, 10, ValueProp.Unpowered, null);
        heroine.Creature.GetPower<FormPower>()!.SetAmount(1);
        targetHp = enemy.CurrentHp;
        await CardCmd.AutoPlay(context, combat.CreateCard<LittleFireball>(heroine), enemy);
        Check(enemy.CurrentHp == targetHp && enemy.Block == 2,
            "a fully blocked attack still triggers fixed burn, and that burn respects the remaining block");
        await CreatureCmd.LoseBlock(context, enemy, enemy.Block, enemy);

        var mark = MarkOn(enemy, heroine.Creature)!;
        await mark.AfterPlayerTurnStart(context, teammate);
        Check(mark.Amount == 32, "another player's turn does not reduce light mark duration");
        await CardCmd.AutoPlay(context, markCard, enemy);
        Check(mark.Amount == 32, "a weaker repeat mark neither stacks burn nor shortens upgraded duration");
        await mark.AfterPlayerTurnStart(context, heroine);
        await mark.AfterPlayerTurnStart(context, heroine);
        Check(mark.Amount == 12, "upgraded mark still exists with one turn remaining after two own turn starts");
        await CardCmd.AutoPlay(context, markCard, enemy);
        Check(mark.Amount == 22, "base recast refreshes to two turns while retaining the stronger existing burn");
        await mark.AfterPlayerTurnStart(context, heroine);
        await mark.AfterPlayerTurnStart(context, heroine);
        Check(MarkOn(enemy, heroine.Creature) == null, "refreshed two-turn mark expires after exactly two more own turn starts");
        await CardCmd.AutoPlay(context, markCard, enemy);
        mark = MarkOn(enemy, heroine.Creature)!;
        await mark.AfterPlayerTurnStart(context, heroine);
        Check(mark.Amount == 11, "base mark lasts into the next own turn with one burn");
        await mark.AfterPlayerTurnStart(context, heroine);
        Check(MarkOn(enemy, heroine.Creature) == null, "base mark expires at the second own turn start");
        var otherMark = MarkOn(otherEnemy, heroine.Creature)!;
        await otherMark.AfterPlayerTurnStart(context, heroine);
        await otherMark.AfterPlayerTurnStart(context, heroine);
        Check(otherMark.Amount == 12, "upgraded mark retains its two burn until the third own turn");
        await otherMark.AfterPlayerTurnStart(context, heroine);
        Check(MarkOn(otherEnemy, heroine.Creature) == null, "upgraded mark expires at the third own turn start");

        await CardCmd.AutoPlay(context, upgradedMark, enemy);
        await PlayPaid(transform, context);
        var snapshot = NetFullCombatState.FromRun(run, null);
        var writer = new PacketWriter();
        snapshot.Serialize(writer);
        var reader = new PacketReader();
        reader.Reset(writer.Buffer);
        var restored = new NetFullCombatState();
        restored.Deserialize(reader);
        var playerPowers = restored.Creatures.Single(c => c.playerId == heroine.NetId).powers;
        Check(playerPowers.Any(p => p.id == ModelDb.Power<FormPower>().Id && p.amount == 2) &&
            playerPowers.Any(p => p.id == ModelDb.Power<FormShiftPower>().Id && p.amount == 1),
            "real combat packet preserves both cat form and this-turn shift counters");
        int restoredMarkAmount = restored.Creatures.SelectMany(c => c.powers).Single(p => p.id == ModelDb.Power<LightMarkPower>().Id).amount;
        var liveMark = MarkOn(enemy, heroine.Creature)!;
        Check(restoredMarkAmount == liveMark.Amount && liveMark.BurnDamage == 2 && liveMark.RemainingTurns == 3,
            "real combat packet amount retains both mark burn and remaining turns without custom fields");
        Check(restored.Players.Single(p => p.playerId == heroine.NetId).piles.Sum(p => p.cards.Count) == state.AllCards.Count(),
            "real combat packet includes all owned form cards and generated combat cards");

        foreach (var card in heroine.Deck.Cards.Where(c => c is LightMark or CatTailSweep))
        {
            card.UpgradeInternal();
            card.FinalizeUpgradeInternal();
        }
        var saved = SaveManager.FromJson<SerializableRun>(SaveManager.ToJson(RunManager.Instance.ToSave(null)));
        var loaded = Player.FromSerializable(saved.SaveData!.Players.Single(p => p.NetId == heroine.NetId));
        Check(saved.Success && HasStartingComposition(loaded) && loaded.Deck.Cards.OfType<LightMark>().Single().IsUpgraded &&
            loaded.Deck.Cards.OfType<CatTailSweep>().Single().IsUpgraded,
            "real run-save inventory roundtrip preserves both nine-card decks and both upgraded signature instances");
    }

    private static LightMarkPower? MarkOn(Creature target, Creature applier) =>
        PowerCmd.FindExistingInstanceForStacking(ModelDb.Power<LightMarkPower>(), target, applier) as LightMarkPower;

    private static bool HasStartingComposition(Player player) =>
        player.Deck.Cards.Count == 19 && player.Deck.Cards.OfType<TransformationMagic>().Count() == 1 &&
        player.Deck.Cards.OfType<LittleFireball>().Count() == 4 &&
        player.Deck.Cards.OfType<LightShield>().Count() == 4 && player.Deck.Cards.OfType<LightMark>().Count() == 1 &&
        player.Deck.Cards.OfType<ClawStrike>().Count() == 4 && player.Deck.Cards.OfType<MiDodge>().Count() == 4 &&
        player.Deck.Cards.OfType<CatTailSweep>().Count() == 1;

    private static async Task PlayPaid(CardModel card, PlayerChoiceContext context)
    {
        var (energy, stars) = await card.SpendResources();
        await card.OnPlayWrapper(context, null, isAutoPlay: false, new ResourceInfo
        {
            EnergySpent = energy, EnergyValue = energy, StarsSpent = stars, StarValue = stars
        }, skipCardPileVisuals: true);
    }
}
