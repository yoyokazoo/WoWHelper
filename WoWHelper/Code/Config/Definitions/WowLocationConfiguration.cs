using System.Collections.Generic;
using System.Numerics;

namespace WoWHelper.Code.WorldState
{
    public class WowLocationConfiguration
    {
        public enum WaypointTraversalMethod
        {
            CIRCULAR, // go from start -> end, then restart at start
            LINEAR // go from start -> end -> start
        }

        public enum WaypointTargetFindMethod
        {
            TAB, // only use tab, only gets a narrow cone in front
            MACRO, // only use macro, often gets stuck re-picking a bad target
            ALTERNATE // alternate between the two
        }

        public enum EngagementMethod
        {
            Charge,
            Shoot,
            Frostbolt
        }

        public List<Vector2> Waypoints { get; set; }
        public WaypointTraversalMethod TraversalMethod { get; set; }
        public WaypointTargetFindMethod TargetFindMethod { get; set; }
        public float DistanceTolerance { get; set; } = 0.5f;

        public EngagementMethod EngageMethod { get; set; }
        public bool UseRend { get; set; } // some mobs are immune to bleed
        public bool PreemptFear { get; set; } // if fighting mobs that Fear, start each fight with Berserker Rage
        public int TooManyAttackersThreshold { get; set; } // how many mobs to panic at (sometimes mobs spawn tiny bugs or something that will get counted)
        public int LogoffLevel { get; set; } // Level to log off at (mostly for low level areas, or if we're going to be learning a spell that the bot will expect to know)

        public WowLocationConfiguration()
        {
            LogoffLevel = 61;
        }
    }
}
