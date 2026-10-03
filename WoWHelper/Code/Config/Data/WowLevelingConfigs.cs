using System.Collections.Generic;
using System.Linq;
using WoWHelper.Code.Gameplay;

namespace WoWHelper.Code.WorldState
{
    public static class WowLevelingConfigs
    {
        public static readonly WowLevelingConfiguration LEVEL_1 = new WowLevelingConfiguration
        {
            Level = 1,

            LocationConfigs = new List<WowLocationConfiguration>
            {
                WowLocationConfigs.LEVEL_1_DUROTAR_BOARS_AND_SCORPS,
                WowLocationConfigs.LEVEL_1_MULGORE_PLAINSTRIDERS,
                WowLocationConfigs.LEVEL_1_TIRISFAL_UNDEAD,
            },

            TrainerConfigs = new List<string>
            {
                "Frang (Valley of Trials, Durotar)",
                "Harutt Thunderhorn (Camp Narache, Mulgore)",
                "Dannal Stern (Deathknell, Tirisfal Glades)",
            },
        };

        public static readonly WowLevelingConfiguration LEVEL_2 = new WowLevelingConfiguration
        {
            Level = 2,

            LocationConfigs = new List<WowLocationConfiguration>
            {
                WowLocationConfigs.LEVEL_1_DUROTAR_BOARS_AND_SCORPS,
                WowLocationConfigs.LEVEL_1_MULGORE_PLAINSTRIDERS,
                WowLocationConfigs.LEVEL_1_TIRISFAL_UNDEAD,
            },

            TrainerConfigs = new List<string>
            {
                "Frang (Valley of Trials, Durotar)",
                "Harutt Thunderhorn (Camp Narache, Mulgore)",
                "Dannal Stern (Deathknell, Tirisfal Glades)",
            },
        };

        public static readonly List<WowLevelingConfiguration> ALL_LEVELING_CONFIGS = new List<WowLevelingConfiguration>
        {
            LEVEL_1,
            LEVEL_2,
        };

        // A config specific to this class wins over an any-class (Class == null) one at the
        // same level. Null if neither exists yet.
        public static WowLevelingConfiguration GetFor(WowCombatConfiguration playerClass, int level)
        {
            return ALL_LEVELING_CONFIGS.FirstOrDefault(c => c.Level == level && c.Class == playerClass)
                ?? ALL_LEVELING_CONFIGS.FirstOrDefault(c => c.Level == level && c.Class == null);
        }
    }
}
