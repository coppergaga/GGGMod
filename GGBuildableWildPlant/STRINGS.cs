using OUI = STRINGS.UI;

namespace GGGMod.BuildableWildPlant {
    public static class STRINGS {
        public static class BUILDINGS {
            public static class PREFABS {
                public static class GGBUILDABLEWILDPLANT {
                    public static LocString NAME = OUI.FormatAsLink("Buildable Wild Plant", BuildableWildPlantConfig.ID);
                    public static LocString DESC = "A farm tile that can turn into dirt";
                    public static LocString EFFECT = "After seeding the seeds here, the tile will turn into diry and seeds grow into plants";
                }
            }

            public static class BUTTONS {
                public static class HAULINGPOINT {
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
