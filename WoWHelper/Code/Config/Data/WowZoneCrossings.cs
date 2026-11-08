using System.Collections.Generic;
using System.Linq;

namespace WoWHelper.Code.WorldState
{
    // Every link between zones, each defined once and walkable both ways. Each crossing's points
    // come from the routes they sit on, so there's only one place to edit them.
    public static class WowZoneCrossings
    {
        public static readonly WowZoneCrossing DUROTAR_TO_ORGRIMMAR = new WowZoneCrossing
        {
            Name = "Durotar To Orgrimmar",
            FromZone = WowZone.Durotar,
            FromPoint = WowRoutes.RAZOR_HILL_TO_ORG_ENTRANCE.Waypoints.Last(),
            ToZone = WowZone.Orgrimmar,
            ToPoint = WowRoutes.ORGRIMMAR_ENTRANCE_ROUTE.Waypoints.First(),
            HeadingDegrees = 7.35f
        };

        private static readonly List<WowZoneCrossing> DEFINED_ZONE_CROSSINGS = new List<WowZoneCrossing>
        {
            DUROTAR_TO_ORGRIMMAR,
        };

        // Both directions of every link, searched by WowPathfinding.GetZoneCrossingsBetween.
        public static readonly List<WowZoneCrossing> ALL_ZONE_CROSSINGS = DEFINED_ZONE_CROSSINGS
            .SelectMany(crossing => new[] { crossing, crossing.Reversed() })
            .ToList();
    }
}
