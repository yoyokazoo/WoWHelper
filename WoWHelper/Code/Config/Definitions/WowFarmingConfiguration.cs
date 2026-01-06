using WoWHelper.Code.Config.Definitions;
using WoWHelper.Code.WorldState;

namespace WoWHelper.Code.Gameplay
{
    public class WowFarmingConfiguration
    {
        public enum EngagementMethod
        {
            Charge,
            Shoot
        }

        public enum WarriorSpec
        {
            Arms,
            Fury
        }

        public WowLocationConfiguration LocationConfiguration { get; set; }
        public WowManagementConfiguration ManagementConfiguration { get; set; }

        public EngagementMethod EngageMethod { get; set; }

        public bool AlertOnPotionUsed => ManagementConfiguration.AlertOnPotionUsed;
        public bool AlertOnFullBags => ManagementConfiguration.AlertOnFullBags;
        public bool AlertOnUnreadWhisper => ManagementConfiguration.AlertOnUnreadWhisper;
        public bool LogoutOnFullBags => ManagementConfiguration.LogoutOnFullBags;
        public bool LogoutOnLowDynamite => ManagementConfiguration.LogoutOnLowDynamite;


        
        public bool UseRend { get; set; } // some mobs are immune to bleed
        public bool PreemptFear { get; set; } // if fighting mobs that Fear, start each fight with Berserker Rage
        public int TooManyAttackersThreshold { get; set; } // how many mobs to panic at (sometimes mobs spawn tiny bugs or something that will get counted)

        public WowFarmingConfiguration()
        {
            EngageMethod = EngagementMethod.Shoot;
            UseRend = true;
            TooManyAttackersThreshold = 3;
            PreemptFear = true;
        }
    }
}
