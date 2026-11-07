using System.Reflection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.Unlocks;
using MegaCrit.Sts2.Core.ValueProps;
using Shenshen.ShenshenCode.CardPools;
using Shenshen.ShenshenCode.Cards;
using Shenshen.ShenshenCode.Character;
using Shenshen.ShenshenCode.Powers;

public partial class Harness
{
    private async Task RunCardBalanceChecks()
    {
        var pool = ModelDb.CardPool<ShenshenCardPool>();
        var solo = pool.GetUnlockedCards(UnlockState.all, CardMultiplayerConstraint.SingleplayerOnly).ToArray();
        var multi = pool.GetUnlockedCards(UnlockState.all, CardMultiplayerConstraint.MultiplayerOnly).ToArray();
        Check(solo.Count(c => c.Rarity == CardRarity.Common) == 20 &&
            solo.Count(c => c.Rarity == CardRarity.Uncommon) == 41 &&
            solo.Count(c => c.Rarity == CardRarity.Rare) == 35,
            "the revised ordinary solo pool contains 20 commons, 41 uncommons and 35 rares");
        var retired = new CardModel[] { ModelDb.Card<LightShadowHandoff>(), ModelDb.Card<ShatteredMirrorFlames>(),
            ModelDb.Card<MoltenLightPierce>(), ModelDb.Card<Kneading>(), ModelDb.Card<Teasing>(), ModelDb.Card<BurnTheMark>() };
        Check(retired.All(c => !solo.Contains(c) && !multi.Contains(c)),
            "all six retired cards are excluded from solo and multiplayer unlocked reward/generation pools");
        var f = await CreateCommonFixture();
        foreach (var canonical in retired)
        {
            var card = f.Combat.CreateCard(canonical, f.Player);
            CardCmd.Upgrade(card);
            var restored = CardModel.FromSerializable(card.ToSerializable());
            Check(restored.Id == canonical.Id && restored.IsUpgraded && restored.Pool == pool,
                canonical.GetType().Name + " retains its model identity, upgrade and pool lookup for old saves");
        }
        Check(new CardModel[] { ModelDb.Card<CrownOfThorns>(), ModelDb.Card<DayNightCycle>(),
            ModelDb.Card<SilentCasting>() }.All(c => c.Rarity == CardRarity.Uncommon && solo.Contains(c)),
            "the three approved rarity downgrades remain in the formal uncommon pool");
        var trace = CommonCard<LightShadowTrace>(f);
        Check(trace.Form == null && trace.Rarity == CardRarity.Uncommon && trace.Type == CardType.Power &&
            trace.EnergyCost.Canonical == 1 && trace.DynamicVars.Cards.IntValue == 1 && solo.Contains(trace.CanonicalInstance),
            "Light and Shadow Traces occupies a shared one-cost uncommon power slot with one-card retention");
        CheckCommonCardDescriptions(f, [ModelDb.Card<LightShadowTrace>(), ModelDb.Card<ReturningClaw>(), ModelDb.Card<RoomwidePounce>()]);
        await CheckRevisedAttacks();
        await CheckLightShadowRetention();
        await CheckLegacyOwnedDeck();
    }

    private async Task CheckLegacyOwnedDeck()
    {
        var f = await CreateCommonFixture();
        var originalFourthCopies = new CardModel[] { ModelDb.Card<LittleFireball>(), ModelDb.Card<LightShield>(),
            ModelDb.Card<ClawStrike>(), ModelDb.Card<MiDodge>(), ModelDb.Card<BurnTheMark>() };
        foreach (var canonical in originalFourthCopies)
        {
            var card = f.Run.CreateCard(canonical, f.Player);
            if (card is BurnTheMark) CardCmd.Upgrade(card);
            await CardPileCmd.Add(card, PileType.Deck);
        }
        var saved = SaveManager.FromJson<SerializableRun>(SaveManager.ToJson(RunManager.Instance.ToSave(null)));
        var loaded = MegaCrit.Sts2.Core.Entities.Players.Player.FromSerializable(saved.SaveData!.Players.Single(p => p.NetId == f.Player.NetId));
        Check(saved.Success && loaded.Deck.Cards.Count == 20 &&
            loaded.Deck.Cards.OfType<LittleFireball>().Count() == 4 && loaded.Deck.Cards.OfType<LightShield>().Count() == 4 &&
            loaded.Deck.Cards.OfType<ClawStrike>().Count() == 4 && loaded.Deck.Cards.OfType<MiDodge>().Count() == 4 &&
            loaded.Deck.Cards.OfType<BurnTheMark>().Single().IsUpgraded,
            "native run saves preserve an old nineteen-card starter deck plus its upgraded retired Burn the Mark without deleting cards");
    }

