using KSerialization;
using System;
using UnityEngine;

namespace GGGMod.AnimalFarm {
    //myStatus |= StoredFlags.MarkDelete;       // add
    //myStatus &= ~StoredFlags.MarkDelete;      // remove
    //(myStatus & StoredFlags.Wild) != 0;       // contains
    [Flags]
    public enum StoredFlags : int {
        None = 0,
        Wild = 1 << 0,
        Egg = 1 << 1,
        Animal = 1 << 2,
        MarkDelete = 1 << 3,
        MarkButcher = 1 << 4,
    }

    [Serializable]
    [SerializationConfig(MemberSerialization.OptIn)]
    public struct StoredData {
        [Serialize] public Tag prefabTag;
        [Serialize] public StoredFlags type;
        [Serialize] public float age;
        [Serialize] public float incubation;
        [Serialize] public float wildness;

        public float IncubationBaseRate => InfoManager.Inst.BaseIncubation(prefabTag);
        public float MaxAge => InfoManager.Inst.MaxAge(prefabTag);
        public Tag PrefabTag {
            get {
                if (IsAnimal) {
                    var threshold = InfoManager.Inst.Threshold(prefabTag);
                    if (threshold >= 0) {
                        if (age > threshold) { return InfoManager.Inst.AdultPrefabTag(prefabTag); }
                        else { return InfoManager.Inst.BabyPrefabTag(prefabTag); }
                    }
                }
                return prefabTag;
            }
        }

        public bool IsThreshold() {
            var threshold = InfoManager.Inst.Threshold(prefabTag);
            return threshold >= 0 && age > threshold;
        }

        public bool IsReachingThreshold() {
            var threshold = InfoManager.Inst.Threshold(prefabTag);
            return (threshold >= 0 && age == (threshold + 1));
        }

        public bool IsAnimal => (type & StoredFlags.Animal) != 0;
        public bool IsWild => (type & StoredFlags.Wild) != 0;
        public bool IsNeedButcher => (type & StoredFlags.MarkButcher) != 0;
        public bool IsNeedDelete => (type & StoredFlags.MarkDelete) != 0;

        public static bool ShouldDelete(StoredData data) => (data.type & StoredFlags.MarkDelete) != 0;
        public static bool ShouldButcher(StoredData data) => (data.type & StoredFlags.MarkButcher) != 0;

        public static int EggAnimalAgeDecreased(StoredData lhs, StoredData rhs) {
            // MarkDelete 最右, Animal 次之, Egg 最左, Animal和Egg内部 Wild最左
            int wa = lhs.IsNeedDelete ? 1000 : (lhs.IsAnimal ? 200 : 100) - (lhs.IsWild ? 10 : 0);
            int wb = rhs.IsNeedDelete ? 1000 : (rhs.IsAnimal ? 200 : 100) - (rhs.IsWild ? 10 : 0);
            int ret = wa.CompareTo(wb);
            if (ret != 0) { return ret; }
            if ((lhs.type & StoredFlags.Animal) != 0) { // 年龄降序
                return rhs.age.CompareTo(lhs.age);
            }
            else {    // 孵化度升序
                return lhs.incubation.CompareTo(rhs.incubation);
            }
        }

        public void SpawnAnimal(Vector3 position) {
            if (!IsAnimal) { return; }
            position.z = Grid.GetLayerZ(Grid.SceneLayer.Creatures);
            GameObject go = Util.KInstantiate(Assets.GetPrefab(PrefabTag), position);
            go.SetActive(true);
            Db.Get().Amounts.Wildness.Lookup(go)?.SetValue(IsWild ? wildness : 0f);
            Db.Get().Amounts.Age.Lookup(go)?.SetValue(age);
        }

        public void SpawnEgg(Vector3 position) {
            if (IsAnimal) { return; }
            position.z = Grid.GetLayerZ(Grid.SceneLayer.Ore);
            GameObject go = Util.KInstantiate(Assets.GetPrefab(PrefabTag), position);
            go.SetActive(true);
            Db.Get().Amounts.Wildness.Lookup(go)?.SetValue(IsWild ? wildness : 0f);
            Db.Get().Amounts.Incubation.Lookup(go)?.SetValue(Mathf.Min(incubation, 100f));
        }

        public string DebugDescStr => $"prefabTag:{prefabTag}, typeFlag:{type}, age:{age}, incubation:{incubation}, wildness:{wildness}";
    }
}
