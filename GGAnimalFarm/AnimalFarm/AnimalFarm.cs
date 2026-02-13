using KSerialization;
using STRINGS;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GGGMod.AnimalFarm {
    [SerializationConfig(MemberSerialization.OptIn)]
    public class AnimalFarm : StateMachineComponent<AnimalFarm.SMI>, IUserControlledCapacity, IGameObjectEffectDescriptor, ISim4000ms {
        [Serialize] private List<StoredData> storedAnimals = new List<StoredData>();
        [Serialize] private int animalLimit = 20;

        [MyCmpReq] private TreeFilterable filterCmp;
        [MyCmpGet] private Operational operationalCmp;
        [MyCmpGet] private PrimaryElement primaryElementCmp;
        [MyCmpGet] private KSelectable selectableCmp;

        private static StatusItem MyStatusItem;
        private static StatusItemCategory MyStatusCategory = new StatusItemCategory("GGAnimalFarm", Db.Get().StatusItemCategories, "Farm Capacity");

        private const int detectRange = 4;
        private static readonly CellOffset cavityOffset = new CellOffset(0, 1);
        private static readonly List<Tag> ignoredTags = new List<Tag>() { 
            GameTags.Stored, GameTags.PickupableStorage, GameTags.Trapped, GameTags.StoredPrivate, GameTags.Dead,
            GameTags.Creatures.Bagged, GameTags.Creatures.Die,
        };

        private bool isNeedUpdateNewDay = false;
        private bool isNeedCheckFilter = false;
        private int cavityCell;
        private CavityInfo cavityInfoCache;
        private readonly HashSet<Tag> cachedAcceptedTags = new HashSet<Tag>();
        private readonly List<Pickupable> pickupables = new List<Pickupable>();
        
        public float Temperature => primaryElementCmp.Temperature;
        public float IncubationEffect => Mathf.Ceil(40f * operationalCmp.GetLastCycleUptime());
        public float ProduceEffect => Mathf.Max(1f, 4f * operationalCmp.GetLastCycleUptime());
        public float LastCycleUptime => operationalCmp.GetLastCycleUptime();

        public int UpLeftCellPos { get; private set; }
        public int UpRightCellPos { get; private set; }
        public int UpCellPos { get; private set; }

        private AnimalFarmSim simHelper = new AnimalFarmSim();

        public float UserMaxCapacity { get => animalLimit; set => animalLimit = Mathf.RoundToInt(value); }
        public float AmountStored => storedAnimals.Count;
        public float MinCapacity => 0f;
        public float MaxCapacity => 20f;
        public bool WholeValues => true;
        public LocString CapacityUnits => UI.UISIDESCREENS.CAPTURE_POINT_SIDE_SCREEN.UNITS_SUFFIX;
        public bool ControlEnabled() { return true; }

        private static readonly Func<object, AnimalFarm, Util.IterationInstruction> AsyncUpdateVisitor = delegate (object obj, AnimalFarm farm) {
            Pickupable pickupable = obj as Pickupable;
            if (farm.IsPickupableRelevantToMyInterests(pickupable.KPrefabID, pickupable.cachedCell)) {
                farm.pickupables.Add(pickupable);
            }
            return Util.IterationInstruction.Continue;
        };

        private static readonly EventSystem.IntraObjectHandler<AnimalFarm> OnCopySettingsDelegate =
            new EventSystem.IntraObjectHandler<AnimalFarm>((cmp, data) => cmp.HandleCopySettings(data));

        protected override void OnSpawn() {
            base.OnSpawn();
            int cell = Grid.PosToCell(this);
            cavityCell = Grid.OffsetCell(cell, cavityOffset);
            UpLeftCellPos = Grid.CellUpLeft(cell);
            UpRightCellPos = Grid.CellUpRight(cell);
            UpCellPos = Grid.CellAbove(cell);
            simHelper.master = this;
            GameClock.Instance.Subscribe((int)GameHashes.NewDay, HandleNewDay);
            GameScheduler.Instance.Schedule("AnimalFarm", 0.1f, (_) => { if (this != null) { this.CacheFilterTags(null); } });
            filterCmp.OnFilterChanged = (Action<HashSet<Tag>>)Delegate.Combine(filterCmp.OnFilterChanged, new Action<HashSet<Tag>>(HandleFilterChanged));
            Subscribe((int)GameHashes.CopySettings, OnCopySettingsDelegate);
            InfoManager.Inst.Setup();
            smi.StartSM();
            SetupStorageStatusItems();
        }

        protected override void OnCleanUp() {
            DropAll();
            if (GameClock.Instance != null) {
                GameClock.Instance.Unsubscribe((int)GameHashes.NewDay, HandleNewDay);
            }
            if (filterCmp != null) {
                TreeFilterable treeFilterable = filterCmp;
                treeFilterable.OnFilterChanged = (Action<HashSet<Tag>>)Delegate.Remove(treeFilterable.OnFilterChanged, new Action<HashSet<Tag>>(HandleFilterChanged));
            }
            Unsubscribe((int)GameHashes.CopySettings, OnCopySettingsDelegate);
            base.OnCleanUp();
        }

        private void HandleFilterChanged(HashSet<Tag> selectedTags) { isNeedCheckFilter = true; }

        private int _lastCycle = -1;
        private void HandleNewDay(object data) {
            int curCycle = GameClock.Instance.GetCycle();
            if (curCycle > _lastCycle) {
                isNeedUpdateNewDay = true;
                _lastCycle = curCycle;
            }
        }

        private void CatchOrSupplementAnimals() {
            cavityInfoCache = Game.Instance.roomProber.GetCavityForCell(cavityCell);
            if (cavityInfoCache == null) { return; }
            // 检测是否为畜舍
            if (cavityInfoCache.room == null || cavityInfoCache.room.roomType != Db.Get().RoomTypes.CreaturePen) { return; }
            // 检测小动物数量
            int count = RefreshAnimalCount();
            int extra = count - (int)UserMaxCapacity;
            if (extra >= 0) {                // 移除范围内多余小动物
                DetectAnimals();            // 检测 宽9*高8 范围内是否有小动物
                DatafyAndDestroy(extra);    // 数据化小动物和蛋
            }
            else {                          // 补充缺少的小动物
                SupplementAnimals(Math.Abs(extra), false);
            }
        }

        private void SupplementAnimals(int extraCount, bool reverseFilter) {
            if (extraCount <= 0) { return; }
            if (storedAnimals.Count == 0) { return; }
            var ret = storedAnimals;
            int supplementCnt = 0;
            for (int i = ret.Count - 1; i >= 0; i--) {
                if (supplementCnt >= extraCount) { break; }
                if (ret[i].IsNeedDelete) { continue; }
                if (!ret[i].IsAnimal) { continue; }
                // 反选场景: 不使用 选中的小动物类别, 但这个小动物 是 这个类别
                if (reverseFilter && cachedAcceptedTags.Contains(ret[i].PrefabTag)) { continue; }
                // 正选场景: 使用 选中的小动物类别, 但这个小动物 不是 这个类别
                if (!reverseFilter && !cachedAcceptedTags.Contains(ret[i].PrefabTag)) { continue; }

                var sd = ret[i];
                sd.SpawnAnimal(transform.GetPosition());
                supplementCnt++;
                sd.type |= StoredFlags.MarkDelete;
                ret[i] = sd;
            }
            ret.RemoveAll(StoredData.ShouldDelete);
        }

        private void DatafyAndDestroy(int extraCount) {
            if (extraCount < 0) { return; }
            if (pickupables.Count == 0) { return; }
            int removedCnt = 0;
            for (int i = 0; i < pickupables.Count; i++) {
                var pick = pickupables[i];
                if (pick == null) continue;
                var prefabID = pick.KPrefabID;
                var data = new StoredData() {
                    prefabTag = prefabID.PrefabTag,
                };
                var wildness = Db.Get().Amounts.Wildness.Lookup(pick.gameObject);
                if (wildness != null && wildness.value > 0) {
                    data.type |= StoredFlags.Wild;
                    data.wildness = wildness.value;
                }
                if (prefabID.HasTag(GameTags.Egg)) {    // 不区分蛋的种类直接拿走
                    data.type |= StoredFlags.Egg;
                    var incubation = Db.Get().Amounts.Incubation.Lookup(pick.gameObject);
                    data.incubation = (incubation != null) ? incubation.value : 0f;
                    storedAnimals.Add(data);
                    Util.KDestroyGameObject(pick.gameObject);
                }
                else {
                    if (removedCnt >= extraCount) { break; }
                    data.type |= StoredFlags.Animal;
                    var age = Db.Get().Amounts.Age.Lookup(pick.gameObject);
                    if (age != null) { data.age = age.value; }
                    else { data.age = 0f; }

                    storedAnimals.Add(data);
                    Util.KDestroyGameObject(pick.gameObject);
                    removedCnt++;
                }
            }
        }

        private void DetectAnimals() {
            pickupables.Clear();
            Grid.CellToXY(cavityCell, out int x, out int y);
            GameScenePartitioner.Instance.VisitEntries(
                x - detectRange, y - 1,
                detectRange * 2 + 1, detectRange * 2,
                GameScenePartitioner.Instance.pickupablesLayer,
                AsyncUpdateVisitor,
                this
            );
        }
        private bool IsPickupableRelevantToMyInterests(KPrefabID prefabID, int storage_cell) {
            // 优先检查是否已经有复制人来拿
            if (prefabID.HasAnyTags(ignoredTags)) { return false; }
            // 判断在同一个房间, 以免穿墙捕获
            var animalCavityInfo = Game.Instance.roomProber.GetCavityForCell(storage_cell);
            if (animalCavityInfo == null || animalCavityInfo != cavityInfoCache) { return false; }
            // 小动物蛋直接拿
            if (prefabID.HasTag(GameTags.Egg)) { return true; }
            // 不是过滤器中的小动物不拿
            if (!cachedAcceptedTags.Contains(prefabID.PrefabTag)) { return false; }
            return true;
        }

        private int RefreshAnimalCount() {
            int num = 0;
            foreach (KPrefabID creature in cavityInfoCache.creatures) {
                if (cachedAcceptedTags.Contains(creature.PrefabTag)) {
                    num++;
                }
            }
            return num;
        }
        private void DropAll() {
            var pos = transform.GetPosition();
            for (int i = 0; i < storedAnimals.Count; i++) {
                var animal = storedAnimals[i];
                if (animal.IsNeedButcher || animal.IsNeedDelete) { continue; }
                if (animal.IsAnimal) {
                    animal.SpawnAnimal(pos);
                }
                else {
                    animal.SpawnEgg(pos);
                }
            }
            storedAnimals.Clear();
        }
        private void DropFilterd() {
            SupplementAnimals(8, true);
            isNeedCheckFilter = false;
        }

        private void CacheFilterTags(object data) {
            cachedAcceptedTags.Clear();
            cachedAcceptedTags.UnionWith(filterCmp.AcceptedTags);
        }

        private int _skipper = 1;
        public void Sim4000ms(float dt) {
            // 倍速游戏时降低扫描频率
            if (SpeedControlScreen.Instance.GetSpeed() > 1 && ++_skipper % 2 == 0) { _skipper = 0; return; }

            if (isNeedUpdateNewDay) {
                simHelper.SimStoreData(storedAnimals, IncubationEffect);
                simHelper.SpawnAnimalDrop();
                isNeedUpdateNewDay = false;
                return;
            }

            if (isNeedCheckFilter) {
                CacheFilterTags(null);
                DropFilterd();
            }
            CatchOrSupplementAnimals();

            int animalCnt = storedAnimals.FindAll(sd => sd.IsAnimal).Count;
            _animalNumStr = animalCnt.ToString();
            _eggNumStr = (storedAnimals.Count - animalCnt).ToString();
            if (selectableCmp.IsSelected) {
                Game.Instance.userMenu.Refresh(gameObject);
            }
        }

        private void HandleCopySettings(object data) {
            GameObject gameObject = (GameObject)data;
            if (!(gameObject == null)) {
                var component = gameObject.GetComponent<AnimalFarm>();
                if (!(component == null)) {
                    UserMaxCapacity = component.UserMaxCapacity;
                }
            }
        }

        private void SetupStorageStatusItems() {
            if (MyStatusItem == null) {
                MyStatusItem = new StatusItem("GGAnimalFarm", "BUILDING", "", StatusItem.IconType.Info, NotificationType.Neutral, allow_multiples: false, OverlayModes.None.ID);
                MyStatusItem.resolveStringCallback = delegate (string str, object data) {
                    AnimalFarm farm = (AnimalFarm)data;
                    str = str.Replace("{0}", farm._animalNumStr);
                    str = str.Replace("{1}", farm._eggNumStr);
                    return str;
                };
            }
            var group = GetComponent<KSelectable>().GetStatusItemGroup();
            if (group != null) {
                group.AddStatusItem(MyStatusItem, this, MyStatusCategory);
            }
        }

        private string _animalNumStr = "0";
        private string _eggNumStr = "0";

        private readonly Dictionary<Tag, int> _recDict = new Dictionary<Tag, int>();
        private string StorageDesc() {
            _recDict.Clear();
            for (int i = 0; i < storedAnimals.Count; i++) {
                var tag = storedAnimals[i].PrefabTag;
                if (!_recDict.TryGetValue(tag, out int num)) { num = 0; }
                _recDict[tag] = num + 1;
            }
            return string.Join(", ", _recDict.Select(kvp => $"{InfoManager.Inst.ProperName(kvp.Key)}: {kvp.Value}"));
        }

        private readonly static string DESCRIPTOR_TOOLTIP = "";
        private readonly List<Descriptor> _descripter = new List<Descriptor>();
        public List<Descriptor> GetDescriptors(GameObject go) {
            _descripter.Clear();
            var d = new Descriptor(StorageDesc(), DESCRIPTOR_TOOLTIP);
            _descripter.Add(d);
            return _descripter;
        }

        public List<StoredData> StoredDatas => storedAnimals;

        public class SMI : GameStateMachine<State, SMI, AnimalFarm>.GameInstance {
            public SMI(AnimalFarm master) : base(master) { }
        }

        public class State : GameStateMachine<State, SMI, AnimalFarm, object> {
            public State off;
            public State on;
            public override void InitializeStates(out BaseState default_state) {
                default_state = off;
                off.PlayAnim("off")
                    .EventTransition(GameHashes.OperationalChanged, on, (smi) => smi.master.operationalCmp.IsOperational);
                on.PlayAnim("on")
                    .Enter(smi => { smi.master.operationalCmp.SetActive(true); })
                    .EventTransition(GameHashes.OperationalChanged, off, (smi) => !smi.master.operationalCmp.IsOperational)
                    .Exit(smi => { smi.master.operationalCmp.SetActive(false); });
            }
        }
    }
}
