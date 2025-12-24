using WoWHelper.Code.Gameplay;
using WoWHelper.Code.WorldState;

namespace WoWHelper.Code.Constants
{
    public class WowFarmingConfigs
    {
        public static readonly WowFarmingConfiguration CURRENT_CONFIG = new WowFarmingConfiguration
        {
            WaypointDefinition = WowWaypointConfigs.LEVEL_55_WINTERSPRING_LAKE,
            EngageMethod = WowFarmingConfiguration.EngagementMethod.Charge,
            AlertOnPotionUsed = true,
            AlertOnFullBags = true,
            LogoutOnLowDynamite = false,
            UseRend = false,
        };
    }
}
