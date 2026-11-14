using System.Collections.Generic;
using System.Linq;
using WoWHelper.Code.Gameplay;

namespace WoWHelper.Code.WorldState
{
    public static class WowLevelingConfigs
    {
        public static readonly WowLevelingConfiguration LEVEL_1_THRU_3 = new WowLevelingConfiguration
        {
            Levels = new List<int> { 1, 2, 3},

            FarmingConfigs = new List<WowFarmingConfiguration>
            {
                WowFarmingConfigs.LEVEL_1_DUROTAR_BOARS_AND_SCORPS,
                WowFarmingConfigs.LEVEL_1_MULGORE_PLAINSTRIDERS,
                WowFarmingConfigs.LEVEL_1_TIRISFAL_UNDEAD,
                WowFarmingConfigs.LEVEL_1_TELDRASSIL_ANIMALS,
            },
        };

        public static readonly WowLevelingConfiguration LEVEL_4_THRU_6 = new WowLevelingConfiguration
        {
            Levels = new List<int> { 4, 5 },

            FarmingConfigs = new List<WowFarmingConfiguration>
            {
                //WowFarmingConfigs.LEVEL_4_DUROTAR_IMPS, // Too crowded
                WowFarmingConfigs.LEVEL_4_DUROTAR_SCORPS,
                WowFarmingConfigs.LEVEL_4_MULGORE_MOUNTAIN_COUGARS,
                WowFarmingConfigs.LEVEL_4_TIRISFAL_ANIMALS,
                WowFarmingConfigs.LEVEL_4_TELDRASSIL_ANIMALS,
            },
        };

        public static readonly WowLevelingConfiguration LEVEL_7_THRU_10 = new WowLevelingConfiguration
        {
            Levels = new List<int> { 6, 7, 8, 9, 10 },

            FarmingConfigs = new List<WowFarmingConfiguration>
            {
                WowFarmingConfigs.LEVEL_6_DUROTAR_BOAR_RAZOR_HILL_LOOP,
                WowFarmingConfigs.LEVEL_6_MULGORE_BATTLEBOARS,
                WowFarmingConfigs.LEVEL_6_TIRISFAL_ZOMBIES,
                WowFarmingConfigs.LEVEL_7_TELDRASSIL_ANIMALS,
            },
        };

        public static readonly WowLevelingConfiguration LEVEL_11_THRU_12 = new WowLevelingConfiguration
        {
            Levels = new List<int> { 11, 12 },

            FarmingConfigs = new List<WowFarmingConfiguration>
            {
                WowFarmingConfigs.LEVEL_9_DUROTAR_SKULL_ROCK_COAST,
            },
        };

        public static readonly List<WowLevelingConfiguration> ALL_LEVELING_CONFIGS = new List<WowLevelingConfiguration>
        {
            LEVEL_1_THRU_3,
            LEVEL_4_THRU_6,
            LEVEL_7_THRU_10,
            LEVEL_11_THRU_12,
        };

        public static WowLevelingConfiguration GetFor(WowCombatConfiguration playerClass, int level)
        {
            return ALL_LEVELING_CONFIGS.FirstOrDefault(c => c.Levels.Contains(level) && c.Class == playerClass)
                ?? ALL_LEVELING_CONFIGS.FirstOrDefault(c => c.Levels.Contains(level) && c.Class == null);
        }
    }
}
