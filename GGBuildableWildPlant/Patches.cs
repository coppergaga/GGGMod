using HarmonyLib;
using KMod;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace GGGMod.BuildableWildPlant {
    public class Patches : UserMod2 {
        public static string gModPath;
        public override void OnLoad(Harmony harmony) {
            base.OnLoad(harmony);
            Settings.Init();
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
            AddBuildingToTech("Agriculture", BuildableWildPlantConfig.ID);
            AddPlanScreen("Food", "GGGMod", BuildableWildPlantConfig.ID);
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

    public static class Settings {
        public static float[] constractionsMass;
        public static void Init() {
            string modPath = KMod.Manager.GetDirectory();
            string settingPath = Path.Combine(modPath, SETTINGS_FINENAME);
            if (!File.Exists(settingPath)) {
                DefaultInit();
                return;
            }
            try {
                string json = File.ReadAllText(settingPath);
                var settingMaps = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                if (settingMaps == null) {
                    DefaultInit();
                    string dj = JsonConvert.SerializeObject(new Dictionary<string, string>() { { CONSTRACTIONS_MASS_KEY, "400" } }, Formatting.Indented);
                    File.WriteAllText(settingPath, dj);
                }
                else {
                    if (settingMaps.TryGetValue(CONSTRACTIONS_MASS_KEY, out string sValue)
                        && float.TryParse(sValue, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var fValue)
                        ) {
                        if (fValue > 0 && fValue < float.MaxValue) { constractionsMass = new float[1] { fValue }; }
                    }
                    else { DefaultInit(); }
                }
            }
            catch (IOException e) { // 处理磁盘空间不足、文件被占用等 IO 问题
                Debug.LogError($"[Mod:AnimalFarm] 保存失败 - IO异常 (磁盘空间或权限): {e.Message}");
            }
            catch (System.UnauthorizedAccessException e) { // 处理系统权限拦截
                Debug.LogError($"[Mod:AnimalFarm] 保存失败 - 拒绝访问 (权限不足): {e.Message}");
            }
            catch (System.Exception e) { // 捕获其他所有未预料的错误，防止游戏闪退
                Debug.LogError($"[Mod:AnimalFarm] 保存时发生未知错误: {e.GetType()}\n{e.StackTrace}");
            }
        }

        private static void DefaultInit() {
            constractionsMass = new float[1] { 400f };
        }

        private static readonly string SETTINGS_FINENAME = "config/ggg_bwp_modsettings.json";
        private static readonly string CONSTRACTIONS_MASS_KEY = "constractions_mass";
    }
}
