using System.Collections.Generic;
using System.Numerics;

namespace WoWHelper.Code.WorldState
{
    public class WowWaypointConfiguration
    {
        public enum WaypointTraversalMethod
        {
            CIRCULAR, // go from start -> end, then restart at start
            LINEAR // go from start -> end -> start
        }

        public enum WaypointTargetFindMethod
        {
            TAB,
            MACRO,
            ALTERNATE
        }

        public List<Vector2> Waypoints { get; set; }
        public WaypointTraversalMethod TraversalMethod { get; set; }
        public WaypointTargetFindMethod TargetFindMethod { get; set; }
        public float DistanceTolerance { get; set; } = 0.5f;
    }
}
