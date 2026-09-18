using WoWHelper.Code.Config;
using WoWHelper.Code.Gameplay;
using WoWHelper.Code.WorldState;

namespace WoWHelper.Code.Constants
{
    public class WowFarmingConfigs
    {
        // LocationConfiguration and CombatConfiguration are deliberately NOT set here --
        // WowPlayer.ResolveFarmingConfigurationTask picks both at startup from live game
        // state (player class, level, zone, and position) instead of them being hardcoded.
        // See WowFarmingConfiguration's constructor for their pre-resolution defaults.
        public static readonly WowFarmingConfiguration CURRENT_CONFIG = new WowFarmingConfiguration
        {
            ManagementConfiguration = WowManagementConfigs.FULL_BABYSIT,
        };
    }
}
