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

            LocationConfigs = new List<WowLocationConfiguration>
            {
                WowLocationConfigs.LEVEL_1_DUROTAR_BOARS_AND_SCORPS,
                WowLocationConfigs.LEVEL_1_MULGORE_PLAINSTRIDERS,
                WowLocationConfigs.LEVEL_1_TIRISFAL_UNDEAD,
            },
        };

        public static readonly WowLevelingConfiguration LEVEL_4_THRU_8 = new WowLevelingConfiguration
        {
            Levels = new List<int> { 4, 5, 6, 7, 8 },

            LocationConfigs = new List<WowLocationConfiguration>
            {
                WowLocationConfigs.LEVEL_4_DUROTAR_IMPS,
                WowLocationConfigs.LEVEL_4_MULGORE_MOUNTAIN_COUGARS,
                WowLocationConfigs.LEVEL_4_TIRISFAL_ANIMALS,
            },
        };

        public static readonly List<WowLevelingConfiguration> ALL_LEVELING_CONFIGS = new List<WowLevelingConfiguration>
        {
            LEVEL_1_THRU_3,
            LEVEL_4_THRU_8,
        };

        // A config specific to this class wins over an any-class (Class == null) one at the
        // same level. Null if neither exists yet.
        public static WowLevelingConfiguration GetFor(WowCombatConfiguration playerClass, int level)
        {
            return ALL_LEVELING_CONFIGS.FirstOrDefault(c => c.Levels.Contains(level) && c.Class == playerClass)
                ?? ALL_LEVELING_CONFIGS.FirstOrDefault(c => c.Levels.Contains(level) && c.Class == null);
        }
    }
}
