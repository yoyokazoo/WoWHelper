using WoWHelper.Code.Config;
using WoWHelper.Code.Gameplay;
using WoWHelper.Code.WorldState;

namespace WoWHelper.Code.Constants
{
    public class WowFarmingConfigs
    {
        public static readonly WowFarmingConfiguration CURRENT_CONFIG = new WowFarmingConfiguration
        {
            LocationConfiguration = WowLocationConfigs.LEVEL_24_STONETALON_WAYPOINTS,
            ManagementConfiguration = WowManagementConfigs.SLEEPING_FOR_EXP,
            CombatConfiguration = WowCombatConfiguration.Shaman
        };
    }
}
