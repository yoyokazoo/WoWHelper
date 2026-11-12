using System.Collections.Generic;
using System.Numerics;

namespace WoWHelper.Code.WorldState
{
    public class WowRoute
    {
        public const float DEFAULT_DISTANCE_TOLERANCE = 0.2f;

        public enum WaypointTraversalMethod
        {
            CIRCULAR, // go from start -> end, then restart at start
            LINEAR // go from start -> end -> start
        }

        public string Name { get; set; }
        public WowZone Zone { get; set; }
        public int MinimumLevel { get; set; }
        public WowFaction Faction { get; set; }
        public WaypointTraversalMethod TraversalMethod { get; set; }
        public float DistanceTolerance { get; set; }

        public List<Vector2> Waypoints { get; set; }

        public WowZoneCrossing ZoneCrossingAtEnd { get; set; }

        public WowRoute()
        {
            TraversalMethod = WaypointTraversalMethod.LINEAR;
            DistanceTolerance = DEFAULT_DISTANCE_TOLERANCE;
            Zone = WowZone.Unknown;
            Faction = WowFaction.EITHER;
        }

        public override string ToString()
        {
            return $"WowRoute ({(string.IsNullOrEmpty(Name) ? "NO NAME" : Name)})";
        }
    }
}
