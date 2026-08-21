using System.Windows.Forms;
using WoWHelper.Code.Config;
using WoWHelper.Code.Config.Definitions;
using WoWHelper.Code.WorldState;
using static WoWHelper.Code.WorldState.WowLocationConfiguration;

namespace WoWHelper.Code.Gameplay
{
    public enum WowCombatConfiguration
    {
        // Not yet resolved -- see WowPlayer.ResolveFarmingConfigurationTask, which sets
        // this from the player's class (WowWorldState.PlayerClass) at startup instead of
        // it being hardcoded. Default so an un-resolved config fails loudly (every
        // Wow*CombatConfig.cs dispatcher's switch has a "default: throw" case) rather than
        // silently behaving like Warrior.
        Unknown = -1,
        Warrior = 0,
        Mage = 1,
        Shaman = 2,
    }

    public class WowFarmingConfiguration
    {
        // Both resolved at startup from live game state -- see
        // WowPlayer.ResolveFarmingConfigurationTask -- rather than being hardcoded here.
        public WowLocationConfiguration LocationConfiguration { get; set; }
        public WowCombatConfiguration CombatConfiguration { get; set; }

        public WowManagementConfiguration ManagementConfiguration { get; set; }
        public WowScreenConfiguration ScreenConfiguration { get; set; }

        public bool AlertOnPotionUsed => ManagementConfiguration.AlertOnPotionUsed;
        public bool AlertOnFullBags => ManagementConfiguration.AlertOnFullBags;
        public bool AlertOnUnreadWhisper => ManagementConfiguration.AlertOnUnreadWhisper;
        public bool LogoutOnFullBags => ManagementConfiguration.LogoutOnFullBags;
        public bool LogoutOnLowDynamite => ManagementConfiguration.LogoutOnLowDynamite;

        public EngagementMethod EngageMethod => LocationConfiguration.EngageMethod;
        public bool UseRend => LocationConfiguration.UseRend;
        public bool PreemptFear => LocationConfiguration.PreemptFear;
        public int TooManyAttackersThreshold => LocationConfiguration.TooManyAttackersThreshold;
        public int LogoffLevel => LocationConfiguration.LogoffLevel;

        public WowFarmingConfiguration()
        {
            // LocationConfiguration stays null and CombatConfiguration stays Unknown until
            // ResolveFarmingConfigurationTask sets them from live game state.
            CombatConfiguration = WowCombatConfiguration.Unknown;

            int width = Screen.PrimaryScreen.Bounds.Width;
            int height = Screen.PrimaryScreen.Bounds.Height;

            if (width == 1920 && height == 1080)
            {
                ScreenConfiguration = WowScreenConfigs.RESOLUTION_1920_X_1080;
            }
            else if (width == 3440 && height == 1440)
            {
                ScreenConfiguration = WowScreenConfigs.RESOLUTION_3440_X_1440;
            }
            else if (width == 2560 && height == 1600)
            {
                ScreenConfiguration = WowScreenConfigs.RESOLUTION_2560_X_1600;
            }
            else
            {
                throw new System.Exception($"No screen config for resolution {width}x{height}!");
            }
        }
    }
}
