using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public WowWaypointConfiguration WaypointDefinition { get; set; }
        public EngagementMethod EngageMethod { get; set; }
        public WarriorSpec Spec { get; set; }
        public bool AlertOnPotionUsed { get; set; }
        public bool AlertOnFullBags { get; set; }
        public bool AlertOnUnreadWhisper { get; set; }
        public bool LogoutOnLowDynamite { get; set; }
        public bool UseRend { get; set; } // some mobs are immune to bleed
        public int TooManyAttackersThreshold { get; set; }
        public bool PreemptFear { get; set; } // if fighting mobs that Fear, start each fight with Berserker Rage

        public WowFarmingConfiguration()
        {
            EngageMethod = EngagementMethod.Shoot;
            AlertOnPotionUsed = true;
            AlertOnFullBags = true;
            AlertOnUnreadWhisper = true;
            LogoutOnLowDynamite = true;
            UseRend = true;
            TooManyAttackersThreshold = 3;
            PreemptFear = true;
            Spec = WarriorSpec.Fury;
        }
    }
}
