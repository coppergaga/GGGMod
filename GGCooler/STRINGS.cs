using OUI = STRINGS.UI;

namespace GGGMod.SuperiorCooler {
    public static class STRINGS {
        public static class BUILDINGS {
            public static class PREFABS {
                public static class GGSUPERIORLIQUIDCOOLER {
                    public static LocString NAME = OUI.FormatAsLink("Superior Liquid Cooler", SuperiorLiquidCoolerConfig.ID);
                    public static LocString DESC = "A liquid cooler without producing heat";
                    public static LocString EFFECT = "Each time the temperature of the liquid is lowered by 14 degrees Celsius";
                }
                public static class GGSUPERIORGASCOOLER {
                    public static LocString NAME = OUI.FormatAsLink("Superior Gas Cooler", SpiceGrinderConfig.ID);
                    public static LocString DESC = "A gas cooler without producing heat";
                    public static LocString EFFECT = "Each time the temperature of the gas is lowered by 14 degrees Celsius";
                }
            }
        }
    }
}
