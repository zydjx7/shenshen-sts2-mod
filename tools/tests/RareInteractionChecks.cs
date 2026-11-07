using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.ValueProps;
using Shenshen.ShenshenCode.Cards;
using Shenshen.ShenshenCode.Powers;

public partial class Harness
{
    private async Task TestRareCardInteractions()
    {
        var f = await CreateCommonFixture();
        var owner = f.Player.Creature;
        var draw = f.Player.PlayerCombatState!.DrawPile;
        var claw = await CommonDrawCard<ClawStrike>(f);
        var attack = await CommonDrawCard<BunnyKick>(f);
        var selector = new TestCardSelector();
        using (CardSelectCmd.UseSelector(selector))
        {
            selector.PrepareToSelect(new[] { claw });
            await PlayRare<HuntByLight>(f, f.Enemy);
        }
        Check(claw.Pile?.Type == PileType.Hand && claw.EnergyCost.GetWithModifiers(CostModifiers.All) == 0 &&
            draw.Cards.Contains(attack), "Hunt by Light retrieves the selected real cat attack from its inactive deck");
        await PlayRare<DoubleChant>(f);
        var falling = await CommonDrawCard<Sunfall>(f);
        var blade = CommonCard<ReturningSunblade>(f);
        await CardPileCmd.AddGeneratedCardToCombat(blade, PileType.Discard, f.Player);
        await Switch(f.Context, f.Player);
        int hp = f.Enemy.CurrentHp;
        await CardCmd.AutoPlay(f.Context, claw, f.Enemy, skipCardPileVisuals: true);
        Check(hp - f.Enemy.CurrentHp == 24 && falling.EnergyCost.GetWithModifiers(CostModifiers.All) == 4,
            "the first shift doubles the next four-hit Claw and discounts Sunfall once");
        await Switch(f.Context, f.Player);
        Check(blade.Pile?.Type == PileType.Hand && blade.EnergyCost.GetWithModifiers(CostModifiers.All) == 0,
            "the first return to girl form recovers the original Sunblade");

        f = await CreateCommonFixture();
        owner = f.Player.Creature;
        await PlayRare<SpreadingSparks>(f);
        await PlayRare<DualLightSigil>(f);
        hp = f.OtherEnemy.CurrentHp;
        await PlayRare<LittleFireball>(f, f.Enemy);
        Check(hp - f.OtherEnemy.CurrentHp == 3, "a single-target hit spreads the upgraded-in-kind Ancient mark burn to the other enemy");
        await PlayRare<BurnTheMark>(f, f.Enemy);
        Check(f.Enemy.GetPower<LightMarkPower>() == null, "Burn the Mark consumes the target’s own remaining mark before attacking");

        f = await CreateCommonFixture();
        var normalClawText = CommonCard<ClawStrike>(f).GetDescriptionForPile(PileType.Hand);
        await PlayRare<UncertainHand>(f, upgraded: true);
        var quantum = f.Player.Creature.GetPower<UncertainHandPower>()!;
        try
        {
            foreach (var (language, title) in new[] { ("eng", "Quantum Effect"), ("zhs", "量子效应") })
            {
                MegaCrit.Sts2.Core.Localization.LocManager.Instance.SetLanguage(language);
                var tip = quantum.HoverTips.OfType<MegaCrit.Sts2.Core.HoverTips.HoverTip>().Single();
                Check(quantum.IsVisible && quantum.Amount == 80 && tip.Title == title &&
                    tip.Description.Contains("80%") && !tip.Description.Contains('{'),
                    language + " Quantum Effect status displays its localized name and actual upgraded chance");
            }
        }
        finally { MegaCrit.Sts2.Core.Localization.LocManager.Instance.SetLanguage("eng"); }
        var held = new List<ClawStrike>();
        for (int i = 0; i < 8; i++)
        {
            var c = CommonCard<ClawStrike>(f);
            await CardPileCmd.AddGeneratedCardToCombat(c, PileType.Hand, f.Player);
            held.Add(c);
        }
        Check(held.All(c => c.GetDescriptionForPile(PileType.Hand) == normalClawText),
            "Quantum Effect does not append the rolled effect to attack card descriptions");
        var pierces = typeof(UncertainHandPower).GetMethod("Pierces",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        var pierced = held.FirstOrDefault(c => (bool)pierces.Invoke(quantum, [c])!);
        Check(pierced != null, "seeded hand still contains an attack affected by Quantum Effect");
        await CreatureCmd.GainBlock(f.Enemy, 50, ValueProp.Unpowered, null);
        hp = f.Enemy.CurrentHp;
        await CardCmd.AutoPlay(f.Context, pierced!, f.Enemy, skipCardPileVisuals: true);
        Check(hp - f.Enemy.CurrentHp == 6 && f.Enemy.Block == 50, "one piercing Claw roll ignores Block for all three hits");
        hp = f.Enemy.CurrentHp;
        await PlayRare<PositronCannon>(f, f.Enemy);
        Check(hp - f.Enemy.CurrentHp == 42 && f.Enemy.Block == 0, "Positron Cannon consumes the shield first and caps the bonus at 24");

        f = await CreateCommonFixture();
        owner = f.Player.Creature;
        await PowerCmd.Apply<ThornsPower>(f.Context, owner, 6, owner, null);
        await PlayRare<CrownOfThorns>(f);
        await PlayRare<ShatteredMirror>(f);
        Check(owner.GetPower<ShatteredMirrorPower>()?.Amount == owner.Block &&
            owner.Block == CommonCard<ShatteredMirror>(f).DynamicVars.Block.IntValue + 2, "Crown adds a distinct fixed mirror quota to a mirror card");
        await PlayRare<LightInGlass>(f);
        await CreatureCmd.GainBlock(owner, 20, ValueProp.Unpowered, null);
        int keep = Math.Min(12, owner.GetPower<ShatteredMirrorPower>()!.Amount);
        await owner.GetPower<LightInGlassPower>()!.AfterPreventingBlockClear(owner.GetPower<LightInGlassPower>()!, owner);
        Check(owner.Block == keep && owner.GetPower<ShatteredMirrorPower>()?.Amount == keep, "Glass preserves only its capped mirror quota");

        f = await CreateCommonFixture(Shenshen.ShenshenCode.Character.ShenshenForm.Cat);
        var sly = CommonCard<FourPawsLanding>(f);
        sly.AddKeyword(CardKeyword.Sly);
        await CardPileCmd.AddGeneratedCardToCombat(sly, PileType.Hand, f.Player);
        hp = f.Enemy.CurrentHp;
        using (CardSelectCmd.UseSelector(selector))
        {
            selector.PrepareToSelect(new[] { sly });
            await PlayRare<KnockOff>(f, f.Enemy);
        }
        Check(sly.Pile?.Type == PileType.Exhaust && hp - f.Enemy.CurrentHp == 6,
            "Knock Off counts a discarded card even when Sly immediately plays and exhausts it");
    }

    private async Task TestRareControlTurns()
    {
        var f = await CreateCommonFixture();
        var owner = f.Player.Creature;
        var monster = f.Enemy.Monster!;
        var hit = new MoveState("RARE_TEST_HIT", async _ => await CommonEnemyAttack(f, 16), new SingleAttackIntent(16));
        monster.SetMoveImmediate(hit, true);
        await PlayRare<StopTime>(f);
        int hp = owner.CurrentHp;
        await monster.PerformMove();
        Check(owner.CurrentHp == hp, "Stop Time delays the current attack");
        monster.RollMove(f.Combat.PlayerCreatures);
        Check(ReferenceEquals(hit, monster.NextMove), "the delayed intent still points to the same move");
        await monster.PerformMove();
        Check(owner.CurrentHp == hp - 16 && f.Enemy.GetPower<StoppedTimePower>() == null, "the original attack executes on the following turn");
        await PlayRare<Polymorph>(f, f.Enemy);
        hp = owner.CurrentHp;
        await monster.PerformMove();
        Check(owner.CurrentHp == hp && f.Enemy.GetPower<SheepPower>() == null, "an undisturbed sheep grazes for one action");
        await PlayRare<Polymorph>(f, f.Enemy);
        await CreatureCmd.Damage(f.Context, f.Enemy, 1, ValueProp.Unpowered, owner);
        Check(f.Enemy.GetPower<SheepPower>() == null, "damage wakes a sheep before its action");

        f = await CreateCommonFixture();
        owner = f.Player.Creature;
        monster = f.Enemy.Monster!;
        monster.SetMoveImmediate(new MoveState("RARE_CHARM", async _ => await CommonEnemyAttack(f, 16), new SingleAttackIntent(16)), true);
        await PowerCmd.Apply<ThornsPower>(f.Context, owner, 3, owner, null);
        await PlayRare<CharmSpell>(f, f.Enemy);
        hp = owner.CurrentHp;
        var enemyHp = f.Enemy.CurrentHp;
        await monster.PerformMove();
        Check(hp - owner.CurrentHp == 12 && enemyHp - f.Enemy.CurrentHp == 9,
            "Charm divides the weakened 16-point intent into three real 4-point hits");
        monster.SetMoveImmediate(new MoveState("RARE_BUFF", _ => Task.CompletedTask, new BuffIntent()), true);
        await PlayRare<CharmSpell>(f, f.Enemy, true);
        hp = owner.CurrentHp;
        enemyHp = f.Enemy.CurrentHp;
        await monster.PerformMove();
        Check(hp == owner.CurrentHp && enemyHp - f.Enemy.CurrentHp == 12, "charmed nonattack adds four zero hits that trigger Thorns");

        f = await CreateCommonFixture();
        var shield = CommonCard<LightMirror>(f);
        await CardPileCmd.AddGeneratedCardToCombat(shield, PileType.Hand, f.Player);
        var selector = new TestCardSelector();
        selector.PrepareToSelect(new[] { shield });
        using (CardSelectCmd.UseSelector(selector))
        {
            await PlayRare<SilentCasting>(f);
        }
        await Switch(f.Context, f.Player);
        hp = f.Player.Creature.CurrentHp;
        await CommonEnemyAttack(f, 8);
        Check(f.Player.Creature.CurrentHp == hp && shield.Pile?.Type != PileType.Play && f.Player.Creature.GetPower<PreparedSpellPower>() == null,
            "the original prepared shield fires before the attack even after changing to cat form");
    }
}
