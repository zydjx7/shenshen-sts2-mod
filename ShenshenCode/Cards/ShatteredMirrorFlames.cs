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
using Shenshen.ShenshenCode.Powers;

namespace Shenshen.ShenshenCode.Cards;

public sealed class ShatteredMirrorFlames : ShenshenCard
{
    public ShatteredMirrorFlames() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AllEnemies) { }
    public override ShenshenForm? Form => ShenshenForm.Human;
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var damage = Owner.Creature.Block;
        await CreatureCmd.LoseBlock(context, Owner.Creature, damage, Owner.Creature);
        if (damage > 0)
            await CreatureCmd.Damage(context, CombatState!.GetOpponentsOf(Owner.Creature).Where(enemy => enemy.IsAlive).ToArray(), damage, ValueProp.Unpowered, Owner.Creature, this, play);
    }
    protected override void OnUpgrade() => EnergyCost.SetCustomBaseCost(0);
}
