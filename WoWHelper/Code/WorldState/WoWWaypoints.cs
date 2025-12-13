using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace WoWHelper.Code.WorldState
{
    public static class WoWWaypoints
    {
        public static readonly WoWWaypointDefinition LEVEL_17_NORTHERN_BARRENS_WAYPOINTS = new WoWWaypointDefinition
        {
            Waypoints = new List<Vector2>
            {
                new Vector2(47.30f, 13.91f),
                new Vector2(46.59f, 14.27f),
                new Vector2(45.76f, 14.68f),
                new Vector2(44.00f, 14.78f),
                new Vector2(45.13f, 15.02f),
                new Vector2(45.15f, 17.36f),
                new Vector2(44.20f, 18.71f),
                new Vector2(43.27f, 20.00f),
                new Vector2(42.24f, 20.87f)
            },
            TraversalMethod = WoWWaypointDefinition.WaypointTraversalMethod.LINEAR,
            TargetFindMethod = WoWWaypointDefinition.WaypointTargetFindMethod.TAB
        };

        public static readonly WoWWaypointDefinition LEVEL_13_BARRENS_ENTRACE_WAYPOINTS = new WoWWaypointDefinition
        {
            Waypoints = new List<Vector2>
            {
                new Vector2(55.00f, 21.13f),
                new Vector2(60.42f, 20.46f),
                new Vector2(61.11f, 22.33f),
                new Vector2(59.88f, 21.84f),
                new Vector2(56.29f, 22.47f),
                new Vector2(56.60f, 22.00f)
            },
            TraversalMethod = WoWWaypointDefinition.WaypointTraversalMethod.CIRCULAR,
            TargetFindMethod = WoWWaypointDefinition.WaypointTargetFindMethod.MACRO
        };

        public static readonly WoWWaypointDefinition LEVEL_11_DUROTAR_COAST_WAYPOINTS = new WoWWaypointDefinition { 
            Waypoints = new List<Vector2>
            { 
                //new Vector2(38.13f, 16.10f),
                new Vector2(37.52f, 22.80f),
                new Vector2(37.07f, 27.55f),
                new Vector2(36.30f, 31.59f),
                new Vector2(36.03f, 33.09f),
                new Vector2(36.19f, 35.34f),
                new Vector2(36.28f, 39.20f),
                new Vector2(36.57f, 43.56f),
                new Vector2(36.60f, 47.55f),
                new Vector2(36.65f, 51.08f),
                new Vector2(36.30f, 53.50f)
            },
            TraversalMethod = WoWWaypointDefinition.WaypointTraversalMethod.LINEAR,
            TargetFindMethod = WoWWaypointDefinition.WaypointTargetFindMethod.TAB
        };
    }
}
