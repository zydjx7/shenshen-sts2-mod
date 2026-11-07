using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using Shenshen.ShenshenCode.CardPools;
using Shenshen.ShenshenCode.Character;
using Shenshen.ShenshenCode.Mechanics;

namespace Shenshen.ShenshenCode.Cards;

/// <summary>Form ownership is separate from rarity and card pile membership.</summary>
[Pool(typeof(ShenshenCardPool))]
public abstract class ShenshenCard : CustomCardModel
{
    protected ShenshenCard(int cost, CardType type, CardRarity rarity, TargetType target)
        : base(cost, type, rarity, target)
    {
    }

    public abstract ShenshenForm? Form { get; }

    // Direct selection effects can put a card into hand without using normal Draw.
    protected override bool IsPlayable => Form is not { } required || CombatState == null ||
        Owner.Character is not Character.Shenshen || FormState.GetForm(Owner) == required;

    public override string CustomPortraitPath
    {
        get
        {
            var portrait = this switch
            {
                ShatteredMirror => "common/shattered_mirror.png",
                ThornPrayer => "common/thorn_prayer.png",
                LightfootBlessing => "common/lightfoot_blessing.png",
                LightCurtain => "common/light_curtain.png",
                LingeringLight => "common/lingering_light.png",
                TwinSparks => "common/twin_sparks.png",
                ChasingFlame => "common/chasing_flame.png",
                DazzlingLight => "common/dazzling_light.png",
                Halo => "common/halo.png",
                RefractedVolley => "common/refracted_volley.png",
                ArchedBack => "common/arched_back.png",
                FurballCurl => "common/furball_curl.png",
                CuriousPeek => "common/curious_peek.png",
                Catnap => "common/catnap.png",
                Grooming => "common/grooming.png",
                ShedFur => "common/shed_fur.png",
                SweepingClaws => "common/sweeping_claws.png",
                BunnyKick => "common/bunny_kick.png",
                BiteTheOpening => "common/bite_the_opening.png",
                BackwardGlance => "common/backward_glance.png",
                LightBlessing => "common/light_blessing.png",
                _ => "card.png"
            };
            return $"{MainFile.ResPath}/images/card_portraits/{portrait}";
        }
    }
}
