using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Shenshen.ShenshenCode.Character;
using Shenshen.ShenshenCode.Mechanics;
using Shenshen.ShenshenCode.Powers;

namespace Shenshen.ShenshenCode.Cards;

public sealed class Hiss : ShenshenCard
{
    public Hiss() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AllEnemies) { }
    public override ShenshenForm? Form => ShenshenForm.Cat;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<WeakPower>(2m), new PowerVar<ThornsPower>(3m)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<WeakPower>(), HoverTipFactory.FromPower<TemporaryThornsPower>()];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        foreach (var enemy in CombatState!.HittableEnemies.ToArray())
            await PowerCmd.Apply<WeakPower>(context, enemy, DynamicVars.Weak.BaseValue, Owner.Creature, this);
        await TemporaryThornsPower.Grant(context, Owner.Creature, DynamicVars["ThornsPower"].IntValue, this);
    }
    protected override void OnUpgrade()
    {
        DynamicVars.Weak.UpgradeValueBy(1m);
        DynamicVars["ThornsPower"].UpgradeValueBy(1m);
    }
}
