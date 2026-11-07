using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Shenshen.ShenshenCode.Cards;
using Shenshen.ShenshenCode.Powers;

public partial class Harness
{
    private async Task RunFormFaceDodgeChecks()
    {
        await CheckShiftTurnDodge();
        await CheckTransformationFaces();
    }

    private async Task CheckShiftTurnDodge()
    {
        var f = await CreateCommonFixture();
        var creature = f.Player.Creature;
        Check(creature.GetPower<CatDodgePower>() == null,
            "a human-form player has no automatic cat dodge before an actual cat entry");
        await Switch(f.Context, f.Player);
        Check(creature.GetPower<FormPower>()?.Amount == 2 && creature.GetPower<CatDodgePower>() is { Amount: 1, IsVisible: true },
            "an actual cat entry grants one visible shift-turn dodge");
        await Hook.BeforeHandDraw(f.Combat, f.Teammate, f.Context);
        Check(creature.GetPower<CatDodgePower>()?.Amount == 1,
            "a teammate's draw hook does not expire the cat-entry dodge");
        CheckCommonPowerPacket(f, ModelDb.Power<CatDodgePower>().Id, 1,
            "native combat packets preserve a ready cat-entry dodge");

        int hp = creature.CurrentHp;
        await CommonEnemyAttack(f, 1);
        Check(creature.CurrentHp == hp - 1 && creature.GetPower<CatDodgePower>()?.Amount == 1,
            "single enemy attacks do not consume the shift-turn dodge");
        await CreatureCmd.GainBlock(creature, 3, ValueProp.Unpowered, null);
        await PowerCmd.Apply<ThornsPower>(f.Context, creature, 2, creature, null);
        await PowerCmd.Apply<ReflectPower>(f.Context, creature, 1, creature, null);
        hp = creature.CurrentHp;
        int enemyHp = f.Enemy.CurrentHp;
        await CommonEnemyAttack(f, 3, 2);
        Check(creature.CurrentHp == hp && creature.Block == 0 && f.Enemy.CurrentHp == enemyHp - 6,
            "shift-turn dodge skips one reception without spending block or retaliating on the skipped hit");
        Check(creature.GetPower<CatDodgePower>() is { Amount: 2, IsVisible: false },
            "the spent dodge marker is hidden while retaining this turn's consumed flag");
        await Switch(f.Context, f.Player);
        await Switch(f.Context, f.Player);
        Check(creature.GetPower<CatDodgePower>() is { Amount: 2, IsVisible: false },
            "returning to cat within the same turn cannot restore an already spent dodge");
        CheckCommonPowerPacket(f, ModelDb.Power<CatDodgePower>().Id, 2,
            "native combat packets preserve the consumed flag across same-turn transformations");

        await Hook.BeforeHandDraw(f.Combat, f.Player, f.Context);
        Check(creature.GetPower<CatDodgePower>() == null && creature.GetPower<FormPower>()?.Amount == 2,
            "the next own draw removes the dodge without removing cat form");
        await PowerCmd.Remove(creature.GetPower<ThornsPower>());
        await PowerCmd.Remove(creature.GetPower<ReflectPower>());
        hp = creature.CurrentHp;
        await CommonEnemyAttack(f, 3, 2);
        Check(creature.CurrentHp == hp - 6,
            "staying in cat form on the following turn receives every enemy multi-hit segment");
        await Hook.BeforeHandDraw(f.Combat, f.Player, f.Context);
        Check(creature.GetPower<CatDodgePower>() == null,
            "additional own turns spent in cat form do not recreate the dodge");

        await Switch(f.Context, f.Player);
        await Switch(f.Context, f.Player);
        Check(creature.GetPower<CatDodgePower>()?.Amount == 1,
            "a later turn's actual new cat entry grants a fresh dodge");
        hp = creature.CurrentHp;
        await CommonEnemyAttack(f, 3, 2);
        Check(creature.CurrentHp == hp - 3,
            "the later cat-entry turn once again avoids only the first multi-hit segment");
        await Hook.BeforeHandDraw(f.Combat, f.Player, f.Context);
        await Switch(f.Context, f.Player);
        await Switch(f.Context, f.Player);
        await Hook.BeforeHandDraw(f.Combat, f.Player, f.Context);
        Check(creature.GetPower<CatDodgePower>() == null,
            "an unused ready dodge also expires at the next own turn");
    }

