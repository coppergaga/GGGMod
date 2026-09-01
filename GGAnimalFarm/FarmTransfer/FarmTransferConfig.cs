using STRINGS;
using UnityEngine;

namespace GGGMod.AnimalFarm {
    public class FarmTransferConfig :IBuildingConfig {
        public static string ID = "GgFarmTransfer";

        public override BuildingDef CreateBuildingDef() {
            BuildingDef obj = BuildingTemplates.CreateBuildingDef(
                ID, 1, 1, "ggfarmtransfer_kanim", 30, 30f,
                TUNING.BUILDINGS.CONSTRUCTION_MASS_KG.TIER0, TUNING.MATERIALS.REFINED_METALS,
                1600f, BuildLocationRule.Anywhere,
                noise: TUNING.NOISE_POLLUTION.NONE, decor: TUNING.BUILDINGS.DECOR.PENALTY.TIER0
            );
            obj.Overheatable = false;
            obj.Floodable = false;
            obj.Entombable = false;
            obj.AudioCategory = "Metal";
            obj.SceneLayer = Grid.SceneLayer.Building;
            obj.AddSearchTerms(SEARCH_TERMS.FOOD);
            obj.AddSearchTerms(SEARCH_TERMS.FARM);
            return obj;
        }

        public override void DoPostConfigureComplete(GameObject go) {
            go.AddOrGet<FarmTransfer>();
        }
    }
}
