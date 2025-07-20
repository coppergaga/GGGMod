using UnityEngine;

namespace GGGMod.LiquidCooler
{
    public class LiquidCoolerConfig: IBuildingConfig
    {
        public const string ID = "GgLiquidCooler";
        public override BuildingDef CreateBuildingDef()
        {
            var buildingdef = BuildingTemplates.CreateBuildingDef(
                ID, 4, 1, "liquidcooler_kanim", 30, 30f,
                TUNING.BUILDINGS.CONSTRUCTION_MASS_KG.TIER7,
                TUNING.MATERIALS.REFINED_METALS,
                3200f,
                BuildLocationRule.Anywhere,
                TUNING.BUILDINGS.DECOR.PENALTY.TIER1,
                TUNING.NOISE_POLLUTION.NONE,
                0.2f
            );
            buildingdef.RequiresPowerInput = true;
            buildingdef.Floodable = false;
            buildingdef.EnergyConsumptionWhenActive = 960f;
            buildingdef.ExhaustKilowattsWhenActive = -4000f;
            buildingdef.SelfHeatKilowattsWhenActive = -64f;
            buildingdef.ViewMode = OverlayModes.Power.ID;
            buildingdef.AudioCategory = "SolidMetal";
            buildingdef.OverheatTemperature = 473.15f;
            buildingdef.LogicInputPorts = LogicOperationalController.CreateSingleInputPortList(new CellOffset(1, 0));
            return buildingdef;
        }
        public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag)
        {
            go.AddOrGet<LoopingSounds>();
            go.AddOrGet<MinimumOperatingTemperature>().minimumTemperature = 100f;
            SpaceHeater sh = go.AddOrGet<SpaceHeater>();
            sh.SetLiquidHeater();
            sh.targetTemperature = 473.15f; // 200°C
            sh.minimumCellMass = 400f;
        }
        public override void DoPostConfigureComplete(GameObject go)
        {
            go.AddOrGet<LogicOperationalController>();
            go.AddOrGetDef<PoweredActiveController.Def>();
        }

        public const float CONSUMPTION_RATE = 1f;
    }
}