    private async Task CheckTransformationFaces()
    {
        var f = await CreateCommonFixture();
        var state = f.Player.PlayerCombatState!;
        var transform = state.AllCards.OfType<TransformationMagic>().Single();
        var owned = state.AllCards.ToHashSet();
        await CardPileCmd.Add(transform, PileType.Hand);
        var canonicalFace = ModelDb.Card<TransformationMagic>();
        Check(canonicalFace.TransformsToCat && canonicalFace.CustomPortraitPath.EndsWith("transformation_cat.tres"),
            "the library's ownerless transformation defaults to the cat destination face");
        Check(transform.TransformsToCat && transform.CustomPortraitPath.EndsWith("transformation_cat.tres"),
            "the actual transformation held in human form shows the cat destination portrait");
        Texture2D catPortrait = transform.Portrait;
        Check(catPortrait.GetWidth() > 0 && catPortrait.GetHeight() > 0,
            "the native card portrait getter loads the cat face from the packaged resource");

        var localization = LocManager.Instance;
        try
        {
            foreach (string language in new[] { "eng", "zhs" })
            {
                localization.SetLanguage(language);
                var strength = language == "eng" ? "Strength" : "力量";
                var dexterity = language == "eng" ? "Dexterity" : "敏捷";
                var thorns = language == "eng" ? "Thorns" : "荆棘";
                var text = transform.GetDescriptionForPile(PileType.Hand);
                Check(text.Contains(thorns) && !text.Contains(dexterity) && !text.Contains(strength) && !text.Contains('{'),
                    language + " human-held base transformation displays only the cat-entry reward");
                var preview = transform.GetDescriptionForUpgradePreview();
                Check(preview.Contains(strength) && preview.Contains(thorns) && !preview.Contains(dexterity) && !preview.Contains('{'),
                    language + " human-held upgrade preview displays only the upgraded cat-entry rewards");
            }

            transform.EnergyCost.SetThisCombat(0);
            await PlayPaid(transform, f.Context);
            Check(!transform.TransformsToCat && transform.CustomPortraitPath.EndsWith("transformation_girl.tres") &&
                ReferenceEquals(state.Hand.Cards.OfType<TransformationMagic>().Single(), transform),
                "the first paid play flips the same actual card to the girl destination face");
            Texture2D girlPortrait = transform.Portrait;
            Check(girlPortrait.GetWidth() > 0 && girlPortrait.GetHeight() > 0 &&
                girlPortrait.ResourcePath != catPortrait.ResourcePath,
                "the native girl portrait loads a distinct packaged face");
            foreach (string language in new[] { "eng", "zhs" })
            {
                localization.SetLanguage(language);
                var text = transform.GetDescriptionForPile(PileType.Hand);
                var preview = transform.GetDescriptionForUpgradePreview();
                var strength = language == "eng" ? "Strength" : "力量";
                var dexterity = language == "eng" ? "Dexterity" : "敏捷";
                var thorns = language == "eng" ? "Thorns" : "荆棘";
                var fixedBlock = language == "eng" ? "fixed" : "不受敏捷影响";
                Check(text.Contains(dexterity) && !text.Contains(strength) && !text.Contains(thorns) && !text.Contains(fixedBlock) && !text.Contains('{'),
                    language + " cat-held base transformation displays only the girl-entry reward");
                Check(preview.Contains(dexterity) && preview.Contains(fixedBlock) && !preview.Contains(strength) &&
                    !preview.Contains(thorns) && !preview.Contains('{'),
                    language + " cat-held upgrade preview displays only dexterity and fixed block");
            }

            transform.UpgradeInternal();
            transform.FinalizeUpgradeInternal();
            await PlayPaid(transform, f.Context);
            Check(transform.TransformsToCat && transform.IsUpgraded && state.AllCards.ToHashSet().SetEquals(owned) &&
                transform.EnergyCost.GetWithModifiers(CostModifiers.Local) == 0 && state.Hand.Cards.Contains(transform),
                "flipping the upgraded card back preserves inventory, actual instance and modified cost");
            foreach (string language in new[] { "eng", "zhs" })
            {
                localization.SetLanguage(language);
                var text = transform.GetDescriptionForPile(PileType.Hand);
                Check(text.Contains(language == "eng" ? "Thorns" : "荆棘") &&
                    !text.Contains(language == "eng" ? "Dexterity" : "敏捷") && !text.Contains('{'),
                    language + " upgraded cat destination face excludes all girl-entry effects");
            }
            await PlayPaid(transform, f.Context);
            foreach (string language in new[] { "eng", "zhs" })
            {
                localization.SetLanguage(language);
                var text = transform.GetDescriptionForPile(PileType.Hand);
                Check(text.Contains(language == "eng" ? "Dexterity" : "敏捷") &&
                    text.Contains(language == "eng" ? "fixed" : "不受敏捷影响") &&
                    !text.Contains(language == "eng" ? "Strength" : "力量") && !text.Contains('{'),
                    language + " upgraded girl destination face excludes all cat-entry effects");
            }

            localization.SetLanguage("zhs");
            Check(ModelDb.Card<MiDodge>().Title == "咪挡" && ModelDb.Power<CatDodgePower>().Title.GetFormattedText() == "咪闪",
                "the block card is named Mi Guard while the cat-entry dodge status owns the Mi Dodge name");
            Check(!f.Combat.CreateCard<LightMirror>(f.Player).GetDescriptionForPile(PileType.Hand).Contains("不叠加") &&
                !f.Combat.CreateCard<FluffedUp>(f.Player).GetDescriptionForPile(PileType.Hand).Contains("本场仅一次") &&
                !f.Combat.CreateCard<FluffedUp>(f.Player).GetDescriptionForPile(PileType.Hand).Contains("不叠加"),
                "short mirror and fluff card text omits the rejected stacking and once-per-combat boilerplate");
        }
        finally
        {
            localization.SetLanguage("eng");
        }
    }
}
