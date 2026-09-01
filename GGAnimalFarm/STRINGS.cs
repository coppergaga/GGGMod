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
                public static class GGFISHPOD {
                    public static LocString NAME = OUI.FormatAsLink("Fishpod", FishpodConfig.ID);
                    public static LocString DESC = "A fishpod that auto manage your extra fishes";
                    public static LocString EFFECT = "Fishes in it will work and produce items every day";
                }
                public static class GGFARMTRANSFER {
                    public static LocString NAME = OUI.FormatAsLink("Farm Transfer", FarmTransferConfig.ID);    // 农场转移点
                    // 自动将农场或者鱼塘的符合条件的小动物转移到目标农场或者鱼塘
                    public static LocString DESC = "Automatically transfer the eligible critters from the source AnimalFarm or Fishpond to the target AnimalFarm or Fishpond";
                    // 通过转移点可以更加方便的自动分类变种小动物
                    public static LocString EFFECT = "By using the FarmTransfer, it becomes much more convenient to automatically classify and breed the mutant critters";
                }
            }
        }

        public static class BUILDING {
            public static class STATUSITEMS {
                public static class GGANIMALFARM {
                    public static LocString NAME = "{0} animal(s), {1} egg(s)";
                    public static LocString TOOLTIP = "Your fantanstic animal farm / fishpod";
                }
            }
        }

        public static class UI {
            public static class UISIDESCREENS {
                public static class FARMTRANSFERSIDESCREEN {
                    public static LocString TITLE = "Select target";   // 选择目标农场
                    public static LocString HEADER = "Binding To: {0}";
                }
            }
        }
    }
}
