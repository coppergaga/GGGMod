using STRINGS;
using UnityEngine;

namespace GGGMod.AnimalFarm {
    public class AnimalFarmConfig : IBuildingConfig {
        public const string ID = "GgAnimalFarm";
        public override BuildingDef CreateBuildingDef() {
            var buildingdef = BuildingTemplates.CreateBuildingDef(
                ID, 5, 3, "gganimalfarm_kanim", 100, 30f,
                TUNING.BUILDINGS.CONSTRUCTION_MASS_KG.TIER5,
                TUNING.MATERIALS.RAW_METALS,
                1600f,
                BuildLocationRule.OnFloor,
                noise: TUNING.NOISE_POLLUTION.NONE,
                decor: TUNING.BUILDINGS.DECOR.NONE
            );
            if (AnimalFarmSettings.powerConsume > 0) {
                buildingdef.RequiresPowerInput = true;
                buildingdef.EnergyConsumptionWhenActive = AnimalFarmSettings.powerConsume;
                buildingdef.PowerInputOffset = new CellOffset(1, 0);
            }
            buildingdef.LogicInputPorts = LogicOperationalController.CreateSingleInputPortList(new CellOffset(-1, 0));
            buildingdef.Floodable = false;
            buildingdef.Overheatable = false;
            buildingdef.AudioCategory = "Metal";
            buildingdef.AddSearchTerms(SEARCH_TERMS.FOOD);
            buildingdef.AddSearchTerms(SEARCH_TERMS.FARM);
            return buildingdef;
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag) {
            go.AddOrGet<Operational>();

            var storage = go.AddComponent<Storage>();
            storage.capacityKg = 99999999f;
            storage.showInUI = true;
            storage.allowItemRemoval = false;
            storage.allowSettingOnlyFetchMarkedItems = false;
            storage.fetchCategory = Storage.FetchCategory.StorageSweepOnly;
            storage.showDescriptor = false;
            storage.storageFilters = TUNING.STORAGEFILTERS.BAGABLE_CREATURES;

            go.AddOrGet<TreeFilterable>().dropIncorrectOnFilterChange = false;
            go.AddOrGet<UserNameable>();
        }

        public override void DoPostConfigureComplete(GameObject go) {
            go.AddOrGet<LogicOperationalController>();
            RoomTracker roomTracker = go.AddOrGet<RoomTracker>();
            roomTracker.requiredRoomType = Db.Get().RoomTypes.CreaturePen.Id;
            roomTracker.requirement = RoomTracker.Requirement.Required;
            go.AddOrGet<AnimalFarm>();
            AddVisualizer(go);
        }

        public override void DoPostConfigurePreview(BuildingDef def, GameObject go) {
            AddVisualizer(go);
        }

        public override void DoPostConfigureUnderConstruction(GameObject go) {
            AddVisualizer(go);
        }

        private static void AddVisualizer(GameObject prefab) {
            RangeVisualizer rangeVisualizer = prefab.AddOrGet<RangeVisualizer>();
            rangeVisualizer.OriginOffset = new Vector2I(0, 0);
            rangeVisualizer.RangeMin.x = -AnimalFarm.DetectRange.x;
            rangeVisualizer.RangeMax.x = AnimalFarm.DetectRange.x;
            rangeVisualizer.RangeMin.y = 0;
            rangeVisualizer.RangeMax.y = AnimalFarm.DetectRange.y - 1;
            rangeVisualizer.BlockingTileVisible = true;
        }
    }
}
