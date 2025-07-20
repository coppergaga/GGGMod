using OUI = STRINGS.UI;
using GGGMod.LiquidCooler;

namespace GGGMod {
    public static class STRINGS {
        public static class BUILDINGS {
            public static class PREFABS {
                public static class GGLIQUIDCOOLER {
                    public static LocString NAME = OUI.FormatAsLink("Liquid Cooler", LiquidCoolerConfig.ID);
                    public static LocString DESC = "Cooling large amounts of liquid";
                    public static LocString EFFECT = "The liquid can be cooled to -173.15°C";
                }
            }
        }
    }
}
