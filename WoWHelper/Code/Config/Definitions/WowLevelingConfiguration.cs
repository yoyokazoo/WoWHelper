using System.Collections.Generic;
using WoWHelper.Code.Gameplay;

namespace WoWHelper.Code.WorldState
{
    public class WowLevelingConfiguration
    {
        // Null = any class
        public WowCombatConfiguration? Class { get; set; }
        public List<int> Levels { get; set; }

        public List<WowFarmingConfiguration> FarmingConfigs { get; set; }

        public WowLevelingConfiguration()
        {
            Class = null;
            Levels = new List<int>();
            FarmingConfigs = new List<WowFarmingConfiguration>();
        }
    }
}
