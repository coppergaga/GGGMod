using KMod;
using HarmonyLib;

namespace GGGMod.Tools {
    public abstract class GHarmonyPatchBase<T> where T : GHarmonyPatchBase<T>, new() {

        protected static T Instance;

        protected Mod Mod { get; private set; }
        protected Harmony Harmony { get; private set; }
        public static void Setup(Mod mod, Harmony harmony) {
            if (Instance == null) {
                Instance = new T() { Mod = mod, Harmony = harmony };
                Instance.AfterInit();
            }
        }
        protected virtual void AfterInit() {
        }
    }
}
