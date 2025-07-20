using GGGMod.Tools;
using HarmonyLib;

namespace GGGMod {
    public class GGModPatches : KMod.UserMod2 {
        public override void OnLoad(Harmony harmony) {
            base.OnLoad(harmony);
            GLocalization.Setup(mod, harmony);
            GLocalization.RegisterLoad(typeof(GGGMod.STRINGS));
            GLocalization.RegisterAddStrings(typeof(GGGMod.STRINGS.BUILDINGS));
            GBuilding.Setup(mod, harmony);
            GBuilding.AddBuildings(
                new BuildingInfo(
                    GGGMod.LiquidCooler.LiquidCoolerConfig.ID,
                    GTypes.PlanType.Utilities,
                    "GGGMod",
                    GTypes.TechID.LiquidTemperature
                )
            );
        }
    }
}
