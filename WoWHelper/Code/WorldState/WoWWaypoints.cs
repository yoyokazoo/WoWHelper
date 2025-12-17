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
        public static readonly WoWWaypointDefinition LEVEL_34_SHIMMERING_FLATS_WAYPOINTS_ALTERNATE = new WoWWaypointDefinition
        {
            Waypoints = new List<Vector2>
            {
                new Vector2(78.16f, 52.21f),
                new Vector2(79.70f, 52.30f),
                new Vector2(81.79f, 52.12f),
                new Vector2(82.08f, 53.51f),
                new Vector2(81.60f, 55.03f),
                new Vector2(82.47f, 56.14f),
                new Vector2(83.00f, 54.67f),
                new Vector2(82.92f, 54.00f),
                new Vector2(84.13f, 56.00f),
                //new Vector2(86.87f, 58.05f),
                //new Vector2(87.96f, 61.20f),
            },
            TraversalMethod = WoWWaypointDefinition.WaypointTraversalMethod.LINEAR,
            TargetFindMethod = WoWWaypointDefinition.WaypointTargetFindMethod.MACRO,
            DistanceTolerance = 0.2f
        };

        /*
        public static readonly WoWWaypointDefinition LEVEL_34_SHIMMERING_FLATS_WAYPOINTS_ALTERNATE = new WoWWaypointDefinition
        {
            Waypoints = new List<Vector2>
            {
                new Vector2(78.16f, 52.21f),
                new Vector2(79.70f, 52.30f),
                new Vector2(81.53f, 53.34f),
                new Vector2(82.13f, 56.16f),
                new Vector2(82.92f, 54.00f),
                new Vector2(84.13f, 56.00f),
                new Vector2(85.53f, 57.50f),
                new Vector2(86.46f, 59.50f),
            },
            TraversalMethod = WoWWaypointDefinition.WaypointTraversalMethod.LINEAR,
            TargetFindMethod = WoWWaypointDefinition.WaypointTargetFindMethod.MACRO,
            DistanceTolerance = 0.2f
        };
        */

        public static readonly WoWWaypointDefinition LEVEL_34_SHIMMERING_FLATS_WAYPOINTS = new WoWWaypointDefinition
        {
            Waypoints = new List<Vector2>
            {
                new Vector2(81.79f, 52.12f),
                new Vector2(82.08f, 53.51f),
                new Vector2(81.60f, 55.03f),
                new Vector2(82.47f, 56.14f),
                new Vector2(83.00f, 54.67f),
                new Vector2(84.03f, 54.25f),
                new Vector2(85.73f, 56.59f),
                new Vector2(86.87f, 58.05f),
                new Vector2(87.96f, 61.20f),
            },
            TraversalMethod = WoWWaypointDefinition.WaypointTraversalMethod.LINEAR,
            TargetFindMethod = WoWWaypointDefinition.WaypointTargetFindMethod.MACRO,
            DistanceTolerance = 0.2f
        };

        // Don't recommend this one without WBs
        public static readonly WoWWaypointDefinition LEVEL_27_NORTH_ASHENVALE_WAYPOINTS = new WoWWaypointDefinition
        {
            Waypoints = new List<Vector2>
            {
                new Vector2(60.43f, 39.81f),
                new Vector2(60.05f, 38.56f),
                new Vector2(58.47f, 37.08f),
                new Vector2(58.70f, 35.47f),
                new Vector2(57.98f, 34.11f),
                new Vector2(57.50f, 31.07f),
                new Vector2(56.00f, 31.00f),
                new Vector2(55.79f, 32.64f),
                new Vector2(54.88f, 32.73f),
                new Vector2(55.34f, 34.93f),
                new Vector2(56.54f, 36.72f),
                new Vector2(57.42f, 37.99f),
                new Vector2(57.30f, 40.60f),
                new Vector2(56.36f, 41.26f),
                new Vector2(57.53f, 41.97f),
                new Vector2(58.87f, 41.32f),
                new Vector2(59.55f, 39.57f),
            },
            TraversalMethod = WoWWaypointDefinition.WaypointTraversalMethod.CIRCULAR,
            TargetFindMethod = WoWWaypointDefinition.WaypointTargetFindMethod.TAB,
            DistanceTolerance = 0.2f
        };

        public static readonly WoWWaypointDefinition LEVEL_29_HILLSBRAD_RIVER_WAYPOINTS = new WoWWaypointDefinition
        {
            Waypoints = new List<Vector2>
            {
                new Vector2(70.12f, 11.00f),
                new Vector2(68.84f, 13.74f),
                new Vector2(67.82f, 17.99f),
                new Vector2(67.78f, 21.78f),
                new Vector2(67.97f, 25.64f),
                new Vector2(67.35f, 30.65f),
                new Vector2(67.29f, 35.17f),
                new Vector2(65.68f, 38.19f),
                new Vector2(64.12f, 40.35f),
                // under bridge, get stuck too often to be worth it
                //new Vector2(62.49f, 42.11f),
                //new Vector2(61.55f, 42.71f),
            },
            TraversalMethod = WoWWaypointDefinition.WaypointTraversalMethod.LINEAR,
            TargetFindMethod = WoWWaypointDefinition.WaypointTargetFindMethod.MACRO,
            DistanceTolerance = 0.3f
        };

        public static readonly WoWWaypointDefinition LEVEL_24_STONETALON_WAYPOINTS = new WoWWaypointDefinition
        {
            Waypoints = new List<Vector2>
            {
                new Vector2(44.43f, 19.01f),
                new Vector2(45.35f, 20.96f),
                new Vector2(44.96f, 23.10f),
                new Vector2(45.68f, 24.14f),
                new Vector2(46.43f, 26.20f),
                new Vector2(46.76f, 28.85f),
                new Vector2(46.75f, 31.71f),
                new Vector2(46.68f, 28.04f),
                new Vector2(46.21f, 26.70f),
                new Vector2(45.30f, 26.44f),
                new Vector2(44.42f, 25.19f),
                new Vector2(44.19f, 22.95f),
                new Vector2(44.71f, 20.78f),
            },
            TraversalMethod = WoWWaypointDefinition.WaypointTraversalMethod.CIRCULAR,
            TargetFindMethod = WoWWaypointDefinition.WaypointTargetFindMethod.ALTERNATE,
            DistanceTolerance = 0.2f
        };

        public static readonly WoWWaypointDefinition LEVEL_21_ZORAMGAR_WAYPOINTS = new WoWWaypointDefinition
        {
            Waypoints = new List<Vector2>
            {
                /*
                new Vector2(16.63f, 28.01f),
                new Vector2(16.51f, 30.17f),
                new Vector2(17.81f, 30.54f),
                new Vector2(18.59f, 32.78f),
                new Vector2(19.16f, 33.77f),
                new Vector2(20.00f, 36.17f),
                new Vector2(21.18f, 38.39f),
                new Vector2(22.32f, 38.82f),
                */
                new Vector2(16.67f, 28.15f),
                new Vector2(16.07f, 29.94f),
                new Vector2(17.30f, 30.17f),
                new Vector2(18.18f, 31.92f),
                new Vector2(19.11f, 33.88f),
                new Vector2(18.30f, 35.24f),
                new Vector2(17.51f, 36.78f),
                new Vector2(16.84f, 37.41f),
                new Vector2(17.15f, 39.34f),
                new Vector2(18.53f, 38.58f),
                new Vector2(19.69f, 38.10f),
                new Vector2(21.14f, 38.54f),
                new Vector2(21.74f, 38.77f),
                new Vector2(23.12f, 38.40f),
                new Vector2(24.29f, 37.91f),
                new Vector2(23.60f, 36.28f),
                new Vector2(23.01f, 34.65f),
                new Vector2(22.12f, 35.79f),
                new Vector2(21.30f, 36.50f),
                new Vector2(19.80f, 35.25f),
                new Vector2(19.46f, 33.86f),
                new Vector2(18.48f, 32.51f),
                new Vector2(17.68f, 30.43f),
                new Vector2(16.72f, 29.32f),
            },
            TraversalMethod = WoWWaypointDefinition.WaypointTraversalMethod.CIRCULAR,
            TargetFindMethod = WoWWaypointDefinition.WaypointTargetFindMethod.TAB,
            DistanceTolerance = 0.2f
        };

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
            TargetFindMethod = WoWWaypointDefinition.WaypointTargetFindMethod.TAB,
            DistanceTolerance = 0.5f
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
            TargetFindMethod = WoWWaypointDefinition.WaypointTargetFindMethod.MACRO,
            DistanceTolerance = 0.5f
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
            TargetFindMethod = WoWWaypointDefinition.WaypointTargetFindMethod.TAB,
            DistanceTolerance = 0.5f
        };
    }
}
