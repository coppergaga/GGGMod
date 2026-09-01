using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace GGGMod.AnimalFarm {
    public class FarmTransferSideScreen : GGSideScreenContent {
        [CopyField, SerializeField] private GameObject rowPrefab;
        [CopyField, SerializeField] private GameObject listContainer;
        [CopyField, SerializeField] private LocText headerLabel;
        [CopyField, SerializeField] private GameObject noChannelRow;

        private FarmTransfer targetFarmTransfer;
        private readonly Dictionary<AnimalFarm, GameObject> farmRows = new Dictionary<AnimalFarm, GameObject>();

        public override bool IsValidForTarget(GameObject target) => target.GetComponent<FarmTransfer>() != null;
        public override void SetTarget(GameObject target) {
            base.SetTarget(target);
            targetFarmTransfer = target.GetComponent<FarmTransfer>();
            Build();
        }

        private void ClearRows() {
            foreach (var kvp in farmRows) {
                Util.KDestroyGameObject(kvp.Value);
            }
            farmRows.Clear();
        }

        private void Build() {
            headerLabel.SetText(STRINGS.UI.UISIDESCREENS.FARMTRANSFERSIDESCREEN.HEADER);
            ClearRows();
            int curWorldID = targetFarmTransfer.GetMyWorldId();
            bool isDlc1Enable = DlcManager.IsExpansion1Active();
            foreach (var farmCmp in TransferManager.Inst.AnimalFarmList(targetFarmTransfer.FarmTypo)) {
                if (farmCmp.UniqueID == targetFarmTransfer.FromFarmID) { continue; }
                if (isDlc1Enable && !AnimalFarmSettings.isGlobalMode && farmCmp.SafeMyWorldID != curWorldID) { continue; } // 默认只显示同星球的农场
                GameObject go = Util.KInstantiateUI(rowPrefab, listContainer);
                go.gameObject.name = farmCmp.SafeProperName;
                farmRows[farmCmp] = go;
                go.SetActive(value: true);
            }
            noChannelRow.SetActive(false);
            Refresh();
        }

        private void Refresh() {
            foreach (var kvp in farmRows) {
                if (kvp.Key.IsNullOrDestroyed()) { continue; }
                kvp.Value.GetComponent<HierarchyReferences>().GetReference<LocText>("Label").SetText(kvp.Key.GetProperName());
                kvp.Value.GetComponent<HierarchyReferences>().GetReference<LocText>("DistanceLabel").SetText(kvp.Key.SimpleDesc);
                kvp.Value.GetComponent<HierarchyReferences>().GetReference<Image>("Icon").sprite = kvp.Key.IconInfo.first;
                kvp.Value.GetComponent<HierarchyReferences>().GetReference<Image>("Icon").color = kvp.Key.IconInfo.second;
                WorldContainer myWorld = kvp.Key.GetMyWorld();
                var worldSprite = (myWorld.IsModuleInterior ? Assets.GetSprite("icon_category_rocketry") : Def.GetUISprite(myWorld.GetComponent<ClusterGridEntity>()).first);
                var worldColor = (myWorld.IsModuleInterior ? Color.white : Def.GetUISprite(myWorld.GetComponent<ClusterGridEntity>()).second);
                kvp.Value.GetComponent<HierarchyReferences>().GetReference<Image>("WorldIcon").sprite = worldSprite;
                kvp.Value.GetComponent<HierarchyReferences>().GetReference<Image>("WorldIcon").color = worldColor;
                kvp.Value.GetComponent<HierarchyReferences>().GetReference<MultiToggle>("Toggle").onClick = delegate {
                    if (targetFarmTransfer == null || kvp.Key == null) { return; }
                    // 设计点击逻辑为再次选中就取消
                    targetFarmTransfer.SetToFarm(kvp.Key);
                    Refresh();
                };
                kvp.Value.GetComponent<HierarchyReferences>().GetReference<MultiToggle>("Toggle").ChangeState(kvp.Key.UniqueID == targetFarmTransfer.ToFarmID ? 1 : 0);
            }
        }

        public override string GetTitle() => STRINGS.UI.UISIDESCREENS.FARMTRANSFERSIDESCREEN.TITLE;
        public override void CopyFieldAfter() { }
    }

    public abstract class GGSideScreenContent : SideScreenContent {
        public virtual void CopyFieldAfter() { }
    }

    public static class SideScreenPatcher {
        private static readonly List<Tuple<Type, Type>> PatchInfos = new List<Tuple<Type, Type>> {
            new Tuple<Type, Type>(typeof(LogicBroadcastChannelSideScreen), typeof(FarmTransferSideScreen))
        };

        public static void DetailsScreen_OnPrefabInit_Patch(List<DetailsScreen.SideScreenRef> ___sideScreens) {
            var configBody = DetailsScreen.Instance?.GetTabOfType(DetailsScreen.SidescreenTabTypes.Config)?.bodyInstance;
            if (configBody is null) { return; }
            foreach (var itemInfo in PatchInfos) {
                CreateSideScreen(___sideScreens, configBody, itemInfo.first, itemInfo.second);
            }
        }

        private static GGSideScreenContent CreateSideScreen(IList<DetailsScreen.SideScreenRef> existing, GameObject parent, Type sourceScreen, Type newScreen) {
            DetailsScreen.SideScreenRef retScreenRef = null;
            GGSideScreenContent ret = null;
            bool isCopySuccess = false;
            foreach (var existScreen in existing) {
                if (existScreen.screenPrefab.GetType() != sourceScreen) continue;
                if (existScreen.screenPrefab == null) { continue; }
                retScreenRef = new DetailsScreen.SideScreenRef();
                ret = CopySideScreen(existScreen.screenPrefab.gameObject, parent, sourceScreen, newScreen);
                retScreenRef.screenPrefab = ret;
                retScreenRef.screenInstance = ret;
                isCopySuccess = true;
                break;
            }
            if (isCopySuccess) {
                existing.Insert(0, retScreenRef);
            }
            return ret;
        }
        private static GGSideScreenContent CopySideScreen(GameObject originalGo, GameObject parent, Type originalScreenType, Type newScreenType) {
            var newGo = UnityEngine.Object.Instantiate(originalGo, parent.transform, false);
            newGo.name = originalGo.name;
            var isActiveSelf = newGo.activeSelf;
            newGo.SetActive(false);

            var originalScreenCmp = (SideScreenContent)newGo.GetComponent(originalScreenType);
            var newScreenCmp = (GGSideScreenContent)newGo.AddComponent(newScreenType);

            var copyFieldDict = GetCopyFieldDict(newScreenType);

            if (copyFieldDict != null && copyFieldDict.Count > 0) {
                foreach (var (newName, sourceName) in copyFieldDict) {
                    var sourceField = originalScreenType.GetField(sourceName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    var newField = newScreenType.GetField(newName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                    if (sourceField == null) { Debug.LogError("[FarmTransfer]not found newField, name: " + sourceName); continue; }
                    if (newField == null) { Debug.LogError("[FarmTransfer]not found newField, name: " + newName); continue; }

                    newField.SetValue(newScreenCmp, sourceField.GetValue(originalScreenCmp));
                }
            }

            newScreenCmp.CopyFieldAfter();

            UnityEngine.Object.DestroyImmediate(originalScreenCmp);
            newGo.SetActive(isActiveSelf);
            newGo.transform.localScale = Vector3.one;
            return newScreenCmp;
        }
        /// <summary>
        /// key:   要复制到自定义类中的字段名
        /// value: 原来类中的字段名
        /// </summary>
        private static Dictionary<string, string> GetCopyFieldDict(Type type) {
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Dictionary<string, string> ret = new Dictionary<string, string>();
            foreach (var info in fields) {
                var cf = (CopyField)Attribute.GetCustomAttribute(info, typeof(CopyField));
                if (cf != null) {
                    if (string.IsNullOrWhiteSpace(cf.alias)) { ret.Add(info.Name, info.Name); }
                    else { ret.Add(info.Name, cf.alias); }
                }
            }
            return ret;
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class CopyField : Attribute {
        public string alias;    // 原来类中的字段名
        public CopyField() { }
        public CopyField(string alias) { this.alias = alias; }
    }
}
