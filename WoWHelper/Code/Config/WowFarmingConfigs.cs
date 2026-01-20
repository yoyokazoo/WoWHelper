using WoWHelper.Code.Config;
using WoWHelper.Code.Gameplay;
using WoWHelper.Code.WorldState;

namespace WoWHelper.Code.Constants
{
    public class WowFarmingConfigs
    {
        public static readonly WowFarmingConfiguration CURRENT_CONFIG = new WowFarmingConfiguration
        {
            LocationConfiguration = WowLocationConfigs.LEVEL_9_DUROTAR_SKULL_ROCK_COAST_WAYPOINTS,
            ManagementConfiguration = WowManagementConfigs.LOOT_BABYSIT,
            CombatConfiguration = WowCombatConfiguration.Shaman
        };
    }
}
