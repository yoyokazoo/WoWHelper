using WoWHelper.Code.Gameplay;
using WoWHelper.Code.WorldState;

namespace WoWHelper.Code.Constants
{
    public class WowFarmingConfigConstants
    {
        public static readonly WowFarmingConfiguration LEVEL_42_TANARIS_TURTLES = new WowFarmingConfiguration
        {
            WaypointDefinition = WowWaypointConstants.TANARIS_TEST_PATHFINDING,
            EngageMethod = WowFarmingConfiguration.EngagementMethod.Charge,
            AlertOnPotionUsed = false,
            AlertOnFullBags = true
        };
    }
}
