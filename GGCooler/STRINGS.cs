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

            public static class BUTTONS {
                public static class GGSUPERIORCOOLER {
                    public static LocString TRANSFERHEAT = "Transfer Heat"; // 转移热量
                    public static LocString TRANSFERHEATTOOLTIP = "Transfer the heat in the coolant to the environment"; // 转移冷却剂中的热量到环境中
                    public static LocString ABSORBHEAT = "Absorb Heat";     // 吞噬热量
                    public static LocString ABSORBHEATTOOLTIP = "Absorb the heat in the coolant without produce any heat";  // 吞噬冷却剂中的热量而不放热
                }
            }
        }
    }
}
