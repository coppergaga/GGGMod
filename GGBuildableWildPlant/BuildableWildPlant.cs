using KSerialization;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GGGMod.BuildableWildPlant {
    public class BuildableWildPlant : KMonoBehaviour {
        [MyCmpGet] private Storage storage;
        [Serialize] private bool isAutoPlant = true;

        private static readonly EventSystem.IntraObjectHandler<BuildableWildPlant> OnCopySettingsDelegate =
            new EventSystem.IntraObjectHandler<BuildableWildPlant>(delegate (BuildableWildPlant component, object data) {
                component.OnCopySettings(data);
            });
        private static readonly EventSystem.IntraObjectHandler<BuildableWildPlant> OnRefreshUserMenuDelegate =
            new EventSystem.IntraObjectHandler<BuildableWildPlant>(delegate (BuildableWildPlant component, object data) {
                component.OnRefreshUserMenu(data);
            });

        private EntityPreview plantPreview;

        [Serialize] public Tag requestedEntityTag;

        public static readonly List<Tag> possibleDepositTagsList = new List<Tag> {
            GameTags.CropSeed, GameTags.WaterSeed, GameTags.BackwallSeed, GameTags.LargeSeed, GameTags.DecorSeed
        };
        public IReadOnlyList<Tag> possibleDepositObjectTags => possibleDepositTagsList;
        [Serialize] public Tag requestedEntityAdditionalFilterTag;
        protected FetchChore fetchChore;
        public FetchChore GetActiveRequest => fetchChore;
        public ChoreType choreType = Db.Get().ChoreTypes.Fetch;
        protected StatusItem statusItemAwaitingDelivery;
        protected StatusItem statusItemNeed;
        protected StatusItem statusItemNoneAvailable;

        public void CreateOrder(Tag entityTag, Tag additionalFilterTag) {
            requestedEntityTag = entityTag;
            requestedEntityAdditionalFilterTag = additionalFilterTag;
            CreateFetchChore(requestedEntityTag, requestedEntityAdditionalFilterTag);
            SetPreview(entityTag);
            UpdateStatusItem();
        }
        public void CancelActiveRequest() {
            if (fetchChore != null) {
                MaterialNeeds.UpdateNeed(requestedEntityTag, -1f, gameObject.GetMyWorldId());
                fetchChore.Cancel("User canceled");
                fetchChore = null;
            }

            requestedEntityTag = Tag.Invalid;
            requestedEntityAdditionalFilterTag = Tag.Invalid;
            UpdateStatusItem();
            SetPreview(Tag.Invalid);
        }
        public void SetPreview(Tag entityTag) {
            PlantableSeed plantableSeed = null;
            GameObject seedPrefab = Assets.GetPrefab(entityTag);
            if (entityTag.IsValid) {
                if (seedPrefab == null) {
                    DebugUtil.LogWarningArgs(gameObject, "Planter tried previewing a tag with no asset! If this was the 'Empty' tag, ignore it, that will go away in new save games. Otherwise... Eh? Tag was: ", entityTag);
                    return;
                }
                plantableSeed = seedPrefab.GetComponent<PlantableSeed>();
            }

            if (plantPreview != null) {
                KPrefabID component = plantPreview.GetComponent<KPrefabID>();
                if (plantableSeed != null && component != null && component.PrefabTag == plantableSeed.PreviewID) {
                    return;
                }
                Util.KDestroyGameObject(plantPreview.gameObject);
            }

            if (!(plantableSeed != null)) { return; }

            var plantPos = EPlantPosition.None;
            var isBackwallSeed = seedPrefab.GetComponent<KPrefabID>().HasTag(GameTags.BackwallSeed);
            if (isBackwallSeed) { plantPos = EPlantPosition.Backwall; }
            else if (SingleEntityReceptacle.ReceptacleDirection.Top == plantableSeed.Direction) { plantPos = EPlantPosition.Top; }
            else if (SingleEntityReceptacle.ReceptacleDirection.Bottom == plantableSeed.Direction) { plantPos = EPlantPosition.Bottom; }
            var pos = CalcProperPlantPos(plantPos);
            GameObject previewGo = GameUtil.KInstantiate(Assets.GetPrefab(plantableSeed.PreviewID), pos, Grid.SceneLayer.Front);
            plantPreview = previewGo.GetComponent<EntityPreview>();
            previewGo.SetActive(value: true);
        }
        public bool IsValidEntity(GameObject candidate) => Game.IsCorrectDlcActiveForCurrentSave(candidate.GetComponent<KPrefabID>());

        protected void UpdateStatusItem() {
            UpdateStatusItem(GetComponent<KSelectable>());
        }
        protected void UpdateStatusItem(KSelectable selectable) {
            if (fetchChore != null) {
                bool flag = fetchChore.fetcher != null;
                WorldContainer myWorld = this.GetMyWorld();
                if (!flag && myWorld != null) {
                    foreach (Tag tag in fetchChore.tags) {
                        if (myWorld.worldInventory.GetTotalAmount(tag, includeRelatedWorlds: true) > 0f) {
                            if (myWorld.worldInventory.GetTotalAmount(requestedEntityAdditionalFilterTag, includeRelatedWorlds: true) > 0f || requestedEntityAdditionalFilterTag == Tag.Invalid) {
                                flag = true;
                            }
                            break;
                        }
                    }
                }

                if (flag) {
                    selectable.SetStatusItem(Db.Get().StatusItemCategories.EntityReceptacle, statusItemAwaitingDelivery);
                }
                else {
                    selectable.SetStatusItem(Db.Get().StatusItemCategories.EntityReceptacle, statusItemNoneAvailable);
                }
            }
            else {
                selectable.SetStatusItem(Db.Get().StatusItemCategories.EntityReceptacle, statusItemNeed);
            }
        }
        protected void CreateFetchChore(Tag entityTag, Tag additionalRequiredTag) {
            if (fetchChore == null && entityTag.IsValid && entityTag != GameTags.Empty) {
                fetchChore = new FetchChore(choreType, storage, GetPrefabFetchMass(entityTag), new HashSet<Tag> { entityTag }, FetchChore.MatchCriteria.MatchID, (additionalRequiredTag.IsValid && additionalRequiredTag != GameTags.Empty) ? additionalRequiredTag : Tag.Invalid, null, null, run_until_complete: true, OnFetchComplete, delegate {
                    UpdateStatusItem();
                }, delegate {
                    UpdateStatusItem();
                }, Operational.State.Functional);
                MaterialNeeds.UpdateNeed(requestedEntityTag, 1f, gameObject.GetMyWorldId());
                UpdateStatusItem();
            }
        }
        private float GetPrefabFetchMass(Tag entityTag) {
            GameObject prefab = Assets.GetPrefab(entityTag);
            if (prefab != null) {
                PrimaryElement component = prefab.GetComponent<PrimaryElement>();
                if (component != null) {
                    return component.MassPerUnit;
                }
            }

            KCrashReporter.ReportDevNotification("SingleEntityReceptacle " + base.name + " is requesting " + entityTag.Name + " which is not an entity", Environment.StackTrace);
            return 1f;
        }
        private void OnFetchComplete(Chore chore) {
            if (fetchChore == null) {
                Debug.LogWarningFormat(gameObject, "{0} OnFetchComplete fetchChore null", gameObject);
            }
            else if (fetchChore.fetchTarget == null) {
                Debug.LogWarningFormat(gameObject, "{0} OnFetchComplete fetchChore.fetchTarget null", gameObject);
            }
            else {
                SetPreview(Tag.Invalid);
                SpawnPlant();
            }
        }

        protected override void OnPrefabInit() {
            base.OnPrefabInit();
            Subscribe((int)GameHashes.CopySettings, OnCopySettingsDelegate);
            Subscribe((int)GameHashes.RefreshUserMenu, OnRefreshUserMenuDelegate);
        }

        protected override void OnCleanUp() {
            base.OnCleanUp();
            if (plantPreview != null) {
                Util.KDestroyGameObject(plantPreview.gameObject);
            }
            Unsubscribe((int)GameHashes.CopySettings, OnCopySettingsDelegate);
            Unsubscribe((int)GameHashes.RefreshUserMenu, OnRefreshUserMenuDelegate);
        }

        private void ToggleIsAutoPlant() {
            isAutoPlant = !isAutoPlant;
            if (isAutoPlant) {
                SetPreview(Tag.Invalid);
                SpawnPlant();
            }
        }

        private void OnCopySettings(object data) {
            GameObject go = (GameObject)data;
            if (!(go != null)) { return; }
            BuildableWildPlant component = go.GetComponent<BuildableWildPlant>();
            if (component != null) {
                isAutoPlant = component.isAutoPlant;
            }

            if (requestedEntityTag != component.requestedEntityTag || requestedEntityAdditionalFilterTag != component.requestedEntityAdditionalFilterTag) {
                CancelActiveRequest();
                CreateOrder(component.requestedEntityTag, component.requestedEntityAdditionalFilterTag);
            }
        }

        private void OnRefreshUserMenu(object data) {
            KIconButtonMenu.ButtonInfo autoDropButton = isAutoPlant
                ? new KIconButtonMenu.ButtonInfo(
                    "action_empty_contents", STRINGS.BUILDINGS.BUTTONS.GGBUILDABLEWILDPLANT.AUTO_PLANT_OFF,
                    ToggleIsAutoPlant,
                    Action.NumActions, null, null, null, STRINGS.BUILDINGS.BUTTONS.GGBUILDABLEWILDPLANT.AUTO_PLANT_OFF_TOOLTIP)
                : new KIconButtonMenu.ButtonInfo(
                    "action_empty_contents", STRINGS.BUILDINGS.BUTTONS.GGBUILDABLEWILDPLANT.AUTO_PLANT_ON,
                    ToggleIsAutoPlant,
                    Action.NumActions, null, null, null, STRINGS.BUILDINGS.BUTTONS.GGBUILDABLEWILDPLANT.AUTO_PLANT_ON_TOOLTIP);
            Game.Instance.userMenu.AddButton(gameObject, autoDropButton);
        }

        private Vector3 CalcProperPlantPos(EPlantPosition pp) {
            if (EPlantPosition.Backwall == pp) {
                return transform.GetPosition() + _backwallPlantOffset;
            }
            if (EPlantPosition.BackwallPreview == pp) {
                return transform.GetPosition() + _backwallPlantPreviewOffset;
            }
            if (EPlantPosition.Top == pp) {
                return transform.GetPosition() + Vector3.up;
            }
            if (EPlantPosition.Bottom == pp) {
                return transform.GetPosition() + Vector3.down;
            }
            return transform.GetPosition();
        }
        private static Vector3 _backwallPlantPreviewOffset = new Vector3(0.49f, 0f, -0.5f);
        private static Vector3 _backwallPlantOffset = new Vector3(0.01f, 0f, -0.5f);
        public void SpawnPlant() {
            if (!isAutoPlant) { return; }
            if (storage == null || storage.IsEmpty()) { return; }

            GameObject firstItem = storage.items[0];
            var plantableSeed = firstItem.GetComponent<PlantableSeed>();
            if (plantableSeed == null) { return; }

            int cell = Grid.PosToCell(transform.GetPosition());

            var prefabID = firstItem.GetComponent<KPrefabID>();
            if (prefabID.HasTag(GameTags.BackwallSeed)) {   // 需要背景墙的2*2大小的植物
                bool isPosInvalid = false;
                var testCellList = new List<int> { cell, Grid.CellRight(cell), Grid.CellAbove(cell), Grid.CellUpRight(cell) };
                foreach (int testCell in testCellList) {
                    if (!Grid.IsValidCell(testCell) || Grid.Foundation[testCell]) { isPosInvalid = true; break; }
                }

                if (isPosInvalid) { return; }

                var material = GetComponent<PrimaryElement>();
                var materialMass = Mathf.Floor(Settings.constractionsMass[0] / 4f);
                foreach (int testCell in testCellList) {
                    if (BackwallManager.HasBackwall(testCell)) { continue; }
                    SimMessages.SetBackwallData(testCell, material.Element.idx, materialMass, material.Temperature);
                }
                GameScheduler.Instance.Schedule("BuildableWildPlant", 0.6f, (_) => {
                    if (gameObject == null) { return; } // it means the building has been destroyed before plant the plant
                    if (plantableSeed == null) { return; }
                    GameObject go = GameUtil.KInstantiate(Assets.GetPrefab(plantableSeed.PlantID), CalcProperPlantPos(EPlantPosition.Backwall), Grid.SceneLayer.BuildingFront);
                    MutantPlant comp = go.GetComponent<MutantPlant>();
                    if (comp != null) { plantableSeed.GetComponent<MutantPlant>().CopyMutationsTo(comp); }
                    go.SetActive(value: true);

                    Pickupable pickupable = plantableSeed.GetComponent<Pickupable>().TakeUnit(1f);
                    if (pickupable != null) {
                        Util.KDestroyGameObject(pickupable.gameObject);
                        Util.KDestroyGameObject(gameObject);
                    }
                    else {
                        KCrashReporter.Assert(condition: false, "Seed has fractional total amount < 1f");
                    }
                });
            }
            else {  // 常规植物
                bool isDirectionTop = plantableSeed.Direction != SingleEntityReceptacle.ReceptacleDirection.Bottom;
                int plantCell = isDirectionTop ? Grid.CellAbove(cell) : Grid.CellBelow(cell);
                if (!Grid.IsValidCell(plantCell)) { return; }
                if (Grid.Foundation[plantCell]) { return; }

                var element = GetComponent<PrimaryElement>();
                SimMessages.ReplaceElement(cell, element.ElementID, null, Settings.constractionsMass[0], element.Temperature);

                GameScheduler.Instance.Schedule("BuildableWildPlant", 0.6f, (_) => {
                    if (gameObject == null) { return; } // it means the building has been destroyed before plant the plant
                    if (plantableSeed == null) { return;  }
                    GameObject go = GameUtil.KInstantiate(
                        Assets.GetPrefab(plantableSeed.PlantID),
                        CalcProperPlantPos(isDirectionTop ? EPlantPosition.Top : EPlantPosition.Bottom),
                        Grid.SceneLayer.BuildingFront
                    );
                    MutantPlant comp = go.GetComponent<MutantPlant>();
                    if (comp != null) { plantableSeed.GetComponent<MutantPlant>().CopyMutationsTo(comp); }
                    go.SetActive(value: true);

                    Pickupable pickupable = plantableSeed.GetComponent<Pickupable>().TakeUnit(1f);
                    if (pickupable != null) {
                        Util.KDestroyGameObject(pickupable.gameObject);
                        Util.KDestroyGameObject(gameObject);
                    }
                    else {
                        KCrashReporter.Assert(condition: false, "Seed has fractional total amount < 1f");
                    }
                });
            }
        }
    }

    public enum EPlantPosition {
        None, Backwall, Top, Bottom, BackwallPreview
    }
}
