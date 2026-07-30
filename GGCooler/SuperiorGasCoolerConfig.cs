using System.Collections.Generic;
using UnityEngine;

namespace GGGMod.SuperiorCooler {
    public class SuperiorGasCoolerConfig : IBuildingConfig {
        public const string ID = "GgSuperiorGasCooler";
        public override BuildingDef CreateBuildingDef() {
            var obj = BuildingTemplates.CreateBuildingDef(
                ID, 3, 1, "ggsuperiorgascooler_kanim", 100, 120f,
                TUNING.BUILDINGS.CONSTRUCTION_MASS_KG.TIER6,
                TUNING.MATERIALS.ALL_METALS,
                1600f,
                BuildLocationRule.Anywhere,
                noise: TUNING.NOISE_POLLUTION.NOISY.TIER3,
                decor: TUNING.BUILDINGS.DECOR.NONE
            );
            BuildingTemplates.CreateElectricalBuildingDef(obj);
            obj.EnergyConsumptionWhenActive = 240;
            obj.SelfHeatKilowattsWhenActive = 0f;
            obj.InputConduitType = ConduitType.Gas;
            obj.OutputConduitType = ConduitType.Gas;
            obj.Floodable = false;
            obj.PowerInputOffset = new CellOffset(0, 0);
            obj.UtilityInputOffset = new CellOffset(1, 0);
            obj.UtilityOutputOffset = new CellOffset(-1, 0);
            obj.PermittedRotations = PermittedRotations.R360;
            obj.ViewMode = OverlayModes.GasConduits.ID;
            obj.OverheatTemperature = 398.15f;
            obj.LogicInputPorts = LogicOperationalController.CreateSingleInputPortList(new CellOffset(1, 0));
            GeneratedBuildings.RegisterWithOverlay(OverlayScreen.GasVentIDs, ID);
            return obj;
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag) {
            go.AddOrGet<LoopingSounds>();
            SuperiorCooler cooler = go.AddOrGet<SuperiorCooler>();
            cooler.isLiquidConditioner = false;
            ConduitConsumer conduitConsumer = go.AddOrGet<ConduitConsumer>();
            conduitConsumer.conduitType = ConduitType.Gas;
            conduitConsumer.consumptionRate = 1f;
            Storage storage = BuildingTemplates.CreateDefaultStorage(go);
            storage.showInUI = true;
            storage.capacityKg = 2f * conduitConsumer.consumptionRate;
            storage.SetDefaultStoredItemModifiers(StoredItemModifiers);
        }

        public override void DoPostConfigureComplete(GameObject go) {
            go.AddOrGet<LogicOperationalController>();
            go.AddOrGetDef<SuperiorCoolerAnimController.Def>();
            go.GetComponent<KPrefabID>().AddTag(GameTags.OverlayBehindConduits);
        }

        private static readonly List<Storage.StoredItemModifier> StoredItemModifiers = new List<Storage.StoredItemModifier> {
            Storage.StoredItemModifier.Hide,
            Storage.StoredItemModifier.Insulate,
            Storage.StoredItemModifier.Seal
        };
    }
}
