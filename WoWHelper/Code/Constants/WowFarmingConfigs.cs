using WoWHelper.Code.Gameplay;
using WoWHelper.Code.WorldState;

namespace WoWHelper.Code.Constants
{
    public class WowFarmingConfigs
    {
        public static readonly WowFarmingConfiguration CURRENT_CONFIG = new WowFarmingConfiguration
        {
            WaypointDefinition = WowWaypointConfigs.LEVEL_56_DALTONS_TEARS_BACKSIDE_WPL,
            EngageMethod = WowFarmingConfiguration.EngagementMethod.Shoot,
            AlertOnPotionUsed = true,
            AlertOnFullBags = true,
            AlertOnUnreadWhisper = true,
            LogoutOnLowDynamite = false,
            UseRend = false,
            TooManyAttackersThreshold = 4
        };

        // AOE Farmed, worms
        public static readonly WowFarmingConfiguration LEVEL_56_DALSONS_TEAR_FRONTSIDE = new WowFarmingConfiguration
        {
            WaypointDefinition = WowWaypointConfigs.LEVEL_56_DALTONS_TEARS_FRONTSIDE_WPL,
            EngageMethod = WowFarmingConfiguration.EngagementMethod.Shoot,
            AlertOnPotionUsed = true,
            AlertOnFullBags = true,
            AlertOnUnreadWhisper = true,
            LogoutOnLowDynamite = false,
            UseRend = true,
            TooManyAttackersThreshold = 4
        };

        // Skeletons that fear and are immune to rend, worms.  Other Dalson's Tear spot is preferred but AOE farmed
        public static readonly WowFarmingConfiguration LEVEL_56_DALSONS_TEAR_BACKSIDE = new WowFarmingConfiguration
        {
            WaypointDefinition = WowWaypointConfigs.LEVEL_56_DALTONS_TEARS_BACKSIDE_WPL,
            EngageMethod = WowFarmingConfiguration.EngagementMethod.Shoot,
            AlertOnPotionUsed = true,
            AlertOnFullBags = true,
            AlertOnUnreadWhisper = true,
            LogoutOnLowDynamite = false,
            UseRend = false,
            TooManyAttackersThreshold = 4
        };
    }
}
