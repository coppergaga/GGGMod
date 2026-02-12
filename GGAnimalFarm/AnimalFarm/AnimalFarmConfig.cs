using STRINGS;
using UnityEngine;

namespace GGGMod.AnimalFarm {
    public class AnimalFarmConfig : IBuildingConfig {
        public const string ID = "GgAnimalFarm";
        public override BuildingDef CreateBuildingDef() {
            var buildingdef = BuildingTemplates.CreateBuildingDef(
                ID, 5, 4, "gganimalfarm_kanim", 100, 30f,
                TUNING.BUILDINGS.CONSTRUCTION_MASS_KG.TIER5,
                TUNING.MATERIALS.RAW_METALS,
                1600f,
                BuildLocationRule.OnFloor,
                noise: TUNING.NOISE_POLLUTION.NONE,
                decor: TUNING.BUILDINGS.DECOR.NONE
            );
            buildingdef.RequiresPowerInput = true;
            buildingdef.EnergyConsumptionWhenActive = 1200f;
            buildingdef.PowerInputOffset = new CellOffset(1, 0);
            buildingdef.UtilityInputOffset = new CellOffset(1, 1);
            buildingdef.InputConduitType = ConduitType.Liquid;
            buildingdef.LogicInputPorts = LogicOperationalController.CreateSingleInputPortList(new CellOffset(-1, 0));
            buildingdef.Floodable = false;
            buildingdef.Overheatable = false;
            buildingdef.AudioCategory = "Metal";
            buildingdef.AddSearchTerms(SEARCH_TERMS.FOOD);
            buildingdef.AddSearchTerms(SEARCH_TERMS.FARM);
            return buildingdef;
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag) {
            go.AddOrGet<Operational>();

            var storage = go.AddComponent<Storage>();
            storage.showInUI = true;
            storage.allowItemRemoval = false;
            storage.allowSettingOnlyFetchMarkedItems = false;
            storage.fetchCategory = Storage.FetchCategory.StorageSweepOnly;
            storage.showDescriptor = false;
            storage.storageFilters = TUNING.STORAGEFILTERS.BAGABLE_CREATURES;

            ConduitConsumer conduitConsumer = go.AddOrGet<ConduitConsumer>();
            conduitConsumer.conduitType = ConduitType.Liquid;
            conduitConsumer.consumptionRate = 2f;
            conduitConsumer.capacityKG = 20f;
            conduitConsumer.capacityTag = ElementLoader.FindElementByHash(SimHashes.Water).tag;
            conduitConsumer.wrongElementResult = ConduitConsumer.WrongElementResult.Dump;

            ElementConverter elementConverter = go.AddOrGet<ElementConverter>();
            elementConverter.consumedElements = new ElementConverter.ConsumedElement[1] {
                new ElementConverter.ConsumedElement(ElementLoader.FindElementByHash(SimHashes.Water).tag, 1f)
            };
            elementConverter.outputElements = new ElementConverter.OutputElement[1] {
                new ElementConverter.OutputElement(0.2f, SimHashes.ToxicSand, 243.15f, useEntityTemperature: false, storeOutput: false)
            };
            go.AddOrGet<TreeFilterable>().dropIncorrectOnFilterChange = false;
            RoomTracker roomTracker = go.AddOrGet<RoomTracker>();
            roomTracker.requiredRoomType = Db.Get().RoomTypes.CreaturePen.Id;
            roomTracker.requirement = RoomTracker.Requirement.Required;
        }

        public override void DoPostConfigureComplete(GameObject go) {
            go.AddOrGet<LogicOperationalController>();
            go.AddOrGet<AnimalFarm>();
            AddVisualizer(go);
        }

        public override void DoPostConfigurePreview(BuildingDef def, GameObject go) {
            AddVisualizer(go);
        }

        public override void DoPostConfigureUnderConstruction(GameObject go) {
            AddVisualizer(go);
        }

        private static void AddVisualizer(GameObject prefab) {
            RangeVisualizer rangeVisualizer = prefab.AddOrGet<RangeVisualizer>();
            rangeVisualizer.OriginOffset = new Vector2I(0, 0);
            rangeVisualizer.RangeMin.x = -4;
            rangeVisualizer.RangeMax.x = 4;
            rangeVisualizer.RangeMin.y = 0;
            rangeVisualizer.RangeMax.y = 8;
            rangeVisualizer.BlockingTileVisible = true;
        }
    }
}
