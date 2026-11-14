using System;
using System.Collections.Generic;
using System.Linq;

namespace WoWHelper.Code.WorldState
{
    public static class WowZoneCrossings
    {
        public static readonly WowZoneCrossing DUROTAR_TO_ORGRIMMAR = new WowZoneCrossing
        {
            Name = "Durotar To Orgrimmar",
            FromZone = WowZone.Durotar,
            FromPoint = WowWalkingRoutes.RAZOR_HILL_TO_ORG_ENTRANCE.Waypoints.Last(),
            ToZone = WowZone.Orgrimmar,
            ToPoint = WowWalkingRoutes.ORGRIMMAR_ENTRANCE_ROUTE.Waypoints.First(),
            HeadingDegrees = 7.35f,
            Faction = WowFaction.HORDE,
        };

        // Both directions of every crossing above (found via reflection), searched by
        // WowPathfinding.GetZoneCrossingsBetween.
        private static readonly Lazy<IReadOnlyList<WowZoneCrossing>> ALL_ZONE_CROSSINGS_LAZY = new Lazy<IReadOnlyList<WowZoneCrossing>>(() =>
            StaticConfigCollector.Collect<WowZoneCrossing>(typeof(WowZoneCrossings))
                .SelectMany(crossing => new[] { crossing, crossing.Reversed() })
                .ToList()
                .AsReadOnly());
        public static IReadOnlyList<WowZoneCrossing> ALL_ZONE_CROSSINGS => ALL_ZONE_CROSSINGS_LAZY.Value;
    }
}
