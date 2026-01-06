using WoWHelper.Code.Config;
using WoWHelper.Code.Gameplay;
using WoWHelper.Code.WorldState;

namespace WoWHelper.Code.Constants
{
    public class WowFarmingConfigs
    {
        // TODO: split into zone specific properties and "am i home/asleep" type properties
        public static readonly WowFarmingConfiguration CURRENT_CONFIG = new WowFarmingConfiguration
        {
            LocationConfiguration = WowLocationConfigs.LEVEL_58_SILITHUS_RUMBLERS,
            ManagementConfiguration = WowManagementConfigs.SLEEPING_FOR_LOOT,
        };

        // Essence of Earth
        // /target desert rumbler
        public static readonly WowFarmingConfiguration LEVEL_58_SILITHUS_RUMBLERS = new WowFarmingConfiguration
        {
            LocationConfiguration = WowLocationConfigs.LEVEL_58_SILITHUS_RUMBLERS,
            ManagementConfiguration = WowManagementConfigs.SLEEPING_FOR_LOOT,
        };

        // AOE Farmed, worms
        public static readonly WowFarmingConfiguration LEVEL_56_DALSONS_TEAR_FRONTSIDE = new WowFarmingConfiguration
        {
            LocationConfiguration = WowLocationConfigs.LEVEL_56_DALTONS_TEARS_FRONTSIDE_WPL,
            ManagementConfiguration = WowManagementConfigs.SLEEPING_FOR_LOOT,
        };

        // Skeletons that fear and are immune to rend, worms.  Other Dalson's Tear spot is preferred but AOE farmed
        // Issues with targeting mobs in buildings (not in line of sight)
        public static readonly WowFarmingConfiguration LEVEL_56_DALSONS_TEAR_BACKSIDE = new WowFarmingConfiguration
        {
            LocationConfiguration = WowLocationConfigs.LEVEL_56_DALTONS_TEARS_BACKSIDE_WPL,
            ManagementConfiguration = WowManagementConfigs.SLEEPING_FOR_LOOT,
        };
    }
}
