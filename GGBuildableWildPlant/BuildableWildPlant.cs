using KSerialization;
using System.Collections.Generic;
using UnityEngine;

namespace GGGMod.BuildableWildPlant {
    public class BuildableWildPlant : KMonoBehaviour, ISim1000ms, IUserControlledCapacity {
        [MyCmpGet] private Storage storage;
        [Serialize] private float userMaxCapacity = 1f;

        [Serialize] private bool isAutoPlant = true;
        private bool isDestroying = false;

        protected FilteredStorage filteredStorage;

        public float UserMaxCapacity {
            get { return Mathf.Min(userMaxCapacity, storage.capacityKg); }
            set {
                userMaxCapacity = Mathf.Min(1f, value);
                filteredStorage.FilterChanged();
            }
        }
        public float AmountStored => storage.MassStored();
        public float MinCapacity => 0f;
        public float MaxCapacity => 1f;
        public bool WholeValues => true;
        public LocString CapacityUnits => GameUtil.GetCurrentMassUnit();
        public bool ControlEnabled() {
            return true;
        }

        public string choreTypeID = Db.Get().ChoreTypes.StorageFetch.Id;

        private static readonly EventSystem.IntraObjectHandler<BuildableWildPlant> OnCopySettingsDelegate =
            new EventSystem.IntraObjectHandler<BuildableWildPlant>(delegate (BuildableWildPlant component, object data) {
                component.OnCopySettings(data);
            });
        private static readonly EventSystem.IntraObjectHandler<BuildableWildPlant> OnRefreshUserMenuDelegate =
            new EventSystem.IntraObjectHandler<BuildableWildPlant>(delegate (BuildableWildPlant component, object data) {
                component.OnRefreshUserMenu(data);
            });

        protected override void OnPrefabInit() {
            Initialize(use_logic_meter: false);
        }

        protected void Initialize(bool use_logic_meter) {
            base.OnPrefabInit();
            ChoreType fetch_chore_type = Db.Get().ChoreTypes.Get(choreTypeID);
            filteredStorage = new FilteredStorage(this, null, null, use_logic_meter, fetch_chore_type);
            Subscribe((int)GameHashes.CopySettings, OnCopySettingsDelegate);
            Subscribe((int)GameHashes.RefreshUserMenu, OnRefreshUserMenuDelegate);
        }

        protected override void OnSpawn() {
            base.OnSpawn();
            filteredStorage.FilterChanged();
            Trigger((int)GameHashes.OnStorageLockerSetupComplete);
        }
        protected override void OnCleanUp() {
            Unsubscribe((int)GameHashes.CopySettings, OnCopySettingsDelegate);
            Unsubscribe((int)GameHashes.RefreshUserMenu, OnRefreshUserMenuDelegate);
            filteredStorage.CleanUp();
        }

        private void ToggleWillSelfDestruct() {
            isAutoPlant = !isAutoPlant;
        }

        private void OnCopySettings(object data) {
            GameObject gameObject = (GameObject)data;
            if (!(gameObject == null)) {
                BuildableWildPlant component = gameObject.GetComponent<BuildableWildPlant>();
                if (!(component == null)) {
                    UserMaxCapacity = component.UserMaxCapacity;
                    isAutoPlant = component.isAutoPlant;
                }
            }
        }

        private void OnRefreshUserMenu(object data) {
            KIconButtonMenu.ButtonInfo autoDropButton = isAutoPlant
                ? new KIconButtonMenu.ButtonInfo(
                    "action_empty_contents", STRINGS.BUILDINGS.BUTTONS.HAULINGPOINT.AUTO_PLANT_OFF,
                    ToggleWillSelfDestruct,
                    Action.NumActions, null, null, null, STRINGS.BUILDINGS.BUTTONS.HAULINGPOINT.AUTO_PLANT_OFF_TOOLTIP)
                : new KIconButtonMenu.ButtonInfo(
                    "action_empty_contents", STRINGS.BUILDINGS.BUTTONS.HAULINGPOINT.AUTO_PLANT_ON,
                    ToggleWillSelfDestruct,
                    Action.NumActions, null, null, null, STRINGS.BUILDINGS.BUTTONS.HAULINGPOINT.AUTO_PLANT_ON_TOOLTIP);
            Game.Instance.userMenu.AddButton(gameObject, autoDropButton);
        }

        public void UpdateForbiddenTag(Tag game_tag, bool forbidden) {
            if (forbidden) {
                filteredStorage.RemoveForbiddenTag(game_tag);
            }
            else {
                filteredStorage.AddForbiddenTag(game_tag);
            }
        }

        private static Vector3 _backwallPlantOffset = new Vector3(0.01f, 0f, -0.5f);
        public void Sim1000ms(float dt) {
            if (!isAutoPlant || isDestroying) { return; }
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
                isDestroying = true;
                var material = GetComponent<PrimaryElement>();
                var materialMass = Mathf.Floor(Settings.constractionsMass[0] / 4f);
                foreach (int testCell in testCellList) {
                    if (BackwallManager.HasBackwall(testCell)) { continue; }
                    SimMessages.SetBackwallData(testCell, material.Element.idx, materialMass, material.Temperature);
                }
                GameScheduler.Instance.Schedule("BuildableWildPlant", 0.6f, (_) => {
                    if (gameObject == null) { return; } // it means the building has been destroyed before plant the plant
                    if (plantableSeed == null) { return; }
                    var pos = transform.GetPosition() + _backwallPlantOffset;
                    GameObject go = GameUtil.KInstantiate(Assets.GetPrefab(plantableSeed.PlantID), pos, Grid.SceneLayer.BuildingFront);
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

                isDestroying = true;
                var element = GetComponent<PrimaryElement>();
                SimMessages.ReplaceElement(cell, element.ElementID, null, Settings.constractionsMass[0], element.Temperature);

                GameScheduler.Instance.Schedule("BuildableWildPlant", 0.6f, (_) => {
                    if (gameObject == null) { return; } // it means the building has been destroyed before plant the plant
                    if (plantableSeed == null) { return;  }
                    Vector3 pos = Grid.CellToPosCBC(plantCell, Grid.SceneLayer.BuildingFront);
                    GameObject go = GameUtil.KInstantiate(Assets.GetPrefab(plantableSeed.PlantID), pos, Grid.SceneLayer.BuildingFront);
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
}
