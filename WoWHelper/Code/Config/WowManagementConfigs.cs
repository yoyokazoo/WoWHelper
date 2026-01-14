using WoWHelper.Code.Config.Definitions;

namespace WoWHelper.Code.Config
{
    public class WowManagementConfigs
    {
        public static readonly WowManagementConfiguration FULL_BABYSIT = new WowManagementConfiguration
        {
            AlertOnPotionUsed = true,
            AlertOnFullBags = true,
            AlertOnUnreadWhisper = true,
            LogoutOnLowDynamite = false,
            LogoutOnFullBags = false,
        };

        public static readonly WowManagementConfiguration SLEEPING_FOR_LOOT = new WowManagementConfiguration
        {
            AlertOnPotionUsed = false,
            AlertOnFullBags = false,
            AlertOnUnreadWhisper = false,
            LogoutOnLowDynamite = false,
            LogoutOnFullBags = true,
        };

        public static readonly WowManagementConfiguration SLEEPING_FOR_EXP = new WowManagementConfiguration
        {
            AlertOnPotionUsed = false,
            AlertOnFullBags = false,
            AlertOnUnreadWhisper = false,
            LogoutOnLowDynamite = false,
            LogoutOnFullBags = false,
        };
    }
}
