using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Unlocks;
using Shenshen.ShenshenCode.Relics;

namespace Shenshen.ShenshenCode.RelicPools;

public sealed class ShenshenRelicPool : CustomRelicPoolModel
{
    public override string BigEnergyIconPath => MainFile.ResPath + "/images/charui/big_energy.png";
    public override string TextEnergyIconPath => MainFile.ResPath + "/images/charui/text_energy.png";

    // Keep the existing prototype rewards without duplicating their collection entries.
    public override IEnumerable<RelicModel> GetUnlockedRelics(UnlockState unlockState) =>
        ModelDb.RelicPool<IroncladRelicPool>().GetUnlockedRelics(unlockState)
            .Append(ModelDb.Relic<SolarHairClasp>());
}
