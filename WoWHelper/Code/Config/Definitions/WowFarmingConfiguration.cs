using WoWHelper.Code.Config.Definitions;
using WoWHelper.Code.WorldState;
using static WoWHelper.Code.WorldState.WowLocationConfiguration;

namespace WoWHelper.Code.Gameplay
{
    public enum WowCombatConfiguration
    {
        Warrior = 0,
        Mage = 1,
    }

    public class WowFarmingConfiguration
    {
        public WowLocationConfiguration LocationConfiguration { get; set; }
        public WowManagementConfiguration ManagementConfiguration { get; set; }
        public WowCombatConfiguration CombatConfiguration { get; set; }

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
            CombatConfiguration = WowCombatConfiguration.Mage;
        }
    }
}
