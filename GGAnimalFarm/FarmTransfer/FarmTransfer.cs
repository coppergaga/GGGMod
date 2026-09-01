using KSerialization;
using System.Collections.Generic;

namespace GGGMod.AnimalFarm {
    [SerializationConfig(MemberSerialization.OptIn)]
    public class FarmTransfer : KMonoBehaviour {
        [Serialize] private string _fromFarmID;
        [Serialize] private string _toFarmID;
        public string FromFarmID => _fromFarmID ?? "";
        public string ToFarmID => _toFarmID ?? "";
        private static readonly EventSystem.IntraObjectHandler<FarmTransfer> OnRoomUpdateDelegate =
            new EventSystem.IntraObjectHandler<FarmTransfer>(delegate (FarmTransfer cmp, object data) { cmp.OnRoomUpdate(data); });
        public AnimalFarm.FarmType FarmTypo {
            get {
                var ret = TransferManager.Inst.TryGetFarmWithID(FromFarmID);
                return ret != null ? ret.FType : AnimalFarm.FarmType.None;
            }
        }

        protected override void OnSpawn() {
            base.OnSpawn();
            Subscribe((int)GameHashes.UpdateRoom, OnRoomUpdateDelegate);
            if (string.IsNullOrEmpty(_fromFarmID)) {
                ScanFromAnimalFarms();
            }

            TransferManager.Inst.RegisterTransfer(this);
        }

        protected override void OnCleanUp() {
            TransferManager.Inst.UnregisterTransfer(this);
            base.OnCleanUp();
        }

        private void OnRoomUpdate(object data) {
            ScanFromAnimalFarms();
        }

        public void ScanFromAnimalFarms() {
            var room = Game.Instance.roomProber.GetRoomOfGameObject(base.gameObject);
            if (room == null) return;
            var temp = "";
            foreach (var prefabID in room.buildings) {
                if (prefabID.IsNullOrDestroyed()) { continue; }
                var animalFarm = prefabID.GetComponent<AnimalFarm>();
                if (animalFarm != null) {
                    temp = animalFarm.UniqueID;
                    break;
                }
            }
            TransferManager.Inst.RemoveFromTo(_fromFarmID);
            _fromFarmID = temp;
        }

        public void SetToFarm(AnimalFarm to) {
            if (to.IsNullOrDestroyed()) return;
            _toFarmID = to.UniqueID != _toFarmID ? to.UniqueID : "";
            TransferManager.Inst.SetFromTo(FromFarmID, ToFarmID);
        }
    }

    public class TransferManager {
        private static TransferManager _inst;
        public static TransferManager Inst {
            get {
                if (_inst == null) { _inst = new TransferManager(); }
                return _inst;
            }
        }

        public AnimalFarm GetToFarm(string fromID) {
            if (_fromToMap.TryGetValue(fromID, out string toID) &&
                !string.IsNullOrEmpty(toID) &&
                _farmsDict.TryGetValue(toID, out var to)) {
                return to;
            }
            return null;
        }

        public void SetFromTo(string fromID, string toID) {
            if (string.IsNullOrEmpty(fromID)) { return; }
            _fromToMap[fromID] = toID;
        }

        public void RemoveFromTo(string fromID) {
            if (string.IsNullOrEmpty(fromID)) { return; }
            _fromToMap.Remove(fromID);
        }

        public void RegisterFarm(AnimalFarm af) {
            _farmsDict[af.UniqueID] = af;
            TriggerFarmTransferScan();
        }

        public void UnregisterFarm(AnimalFarm af) {
            _farmsDict.Remove(af.UniqueID);
            RemoveFromTo(af.UniqueID);
            TriggerFarmTransferScan();
        }

        public void TriggerFarmTransferScan() {
            foreach (var transfer in _transfersList) {
                if (transfer.IsNullOrDestroyed()) { continue; }
                if (_farmsDict.ContainsKey(transfer.FromFarmID)) { continue; }
                transfer.ScanFromAnimalFarms();
                SetFromTo(transfer.FromFarmID, transfer.ToFarmID);
            }
        }

        public void RegisterTransfer(FarmTransfer ft) {
            _transfersList.Add(ft);
            SetFromTo(ft.FromFarmID, ft.ToFarmID);
        }
        public void UnregisterTransfer(FarmTransfer ft) {
            _transfersList.Remove(ft);
            RemoveFromTo(ft.FromFarmID);
        }

        public AnimalFarm TryGetFarmWithID(string farmID) {
            AnimalFarm ret = null;
            if (!string.IsNullOrEmpty(farmID) && _farmsDict.TryGetValue(farmID, out var af)) {
                ret = af;
            }
            return ret;
        }

        private readonly Dictionary<string, AnimalFarm> _farmsDict = new Dictionary<string, AnimalFarm>();
        private readonly List<FarmTransfer> _transfersList = new List<FarmTransfer>();

        private readonly Dictionary<string, string> _fromToMap = new Dictionary<string, string>();

        private readonly List<AnimalFarm> _recycleList = new List<AnimalFarm>();
        public List<AnimalFarm> AnimalFarmList(AnimalFarm.FarmType typo) {
            _recycleList.Clear();
            if (AnimalFarm.FarmType.None == typo) { return _recycleList; }
            foreach (var farm in _farmsDict.Values) {
                if (!farm.IsNullOrDestroyed() && farm.FType == typo) { _recycleList.Add(farm); }
            }
            return _recycleList;
        }
    }
}
