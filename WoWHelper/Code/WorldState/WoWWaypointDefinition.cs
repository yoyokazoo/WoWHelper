using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace WoWHelper.Code.WorldState
{
    public class WoWWaypointDefinition
    {
        public enum WaypointTraversalMethod
        {
            CIRCULAR, // go from start -> end, then restart at start
            LINEAR // go from start -> end -> start
        }

        public enum WaypointTargetFindMethod
        {
            TAB,
            MACRO
        }

        public List<Vector2> Waypoints { get; set; }
        public WaypointTraversalMethod TraversalMethod { get; set; }
        public WaypointTargetFindMethod TargetFindMethod { get; set; }
        public float DistanceTolerance { get; set; } = 0.5f;
    }
}
