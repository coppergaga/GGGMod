using OUI = STRINGS.UI;

namespace GGGMod {
    public static class STRINGS {
        public static class BUILDINGS {
            public static class PREFABS {
                public static class GGLIQUIDCOOLER {
                    public static LocString NAME = OUI.FormatAsLink("Liquid Cooler", LiquidCooler.LiquidCoolerConfig.ID);
                    public static LocString DESC = "Cooling large amounts of liquid";
                    public static LocString EFFECT = "The liquid can be cooled to -173.15°C";
                }
                public static class GGBUILDABLEWILDPLANT {
                    public static LocString NAME = OUI.FormatAsLink("Buildable Wild Plant", BuildableWildPlant.BuildableWildPlantConfig.ID);
                    public static LocString DESC = "A farm tile that simulates wild conditions";
                    public static LocString EFFECT = " Any plants planted in it will be considered as wild plants";
                }
            }

            public class BUTTONS {
                public class HAULINGPOINT {
                    // Auto-Drop
                    public static LocString AUTO_PLANT_ON = "Enable Auto-Plant";
                    public static LocString AUTO_PLANT_ON_TOOLTIP = "If enabled, automatically plant the seed when storage is full";
                    public static LocString AUTO_PLANT_OFF = "Disable Auto-Plant";
                    public static LocString AUTO_PLANT_OFF_TOOLTIP = "Cancel automatic planting when full";
                }
            }
        }
    }
}
