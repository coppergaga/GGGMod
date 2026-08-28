using OSTR = global::STRINGS;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace GGGMod.BuildableWildPlant {
    /// <summary>
    /// logic from class ReceptacleSideScreen and PlanterSideScreen
    /// </summary>
    public class SeedSelectorSideScreen : GGSideScreenContent, IRender1000ms {
        // -- field from ReceptacleSideScreen -- //
        protected class SelectableEntity {
            public Tag tag;
            public SingleEntityReceptacle.ReceptacleDirection direction;
            public GameObject asset;
            public float lastAmount = -1f;
        }

        protected bool ALLOW_ORDER_IGNORING_WOLRD_NEED = true;

        [CopyField, SerializeField]
        protected KButton requestSelectedEntityBtn;

        [CopyField, SerializeField]
        private string requestStringDeposit;

        [CopyField, SerializeField]
        private string requestStringCancelDeposit;

        [CopyField]
        public GameObject activeEntityContainer;
        [CopyField]
        public GameObject nothingDiscoveredContainer;

        [SerializeField]
        private bool categoryStartExpanded;

        [CopyField, SerializeField]
        private GameObject categoryContainerPrefab;

        private Dictionary<Tag, GameObject> contentContainers = new Dictionary<Tag, GameObject>();

        [CopyField, SerializeField]
        protected LocText descriptionLabel;

        protected Dictionary<BuildableWildPlant, int> entityPreviousSelectionMap = new Dictionary<BuildableWildPlant, int>();

        [CopyField, SerializeField]
        private string subtitleStringSelect;

        [CopyField, SerializeField]
        private string subtitleStringSelectDescription;

        [CopyField, SerializeField]
        private string subtitleStringAwaitingSelection;

        [CopyField, SerializeField]
        private string subtitleStringAwaitingDelivery;

        [CopyField, SerializeField]
        private LocText subtitleLabel;

        [CopyField, SerializeField]
        private List<DescriptorPanel> descriptorPanels;

        [CopyField]
        public Material defaultMaterial;

        [CopyField]
        public Material desaturatedMaterial;

        [CopyField, SerializeField]
        private GameObject requestObjectListContainer;

        [CopyField, SerializeField]
        private GameObject requestObjectListContainerContent;

        [CopyField, SerializeField]
        private GameObject scrollBarContainer;

        [CopyField, SerializeField]
        private GameObject entityToggle;

        [CopyField, SerializeField]
        private Sprite buttonSelectedBG;

        [CopyField, SerializeField]
        private Sprite buttonNormalBG;

        [CopyField, SerializeField]
        private Sprite elementPlaceholderSpr;

        [CopyField, SerializeField]
        private bool hideUndiscoveredEntities;

        protected ReceptacleToggle selectedEntityToggle;

        protected BuildableWildPlant targetReceptacle;

        protected Tag selectedDepositObjectTag;

        protected Tag selectedDepositObjectAdditionalTag;

        protected Dictionary<ReceptacleToggle, SelectableEntity> depositObjectMap;

        protected List<ReceptacleToggle> entityToggles = new List<ReceptacleToggle>();

        private List<GameObject> recycledEntityToggles = new List<GameObject>();

        private Dictionary<Tag, bool> categoryExpandedStatus = new Dictionary<Tag, bool>();

        private int onOccupantValidChangedHandle = -1;

        private int onStorageChangedHandle = -1;

        private void RecycleToggle(GameObject toggle) {
            toggle.SetActive(value: false);
            recycledEntityToggles.Add(toggle);
        }

        private GameObject SpawnToggle(GameObject parent) {
            if (recycledEntityToggles.Count > 0) {
                GameObject obj = recycledEntityToggles[recycledEntityToggles.Count - 1];
                recycledEntityToggles.RemoveAt(recycledEntityToggles.Count - 1);
                obj.transform.SetParent(parent.transform);
                obj.SetActive(value: true);
                return obj;
            }

            return Util.KInstantiateUI(entityToggle, parent, force_active: true);
        }

        private void RefreshCategoryOpen(GameObject categoryHeader, GameObject categoryGrid, Tag tag) {
            categoryHeader.GetComponent<MultiToggle>().ChangeState((!categoryExpandedStatus[tag]) ? 1 : 0);
            categoryGrid.gameObject.SetActive(categoryExpandedStatus[tag]);
        }

        public void Initialize(BuildableWildPlant target) {
            if (target == null) {
                Debug.LogError("SingleObjectReceptacle provided was null.");
                return;
            }

            targetReceptacle = target;
            base.gameObject.SetActive(value: true);
            depositObjectMap = new Dictionary<ReceptacleToggle, SelectableEntity>();
            entityToggles.ForEach(delegate (ReceptacleToggle rbi) {
                RecycleToggle(rbi.gameObject);
            });
            entityToggles.Clear();
            List<GameObject> list = new List<GameObject>();
            if (targetReceptacle.possibleDepositObjectTags.Count == 1) {
                categoryStartExpanded = true;
            }

            foreach (Tag tag in targetReceptacle.possibleDepositObjectTags) {
                List<GameObject> prefabsWithTag = Assets.GetPrefabsWithTag(tag);
                int num = prefabsWithTag.Count;
                if (categoryExpandedStatus.ContainsKey(tag)) {
                    categoryExpandedStatus[tag] = categoryStartExpanded;
                }

                if (!contentContainers.ContainsKey(tag)) {
                    GameObject gameObject = Util.KInstantiateUI(categoryContainerPrefab, requestObjectListContainerContent, force_active: true);
                    contentContainers.Add(tag, gameObject);
                    HierarchyReferences component = gameObject.GetComponent<HierarchyReferences>();
                    component.GetReference<LocText>("HeaderLabel").SetText(tag.ProperName());
                    categoryExpandedStatus.Add(tag, categoryStartExpanded);
                    MultiToggle toggle = gameObject.GetComponent<HierarchyReferences>().GetReference<MultiToggle>("HeaderToggle");
                    GridLayoutGroup grid = component.GetReference<GridLayoutGroup>("GridLayout");
                    MultiToggle multiToggle = toggle;
                    multiToggle.onClick = (System.Action)Delegate.Combine(multiToggle.onClick, (System.Action)delegate {
                        categoryExpandedStatus[tag] = !categoryExpandedStatus[tag];
                        RefreshCategoryOpen(toggle.gameObject, grid.gameObject, tag);
                    });
                    RefreshCategoryOpen(toggle.gameObject, grid.gameObject, tag);
                }

                RefreshCategoryOpen(contentContainers[tag].GetComponent<HierarchyReferences>().GetReference<MultiToggle>("HeaderToggle").gameObject, contentContainers[tag].GetComponent<HierarchyReferences>().GetReference<GridLayoutGroup>("GridLayout").gameObject, tag);
                List<IHasSortOrder> list2 = new List<IHasSortOrder>();
                foreach (GameObject item in prefabsWithTag) {
                    if (!targetReceptacle.IsValidEntity(item) || list.Contains(item)) {
                        num--;
                        continue;
                    }

                    IHasSortOrder component2 = item.GetComponent<IHasSortOrder>();
                    if (component2 != null) {
                        list.Add(item);
                        list2.Add(component2);
                    }
                }

                Debug.Assert(list2.Count == num, "Not all entities in this receptacle implement IHasSortOrder!");
                list2.Sort((IHasSortOrder a, IHasSortOrder b) => a.sortOrder - b.sortOrder);
                foreach (IHasSortOrder item2 in list2) {
                    GameObject gameObject2 = (item2 as MonoBehaviour).gameObject;
                    GameObject gameObject3 = SpawnToggle(contentContainers[tag].GetComponent<HierarchyReferences>().GetReference("GridLayout").gameObject);
                    gameObject3.transform.SetAsLastSibling();
                    gameObject3.SetActive(value: true);
                    ReceptacleToggle newToggle = gameObject3.GetComponent<ReceptacleToggle>();
                    IReceptacleDirection component3 = gameObject2.GetComponent<IReceptacleDirection>();
                    string entityName = GetEntityName(gameObject2.PrefabID());
                    newToggle.title.text = entityName;
                    Sprite entityIcon = GetEntityIcon(gameObject2.PrefabID());
                    if (entityIcon == null) {
                        entityIcon = elementPlaceholderSpr;
                    }

                    newToggle.image.sprite = entityIcon;
                    if (newToggle.toggle == null) {
                        newToggle.toggle = newToggle.GetComponentInChildren<MultiToggle>();
                    }

                    MultiToggle toggle2 = newToggle.toggle;
                    toggle2.onClick = (System.Action)Delegate.Combine(toggle2.onClick, (System.Action)delegate {
                        ToggleClicked(newToggle);
                    });
                    ToolTip component4 = newToggle.GetComponent<ToolTip>();
                    if (component4 != null) {
                        component4.SetSimpleTooltip(GetEntityTooltip(gameObject2.PrefabID()));
                    }

                    depositObjectMap.Add(newToggle, new SelectableEntity {
                        tag = gameObject2.PrefabID(),
                        direction = (component3?.Direction ?? SingleEntityReceptacle.ReceptacleDirection.Top),
                        asset = gameObject2
                    });
                    entityToggles.Add(newToggle);
                }
            }

            RestoreSelectionFromOccupant();
            selectedEntityToggle = null;
            if (entityToggles.Count > 0) {
                if (entityPreviousSelectionMap.ContainsKey(targetReceptacle)) {
                    int index = entityPreviousSelectionMap[targetReceptacle];
                    ToggleClicked(entityToggles[index]);
                }
                else {
                    subtitleLabel.SetText(Strings.Get(subtitleStringSelect).ToString());
                    requestSelectedEntityBtn.isInteractable = false;
                    descriptionLabel.SetText(Strings.Get(subtitleStringSelectDescription).ToString());
                    HideAllDescriptorPanels();
                }
            }

            onStorageChangedHandle = targetReceptacle.gameObject.Subscribe(-1697596308, CheckAmountsAndUpdate);
            onOccupantValidChangedHandle = targetReceptacle.gameObject.Subscribe(-1820564715, OnOccupantValidChanged);
            UpdateState(null);
            SimAndRenderScheduler.instance.Add(this);
        }

        protected virtual void UpdateState(object data) {
            requestSelectedEntityBtn.ClearOnClick();
            if (targetReceptacle == null) { return; }

            if (targetReceptacle.GetActiveRequest != null) {
                requestSelectedEntityBtn.onClick += delegate {
                    targetReceptacle.CancelActiveRequest();
                    ClearSelection();
                    UpdateAvailableAmounts(null);
                    UpdateState(null);
                };
                requestSelectedEntityBtn.GetComponentInChildren<LocText>().text = Strings.Get(requestStringCancelDeposit).ToString();
                requestSelectedEntityBtn.isInteractable = true;
                ToggleObjectPicker(Show: false);
                ConfigureActiveEntity(targetReceptacle.GetActiveRequest.tagsFirst);
                GameObject prefab = Assets.GetPrefab(targetReceptacle.GetActiveRequest.tagsFirst);
                if (prefab != null) {
                    subtitleLabel.SetText(string.Format(Strings.Get(subtitleStringAwaitingDelivery).ToString(), prefab.GetProperName()));
                    SetResultDescriptions(prefab);
                }
            }
            else if (selectedEntityToggle != null) {
                requestSelectedEntityBtn.onClick += delegate {
                    targetReceptacle.CreateOrder(selectedDepositObjectTag, selectedDepositObjectAdditionalTag);
                    UpdateAvailableAmounts(null);
                    UpdateState(null);
                };
                requestSelectedEntityBtn.GetComponentInChildren<LocText>().text = Strings.Get(requestStringDeposit).ToString();
                targetReceptacle.SetPreview(depositObjectMap[selectedEntityToggle].tag);
                bool isInteractable = CanDepositEntity(depositObjectMap[selectedEntityToggle], runAdditionalCanDepositTest: true);
                requestSelectedEntityBtn.isInteractable = isInteractable;
                ToggleObjectPicker(Show: true);
                GameObject prefab2 = Assets.GetPrefab(selectedDepositObjectTag);
                if (prefab2 != null) {
                    subtitleLabel.SetText(string.Format(Strings.Get(subtitleStringAwaitingSelection).ToString(), prefab2.GetProperName()));
                    SetResultDescriptions(prefab2);
                }
            }
            else {
                requestSelectedEntityBtn.GetComponentInChildren<LocText>().text = Strings.Get(requestStringDeposit).ToString();
                requestSelectedEntityBtn.isInteractable = false;
                ToggleObjectPicker(Show: true);
            }

            UpdateAvailableAmounts(null);
            RefreshToggleStates();

            requestSelectedEntityBtn.onClick += RefreshSubspeciesToggles;
            RefreshSubspeciesToggles();
        }

        private void OnOccupantValidChanged(object _) {
            if (!(targetReceptacle == null) && targetReceptacle.GetActiveRequest != null) {
                bool flag = false;
                if (depositObjectMap.TryGetValue(selectedEntityToggle, out var value)) {
                    flag = CanDepositEntity(value, runAdditionalCanDepositTest: true);
                }

                if (!flag) {
                    targetReceptacle.CancelActiveRequest();
                    ClearSelection();
                    UpdateState(null);
                    UpdateAvailableAmounts(null);
                }
            }
        }

        protected bool CanDepositEntity(SelectableEntity entity, bool runAdditionalCanDepositTest = false) {
            if (!RequiresAvailableAmountToDeposit() || GetAvailableAmount(entity.tag) > 0f) {
                if (runAdditionalCanDepositTest) {
                    return AdditionalCanDepositTest();
                }
                return true;
            }
            return false;
        }

        protected virtual bool RequiresAvailableAmountToDeposit() {
            return true;
        }

        private void ClearSelection() {
            selectedEntityToggle = null;
            RefreshToggleStates();
        }

        private void ToggleObjectPicker(bool Show) {
            requestObjectListContainer.SetActive(Show);
            if (scrollBarContainer != null) {
                scrollBarContainer.SetActive(Show);
            }

            requestObjectListContainer.SetActive(Show);
            activeEntityContainer.SetActive(!Show);
        }

        private void ConfigureActiveEntity(Tag tag) {
            string properName = Assets.GetPrefab(tag).GetProperName();
            HierarchyReferences component = activeEntityContainer.GetComponent<HierarchyReferences>();
            component.GetReference<LocText>("Label").text = properName;
            component.GetReference<Image>("Icon").sprite = GetEntityIcon(tag);
        }

        protected string GetEntityName(Tag prefabTag) {
            PlantableSeed component = Assets.GetPrefab(prefabTag).GetComponent<PlantableSeed>();
            if (component != null) {
                return Assets.GetPrefab(component.PlantID).GetProperName();
            }
            return Assets.GetPrefab(prefabTag).GetProperName();
        }

        protected Sprite GetEntityIcon(Tag prefabTag) {
            PlantableSeed component = Assets.GetPrefab(prefabTag).GetComponent<PlantableSeed>();
            if (component != null) {
                return Def.GetUISprite(Assets.GetPrefab(component.PlantID)).first;
            }
            return Def.GetUISprite(Assets.GetPrefab(prefabTag)).first;
        }

        public override bool IsValidForTarget(GameObject target) {
            return target.GetComponent<BuildableWildPlant>() != null;
        }

        public override void SetTarget(GameObject target) {
            selectedDepositObjectTag = Tag.Invalid;
            selectedDepositObjectAdditionalTag = Tag.Invalid;
            var component = target.GetComponent<BuildableWildPlant>();
            if (component == null) {
                Debug.LogError("The object selected doesn't have a SingleObjectReceptacle!");
                return;
            }

            Initialize(component);
            UpdateState(null);

            LoadTargetSubSpeciesRequest();
        }

        public override void ClearTarget() {
            if (targetReceptacle != null) {
                targetReceptacle.Unsubscribe(onStorageChangedHandle);
                onStorageChangedHandle = -1;
                targetReceptacle.Unsubscribe(onOccupantValidChangedHandle);
                onOccupantValidChangedHandle = -1;
                if (targetReceptacle.GetActiveRequest == null) {
                    targetReceptacle.SetPreview(Tag.Invalid);
                }

                SimAndRenderScheduler.instance.Remove(this);
                targetReceptacle = null;
            }
        }

        protected void RefreshToggleStates() {
            foreach (KeyValuePair<ReceptacleToggle, SelectableEntity> item in depositObjectMap) {
                if (selectedEntityToggle != item.Key) {
                    if (CanDepositEntity(item.Value)) {
                        SetToggleState(item.Key.toggle, ImageToggleState.State.Inactive);
                    }
                    else {
                        SetToggleState(item.Key.toggle, ImageToggleState.State.Disabled);
                    }
                }
                else if (CanDepositEntity(item.Value)) {
                    SetToggleState(item.Key.toggle, ImageToggleState.State.Active);
                }
                else {
                    SetToggleState(item.Key.toggle, ImageToggleState.State.DisabledActive);
                }
            }
        }

        protected void SetToggleState(MultiToggle toggle, ImageToggleState.State state) {
            switch (state) {
                case ImageToggleState.State.Active:
                    toggle.ChangeState(1);
                    toggle.gameObject.GetComponentsInChildrenOnly<Image>()[1].material = defaultMaterial;
                    break;
                case ImageToggleState.State.Inactive:
                    toggle.ChangeState(0);
                    toggle.gameObject.GetComponentsInChildrenOnly<Image>()[1].material = defaultMaterial;
                    break;
                case ImageToggleState.State.Disabled:
                    toggle.ChangeState(2);
                    toggle.gameObject.GetComponentsInChildrenOnly<Image>()[1].material = desaturatedMaterial;
                    break;
                case ImageToggleState.State.DisabledActive:
                    toggle.ChangeState(3);
                    toggle.gameObject.GetComponentsInChildrenOnly<Image>()[1].material = desaturatedMaterial;
                    break;
            }
        }

        public void Render1000ms(float dt) {
            CheckAmountsAndUpdate(null);
        }

        private void CheckAmountsAndUpdate(object data) {
            if (!(targetReceptacle == null) && UpdateAvailableAmounts(null)) {
                UpdateState(null);
            }
        }

        private bool UpdateAvailableAmounts(object data) {
            bool result = false;
            foreach (KeyValuePair<ReceptacleToggle, SelectableEntity> item in depositObjectMap) {
                if (!DebugHandler.InstantBuildMode && hideUndiscoveredEntities && !DiscoveredResources.Instance.IsDiscovered(item.Value.tag)) {
                    item.Key.gameObject.SetActive(value: false);
                }
                else if (!item.Key.gameObject.activeSelf) {
                    item.Key.gameObject.SetActive(value: true);
                }

                float availableAmount = GetAvailableAmount(item.Value.tag);
                if (item.Value.lastAmount != availableAmount) {
                    result = true;
                    item.Value.lastAmount = availableAmount;
                    item.Key.amount.text = availableAmount.ToString();
                }

                if (availableAmount <= 0f) {
                    if (selectedEntityToggle != item.Key) {
                        item.Key.toggle.ChangeState(2);
                    }
                    else {
                        item.Key.toggle.ChangeState(3);
                    }
                }
                else if (selectedEntityToggle != item.Key) {
                    item.Key.toggle.ChangeState(0);
                }
                else {
                    item.Key.toggle.ChangeState(1);
                }
            }

            foreach (KeyValuePair<Tag, GameObject> contentContainer in contentContainers) {
                Transform transform = contentContainer.Value.GetComponent<HierarchyReferences>().GetReference<GridLayoutGroup>("GridLayout").transform;
                bool flag = false;
                for (int i = 0; i < transform.childCount; i++) {
                    if (transform.GetChild(i).gameObject.activeSelf) {
                        flag = true;
                        break;
                    }
                }

                if (contentContainer.Value.activeSelf != flag) {
                    contentContainer.Value.SetActive(flag);
                }
            }

            return result;
        }

        protected float GetAvailableAmount(Tag tag) {
            if (ALLOW_ORDER_IGNORING_WOLRD_NEED) {
                ICollection<Pickupable> pickupables = targetReceptacle.GetMyWorld().worldInventory.GetPickupables(tag, includeRelatedWorlds: true);
                float num = 0f;
                {
                    foreach (Pickupable item in pickupables) {
                        num += Mathf.CeilToInt(item.TotalAmount);
                    }

                    return num;
                }
            }

            return targetReceptacle.GetMyWorld().worldInventory.GetAmount(tag, includeRelatedWorlds: true);
        }

        protected void ToggleClicked(ReceptacleToggle toggle) {
            if (!depositObjectMap.ContainsKey(toggle)) {
                Debug.LogError("Recipe not found on recipe list.");
                return;
            }

            LoadTargetSubSpeciesRequest();

            selectedEntityToggle = toggle;
            entityPreviousSelectionMap[targetReceptacle] = entityToggles.IndexOf(toggle);
            selectedDepositObjectTag = depositObjectMap[toggle].tag;
            MutantPlant component = depositObjectMap[toggle].asset.GetComponent<MutantPlant>();
            selectedDepositObjectAdditionalTag = (component ? component.SubSpeciesID : Tag.Invalid);
            RefreshToggleStates();
            UpdateAvailableAmounts(null);
            UpdateState(null);
        }

        protected virtual void HideAllDescriptorPanels() {
            for (int i = 0; i < descriptorPanels.Count; i++) {
                descriptorPanels[i].gameObject.SetActive(value: false);
            }
        }

        // -- field from PlanterSideScreen -- //
        [CopyField]
        public DescriptorPanel RequirementsDescriptorPanel;

        [CopyField]
        public DescriptorPanel HarvestDescriptorPanel;

        [CopyField]
        public DescriptorPanel EffectsDescriptorPanel;

        [CopyField]
        public GameObject mutationPanel;

        [CopyField]
        public GameObject mutationViewport;

        [CopyField]
        public GameObject mutationContainer;

        [CopyField]
        public GameObject mutationOption;

        [CopyField]
        public GameObject blankMutationOption;

        [CopyField]
        public GameObject selectSpeciesPrompt;

        private bool mutationPanelCollapsed = true;

        public Dictionary<GameObject, Tag> subspeciesToggles = new Dictionary<GameObject, Tag>();

        private List<GameObject> blankMutationObjects = new List<GameObject>();

        private Dictionary<BuildableWildPlant, Tag> entityPreviousSubSelectionMap = new Dictionary<BuildableWildPlant, Tag>();

        private Coroutine activeAnimationRoutine;

        private const float EXPAND_DURATION = 0.33f;

        private const float EXPAND_MIN = 24f;

        private const float EXPAND_MAX = 118f;

        private Tag selectedSubspecies {
            get {
                if (!entityPreviousSubSelectionMap.ContainsKey(targetReceptacle)) {
                    entityPreviousSubSelectionMap.Add(targetReceptacle, Tag.Invalid);
                }

                return entityPreviousSubSelectionMap[targetReceptacle];
            }
            set {
                if (!entityPreviousSubSelectionMap.ContainsKey(targetReceptacle)) {
                    entityPreviousSubSelectionMap.Add(targetReceptacle, Tag.Invalid);
                }

                entityPreviousSubSelectionMap[targetReceptacle] = value;
                selectedDepositObjectAdditionalTag = value;
            }
        }

        private void LoadTargetSubSpeciesRequest() {
            var buildablePlant = targetReceptacle;
            Tag tag = Tag.Invalid;
            if (buildablePlant.requestedEntityTag != Tag.Invalid && buildablePlant.requestedEntityTag != GameTags.Empty) {
                tag = buildablePlant.requestedEntityTag;
            }
            else if (selectedEntityToggle != null) {
                tag = depositObjectMap[selectedEntityToggle].tag;
            }

            if (!DlcManager.FeaturePlantMutationsEnabled() || !tag.IsValid) { return; }

            MutantPlant component = Assets.GetPrefab(tag).GetComponent<MutantPlant>();
            if (component == null) {
                selectedSubspecies = Tag.Invalid;
            }
            else if (buildablePlant.requestedEntityAdditionalFilterTag != Tag.Invalid && buildablePlant.requestedEntityAdditionalFilterTag != GameTags.Empty) {
                selectedSubspecies = buildablePlant.requestedEntityAdditionalFilterTag;
            }
            else if (selectedSubspecies == Tag.Invalid) {
                if (PlantSubSpeciesCatalog.Instance.GetOriginalSubSpecies(component.SpeciesID, out var subSpeciesInfo)) {
                    selectedSubspecies = subSpeciesInfo.ID;
                }

                buildablePlant.requestedEntityAdditionalFilterTag = selectedSubspecies;
            }
        }

        protected void MutationToggleClicked(GameObject toggle) {
            selectedSubspecies = subspeciesToggles[toggle];
            UpdateState(null);
        }

        private IEnumerator ExpandMutations() {
            LayoutElement le = mutationViewport.GetComponent<LayoutElement>();
            float num = 94f;
            float travelPerSecond = num / EXPAND_DURATION;
            while (le.minHeight < EXPAND_MAX) {
                float minHeight = le.minHeight;
                float num2 = Time.unscaledDeltaTime * travelPerSecond;
                minHeight = (le.minHeight = Mathf.Min(minHeight + num2, EXPAND_MAX));
                le.preferredHeight = minHeight;
                yield return SequenceUtil.WaitForEndOfFrame;
            }

            mutationPanelCollapsed = false;
            activeAnimationRoutine = null;
            yield return 0;
        }

        private IEnumerator CollapseMutations() {
            LayoutElement le = mutationViewport.GetComponent<LayoutElement>();
            float num = -94f;
            float travelPerSecond = num / EXPAND_DURATION;
            while (le.minHeight > EXPAND_MIN) {
                float minHeight = le.minHeight;
                float num2 = Time.unscaledDeltaTime * travelPerSecond;
                minHeight = (le.minHeight = Mathf.Max(minHeight + num2, EXPAND_MIN));
                le.preferredHeight = minHeight;
                yield return SequenceUtil.WaitForEndOfFrame;
            }

            mutationPanelCollapsed = true;
            activeAnimationRoutine = null;
            yield return SequenceUtil.WaitForNextFrame;
        }

        private void RefreshSubspeciesToggles() {
            foreach (KeyValuePair<GameObject, Tag> subspeciesToggle in subspeciesToggles) {
                UnityEngine.Object.Destroy(subspeciesToggle.Key);
            }

            subspeciesToggles.Clear();
            if (!PlantSubSpeciesCatalog.Instance.AnyNonOriginalDiscovered) {
                mutationPanel.SetActive(value: false);
                return;
            }

            mutationPanel.SetActive(value: true);
            foreach (GameObject blankMutationObject in blankMutationObjects) {
                UnityEngine.Object.Destroy(blankMutationObject);
            }

            blankMutationObjects.Clear();
            selectSpeciesPrompt.SetActive(value: false);
            if (selectedDepositObjectTag.IsValid) {
                Tag plantID = Assets.GetPrefab(selectedDepositObjectTag).GetComponent<PlantableSeed>().PlantID;
                List<PlantSubSpeciesCatalog.SubSpeciesInfo> allSubSpeciesForSpecies = PlantSubSpeciesCatalog.Instance.GetAllSubSpeciesForSpecies(plantID);
                if (allSubSpeciesForSpecies != null) {
                    foreach (PlantSubSpeciesCatalog.SubSpeciesInfo item in allSubSpeciesForSpecies) {
                        GameObject option = Util.KInstantiateUI(mutationOption, mutationContainer, force_active: true);
                        option.GetComponentInChildren<LocText>().text = item.GetNameWithMutations(plantID.ProperName(), PlantSubSpeciesCatalog.Instance.IsSubSpeciesIdentified(item.ID), cleanOriginal: false);
                        MultiToggle component = option.GetComponent<MultiToggle>();
                        component.onClick = (System.Action)Delegate.Combine(component.onClick, (System.Action)delegate {
                            MutationToggleClicked(option);
                        });
                        option.GetComponent<ToolTip>().SetSimpleTooltip(item.GetMutationsTooltip());
                        subspeciesToggles.Add(option, item.ID);
                    }

                    for (int num = allSubSpeciesForSpecies.Count; num < 5; num++) {
                        blankMutationObjects.Add(Util.KInstantiateUI(blankMutationOption, mutationContainer, force_active: true));
                    }

                    if (!selectedSubspecies.IsValid || !subspeciesToggles.ContainsValue(selectedSubspecies)) {
                        selectedSubspecies = allSubSpeciesForSpecies[0].ID;
                    }
                }
            }
            else {
                selectSpeciesPrompt.SetActive(value: true);
            }

            ICollection<Pickupable> collection = new List<Pickupable>();
            bool flag2 = targetReceptacle.GetActiveRequest != null;
            WorldContainer myWorld = targetReceptacle.GetMyWorld();
            collection = myWorld.worldInventory.GetPickupables(selectedDepositObjectTag, myWorld.IsModuleInterior);
            foreach (KeyValuePair<GameObject, Tag> subspeciesToggle2 in subspeciesToggles) {
                float num2 = 0f;
                bool flag3 = PlantSubSpeciesCatalog.Instance.IsSubSpeciesIdentified(subspeciesToggle2.Value);
                if (collection != null) {
                    foreach (Pickupable item2 in collection) {
                        if (item2.HasTag(subspeciesToggle2.Value)) {
                            num2 += item2.GetComponent<PrimaryElement>().Units;
                        }
                    }
                }

                if (num2 > 0f && flag3) {
                    subspeciesToggle2.Key.GetComponent<MultiToggle>().ChangeState((subspeciesToggle2.Value == selectedSubspecies) ? 1 : 0);
                }
                else {
                    subspeciesToggle2.Key.GetComponent<MultiToggle>().ChangeState((subspeciesToggle2.Value == selectedSubspecies) ? 3 : 2);
                }

                subspeciesToggle2.Key.GetComponentInChildren<LocText>().text += $" ({num2})";
                if (flag2) {
                    if (subspeciesToggle2.Value == selectedSubspecies) {
                        subspeciesToggle2.Key.SetActive(value: true);
                        subspeciesToggle2.Key.GetComponent<MultiToggle>().ChangeState(1);
                    }
                    else {
                        subspeciesToggle2.Key.SetActive(value: false);
                    }
                }
                else {
                    subspeciesToggle2.Key.SetActive(selectedEntityToggle != null);
                }
            }

            bool flag4 = !flag2 && selectedEntityToggle != null && subspeciesToggles.Count >= 1;
            if (flag4 && mutationPanelCollapsed) {
                if (activeAnimationRoutine != null) {
                    StopCoroutine(activeAnimationRoutine);
                }

                activeAnimationRoutine = StartCoroutine(ExpandMutations());
            }
            else if (!flag4 && !mutationPanelCollapsed) {
                if (activeAnimationRoutine != null) {
                    StopCoroutine(activeAnimationRoutine);
                }

                activeAnimationRoutine = StartCoroutine(CollapseMutations());
            }
        }

        protected string GetEntityTooltip(Tag prefabTag) {
            PlantableSeed component = Assets.GetPrefab(prefabTag).GetComponent<PlantableSeed>();
            return string.Format(OSTR.UI.UISIDESCREENS.PLANTERSIDESCREEN.TOOLTIPS.PLANT_TOGGLE_TOOLTIP, GetEntityName(prefabTag), component.domesticatedDescription, GetAvailableAmount(prefabTag));
        }

        protected void SetResultDescriptions(GameObject seed_or_plant) {
            string text = "";
            GameObject gameObject = seed_or_plant;
            PlantableSeed component = seed_or_plant.GetComponent<PlantableSeed>();
            List<Descriptor> list = new List<Descriptor>();
            bool flag = true;
            if (seed_or_plant.GetComponent<MutantPlant>() != null && selectedDepositObjectAdditionalTag != Tag.Invalid) {
                flag = PlantSubSpeciesCatalog.Instance.IsSubSpeciesIdentified(selectedDepositObjectAdditionalTag);
            }

            if (!flag) {
                text = string.Concat(OSTR.CREATURES.PLANT_MUTATIONS.UNIDENTIFIED, "\n\n", OSTR.CREATURES.PLANT_MUTATIONS.UNIDENTIFIED_DESC);
            }
            else if (component != null) {
                list = component.GetDescriptors(component.gameObject);

                gameObject = Assets.GetPrefab(component.PlantID);
                MutantPlant component2 = gameObject.GetComponent<MutantPlant>();
                if (component2 != null && selectedDepositObjectAdditionalTag.IsValid) {
                    component2.DummySetSubspecies(PlantSubSpeciesCatalog.Instance.GetSubSpecies(component.PlantID, selectedDepositObjectAdditionalTag).mutationIDs);
                }

                if (!string.IsNullOrEmpty(component.domesticatedDescription)) {
                    text += component.domesticatedDescription;
                }
            }
            else {
                InfoDescription component3 = gameObject.GetComponent<InfoDescription>();
                if ((bool)component3) {
                    text += component3.description;
                }
            }

            descriptionLabel.SetText(text);
            List<Descriptor> plantLifeCycleDescriptors = GameUtil.GetPlantLifeCycleDescriptors(gameObject);
            if (plantLifeCycleDescriptors.Count > 0 && flag) {
                HarvestDescriptorPanel.SetDescriptors(plantLifeCycleDescriptors);
                HarvestDescriptorPanel.gameObject.SetActive(value: true);
            }
            else {
                HarvestDescriptorPanel.gameObject.SetActive(value: false);
            }

            List<Descriptor> plantRequirementDescriptors = GameUtil.GetPlantRequirementDescriptors(gameObject);
            if (list.Count > 0) {
                GameUtil.IndentListOfDescriptors(list);
                plantRequirementDescriptors.InsertRange(plantRequirementDescriptors.Count, list);
            }

            if (plantRequirementDescriptors.Count > 0 && flag) {
                RequirementsDescriptorPanel.SetDescriptors(plantRequirementDescriptors);
                RequirementsDescriptorPanel.gameObject.SetActive(value: true);
            }
            else {
                RequirementsDescriptorPanel.gameObject.SetActive(value: false);
            }

            List<Descriptor> plantEffectDescriptors = GameUtil.GetPlantEffectDescriptors(gameObject);
            if (plantEffectDescriptors.Count > 0 && flag) {
                EffectsDescriptorPanel.SetDescriptors(plantEffectDescriptors);
                EffectsDescriptorPanel.gameObject.SetActive(value: true);
            }
            else {
                EffectsDescriptorPanel.gameObject.SetActive(value: false);
            }
        }

        protected bool AdditionalCanDepositTest() {
            bool flag = false;
            if (selectedDepositObjectTag.IsValid) {
                flag = ((!DlcManager.FeaturePlantMutationsEnabled()) ? selectedDepositObjectTag.IsValid : PlantSubSpeciesCatalog.Instance.IsValidPlantableSeed(selectedDepositObjectTag, selectedDepositObjectAdditionalTag));
            }

            WorldContainer myWorld = targetReceptacle.GetMyWorld();
            if (flag) {
                return myWorld.worldInventory.GetCountWithAdditionalTag(selectedDepositObjectTag, selectedDepositObjectAdditionalTag, myWorld.IsModuleInterior) > 0;
            }

            return false;
        }

        protected void RestoreSelectionFromOccupant() {
            BuildableWildPlant plantablePlot = targetReceptacle;
            Tag tag = Tag.Invalid;
            Tag value = Tag.Invalid;
            bool flag = false;
            if (plantablePlot.GetActiveRequest != null) {
                tag = plantablePlot.requestedEntityTag;
                value = plantablePlot.requestedEntityAdditionalFilterTag;
                selectedDepositObjectTag = tag;
                selectedDepositObjectAdditionalTag = value;
                flag = true;
            }

            if (!(tag != Tag.Invalid)) {
                return;
            }

            if (!entityPreviousSelectionMap.ContainsKey(plantablePlot) || flag) {
                int value2 = 0;
                foreach (KeyValuePair<ReceptacleToggle, SelectableEntity> item in depositObjectMap) {
                    if (item.Value.tag == tag) {
                        value2 = entityToggles.IndexOf(item.Key);
                    }
                }

                if (!entityPreviousSelectionMap.ContainsKey(plantablePlot)) {
                    entityPreviousSelectionMap.Add(plantablePlot, -1);
                }

                entityPreviousSelectionMap[plantablePlot] = value2;
            }

            if (!entityPreviousSubSelectionMap.ContainsKey(plantablePlot)) {
                entityPreviousSubSelectionMap.Add(plantablePlot, Tag.Invalid);
            }

            if (entityPreviousSubSelectionMap[plantablePlot] == Tag.Invalid || flag) {
                entityPreviousSubSelectionMap[plantablePlot] = value;
            }
        }

        public override void CopyFieldAfter() {
            titleKey = "STRINGS.UI.UISIDESCREENS.SEEDSELECTORSIDESCREEN.GGSEEDSELECTOR";
        }
    }

    public abstract class GGSideScreenContent : SideScreenContent {
        public virtual void CopyFieldAfter() { }
    }

    public static class SideScreenPatcher {
        private static readonly List<Tuple<Type, Type>> PatchInfos = new List<Tuple<Type, Type>> {
            new Tuple<Type, Type>(typeof(PlanterSideScreen), typeof(SeedSelectorSideScreen))
        };

        public static void DetailsScreen_OnPrefabInit_Patch(List<DetailsScreen.SideScreenRef> ___sideScreens) {
            var configBody = DetailsScreen.Instance?.GetTabOfType(DetailsScreen.SidescreenTabTypes.Config)?.bodyInstance;
            if (configBody is null) { return; }
            foreach (var itemInfo in PatchInfos) {
                CreateSideScreen(___sideScreens, configBody, itemInfo.first, itemInfo.second);
            }
        }

        private static GGSideScreenContent CreateSideScreen(IList<DetailsScreen.SideScreenRef> existing, GameObject parent, Type sourceScreen, Type newScreen) {
            if (sourceScreen.IsAssignableFrom(typeof(SideScreenContent)))
                throw new TypeLoadException("参数sourceScreen不可用，该类型必须继承" + typeof(SideScreenContent).FullName);

            if (newScreen.IsAssignableFrom(typeof(GGSideScreenContent)))
                throw new TypeLoadException("参数newScreen不可用，该类型必须继承" + typeof(GGSideScreenContent).FullName);

            DetailsScreen.SideScreenRef retScreenRef = null;
            GGSideScreenContent ret = null;
            bool isCopySuccess = false;
            foreach (var existScreen in existing) {
                if (existScreen.screenPrefab.GetType() != sourceScreen) continue;
                if (existScreen.screenPrefab == null) { continue; }
                retScreenRef = new DetailsScreen.SideScreenRef();
                ret = CopySideScreen(existScreen.screenPrefab.gameObject, parent, sourceScreen, newScreen);  // 这里会找 PlanterSideScreen 和 ReceptacleSideScreen 中的部分字段拼到一起来构造一个自定义的侧边栏
                retScreenRef.name = newScreen.Name;
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
                var srcScreenParentType = originalScreenType.BaseType;
                foreach (var (newName, sourceName) in copyFieldDict) {
                    var sourceField = originalScreenType.GetField(sourceName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    var newField = newScreenType.GetField(newName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    if (sourceField == null) {
                        sourceField = srcScreenParentType.GetField(sourceName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    }

                    if (sourceField == null) { Debug.LogError("[BuildableWildPlant]not found newField, name: " + sourceName); continue; }
                    if (newField == null) { Debug.LogError("[BuildableWildPlant]not found newField, name: " + newName); continue; }

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