    private async Task CheckRevisedAttacks()
    {
        var f = await CreateCommonFixture(ShenshenForm.Cat);
        var undrawn = await CommonDrawCard<ClawStrike>(f);
        foreach (var upgraded in new[] { false, true })
        {
            int hp = f.Enemy.CurrentHp;
            await PlayRare<ReturningClaw>(f, f.Enemy, upgraded);
            Check(hp - f.Enemy.CurrentHp == (upgraded ? 12 : 8) && undrawn.Pile?.Type == PileType.Draw,
                "Returning Claw" + (upgraded ? "+" : "") + " executes its two hits without an unconditional draw");
        }
        await PowerCmd.Apply<StrengthPower>(f.Context, f.Player.Creature, 5, f.Player.Creature, null);
        int before = f.Enemy.CurrentHp;
        await PlayRare<ReturningClaw>(f, f.Enemy);
        Check(before - f.Enemy.CurrentHp == 18, "Returning Claw applies Strength separately to both hits");
        await PowerCmd.Remove(f.Player.Creature.GetPower<StrengthPower>());
        await LightMarkPower.ApplyMark(f.Context, f.Enemy, f.Player.Creature, 1, 2, CommonCard<LightMark>(f));
        before = f.Enemy.CurrentHp;
        await PlayRare<ReturningClaw>(f, f.Enemy, true);
        Check(before - f.Enemy.CurrentHp == 14, "both upgraded Returning Claw hits trigger the owner's Light Mark");
        await PlayRare<ShatteredMirror>(f);
        f.Combat.CurrentSide = CombatSide.Enemy;
        await CommonEnemyAttack(f, 1);
        await Hook.AfterSideTurnEnd(f.Combat, CombatSide.Enemy, [f.Enemy, f.OtherEnemy]);
        f.Combat.CurrentSide = CombatSide.Player;
        var secondDraw = await CommonDrawCard<MiDodge>(f);
        await PlayRare<ReturningClaw>(f, f.Enemy);
        Check(undrawn.Pile?.Type == PileType.Hand && secondDraw.Pile?.Type == PileType.Hand,
            "Returning Claw still draws two real cards after reflection during the preceding enemy turn");

        f = await CreateCommonFixture(ShenshenForm.Cat);
        await PowerCmd.Apply<DexterityPower>(f.Context, f.Player.Creature, 20, f.Player.Creature, null);
        await LightMarkPower.ApplyMark(f.Context, f.Enemy, f.Player.Creature, 1, 2, CommonCard<LightMark>(f));
        foreach (var upgraded in new[] { false, true })
        {
            int first = f.Enemy.CurrentHp;
            int second = f.OtherEnemy.CurrentHp;
            var card = await PlayRare<RoomwidePounce>(f, upgraded: upgraded);
            Check(first - f.Enemy.CurrentHp == (upgraded ? 15 : 12) &&
                second - f.OtherEnemy.CurrentHp == (upgraded ? 12 : 9) && f.Player.Creature.Block == 0 && !card.GainsBlock,
                "Roomwide Pounce" + (upgraded ? "+" : "") + " attacks both enemies three times and grants no Block despite marks and Dexterity");
        }
    }

