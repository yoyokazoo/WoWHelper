using WoWHelper.Code.Gameplay;
using WoWHelper.Code.WorldState;

namespace WoWHelper.Code.Constants
{
    public class WowFarmingConfigs
    {
        public static readonly WowFarmingConfiguration LEVEL_42_TANARIS_TURTLES = new WowFarmingConfiguration
        {
            WaypointDefinition = WowWaypointConfigs.LEVEL_42_TANARIS_TURTLES,
            EngageMethod = WowFarmingConfiguration.EngagementMethod.Charge,
            AlertOnPotionUsed = true,
            AlertOnFullBags = true,
            LogoutOnLowDynamite = false,
        };
    }
}
