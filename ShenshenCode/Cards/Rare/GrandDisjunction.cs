using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using Shenshen.ShenshenCode.Character;
using Shenshen.ShenshenCode.Powers;

namespace Shenshen.ShenshenCode.Cards;

public sealed class GrandDisjunction : RareCard
{
    public GrandDisjunction() : base(1, CardType.Skill, TargetType.AllEnemies, ShenshenForm.Human) { }
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Turns", 2m), new DynamicVar("Burn", 1m)];

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        foreach (var enemy in CombatState!.HittableEnemies.ToArray())
        {
            await CreatureCmd.LoseBlock(context, enemy, enemy.Block, Owner.Creature);
            if (enemy.GetPower<ArtifactPower>() is { } artifact) await PowerCmd.Remove(artifact);
            await LightMarkPower.ApplyMark(context, enemy, Owner.Creature, DynamicVars["Burn"].IntValue, DynamicVars["Turns"].IntValue, this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Turns"].UpgradeValueBy(1);
        DynamicVars["Burn"].UpgradeValueBy(1);
    }
}
