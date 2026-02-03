using STRINGS;
using UnityEngine;

namespace GGGMod.BuildableWildPlant {
    public class BuildableWildPlantConfig : IBuildingConfig {
        public const string ID = "GgBuildableWildPlant";
        public override BuildingDef CreateBuildingDef() {
            var buildingdef = BuildingTemplates.CreateBuildingDef(
                ID, 1, 1, "farmtile_kanim", 30, 30f,
                TUNING.BUILDINGS.CONSTRUCTION_MASS_KG.TIER4,
                TUNING.MATERIALS.FARMABLE,
                1600f,
                BuildLocationRule.Anywhere,
                noise: TUNING.NOISE_POLLUTION.NONE,
                decor: TUNING.BUILDINGS.DECOR.NONE
            );
            buildingdef.Floodable = false;
            buildingdef.Overheatable = false;
            buildingdef.AudioCategory = "HollowMetal";
            buildingdef.AudioSize = "small";
            buildingdef.ConstructionOffsetFilter = BuildingDef.ConstructionOffsetFilter_OneDown;
            buildingdef.DragBuild = true;
            buildingdef.AddSearchTerms(SEARCH_TERMS.FOOD);
            buildingdef.AddSearchTerms(SEARCH_TERMS.FARM);
            return buildingdef;
        }
        public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag) {
            GeneratedBuildings.MakeBuildingAlwaysOperational(go);
            BuildingConfigManager.Instance.IgnoreDefaultKComponent(typeof(RequiresFoundation), prefab_tag);
            Prioritizable.AddRef(go);
            var storage = go.AddOrGet<Storage>();
            storage.capacityKg = 1f;
            storage.showInUI = true;
            storage.allowItemRemoval = false;
            storage.showDescriptor = true;
            storage.storageFilters = new System.Collections.Generic.List<Tag> { GameTags.Seed };
            storage.storageFullMargin = TUNING.STORAGE.STORAGE_LOCKER_FILLED_MARGIN;
            storage.fetchCategory = Storage.FetchCategory.GeneralStorage;
            storage.showCapacityStatusItem = true;
            storage.showCapacityAsMainStatus = true;
            go.AddOrGet<CopyBuildingSettings>().copyGroupTag = GameTags.Seed;
            go.AddOrGet<BuildableWildPlant>();
        }

        public override void DoPostConfigureComplete(GameObject go) {
            GeneratedBuildings.RemoveLoopingSounds(go);
            go.AddOrGetDef<StorageController.Def>();
        }
    }
}