    private async Task CheckLightShadowRetention()
    {
        var f = await CreateCommonFixture();
        await PlayRare<LightShadowTrace>(f);
        var original = CommonCard<LittleFireball>(f);
        await CardPileCmd.AddGeneratedCardToCombat(original, PileType.Hand, f.Player);
        var selector = new TestCardSelector();
        using (CardSelectCmd.UseSelector(selector))
        {
            selector.PrepareToSelect(new[] { original });
            await Hook.BeforeFlush(f.Combat, f.Player);
            await Hook.BeforeFlush(f.Combat, f.Teammate);
            Check(!original.ShouldRetainThisTurn, "Light and Shadow Traces cannot retain without a shift or for another player's end turn");
            selector.Cleanup();
            await Switch(f.Context, f.Player);
            var claw = CommonCard<ClawStrike>(f, true);
            var other = CommonCard<ReturningClaw>(f);
            var permanent = CommonCard<TransformationMagic>(f);
            await CardPileCmd.AddGeneratedCardToCombat(claw, PileType.Hand, f.Player);
            await CardPileCmd.AddGeneratedCardToCombat(other, PileType.Hand, f.Player);
            await CardPileCmd.AddGeneratedCardToCombat(permanent, PileType.Hand, f.Player);
            // The test selector can return more than maxSelect; the runtime must still respect the quota.
            selector.PrepareToSelect(new CardModel[] { claw, other });
            await Hook.BeforeFlush(f.Combat, f.Player);
            Check(claw.ShouldRetainThisTurn && !other.ShouldRetainThisTurn && permanent.ShouldRetainThisTurn,
                "base retention selects at most one eligible current-form hand card alongside native permanent Retain");
            await FlushBalanceHand(f);
            Check(claw.Pile?.Type == PileType.Hand && claw.IsUpgraded &&
                f.Player.PlayerCombatState!.Hand.Cards.Contains(claw) && other.Pile?.Type == PileType.Discard &&
                !claw.ShouldRetainThisTurn && !claw.Keywords.Contains(CardKeyword.Retain),
                "native end-turn flushing keeps the same upgraded card instance and clears its temporary Retain");
            await Hook.BeforeHandDraw(f.Combat, f.Player, f.Context);
            await Hook.BeforeFlush(f.Combat, f.Player);
            await FlushBalanceHand(f);
            Check(claw.Pile?.Type == PileType.Discard && permanent.Pile?.Type == PileType.Hand,
                "a later turn without a shift discards the previously selected card normally");
        }

        f = await CreateCommonFixture();
        await PlayRare<LightShadowTrace>(f, upgraded: true);
        await Switch(f.Context, f.Player);
        await Switch(f.Context, f.Player);
        var first = CommonCard<LittleFireball>(f);
        var second = CommonCard<LightShield>(f);
        var third = CommonCard<LightMark>(f);
        foreach (var card in new CardModel[] { first, second, third })
            await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, f.Player);
        selector = new TestCardSelector();
        using (CardSelectCmd.UseSelector(selector))
        {
            selector.PrepareToSelect(new CardModel[] { first, second, third });
            await Hook.BeforeFlush(f.Combat, f.Player);
            Check(first.ShouldRetainThisTurn && second.ShouldRetainThisTurn && !third.ShouldRetainThisTurn,
                "the upgrade retains at most two girl cards after repeated real shifts, without extra quotas per shift");
            await FlushBalanceHand(f);
            Check(first.Pile?.Type == PileType.Hand && second.Pile?.Type == PileType.Hand && third.Pile?.Type == PileType.Discard,
                "native flushing honors both upgraded retention selections");
            selector.PrepareToSelect(Array.Empty<CardModel>());
            await Hook.BeforeFlush(f.Combat, f.Player);
            Check(!first.ShouldRetainThisTurn && !second.ShouldRetainThisTurn,
                "the player may choose zero cards even when the power can trigger");
        }
        CheckCommonPowerPacket(f, ModelDb.Power<LightShadowTracePower>().Id, 2,
            "the native multiplayer combat snapshot preserves the upgraded retention amount");
        try
        {
            foreach (var language in new[] { "eng", "zhs" })
            {
                LocManager.Instance.SetLanguage(language);
                var power = f.Player.Creature.GetPower<LightShadowTracePower>()!;
                var tip = power.HoverTips.OfType<HoverTip>().Single();
                var prompt = new LocString("powers", power.Id.Entry + ".selectionScreenPrompt");
                prompt.Add("Cards", 2);
                Check(tip.Description.Contains('2') && !tip.Description.Contains('{') &&
                    prompt.GetFormattedText().Contains('2') && !prompt.GetFormattedText().Contains('{'),
                    language + " retention status and selection prompt show the actual upgraded quota");
            }
        }
        finally { LocManager.Instance.SetLanguage("eng"); }
    }

    private static Task FlushBalanceHand(CommonFixture f)
    {
        var manager = CombatManager.Instance;
        var turn = typeof(CombatManager).GetField("_turnState", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(manager)!;
        var context = new HookPlayerChoiceContext(f.Player, 1, GameActionType.Combat);
        return (Task)typeof(CombatManager).GetMethod("FlushPlayerHand", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(manager, [turn, f.Player, context])!;
    }
}
