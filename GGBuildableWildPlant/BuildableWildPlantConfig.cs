using STRINGS;
using UnityEngine;

namespace GGGMod.BuildableWildPlant {
    public class BuildableWildPlantConfig : IBuildingConfig {
        public const string ID = "GgBuildableWildPlant";
        public override BuildingDef CreateBuildingDef() {
            var m_materials = new string[1] { $"{TUNING.MATERIALS.FARMABLE[0]}&{TUNING.MATERIALS.RAW_MINERALS_OR_METALS[0]}" };
            var buildingdef = BuildingTemplates.CreateBuildingDef(
                ID, 1, 1, "ggplanttile_kanim", 30, 30f,
                Settings.constractionsMass,
                m_materials,
                1600f,
                BuildLocationRule.Anywhere,
                noise: TUNING.NOISE_POLLUTION.NONE,
                decor: TUNING.BUILDINGS.DECOR.NONE
            );
            buildingdef.ObjectLayer = ObjectLayer.Backwall;
            buildingdef.SceneLayer = Grid.SceneLayer.Backwall;
            buildingdef.ForegroundLayer = Grid.SceneLayer.BuildingBack;
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
            BuildingTemplates.CreateDefaultStorage(go).SetDefaultStoredItemModifiers(Storage.StandardSealedStorage);
            go.AddOrGet<CopyBuildingSettings>().copyGroupTag = GameTags.Farm;
            go.AddOrGet<BuildableWildPlant>();
        }

        public override void DoPostConfigureComplete(GameObject go) {
            GeneratedBuildings.RemoveLoopingSounds(go);
        }
    }
}
