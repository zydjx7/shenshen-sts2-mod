using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Shenshen.ShenshenCode.Character;

namespace Shenshen.ShenshenCode.Cards;

public sealed class KnockOff : RareCard
{
    public KnockOff() : base(1, CardType.Attack, TargetType.AnyEnemy, ShenshenForm.Cat) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(6m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        bool Eligible(MegaCrit.Sts2.Core.Models.CardModel card) => card != this && card is ShenshenCard { Form: ShenshenForm.Cat };
        int available = PileType.Hand.GetPile(Owner).Cards.Count(Eligible);
        if (available == 0) return;
        var selected = (await CardSelectCmd.FromHand(context, Owner, new CardSelectorPrefs(SelectionScreenPrompt, 0, available),
            Eligible, this)).ToArray();
        if (selected.Length == 0) return;
        await CardCmd.Discard(context, selected);
        // Sly and other discard triggers can move a discarded card again before this attack.
        await Attack(context, play, selected.Length);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2);
}
