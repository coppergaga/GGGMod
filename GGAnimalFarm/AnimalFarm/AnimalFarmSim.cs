using STRINGS;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GGGMod.AnimalFarm {
    public class AnimalFarmSim {
        public AnimalFarm master;
        private readonly Dictionary<Tag, int> mButchersRec = new Dictionary<Tag, int>();
        private readonly Dictionary<Tag, int> mDropsRec = new Dictionary<Tag, int>();
        private readonly Dictionary<Tag, int> mGrownDropsRec = new Dictionary<Tag, int>();
        private readonly Dictionary<Tag, Dictionary<string, float>> mButcherDropsCache = new Dictionary<Tag, Dictionary<string, float>>();
        private readonly Dictionary<Tag, List<KeyValuePair<Tag, float>>> mDailyPoopsCache = new Dictionary<Tag, List<KeyValuePair<Tag, float>>>();
        private readonly Dictionary<Tag, Tuple<Tag, float>> mShearDropsCache = new Dictionary<Tag, Tuple<Tag, float>>();
        private readonly HashSet<Tag> mCannotButchCache = new HashSet<Tag>();
        private readonly HashSet<Tag> mCannotDropCache = new HashSet<Tag>();
        private readonly HashSet<Tag> mCannotShearCache = new HashSet<Tag>();

        private const float POOP_SOLID_BASE = 20f;
        private const float POOP_LIQUID_BASE = 40f;
        private const float POOP_GAS_BASE = 5f;

        public void SimStoreData(List<StoredData> storedAnimals, float incubationEffect) {
            bool hasButcher = false;
            for (int i = 0; i < storedAnimals.Count; i++) {
                var sd = storedAnimals[i];
                if (sd.IsAnimal) {
                    if (sd.MaxAge > 0 && sd.age > sd.MaxAge) {
                        sd.type |= StoredFlags.MarkButcher;
                        hasButcher = true;
                        RecordDrops(sd.PrefabTag, mButchersRec);
                    }
                    else if (sd.IsThreshold()) {
                        RecordDrops(sd.PrefabTag, mDropsRec);
                    }
                    else if (AwfulDropDict.TryGetValue(sd.PrefabTag, out var crabDrops) && sd.IsReachingThreshold()) {
                        RecordDrops(sd.prefabTag, mGrownDropsRec);
                    }
                    // 写这里会导致信息记录比实际的大1周期, 但是也仅此而已, 不会影响正常逻辑
                    // 因为屠宰标记的判断在先
                    sd.age += 1;
                }
                else {
                    if (sd.incubation >= 100) {
                        sd.type |= StoredFlags.Animal;
                        sd.age = 0f;
                    }
                    else {
                        sd.incubation += (sd.IncubationBaseRate + incubationEffect);
                    }
                }
                storedAnimals[i] = sd;
            }
            if (hasButcher) {
                storedAnimals.RemoveAll(StoredData.ShouldButcher);
            }
            storedAnimals.Sort(StoredData.EggAnimalAgeDecreased);
        }

        private void RecordDrops(Tag prefabTag, Dictionary<Tag, int> ret) {
            if (!ret.TryGetValue(prefabTag, out var cnt)) { cnt = 0; }
            ret[prefabTag] = ++cnt;
        }

        private void ClearDrops() {
            mButchersRec.Clear();
            mDropsRec.Clear();
            mGrownDropsRec.Clear();
        }

        public void SpawnAnimalDrop() {
            foreach (var kvp in mButchersRec) {
                if (kvp.Value <= 0) { continue; }
                var animalPrefabTag = kvp.Key; var animalNum = kvp.Value;
                if (mButcherDropsCache.TryGetValue(animalPrefabTag, out var butcherDropsDict)) {
                    SpawnButcherDrop(butcherDropsDict, animalNum);
                }
                else if (!mCannotButchCache.Contains(animalPrefabTag)) {
                    var comp = Assets.GetPrefab(animalPrefabTag).GetComponent<Butcherable>();
                    if (comp == null || comp.drops == null) { mCannotButchCache.Add(animalPrefabTag); continue; }
                    mButcherDropsCache[animalPrefabTag] = comp.drops;
                    SpawnButcherDrop(comp.drops, animalNum);
                }
            }

            float multiplier = Mathf.Ceil(master.ProduceEffect);
            float uptime = master.LastCycleUptime;
            foreach (var kvp in mDropsRec) {
                if (kvp.Value <= 0) { continue; }
                var animalPrefabTag = kvp.Key; var animalNum = kvp.Value;
                if (mDailyPoopsCache.TryGetValue(animalPrefabTag, out var poopsDict)) {
                    BatchSpawnDailyPoops(animalNum, poopsDict, multiplier);
                }
                else if (!mCannotDropCache.Contains(animalPrefabTag)) {
                    var ccmd = Assets.GetPrefab(animalPrefabTag).GetDef<CreatureCalorieMonitor.Def>();
                    if (ccmd == null || ccmd.diet?.producedTags == null) { mCannotDropCache.Add(animalPrefabTag); continue; }
                    mDailyPoopsCache[animalPrefabTag] = ccmd.diet.producedTags;
                    BatchSpawnDailyPoops(animalNum, ccmd.diet.producedTags, multiplier);
                }

                if (!GetDynamicProbability(uptime)) { continue; }
                // haven't handled: Bee, Moo (can't bagged), Mole, MoleDelicacy
                if (mShearDropsCache.TryGetValue(animalPrefabTag, out var shearDropsTuple)) {
                    SpawnDailyProbablyDrop(animalNum, shearDropsTuple);
                }
                else if (!mCannotShearCache.Contains(animalPrefabTag)) {
                    var animalPrefab = Assets.GetPrefab(animalPrefabTag);
                    var sgmd = animalPrefab.GetDef<ScaleGrowthMonitor.Def>();
                    var wfsd = animalPrefab.GetDef<WellFedShearable.Def>();
                    if (sgmd == null && wfsd == null) { mCannotShearCache.Add(animalPrefabTag); continue; }
                    // sgmd list: Drecko, DreckoPlastic
                    // wfsd list: WoodDeer, GlassDeer, IceBelly, GoldBelly, Raptor
                    var sheardrops = wfsd == null
                        ? new Tuple<Tag, float>(sgmd.itemDroppedOnShear, sgmd.dropMass)
                        : new Tuple<Tag, float>(wfsd.itemDroppedOnShear, wfsd.dropMass);
                    mShearDropsCache[animalPrefabTag] = sheardrops;
                    SpawnDailyProbablyDrop(animalNum, sheardrops);
                }
            }
            foreach (var kvp in mGrownDropsRec) {
                SpawnGrownDrop(kvp.Value, AwfulDropDict[kvp.Key.Name]);
            }
            ClearDrops();
        }
        // 屠宰产出
        private void SpawnButcherDrop(Dictionary<string, float> dropsKV, float multiplier = 1f) {
            int num = 0;
            foreach (KeyValuePair<string, float> drop in dropsKV) {
                GameObject gameObject = Scenario.SpawnPrefab(master.UpRightCellPos, 0, 0, drop.Key);
                gameObject.SetActive(value: true);
                PrimaryElement component = gameObject.GetComponent<PrimaryElement>();
                component.Mass = component.Mass * multiplier * drop.Value;
                component.Temperature = master.Temperature;
                if (gameObject.TryGetComponent<Edible>(out var edibleComp)) {
                    ReportManager.Instance.ReportValue(
                        ReportManager.ReportType.CaloriesCreated,
                        edibleComp.Calories,
                        StringFormatter.Replace(UI.ENDOFDAYREPORT.NOTES.BUTCHERED, "{0}", gameObject.GetProperName()),
                        UI.ENDOFDAYREPORT.NOTES.BUTCHERED_CONTEXT
                    );
                }
                num++;
            }
        }

        // 每日产出
        private void BatchSpawnDailyPoops(int animalNum, List<KeyValuePair<Tag, float>> producedList, float multiplier) {
            for (int i = 0; i < producedList.Count; i++) {
                SpawnDailyPoop(animalNum, producedList[i], multiplier);
            }
        }

        // 每日概率掉落(原先剪毛逻辑)
        private void SpawnDailyProbablyDrop(int animalNum, Tuple<Tag, float> dropTuple) {
            GameObject gameObject = Util.KInstantiate(Assets.GetPrefab(dropTuple.first));
            int cell = master.UpRightCellPos;
            gameObject.transform.SetPosition(Grid.CellToPosCCC(cell, Grid.SceneLayer.Ore));
            PrimaryElement component2 = gameObject.GetComponent<PrimaryElement>();
            component2.Temperature = master.Temperature;
            component2.Mass = Mathf.Ceil(dropTuple.second * (float)animalNum / 10f);
            gameObject.SetActive(value: true);
            Vector2 initial_velocity = new Vector2(Random.Range(-1f, 1f) * 1f, Random.value * 2f + 2f);
            if (GameComps.Fallers.Has(gameObject)) {
                GameComps.Fallers.Remove(gameObject);
            }

            GameComps.Fallers.Add(gameObject, initial_velocity);
        }

        private void SpawnDailyPoop(int animalNum, KeyValuePair<Tag, float> produceKV, float multiplier) {
            Tag pe = produceKV.Key;
            float convertRate = produceKV.Value * multiplier;
            string text = null;
            Element element = ElementLoader.GetElement(pe);
            Sprite mainIcon = null;
            Color iconTint = Color.white;
            if (element != null) {
                text = element.name;
                mainIcon = global::Def.GetUISprite(element).first;
                iconTint = (element.IsSolid ? Color.white : ((Color)element.substance.colour));
            }

            int dropToCell = master.UpRightCellPos;
            float temperature = master.Temperature;
            if (element == null) {
                GameObject pePrefab = Assets.GetPrefab(pe);
                GameObject pePrefabInst = GameUtil.KInstantiate(pePrefab, Grid.CellToPos(dropToCell, CellAlignment.Top, Grid.SceneLayer.Ore), Grid.SceneLayer.Ore);
                PrimaryElement peComp = pePrefabInst.GetComponent<PrimaryElement>();
                peComp.Mass = peComp.Mass * animalNum;
                peComp.Temperature = temperature;
                pePrefabInst.SetActive(value: true);
                text = pePrefabInst.GetProperName();
                mainIcon = global::Def.GetUISprite(pePrefab).first;
            }
            else if (element.IsLiquid) {
                FallingWater.instance.AddParticle(dropToCell, element.idx, POOP_LIQUID_BASE * animalNum * convertRate, temperature, byte.MaxValue, 0, skip_sound: true);
            }
            else if (element.IsGas) {
                SimMessages.AddRemoveSubstance(dropToCell, element.idx, CellEventLogger.Instance.ElementConsumerSimUpdate, POOP_GAS_BASE * animalNum * convertRate, temperature, byte.MaxValue, 0);
            }
            else {
                element.substance.SpawnResource(Grid.CellToPosCCC(dropToCell, Grid.SceneLayer.Ore), POOP_SOLID_BASE * animalNum * convertRate, temperature, byte.MaxValue, 0);
            }

            PopFX popFX = PopFXManager.Instance.SpawnFX(mainIcon, PopFXManager.Instance.sprite_Plus, text, master.transform, Vector3.zero);
            if (popFX != null) { popFX.SetIconTint(iconTint); }
        }
        
        private void SpawnGrownDrop(int animalNum, Tuple<Tag, float> dropTuple) {
            int cell = master.UpRightCellPos;
            GameObject obj = Util.KInstantiate(Assets.GetPrefab(dropTuple.first));
            obj.transform.SetPosition(Grid.CellToPosCCC(cell, Grid.SceneLayer.Creatures));
            obj.GetComponent<PrimaryElement>().Mass *= dropTuple.second;
            obj.SetActive(value: true);
        }

        private readonly Dictionary<Tag, Tuple<Tag, float>> AwfulDropDict = new Dictionary<Tag, Tuple<Tag, float>>() {
            { "CrabEgg", new Tuple<Tag, float>("CrabShell", 5) },
            { "CrabBaby", new Tuple<Tag, float>("CrabShell", 5) },
            { "Crab", new Tuple<Tag, float>("CrabShell", 5) },
            { "CrabWoodEgg", new Tuple<Tag, float>("CrabWoodShell", 5) },
            { "CrabWoodBaby", new Tuple<Tag, float>("CrabWoodShell", 5) },
            { "CrabWood", new Tuple<Tag, float>("CrabWoodShell", 5) },
        };

        private bool GetDynamicProbability(float progress) {
            float successThreshold = Mathf.Lerp(0.1f, 0.8f, Mathf.Clamp01(progress));
            float roll = Random.value;
            return roll < successThreshold;
        }

        private void TestFormattedAnimalStoreData(string str) {
            Debug.Log($"ggg===Stored Animal Infos {str}:\n" + string.Join("\n", master.StoredDatas.Select(sd => $"{sd.prefabTag.Name} age:{sd.age} incubation:{sd.incubation}")));
        }
    }
}
