using HarmonyLib;
using KMod;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace GGGMod.AnimalFarm {
    public class Patches : UserMod2 {
        public static string gModPath;
        public override void OnLoad(Harmony harmony) {
            base.OnLoad(harmony);
            AnimalFarmSettings.Init();
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
            Type root = typeof(GGGMod.AnimalFarm.STRINGS);
            Localization.RegisterForTranslation(root);
            var localeCode = Localization.GetLocale()?.Code;
            if (!localeCode.IsNullOrWhiteSpace()) {
                var path = Path.Combine(Patches.gModPath, "translations", localeCode + ".po");
                if (File.Exists(path)) {
                    Localization.OverloadStrings(Localization.LoadStringsFile(path, false));
                }
            }
            LocString.CreateLocStringKeys(typeof(GGGMod.AnimalFarm.STRINGS.BUILDINGS));
            LocString.CreateLocStringKeys(typeof(GGGMod.AnimalFarm.STRINGS.BUILDING));
        }
        public static void Db_Initialize_Postfix() {
            AddBuildingToTech("AnimalControl", AnimalFarmConfig.ID);
            AddPlanScreen("Food", "GGGMod", AnimalFarmConfig.ID);
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

    public class AnimalFarmSettings {
        public static float waterConsumeKgPerSenond;
        public static float toxicSandConvertKgPerSenond;
        public static float powerConsume;
        public static float dailyPoopMultiplier;
        public static float dailyShearMultiplier;

        public static void Init() {
            string settingPath = ModCachePath();
            if (File.Exists(settingPath)) {
                try {
                    string json = File.ReadAllText(settingPath);
                    var settingMaps = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                    if (settingMaps == null) {
                        DefaultInit();
                        WriteDefaultSettings();
                    }
                    else {
                        waterConsumeKgPerSenond = TryGetSettings(settingMaps, WATERKEY, false);
                        toxicSandConvertKgPerSenond = TryGetSettings(settingMaps, TOXICKEY, false);
                        powerConsume = TryGetSettings(settingMaps, POWERKEY, false);
                        dailyPoopMultiplier = TryGetSettings(settingMaps, POOOPKEY, true);
                        dailyShearMultiplier = TryGetSettings(settingMaps, SHEARKEY, true);
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
            else {
                DefaultInit();
            }
        }

        private static float TryGetSettings(Dictionary<string, string> map, string key, bool mustGreaterThanZero) {
            if (map.TryGetValue(WATERKEY, out string sValue) &&
                float.TryParse(sValue, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var fValue)) {
                if (!mustGreaterThanZero) { return fValue; }
                if (mustGreaterThanZero && fValue > 0) { return fValue; }
            }
            return DefaultSettings[key];
        }

        private static void DefaultInit() {
            waterConsumeKgPerSenond = DefaultSettings[WATERKEY];
            toxicSandConvertKgPerSenond = DefaultSettings[TOXICKEY];
            powerConsume = DefaultSettings[POWERKEY];
            dailyPoopMultiplier = DefaultSettings[POOOPKEY];
            dailyShearMultiplier = DefaultSettings[SHEARKEY];
        }

        private static void WriteDefaultSettings() {
            try {
                string json = JsonConvert.SerializeObject(SS, Formatting.Indented);
                File.WriteAllText(ModCachePath(), json);
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

        private static string ModCachePath() {
            string saveFilePath = SaveLoader.GetActiveSaveFilePath();
            string folder = Path.GetDirectoryName(saveFilePath);
            return Path.Combine(folder, SETTINGS_FINENAME);
        }

        private static readonly string SETTINGS_FINENAME = "ggg_animalfarm_modsettings.json";
        private static readonly string WATERKEY = "water_consume_kg_per_senond";
        private static readonly string TOXICKEY = "toxic_sand_convert_kg_per_senond";
        private static readonly string POWERKEY = "power_consume";
        private static readonly string POOOPKEY = "daily_poop_multiplier";
        private static readonly string SHEARKEY = "daily_shear_multiplier";

        public static Dictionary<string, float> DefaultSettings = new Dictionary<string, float> {
            { WATERKEY, 1f },
            { TOXICKEY, 0.2f },
            { POWERKEY, 1200f },
            { POOOPKEY, 1f },
            { SHEARKEY, 1f },
        };
        private static Dictionary<string, string> SS => DefaultSettings.ToDictionary(
                item => item.Key,
                item => item.Value.ToString()
            );
    }
}
