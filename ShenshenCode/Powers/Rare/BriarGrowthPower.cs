using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Shenshen.ShenshenCode.Powers;

public sealed class BriarGrowthPower : RarePower
{
    internal Task Grow(PlayerChoiceContext context) =>
        PowerCmd.Apply<ThornsPower>(context, Owner, Amount, Owner, null);
}
