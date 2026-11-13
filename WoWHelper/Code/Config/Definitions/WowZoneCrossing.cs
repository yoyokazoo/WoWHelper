using System.Numerics;

namespace WoWHelper.Code.WorldState
{
    // One link between two zones. Map coordinates are per-zone, so routes in different zones can
    // never share a waypoint -- this is what connects them instead. Each instance is one
    // direction of travel; WowZoneCrossings.cs defines each link once, and Reversed() makes the
    // way back.
    public class WowZoneCrossing
    {
        public string Name { get; set; }

        // Walk to FromPoint, which must be a waypoint on a route in FromZone...
        public WowZone FromZone { get; set; }
        public Vector2 FromPoint { get; set; }

        // ...then hold HeadingDegrees until WorldState.CurrentZone reads ToZone. ToPoint must be
        // a waypoint on a route in ToZone, close enough to where the player comes out that
        // WowPathfinding can pick up from there (MAX_DISTANCE_FROM_ROUTE_WAYPOINT).
        public WowZone ToZone { get; set; }
        public Vector2 ToPoint { get; set; }

        // Same convention as WorldState.FacingDegrees. Facing is world-relative, so unlike
        // map coordinates it means the same thing on both sides of the border.
        public float HeadingDegrees { get; set; }

        // The heading for the way back. Null means straight back (HeadingDegrees + 180), so this
        // is only needed where the path bends at the border.
        public float? ReturnHeadingDegrees { get; set; }

        public int MinimumLevel { get; set; }

        // Which faction can safely take this crossing (both directions). EITHER matches both.
        public WowFaction Faction { get; set; } = WowFaction.EITHER;

        public WowZoneCrossing Reversed()
        {
            return new WowZoneCrossing
            {
                Name = $"{Name} (Reversed)",
                FromZone = ToZone,
                FromPoint = ToPoint,
                ToZone = FromZone,
                ToPoint = FromPoint,
                HeadingDegrees = ReturnHeadingDegrees ?? (HeadingDegrees + 180f) % 360f,
                ReturnHeadingDegrees = HeadingDegrees,
                MinimumLevel = MinimumLevel,
                Faction = Faction,
            };
        }

        public override string ToString()
        {
            return $"WowZoneCrossing ({(string.IsNullOrEmpty(Name) ? "NO NAME" : Name)})";
        }
    }
}
