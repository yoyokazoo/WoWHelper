using WoWHelper.Code.Gameplay;
using WoWHelper.Code.WorldState;

namespace WoWHelper.Code.Constants
{
    public class WowFarmingConfigs
    {
        public static readonly WowFarmingConfiguration CURRENT_CONFIG = new WowFarmingConfiguration
        {
            WaypointDefinition = WowWaypointConfigs.LEVEL_51_FELWOOD_SOUTH,
            EngageMethod = WowFarmingConfiguration.EngagementMethod.Charge,
            AlertOnPotionUsed = false,
            AlertOnFullBags = false,
            LogoutOnLowDynamite = false,
        };
    }
}
