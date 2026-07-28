using STRINGS;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GGGMod.SuperiorCooler {
    public class SuperiorCooler : KMonoBehaviour, IGameObjectEffectDescriptor, ISim200ms {
        [MyCmpReq]
        private KSelectable selectable;
        [MyCmpReq]
        protected Storage storage;
        [MyCmpReq]
        protected Operational operational;
        [MyCmpReq]
        private ConduitConsumer consumer;
        [MyCmpReq]
        private BuildingComplete building;
        [MyCmpGet]
        private KBatchedAnimController controller;

        public bool isLiquidConditioner;

        public float temperatureDelta = -14f;

        public float maxEnvironmentDelta = -50f;


        private int cooledAirOutputCell = -1;

        private static readonly EventSystem.IntraObjectHandler<SuperiorCooler> OnOperationalChangedDelegate = new EventSystem.IntraObjectHandler<SuperiorCooler>(delegate (SuperiorCooler component, object data) {
            component.OnOperationalChanged(data);
        });

        private static readonly EventSystem.IntraObjectHandler<SuperiorCooler> OnActiveChangedDelegate = new EventSystem.IntraObjectHandler<SuperiorCooler>(delegate (SuperiorCooler component, object data) {
            component.OnActiveChanged(data);
        });

        protected override void OnPrefabInit() {
            base.OnPrefabInit();
            Subscribe(-592767678, OnOperationalChangedDelegate);
            Subscribe(824508782, OnActiveChangedDelegate);
        }

        protected override void OnSpawn() {
            base.OnSpawn();
            cooledAirOutputCell = building.GetUtilityOutputCell();
        }



        private float lowTempLag;
        private bool showingLowTemp;
        private bool showingHotEnv;
        private SimHashes lastElement = SimHashes.Vacuum;
        private Guid statusHandle;

        private void UpdateState(float dt) {
            bool value = consumer.IsSatisfied;

            List<GameObject> items = storage.items;
            for (int i = 0; i < items.Count; i++) {
                PrimaryElement component = items[i].GetComponent<PrimaryElement>();
                if (component.Mass > 0f && (!isLiquidConditioner || !component.Element.IsGas) && (isLiquidConditioner || !component.Element.IsLiquid)) {
                    value = true;
                    float num = component.Temperature + temperatureDelta;
                    if (num < 1f) {
                        num = 1f;
                        lowTempLag = Mathf.Min(lowTempLag + dt / 5f, 1f);
                    }
                    else {
                        lowTempLag = Mathf.Min(lowTempLag - dt / 5f, 0f);
                    }

                    float num2 = (isLiquidConditioner ? Game.Instance.liquidConduitFlow : Game.Instance.gasConduitFlow).AddElement(cooledAirOutputCell, component.ElementID, component.Mass, num, component.DiseaseIdx, component.DiseaseCount);
                    component.KeepZeroMassObject = true;
                    float num3 = num2 / component.Mass;
                    int num4 = (int)((float)component.DiseaseCount * num3);
                    component.Mass -= num2;
                    component.ModifyDiseaseCount(-num4, "GGGSupriorCooler.UpdateState");
                    float num5 = (num - component.Temperature) * component.Element.specificHeatCapacity * num2;
                    if (isLiquidConditioner && lastElement != component.ElementID) {
                        GameUtil.TintLiquidSymbolOnBuilding("liquid", controller, component.Element);
                    }

                    lastElement = component.ElementID;
                    break;
                }
            }

            operational.SetActive(value);
            UpdateStatus();
        }

        private void OnOperationalChanged(object _) {
            if (operational.IsOperational) {
                UpdateState(0f);
            }
        }

        private void OnActiveChanged(object _) {
            UpdateStatus();
        }

        private void UpdateStatus() {
            if (operational.IsActive) {
                if (lowTempLag >= 1f && !showingLowTemp) {
                    statusHandle = (isLiquidConditioner ? selectable.SetStatusItem(Db.Get().StatusItemCategories.Main, Db.Get().BuildingStatusItems.CoolingStalledColdLiquid, this) : selectable.SetStatusItem(Db.Get().StatusItemCategories.Main, Db.Get().BuildingStatusItems.CoolingStalledColdGas, this));
                    showingLowTemp = true;
                    showingHotEnv = false;
                }
                else if (lowTempLag <= 0f && (showingHotEnv || showingLowTemp)) {
                    statusHandle = selectable.SetStatusItem(Db.Get().StatusItemCategories.Main, Db.Get().BuildingStatusItems.Cooling);
                    showingLowTemp = false;
                    showingHotEnv = false;
                }
                else if (statusHandle == Guid.Empty) {
                    statusHandle = selectable.SetStatusItem(Db.Get().StatusItemCategories.Main, Db.Get().BuildingStatusItems.Cooling);
                    showingLowTemp = false;
                    showingHotEnv = false;
                }
            }
            else {
                statusHandle = selectable.SetStatusItem(Db.Get().StatusItemCategories.Main, null);
            }
        }

        public void Sim200ms(float dt) {
            if (operational != null && !operational.IsOperational) {
                operational.SetActive(value: false);
            }
            else {
                UpdateState(dt);
            }
        }

        public List<Descriptor> GetDescriptors(GameObject go) {
            List<Descriptor> list = new List<Descriptor>();
            string formattedTemperature = GameUtil.GetFormattedTemperature(temperatureDelta, GameUtil.TimeSlice.None, GameUtil.TemperatureInterpretation.Relative);
            Descriptor item2 = default(Descriptor);
            item2.SetupDescriptor(string.Format(isLiquidConditioner ? UI.BUILDINGEFFECTS.LIQUIDCOOLING : UI.BUILDINGEFFECTS.GASCOOLING, formattedTemperature), string.Format(isLiquidConditioner ? UI.BUILDINGEFFECTS.TOOLTIPS.LIQUIDCOOLING : UI.BUILDINGEFFECTS.TOOLTIPS.GASCOOLING, formattedTemperature));
            list.Add(item2);
            return list;
        }
    }
}
