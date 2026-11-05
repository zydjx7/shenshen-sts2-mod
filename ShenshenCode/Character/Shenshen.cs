using BaseLib.Abstracts;
using BaseLib.Utils.NodeFactories;
using Shenshen.ShenshenCode.Extensions;
using Godot;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.PotionPools;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace Shenshen.ShenshenCode.Character;

public class Shenshen : PlaceholderCharacterModel
{
    public const string CharacterId = "Shenshen";
    public const string ChineseDisplayName = "光之魔法少女深深";
    public const string EnglishDisplayName = "Shenshen, Magical Girl of Light";
    public const string HumanFormDisplayName = "人形";
    public const string CatFormDisplayName = "猫形";

    // Design facts only: no active form, switching, or combat effects exist yet.
    public const bool FormMechanicsImplemented = false;

    // White, neutral gender, and 70 HP are inherited development placeholders from the template.
    public static readonly Color Color = new("ffffff");

    public override Color NameColor => Color;
    public override CharacterGender Gender => CharacterGender.Neutral;
    public override int StartingHp => 70;

    public override IEnumerable<CardModel> StartingDeck => [
        ModelDb.Card<StrikeIronclad>(),
        ModelDb.Card<StrikeIronclad>(),
        ModelDb.Card<StrikeIronclad>(),
        ModelDb.Card<StrikeIronclad>(),
        ModelDb.Card<StrikeIronclad>(),
        ModelDb.Card<DefendIronclad>(),
        ModelDb.Card<DefendIronclad>(),
        ModelDb.Card<DefendIronclad>(),
        ModelDb.Card<DefendIronclad>(),
        ModelDb.Card<DefendIronclad>()
    ];

    public override IReadOnlyList<RelicModel> StartingRelics =>
    [
        ModelDb.Relic<BurningBlood>()
    ];

    // Use stock content while the original character's mechanics and content are being designed.
    // The template's five Strikes, five Defends, and Burning Blood above are placeholders too.
    public override CardPoolModel CardPool => ModelDb.CardPool<IroncladCardPool>();
    public override RelicPoolModel RelicPool => ModelDb.RelicPool<IroncladRelicPool>();
    public override PotionPoolModel PotionPool => ModelDb.PotionPool<IroncladPotionPool>();

    // All visuals still use template or base-game placeholders, including the combat appearance.
    // Human/cat artwork directories are reserved only; neither form is wired to these assets.
    public override Control CustomIcon
    {
        get
        {
            var icon = NodeFactory<Control>.CreateFromResource(CustomIconTexturePath);
            icon.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            return icon;
        }
    }
    public override string CustomIconTexturePath => "character_icon_char_name.png".CharacterUiPath();
    public override string CustomCharacterSelectIconPath => "char_select_char_name.png".CharacterUiPath();
    public override string CustomCharacterSelectLockedIconPath => "char_select_char_name_locked.png".CharacterUiPath();
    public override string CustomMapMarkerPath => "map_marker_char_name.png".CharacterUiPath();
}
