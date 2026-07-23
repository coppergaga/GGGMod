using STRINGS;
using UnityEngine;

namespace GGGMod.AnimalFarm {
    public class FishpodConfig : IBuildingConfig {
        public const string ID = "GgFishpod";
        public override BuildingDef CreateBuildingDef() {
            BuildingDef obj = BuildingTemplates.CreateBuildingDef(
                ID, 3, 2, "ggfishpod_kanim", 30, 30f,
                TUNING.BUILDINGS.CONSTRUCTION_MASS_KG.TIER4,
                TUNING.MATERIALS.ALL_METALS,
                1600f,
                BuildLocationRule.OnBackWall,
                noise: TUNING.NOISE_POLLUTION.NOISY.TIER1,
                decor: TUNING.BUILDINGS.DECOR.NONE);
            obj.Floodable = false;
            obj.Overheatable = false;
            obj.AudioCategory = "Metal";
            if (AnimalFarmSettings.powerConsume > 0) {
                obj.RequiresPowerInput = true;
                obj.EnergyConsumptionWhenActive = AnimalFarmSettings.powerConsume;
                obj.PowerInputOffset = new CellOffset(1, 0);
            }
            obj.LogicInputPorts = LogicOperationalController.CreateSingleInputPortList(new CellOffset(-1, 0));
            obj.AddSearchTerms(SEARCH_TERMS.FOOD);
            obj.AddSearchTerms(SEARCH_TERMS.FARM);
            return obj;
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
            storage.storageFilters = TUNING.STORAGEFILTERS.SWIMMING_CREATURES;

            go.AddOrGet<TreeFilterable>().dropIncorrectOnFilterChange = false;
        }

        public override void DoPostConfigureComplete(GameObject go) {
            go.AddOrGet<LogicOperationalController>();
            RoomTracker roomTracker = go.AddOrGet<RoomTracker>();
            roomTracker.requiredRoomType = Db.Get().RoomTypes.CreaturePen.Id;
            roomTracker.requirement = RoomTracker.Requirement.Required;
            go.AddOrGet<AnimalFarm>().FType = AnimalFarm.FarmType.SwimmingCreatures;
        }
    }
}
