using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace GGGMod.AnimalFarm {
    public class InfoManager {
        private static InfoManager _inst;
        public static InfoManager Inst {
            get {
                if (_inst == null) { _inst = new InfoManager(); _inst.AfterInit(); }
                _inst.CacheNecessaryInfos();
                return _inst;
            }
        }

        private const string INFO_VERSION = "100005";
        private const string INFO_HEADER = "ggg_info_header";
        private static readonly string FILENAME = "ggg_animalfarm_moddata.json";
        
        /* DATA structure sample:
         *                   info_version, game_version, build_version
         * ggg_info_header: [100000,       u57,          u57-xx-xx]
         * prefab_tag(egg or baby or adult): [EggPrefabTag, BabyPrefabTag, AdultPrefabTag, Threshold(成年周期), MaxAge, BaseIncubation]
         * ... etc ...
         * */
        enum Index : int {
            EggPrefabTag = 0, BabyPrefabTag = 1, AdultPrefabTag = 2, Threshold = 3, MaxAge = 4, IncubationRate = 5,
            ProperName = 6,
        }
        private Dictionary<string, string[]> mInfoMaps;
        private readonly Dictionary<string, float> mFloatCastCache = new Dictionary<string, float>();
        private int mRetryCount = 0;
        private bool mIsCollecting = false;

        public string EggPrefabTag(Tag prefabTag) {
            if (mInfoMaps.TryGetValue(prefabTag.Name, out var val)) {
                return val[(int)Index.EggPrefabTag];
            }
            return "";
        }
        public string BabyPrefabTag(Tag prefabTag) {
            if (mInfoMaps.TryGetValue(prefabTag.Name, out var val)) {
                return val[(int)Index.BabyPrefabTag];
            }
            return "";
        }
        public string AdultPrefabTag(Tag prefabTag) {
            if (mInfoMaps.TryGetValue(prefabTag.Name, out var val)) {
                return val[(int)Index.AdultPrefabTag];
            }
            return "";
        }
        public float Threshold(Tag prefabTag) {
            if (mInfoMaps.TryGetValue(prefabTag.Name, out var val)) {
                var rets = val[(int)Index.Threshold];
                if (mFloatCastCache.TryGetValue(rets, out var retf)) {
                    return retf;
                }
                if (float.TryParse(rets, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var retf2)) {
                    mFloatCastCache[rets] = retf2;
                    return retf2;
                }
            }
            return -1f;
        }
        public float MaxAge(Tag prefabTag) {
            if (mInfoMaps.TryGetValue(prefabTag.Name, out var val)) {
                var rets = val[(int)Index.MaxAge];
                if (mFloatCastCache.TryGetValue(rets, out var retf)) {
                    return retf;
                }
                if (float.TryParse(rets, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var retf2)) {
                    mFloatCastCache[rets] = retf2;
                    return retf2;
                }
            }
            return -1f;
        }
        public float BaseIncubation(Tag prefabTag) {
            if (mInfoMaps.TryGetValue(prefabTag.Name, out var val)) {
                var rets = val[(int)Index.IncubationRate];
                if (mFloatCastCache.TryGetValue(rets, out var retf)) {
                    return retf;
                }
                if (float.TryParse(rets, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var retf2)) {
                    retf2 = Mathf.Round(retf2 * 600);
                    mFloatCastCache[rets] = retf2;
                    return retf2;
                }
            }
            return -1f;
        }
        public string ProperName(Tag prefabTag) {
            if (mInfoMaps.TryGetValue(prefabTag.Name, out var val)) {
                return val[(int)Index.ProperName];
            }
            return prefabTag.Name;
        }

        public void Setup() { }

        private void CacheNecessaryInfos() {
            if (mIsCollecting) { return; }
            mIsCollecting = true;
            if (ShouldCollect()) {
                Debug.Log($"[AnimalFarm]===start cache animal info retrycount={mRetryCount}  mapcout={mInfoMaps.Count}");
                mRetryCount++;
                PrepareData();
                SaveToFile();
            }
            mIsCollecting = false;
        }

        private void AfterInit() {
            LoadFileCache();
            bool isOutdated = false;
            if (mInfoMaps != null && mInfoMaps.TryGetValue(INFO_HEADER, out var ret) && ret.Length > 2) {
                isOutdated = (isOutdated || !string.Equals(ret[0], INFO_VERSION));                     // mod更新
                isOutdated = (isOutdated || !string.Equals(ret[1], LaunchInitializer.BuildPrefix())); // 游戏更新
            }
            else {
                isOutdated = true;
            }
            if (isOutdated) {
                mInfoMaps = new Dictionary<string, string[]>();
            }
        }

        private bool ShouldCollect() {
            if (mInfoMaps.Count < 3 && mRetryCount < 3) { return true; } // 缺少数据并且没有到达重试上限
            return false;
        }

        private void PrepareData() {
            var eggs = Assets.GetPrefabsWithTag(GameTags.Egg);
            for (int i = 0; i < eggs.Count; i++) {
                if (!eggs[i].TryGetComponent<KPrefabID>(out var eggPrefabID)) { continue; }
                if (mInfoMaps.ContainsKey(eggPrefabID.PrefabTag.Name)) { continue; }

                var eggPrefabTag = eggPrefabID.PrefabTag.Name;
                var eggProperName = eggs[i].GetProperName();
                var babyPrefabTag = "";      var adultPrefabTag = "";
                var babyProperName = "";     var adultProperName = "";
                float adultThreshold = 0f; float maxAge = 0f; float baseIncubation = 0f;

                var imd = eggs[i].GetDef<IncubationMonitor.Def>();
                if (imd != null && imd.spawnedCreature != null) {
                    babyPrefabTag = imd.spawnedCreature.Name;
                    baseIncubation = imd.baseIncubationRate;
                    var babyPrefab = Assets.GetPrefab(imd.spawnedCreature);
                    babyProperName = babyPrefab.GetProperName();
                    var bmd = babyPrefab.GetDef<BabyMonitor.Def>();
                    if (bmd != null && bmd.adultPrefab != null) {
                        //TestCollectDropAndPoopInfo(bmd.adultPrefab);
                        adultPrefabTag = bmd.adultPrefab.Name;
                        adultThreshold = bmd.adultThreshold;

                        var adultPrefab = Assets.GetPrefab(adultPrefabTag);
                        adultProperName = adultPrefab.GetProperName();
                        var adultModifiers = adultPrefab.GetComponent<Klei.AI.Modifiers>();
                        if (adultModifiers != null) { maxAge = adultModifiers.GetPreModifiedAttributeValue(Db.Get().Amounts.Age.maxAttribute); }
                    }
                }
                var eggInfos = new string[] { eggPrefabTag, babyPrefabTag, adultPrefabTag, adultThreshold.ToString(), maxAge.ToString(), baseIncubation.ToString(), eggProperName };
                mInfoMaps[eggPrefabTag] = eggInfos;
                if (babyPrefabTag.Length > 0) {
                    var babyInfos = new string[] { eggPrefabTag, babyPrefabTag, adultPrefabTag, adultThreshold.ToString(), maxAge.ToString(), baseIncubation.ToString(), babyProperName };
                    mInfoMaps[babyPrefabTag] = babyInfos;
                }
                if (adultPrefabTag.Length > 0) {
                    var adultInfos = new string[] { eggPrefabTag, babyPrefabTag, adultPrefabTag, adultThreshold.ToString(), maxAge.ToString(), baseIncubation.ToString(), adultProperName };
                    mInfoMaps[adultPrefabTag] = adultInfos;
                }
            }
            FormattedInfoLog();
        }

        public string ModCachePath() {
            string saveFilePath = SaveLoader.GetActiveSaveFilePath();
            string folder = Path.GetDirectoryName(saveFilePath);
            return Path.Combine(folder, FILENAME);
        }

        private void LoadFileCache() {
            string dataPath = ModCachePath();
            if (!File.Exists(dataPath)) { return; }
            try {
                string json = File.ReadAllText(dataPath);
                mInfoMaps = JsonConvert.DeserializeObject<Dictionary<string, string[]>>(json);
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
        private void SaveToFile() {
            mInfoMaps[INFO_HEADER] = new string[] { INFO_VERSION, LaunchInitializer.BuildPrefix(), BuildWatermark.GetBuildText() };
            try {
                string json = JsonConvert.SerializeObject(mInfoMaps, Formatting.Indented);
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

        private void TestCollectDropAndPoopInfo(Tag prefabTag) {
            if (Assets.GetPrefab(prefabTag).TryGetComponent<Butcherable>(out var comp)) {
                Debug.Log($"[AnimalFarm]===ButcherDrops {prefabTag.Name} => " + string.Join(",", comp.drops.Select(kv => $"{kv.Key}:{kv.Value}")));
            }
            var ccmd = Assets.GetPrefab(prefabTag).GetDef<CreatureCalorieMonitor.Def>();
            if (ccmd == null || ccmd.diet?.producedTags == null) { return; }
            Debug.Log($"[AnimalFarm]===Poops {prefabTag.Name} => " + string.Join(",", ccmd.diet.producedTags.Select(kvp => $"{kvp.Key.Name}:{kvp.Value}")));
        }

        private void FormattedInfoLog() {
            int maxKeyWidth = 30;
            int columnCount = 7;
            int[] maxColWidths = new int[] { 30, 30, 30, 5, 5, 15, 60};

            string result = string.Join("\n", mInfoMaps.Select(kvp => {
                string keyPart = kvp.Key.PadRight(maxKeyWidth);
                string[] paddedValues = new string[columnCount];
                for (int i = 0; i < columnCount; i++) {
                    string val = kvp.Value.Length > i ? kvp.Value[i] : "";
                    paddedValues[i] = val.PadRight(maxColWidths[i]);
                }
                return $"{keyPart} [ {string.Join(" | ", paddedValues)} ]";
            }));
            Debug.Log($"[AnimalFarm]===InfoMap =>\n" + result);
        }
    }
}
