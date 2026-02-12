using OUI = STRINGS.UI;

namespace GGGMod.AnimalFarm {
    public static class STRINGS {
        public static class BUILDINGS {
            public static class PREFABS {
                public static class GGANIMALFARM {
                    public static LocString NAME = OUI.FormatAsLink("Animal Farm", AnimalFarmConfig.ID);
                    public static LocString DESC = "A farm that auto manage your extra animals";
                    public static LocString EFFECT = "Animals in it will work and produce items every day";
                }
            }
        }

        public static class BUILDING {
            public static class STATUSITEMS {
                public static class GGANIMALFARM {
                    public static LocString NAME = "{0} animal(s), {1} egg(s)";
                    public static LocString TOOLTIP = "Your fantanstic animal farm";
                }
            }
        }
    }
}
