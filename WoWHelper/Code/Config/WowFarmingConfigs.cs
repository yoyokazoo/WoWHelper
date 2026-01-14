using WoWHelper.Code.Config;
using WoWHelper.Code.Gameplay;
using WoWHelper.Code.WorldState;

namespace WoWHelper.Code.Constants
{
    public class WowFarmingConfigs
    {
        public static readonly WowFarmingConfiguration CURRENT_CONFIG = new WowFarmingConfiguration
        {
            LocationConfiguration = WowLocationConfigs.LEVEL_4_MULGORE_MOUNTAIN_COUGARS,
            ManagementConfiguration = WowManagementConfigs.FULL_BABYSIT,
            CombatConfiguration = WowCombatConfiguration.Shaman
        };
    }
}
