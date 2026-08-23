using HarmonyLib;
using Klei;
using KMod;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace GGGMod.GGSpacefarerModule {
    public class Patches : UserMod2 {
        public static string gModPath;
        public override void OnLoad(Harmony harmony) {
            base.OnLoad(harmony);
            gModPath = mod.ContentPath;
            CopyTemplateToGameSrcPath();
            if (TryFind(typeof(Localization), "Initialize", out var method1)) {
                harmony.Patch(method1, postfix: new HarmonyMethod(typeof(PatchManager), nameof(PatchManager.Localization_Initialize_Patch)));
            }
            if (TryFind(typeof(Db), "Initialize", out var method)) {
                harmony.Patch(method, postfix: new HarmonyMethod(typeof(PatchManager), nameof(PatchManager.Db_Initialize_Postfix)));
            }
            if (TryFind(typeof(ClusterManager), "CreateRocketInteriorWorld", out var method2)) {
                harmony.Patch(
                    method2,
                    prefix: new HarmonyMethod(typeof(PatchManager), nameof(PatchManager.ClusterManager_CreateRocketInteriorWorld_Prefix)),
                    postfix: new HarmonyMethod(typeof(PatchManager), nameof(PatchManager.ClusterManager_CreateRocketInteriorWorld_Postfix))
                );
            }
        }

        private bool TryFind(Type clazz, string methodName, out MethodInfo method) {
            try {
                method = clazz.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (method != null) { return true; }
                else {
                    Debug.LogWarningFormat("Unable to find method {0} on type {1}", methodName, clazz.FullName);
                    return false;
                }
            }
            catch (AmbiguousMatchException ex) {
                Debug.LogException(ex);
                method = null;
                return false;
            }
        }

        private void CopyTemplateToGameSrcPath() {
            var path = FileSystem.Normalize(
                    Path.Combine(Application.streamingAssetsPath, "dlc","expansion1","templates","interiors", "ggg_habitat_promax.yaml")
                );
            if (File.Exists(path)) { return; }
            var srcPath = Path.Combine(gModPath, "templates", "ggg_habitat_promax.yaml");
            File.Copy(srcPath, path);
        }
    }

    public static class PatchManager {
        public static Vector2I INTERIOR_SIZE = new Vector2I(64, 64);
        public static Vector2I ORIGINAL_SIZE = new Vector2I(32, 32);
        public static bool ClusterManager_CreateRocketInteriorWorld_Prefix(string interiorTemplateName) {
            if (interiorTemplateName.Contains("ggg_habitat_promax")) {
                TUNING.ROCKETRY.ROCKET_INTERIOR_SIZE = INTERIOR_SIZE;
            }
            return true;
        }
        public static void ClusterManager_CreateRocketInteriorWorld_Postfix(string interiorTemplateName) {
            if (interiorTemplateName.Contains("ggg_habitat_promax")) {
                TUNING.ROCKETRY.ROCKET_INTERIOR_SIZE = ORIGINAL_SIZE;
            }
        }

        public static void Localization_Initialize_Patch() {
            Type root = typeof(GGGMod.GGSpacefarerModule.STRINGS);
            //Localization.GenerateStringsTemplate(root, Path.Combine(Patches.gModPath, "translations"));
            Localization.RegisterForTranslation(root);
            var localeCode = Localization.GetLocale()?.Code;
            if (!localeCode.IsNullOrWhiteSpace()) {
                var path = Path.Combine(Patches.gModPath, "translations", localeCode + ".po");
                if (File.Exists(path)) {
                    Localization.OverloadStrings(Localization.LoadStringsFile(path, false));
                }
            }
            LocString.CreateLocStringKeys(typeof(GGGMod.GGSpacefarerModule.STRINGS.BUILDINGS));
        }
        public static void Db_Initialize_Postfix() {
            AddBuildingToTech("DurableLifeSupport", SpacefarerModuleProMaxConfig.ID);
            if (!SelectModuleSideScreen.moduleButtonSortOrder.Contains(SpacefarerModuleProMaxConfig.ID)) {
                SelectModuleSideScreen.moduleButtonSortOrder.Add(SpacefarerModuleProMaxConfig.ID);
            }
        }

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
            if (subcategoryID != null && TUNING.BUILDINGS.PLANSUBCATEGORYSORTING != null) {
                if (!TUNING.BUILDINGS.PLANSUBCATEGORYSORTING.ContainsKey(buildingID)) {
                    TUNING.BUILDINGS.PLANSUBCATEGORYSORTING[buildingID] = subcategoryID;
                }
            }
            ModUtil.AddBuildingToPlanScreen(category, buildingID, subcategoryID);
        }
    }
}
