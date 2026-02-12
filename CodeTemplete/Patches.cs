using HarmonyLib;
using KMod;
using System;
using System.IO;
using System.Reflection;

namespace GGGMod.BuildableWildPlant {
    public class Patches : UserMod2 {
        public static string gModPath;
        public override void OnLoad(Harmony harmony) {
            base.OnLoad(harmony);
            gModPath = mod.ContentPath;
            if (TryFind(typeof(Localization), "Initialize", out var method1)) {
                harmony.Patch(method1, postfix: new HarmonyMethod(typeof(PatchManager), nameof(PatchManager.Localization_Initialize_Patch)));
            }
            if (TryFind(typeof(Db), "Initialize", out var method)) {
                harmony.Patch(method, postfix: new HarmonyMethod(typeof(PatchManager), nameof(PatchManager.Db_Initialize_Postfix)));
            }
        }

        private bool TryFind(Type clazz, string methodName, out MethodInfo method) {
            try {
                method = clazz.GetMethod(methodName,
                    BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
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
    }

    public static class PatchManager {
        public static void Localization_Initialize_Patch() {
            Type root = typeof(GGGMod.BuildableWildPlant.STRINGS);
            //Localization.GenerateStringsTemplate(root, Path.Combine(Patches.gModPath, "strings_templates"));
            Localization.RegisterForTranslation(root);
            var localeCode = Localization.GetLocale()?.Code;
            if (!localeCode.IsNullOrWhiteSpace()) {
                var path = Path.Combine(Patches.gModPath, "translations", localeCode + ".po");
                if (File.Exists(path)) {
                    Localization.OverloadStrings(Localization.LoadStringsFile(path, false));
                }
            }
            LocString.CreateLocStringKeys(typeof(GGGMod.BuildableWildPlant.STRINGS.BUILDINGS));
        }
        public static void Db_Initialize_Postfix() {
            //AddBuildingToTech("Agriculture", BuildingConfig.ID);
            //AddPlanScreen("Food", "GGGMod", BuildingConfig.ID);
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
