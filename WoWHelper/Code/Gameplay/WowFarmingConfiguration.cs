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

        public WowWaypointDefinition WaypointDefinition { get; set; }
        public EngagementMethod EngageMethod { get; set; }
        public bool AlertOnPotionUsed { get; set; }

        public WowFarmingConfiguration()
        {
            EngageMethod = EngagementMethod.Shoot;
            AlertOnPotionUsed = true;
        }
    }
}
