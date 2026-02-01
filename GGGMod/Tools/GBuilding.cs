using System.Collections.Generic;
using HarmonyLib;
using TUNING;

namespace GGGMod.Tools {
    public class GBuilding : GHarmonyPatchBase<GBuilding> {
        private readonly List<BuildingInfo> planScreenInfos = new List<BuildingInfo>();
        protected override void AfterInit() {
            Harmony.Patch(
                typeof(Db),
                "Initialize",
                postfix: new HarmonyMethod(typeof(GBuilding), nameof(Db_Initialize_Postfix))
            );
        }

        private static void Db_Initialize_Postfix() {
            foreach (BuildingInfo buildingInfo in Instance.planScreenInfos) {
                if (!buildingInfo.IsInfoValid) { return; }

                if (buildingInfo.IsAddTech) {
                    AddBuildingToTech(buildingInfo.techID, buildingInfo.buildingID);
                }

                if (buildingInfo.IsAddPlan) {
                    AddPlanScreen(buildingInfo.category, buildingInfo.subcategoryID, buildingInfo.buildingID);
                }
            }
        }
        /// <summary>
        /// 添加建筑到研究中
        /// </summary>
        public static void AddBuildingToTech(string techID, string buildingID) {
            var tech = Db.Get().Techs?.TryGet(techID);
            if (tech != null)
                tech.unlockedItemIDs?.Add(buildingID);
            else
                Debug.LogWarning("AddBuildingToTech() Failed to find tech ID: " + techID);
        }
        /// <summary>
        /// 添加建筑到建造栏
        /// </summary>
        public static void AddPlanScreen(HashedString category, string subcategoryID, string buildingID) {
            if (subcategoryID != null && BUILDINGS.PLANSUBCATEGORYSORTING != null) {
                if (!BUILDINGS.PLANSUBCATEGORYSORTING.ContainsKey(buildingID)) {
                    BUILDINGS.PLANSUBCATEGORYSORTING[buildingID] = subcategoryID;
                }
            }
            ModUtil.AddBuildingToPlanScreen(category, buildingID, subcategoryID);
        }

        public static void AddBuildings(BuildingInfo bi) {
            if (bi.onlyDlc1 && !DlcManager.IsExpansion1Active()) {
                return;
            }
            Instance.planScreenInfos.Add(bi);
        }
    }

    public class BuildingInfo {
        public bool IsInfoValid => buildingID != null;
        public bool IsAddPlan => category != null;
        public bool IsAddTech => techID != null;
        public bool onlyDlc1;

        public HashedString category;
        public string techID;
        public string buildingID;
        public string subcategoryID;
        public BuildingInfo(string buildingID, HashedString category, string subcategoryID = null, string techID = null, bool onlyDlc1 = false) {
            this.onlyDlc1 = onlyDlc1;
            this.category = category;
            this.techID = techID;
            this.buildingID = buildingID;
            this.subcategoryID = subcategoryID;
        }
    }
}
