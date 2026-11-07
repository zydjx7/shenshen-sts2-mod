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
                LittleFireball => "basic/little_fireball.png",
                LightShield => "basic/light_shield.png",
                LightMark => "basic/light_mark.png",
                ClawStrike => "basic/claw_strike.png",
                MiDodge => "basic/mi_dodge.png",
                CatTailSweep => "basic/cat_tail_sweep.png",
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
                IDontLikeYou => "uncommon/i_dont_like_you.png",
                LightMirror => "uncommon/light_mirror.png",
                LightReserve => "uncommon/light_reserve.png",
                MirrorAfterglow => "uncommon/mirror_afterglow.png",
                FirefliesAbove => "uncommon/fireflies_above.png",
                RefractedFlameArrows => "uncommon/refracted_flame_arrows.png",
                LightShadowHandoff => "uncommon/light_shadow_handoff.png",
                PureLightPrayer => "uncommon/pure_light_prayer.png",
                CompanionWish => "uncommon/companion_wish.png",
                ShatteredMirrorFlames => "uncommon/shattered_mirror_flames.png",
                FireRain => "uncommon/fire_rain.png",
                MoltenLightPierce => "uncommon/molten_light_pierce.png",
                KindleTheSpark => "uncommon/kindle_the_spark.png",
                MirroredVestments => "uncommon/mirrored_vestments.png",
                WardingPrayer => "uncommon/warding_prayer.png",
                StarlitWish => "uncommon/starlit_wish.png",
                DazzlingBarrier => "uncommon/dazzling_barrier.png",
                InsightCharm => "uncommon/insight_charm.png",
                CleansingFlame => "uncommon/cleansing_flame.png",
                TomorrowWish => "uncommon/tomorrow_wish.png",
                FluffedUp => "uncommon/fluffed_up.png",
                ThornyPurr => "uncommon/thorny_purr.png",
                ChaseTheLight => "uncommon/chase_the_light.png",
                ProtectiveFluff => "uncommon/protective_fluff.png",
                ScratchTheLight => "uncommon/scratch_the_light.png",
                ReturningClaw => "uncommon/returning_claw.png",
                DandelionPuff => "uncommon/dandelion_puff.png",
                CatchTheLight => "uncommon/catch_the_light.png",
                CardboardHideout => "uncommon/cardboard_hideout.png",
                HiddenToy => "uncommon/hidden_toy.png",
                Kneading => "uncommon/kneading.png",
                RollingCounter => "uncommon/rolling_counter.png",
                Teasing => "uncommon/teasing.png",
                RoomwidePounce => "uncommon/roomwide_pounce.png",
                Hiss => "uncommon/hiss.png",
                PretendSleep => "uncommon/pretend_sleep.png",
                RetrieveTheStaff => "uncommon/retrieve_the_staff.png",
                GuardTheBelly => "uncommon/guard_the_belly.png",
                BigStretch => "uncommon/big_stretch.png",
                FoodGuardian => "uncommon/food_guardian.png",
                AtomicFission => "rare/atomic_fission.png",
                CharmSpell => "rare/charm_spell.png",
                DawnReflection => "rare/dawn_reflection.png",
                DoubleChant => "rare/double_chant.png",
                SolarEclipse => "rare/eclipse.png",
                LichPhylactery => "rare/lich_phylactery.png",
                LightInGlass => "rare/light_in_glass.png",
                HuntByLight => "rare/look_sharp.png",
                MadeReal => "rare/made_real.png",
                OttoDance => "rare/otto_dance.png",
                OvernightBloom => "rare/overnight_bloom.png",
                SevenfoldMirror => "rare/sevenfold_mirror_shield.png",
                SilentCasting => "rare/silent_casting.png",
                SpreadingSparks => "rare/spreading_sparks.png",
                Sunbeam => "rare/sunbeam.png",
                Sunfall => "rare/sunfall.png",
                FourPawsLanding => "rare/four_paws_landing.png",
                Grudge => "rare/grudge.png",
                KnockOff => "rare/knock_off.png",
                Ransack => "rare/ransack.png",
                SheathedClaws => "rare/sheathed_claws.png",
                SwiftPounce => "rare/swift_pounce.png",
                TailtipMark => "rare/tailtip_mark.png",
                _ => "card.png"
            };
            return $"{MainFile.ResPath}/images/card_portraits/{portrait}";
        }
    }
}
