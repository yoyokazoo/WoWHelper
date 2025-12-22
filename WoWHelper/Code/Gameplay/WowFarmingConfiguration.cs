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
        public bool LogoutOnLowDynamite { get; set; }

        public WowFarmingConfiguration()
        {
            EngageMethod = EngagementMethod.Shoot;
            AlertOnPotionUsed = true;
            AlertOnFullBags = true;
            LogoutOnLowDynamite = true;
            Spec = WarriorSpec.Fury;
        }
    }
}
