using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Shenshen.ShenshenCode.Character;
using Shenshen.ShenshenCode.Powers;
using TemporaryDexterityPower = Shenshen.ShenshenCode.Powers.TemporaryDexterityPower;

namespace Shenshen.ShenshenCode.Cards;

public sealed class LightfootBlessing : ShenshenCard
{
    public LightfootBlessing() : base(0, CardType.Skill, CardRarity.Common, TargetType.Self) { }
    public override ShenshenForm? Form => ShenshenForm.Human;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<DexterityPower>(), HoverTipFactory.FromPower<TemporaryDexterityPower>()];
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<DexterityPower>(2m), new CardsVar(1)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        int previous = Owner.Creature.GetPower<DexterityPower>()?.Amount ?? 0;
        await PowerCmd.Apply<DexterityPower>(choiceContext, Owner.Creature, DynamicVars["DexterityPower"].BaseValue,
            Owner.Creature, this);
        int gained = (Owner.Creature.GetPower<DexterityPower>()?.Amount ?? 0) - previous;
        if (gained > 0)
            await PowerCmd.Apply<TemporaryDexterityPower>(choiceContext, Owner.Creature, gained,
                Owner.Creature, this);
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, Owner);
    }

    protected override void OnUpgrade() => DynamicVars["DexterityPower"].UpgradeValueBy(1m);
}
