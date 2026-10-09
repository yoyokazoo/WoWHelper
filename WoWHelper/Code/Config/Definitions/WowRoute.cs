using System.Collections.Generic;
using System.Numerics;

namespace WoWHelper.Code.WorldState
{
    // A farming route's path: the waypoints to walk, how to walk them, how close counts as
    // "arrived", which zone they're in, and the minimum level to run it. Every instance lives
    // in WowRoutes.cs; each WowLocationConfiguration points at its own one via its Route property.
    public class WowRoute
    {
        public enum WaypointTraversalMethod
        {
            CIRCULAR, // go from start -> end, then restart at start
            LINEAR // go from start -> end -> start
        }

        public WowZone Zone { get; set; } // for validation -- character should be in this zone before starting
        public int MinimumLevel { get; set; } // for validation -- character should be at least this level before starting
        public WaypointTraversalMethod TraversalMethod { get; set; }
        public float DistanceTolerance { get; set; }

        public List<Vector2> Waypoints { get; set; }

        public WowRoute()
        {
            TraversalMethod = WaypointTraversalMethod.CIRCULAR;
            DistanceTolerance = 0.2f;

            // Default to Unknown, not the implicit Durotar (enum value 0) -- a route that
            // forgets to set Zone should fail loudly/obviously, not silently claim Durotar.
            Zone = WowZone.Unknown;
        }
    }
}
