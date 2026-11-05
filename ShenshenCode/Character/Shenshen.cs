using BaseLib.Abstracts;
using BaseLib.Utils.NodeFactories;
using Godot;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.PotionPools;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;
using Shenshen.ShenshenCode.Cards;
using Shenshen.ShenshenCode.CardPools;
using Shenshen.ShenshenCode.Visuals;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace Shenshen.ShenshenCode.Character;

public class Shenshen : PlaceholderCharacterModel
{
    public const string CharacterId = "Shenshen";
    public const string ChineseDisplayName = "光之魔法少女深深";
    public const string EnglishDisplayName = "Shenshen, Magical Girl of Light";
    public const string HumanFormDisplayName = "人形";
    public const string CatFormDisplayName = "猫形";

    public const bool FormMechanicsImplemented = true;

    public static readonly Color Color = new("e8a1b8");

    public override Color NameColor => Color;
    public override CharacterGender Gender => CharacterGender.Feminine;
    public override int StartingHp => 70;

    public override IEnumerable<CardModel> StartingDeck =>
        Enumerable.Repeat<CardModel>(ModelDb.Card<LittleFireball>(), 4)
            .Concat(Enumerable.Repeat(ModelDb.Card<LightShield>(), 4))
            .Append(ModelDb.Card<LightMark>())
            .Concat(Enumerable.Repeat(ModelDb.Card<ClawStrike>(), 4))
            .Concat(Enumerable.Repeat(ModelDb.Card<MiDodge>(), 4))
            .Append(ModelDb.Card<CatTailSweep>());

    public override IReadOnlyList<RelicModel> StartingRelics =>
    [
        ModelDb.Relic<BurningBlood>()
    ];

    // Burning Blood and the relic/potion pools remain explicit prototype placeholders.
    public override CardPoolModel CardPool => ModelDb.CardPool<ShenshenCardPool>();
    public override RelicPoolModel RelicPool => ModelDb.RelicPool<IroncladRelicPool>();
    public override PotionPoolModel PotionPool => ModelDb.PotionPool<IroncladPotionPool>();

    public override NCreatureVisuals CreateCustomVisuals() => ShenshenVisuals.Create();
    public override float AttackAnimDelay => ShenshenVisuals.AttackDelay;
    public override float CastAnimDelay => ShenshenVisuals.CastDelay;
    public override float DeathAnimTime => ShenshenVisuals.DeathDuration;

    // Character-select and combat art use separate resources.
    public override Control CustomIcon
    {
        get
        {
            var icon = NodeFactory<Control>.CreateFromResource(CustomIconTexturePath);
            icon.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            return icon;
        }
    }
    // Native selection and map getters require CompressedTexture2D, so these paths must
    // resolve to imported PNG textures rather than AtlasTexture resources.
    public override string CustomIconTexturePath => ShenshenVisuals.AssetDirectory + "/avatar.png";
    public override string CustomIconOutlineTexturePath => ShenshenVisuals.AssetDirectory + "/avatar.png";
    public override string CustomCharacterSelectIconPath => ShenshenVisuals.AssetDirectory + "/select-icon.png";
    public override string CustomCharacterSelectLockedIconPath => ShenshenVisuals.AssetDirectory + "/select-icon.png";
    public override string CustomMapMarkerPath => ShenshenVisuals.AssetDirectory + "/avatar.png";
    public override string CustomCharacterSelectBg => "res://Shenshen/scenes/character_select/shenshen_select_bg.tscn";
}
