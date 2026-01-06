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

        public List<Vector2> Waypoints { get; set; }
        public WaypointTraversalMethod TraversalMethod { get; set; }
        public WaypointTargetFindMethod TargetFindMethod { get; set; }
        public float DistanceTolerance { get; set; } = 0.5f;
    }
}
