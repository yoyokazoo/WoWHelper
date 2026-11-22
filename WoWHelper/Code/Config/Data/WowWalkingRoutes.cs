using System;
using System.Collections.Generic;
using System.Numerics;

namespace WoWHelper.Code.WorldState
{
    public static class WowWalkingRoutes
    {
        #region Durotar

        public static readonly WowRoute BOARS_AND_SCORPS_TO_MERCHANT_ROUTE = new WowRoute
        {
            Name = "BOARS_AND_SCORPS_TO_MERCHANT_ROUTE",
            Zone = WowZone.Durotar,
            Faction = WowFaction.HORDE,

            Waypoints = new List<Vector2>
            {
                new Vector2(44.19f, 66.23f),
                new Vector2(44.31f, 67.14f),
                new Vector2(43.81f, 67.96f),
                new Vector2(42.58f, 67.34f),
            },
        };

        public static readonly WowRoute BOARS_AND_SCORPS_TO_WARRIOR_TRAINER_ROUTE = new WowRoute
        {
            Name = "BOARS_AND_SCORPS_TO_WARRIOR_TRAINER_ROUTE",
            Zone = WowZone.Durotar,
            Faction = WowFaction.HORDE,

            Waypoints = new List<Vector2>
            {
                new Vector2(44.19f, 66.23f),
                new Vector2(44.33f, 67.07f),
                new Vector2(43.44f, 68.62f),
                new Vector2(42.89f, 69.44f)
            },
        };

        public static readonly WowRoute BOARS_AND_SCORPS_TO_IMPS = new WowRoute
        {
            Name = "BOARS_AND_SCORPS_TO_IMPS",
            Zone = WowZone.Durotar,
            Faction = WowFaction.HORDE,

            Waypoints = new List<Vector2>
            {
                new Vector2(44.29f, 62.22f),
                new Vector2(44.32f, 60.94f),
                new Vector2(44.36f, 58.71f),
            },
        };

        public static readonly WowRoute BOARS_AND_SCORPS_TO_SCORPS = new WowRoute
        {
            Name = "BOARS_AND_SCORPS_TO_SCORPS",
            Zone = WowZone.Durotar,
            Faction = WowFaction.HORDE,

            Waypoints = new List<Vector2>
            {
                new Vector2(40.71f, 66.17f),
                new Vector2(40.60f, 65.42f),
                new Vector2(40.85f, 64.59f),
                new Vector2(41.03f, 64.58f),
            },
        };

        public static readonly WowRoute VALLEY_OF_TRIALS_TO_RAZOR_HILL_LOOP = new WowRoute
        {
            Name = "VALLEY_OF_TRIALS_TO_RAZOR_HILL_LOOP",
            Zone = WowZone.Durotar,
            Faction = WowFaction.HORDE,

            Waypoints = new List<Vector2>
            {
                new Vector2(43.81f, 67.96f),
                new Vector2(43.80f, 68.36f),
                new Vector2(46.51f, 68.22f),
                new Vector2(47.74f, 67.38f),
                new Vector2(48.59f, 68.03f),
                new Vector2(48.69f, 69.18f),
                new Vector2(49.16f, 69.30f),
                new Vector2(49.44f, 68.22f),
                new Vector2(50.10f, 68.49f),
                new Vector2(51.82f, 68.13f),
                new Vector2(51.82f, 66.99f),
            }
        };

        public static readonly WowRoute RAZOR_HILL_LOOP_TO_RAZOR_HILL = new WowRoute
        {
            Name = "RAZOR_HILL_LOOP_TO_RAZOR_HILL",
            Zone = WowZone.Durotar,
            Faction = WowFaction.HORDE,

            Waypoints = new List<Vector2>
            {
                new Vector2(53.74f, 47.89f),
                new Vector2(52.98f, 47.02f),
                new Vector2(52.61f, 45.56f),
                new Vector2(52.47f, 44.34f),
                new Vector2(52.74f, 42.58f),
                new Vector2(52.55f, 42.21f),
            }
        };

        public static readonly WowRoute RAZOR_HILL_TO_MERCHANT = new WowRoute
        {
            Name = "RAZOR_HILL_TO_MERCHANT",
            Zone = WowZone.Durotar,
            Faction = WowFaction.HORDE,

            Waypoints = new List<Vector2>
            {
                new Vector2(52.55f, 42.21f),
                new Vector2(52.98f, 41.03f),
            }
        };

        public static readonly WowRoute RAZOR_HILL_TO_WARRIOR_TRAINER = new WowRoute
        {
            Name = "RAZOR_HILL_TO_WARRIOR_TRAINER",
            Zone = WowZone.Durotar,
            Faction = WowFaction.HORDE,

            Waypoints = new List<Vector2>
            {
                new Vector2(52.74f, 42.58f),
                new Vector2(53.71f, 42.52f),
                new Vector2(53.84f, 42.96f),
                new Vector2(53.96f, 42.97f),
                new Vector2(54.07f, 42.44f),
                new Vector2(54.19f, 42.47f),
            },
            DistanceTolerance = 0.04f
        };

        public static readonly WowRoute RAZOR_HILL_TO_ORG_ENTRANCE = new WowRoute
        {
            Name = "RAZOR_HILL_TO_ORG_ENTRANCE",
            Zone = WowZone.Durotar,
            Faction = WowFaction.HORDE,

            Waypoints = new List<Vector2>
            {
                new Vector2(52.55f, 42.21f),
                new Vector2(52.35f, 39.17f),
                new Vector2(52.41f, 33.23f),
                new Vector2(50.33f, 29.81f),
                new Vector2(48.61f, 28.66f),
                new Vector2(47.61f, 23.55f),
                new Vector2(46.72f, 22.40f),
                new Vector2(46.37f, 19.16f),
                new Vector2(46.19f, 17.12f),
                new Vector2(45.55f, 12.36f),
            }
        };

        public static readonly WowRoute ORG_ENTRANCE_TO_DUROTAR_SKULL_ROCK_COAST = new WowRoute
        {
            Name = "ORG_ENTRANCE_TO_DUROTAR_SKULL_ROCK_COAST",
            Zone = WowZone.Durotar,
            Faction = WowFaction.HORDE,

            Waypoints = new List<Vector2>
            {
                new Vector2(46.37f, 19.16f),
                new Vector2(47.75f, 18.52f),
                new Vector2(49.77f, 18.06f),
                new Vector2(50.92f, 16.38f),
                new Vector2(52.62f, 17.13f),
                new Vector2(52.93f, 17.36f),
            }
        };

        // Need to implement cross-zone pathing to work with Org
        /*
        public static readonly WowRoute ORG_ENTRANCE_TO_SKINNING_TRAINER = new WowRoute
        {
            Name = "ORG_ENTRANCE_TO_SKINNING_TRAINER",
            Zone = WowZone.Durotar,
            Faction = WowFaction.HORDE,

            Waypoints = new List<Vector2>
            {
                new Vector2(45.55f, 12.36f),
                new Vector2(48.73f, 
            }
        };
        */

        #endregion

        #region Orgrimmar

        public static readonly WowRoute ORGRIMMAR_ENTRANCE_ROUTE = new WowRoute
        {
            Name = "ORGRIMMAR_ENTRANCE_ROUTE",
            Zone = WowZone.Orgrimmar,
            Faction = WowFaction.HORDE,

            Waypoints = new List<Vector2>
            {
                new Vector2(49.02f, 94.24f),
                new Vector2(48.71f, 90.27f),
                new Vector2(52.91f, 89.44f),
                new Vector2(52.22f, 81.56f),
                new Vector2(52.58f, 74.95f),
                new Vector2(50.52f, 71.63f),
            }
        };

        #endregion

        #region Teldrassil

        public static readonly WowRoute TELDRASSIL_ANIMALS_TO_MERCHANT_ROUTE = new WowRoute
        {
            Name = "TELDRASSIL_ANIMALS_TO_MERCHANT_ROUTE",
            Zone = WowZone.Teldrassil,
            Faction = WowFaction.ALLIANCE,
            TraversalMethod = WowRoute.WaypointTraversalMethod.LINEAR,

            Waypoints = new List<Vector2>
            {
                new Vector2(62.37f, 38.78f),
                new Vector2(61.87f, 40.18f),
                new Vector2(62.03f, 42.28f),
                new Vector2(61.25f, 42.43f),
                new Vector2(60.11f, 42.64f),
                new Vector2(59.80f, 42.02f),
                new Vector2(59.31f, 41.09f),
            },
        };

        public static readonly WowRoute TELDRASSIL_MERCHANT_TO_TRAINER_ROUTE = new WowRoute
        {
            Name = "TELDRASSIL_MERCHANT_TO_WARRIOR_TRAINER_ROUTE",
            Zone = WowZone.Teldrassil,
            Faction = WowFaction.ALLIANCE,
            TraversalMethod = WowRoute.WaypointTraversalMethod.LINEAR,

            Waypoints = new List<Vector2>
            {
                new Vector2(59.80f, 42.02f),
                new Vector2(59.48f, 41.05f),
                new Vector2(59.16f, 40.27f),
                new Vector2(59.21f, 39.64f),
                new Vector2(59.27f, 39.10f),
            },
        };

        public static readonly WowRoute TELDRASSIL_TRAINER_TO_WARRIOR_TRAINER_ROUTE = new WowRoute
        {
            Name = "TELDRASSIL_TRAINER_TO_WARRIOR_TRAINER_ROUTE",
            Zone = WowZone.Teldrassil,
            Faction = WowFaction.ALLIANCE,
            TraversalMethod = WowRoute.WaypointTraversalMethod.LINEAR,

            Waypoints = new List<Vector2>
            {
                new Vector2(59.27f, 39.10f),
                new Vector2(59.64f, 38.44f),
            },
        };

        public static readonly WowRoute TELDRASSIL_TRAINER_TO_ROGUE_TRAINER_ROUTE = new WowRoute
        {
            Name = "TELDRASSIL_TRAINER_TO_ROGUE_TRAINER_ROUTE",
            Zone = WowZone.Teldrassil,
            Faction = WowFaction.ALLIANCE,
            TraversalMethod = WowRoute.WaypointTraversalMethod.LINEAR,

            Waypoints = new List<Vector2>
            {
                new Vector2(59.27f, 39.10f),
                new Vector2(59.64f, 38.66f),
            },
        };

        // TODO: this route can't be joined or started mid-way, only at its first/last waypoint. If this ever becomes a problem,
        // add an EndpointsOnly flag to WowRoute that exposes just those two waypoints as EntryPoints, and have the
        // nearest-route/nearest-waypoint/intersection logic in WowPathfinding and PickStartingWaypoint use them.
        public static readonly WowRoute TELDRASSIL_BASE_OF_STAIRS_TO_DRUID_TRAINER = new WowRoute
        {
            Name = "TELDRASSIL_BASE_OF_STAIRS_TO_DRUID_TRAINER",
            Zone = WowZone.Teldrassil,
            Faction = WowFaction.ALLIANCE,
            TraversalMethod = WowRoute.WaypointTraversalMethod.LINEAR,

            Waypoints = new List<Vector2>
            {
                new Vector2(57.43f, 41.96f),
                new Vector2(57.54f, 41.65f),
                new Vector2(57.77f, 41.10f),
                new Vector2(57.98f, 40.63f),
                new Vector2(58.06f, 40.25f),
                new Vector2(58.08f, 39.81f),
                new Vector2(58.11f, 39.38f),
                new Vector2(58.25f, 38.98f),
                new Vector2(58.47f, 38.72f),
                new Vector2(58.77f, 38.60f),
                new Vector2(58.80f, 38.88f),
                new Vector2(58.59f, 38.97f),
                new Vector2(58.36f, 39.37f),
                new Vector2(58.24f, 39.86f),
                new Vector2(58.31f, 40.60f),
                new Vector2(58.49f, 40.78f),
                new Vector2(58.61f, 40.48f),
                new Vector2(58.62f, 40.28f),
            },
        };

        public static readonly WowRoute TELDRASSIL_MERCHANTS_TO_BASE_OF_STAIRS = new WowRoute
        {
            Name = "TELDRASSIL_MERCHANTS_TO_BASE_OF_STAIRS",
            Zone = WowZone.Teldrassil,
            Faction = WowFaction.ALLIANCE,
            TraversalMethod = WowRoute.WaypointTraversalMethod.LINEAR,

            Waypoints = new List<Vector2>
            {
                new Vector2(60.11f, 42.64f),
                new Vector2(59.79f, 43.17f),
                new Vector2(58.64f, 42.98f),
                new Vector2(57.43f, 41.96f),
            },
        };

        public static readonly WowRoute TELDRASSIL_LEVEL_1_TO_4_ROUTE = new WowRoute
        {
            Name = "TELDRASSIL_LEVEL_1_TO_4_ROUTE",
            Zone = WowZone.Teldrassil,
            Faction = WowFaction.ALLIANCE,
            TraversalMethod = WowRoute.WaypointTraversalMethod.LINEAR,

            Waypoints = new List<Vector2>
            {
                new Vector2(61.73f, 35.95f),
                new Vector2(61.98f, 35.37f),
                new Vector2(60.71f, 34.76f),
                new Vector2(60.23f, 34.38f),
                new Vector2(59.27f, 33.39f),
                new Vector2(58.04f, 32.94f),
            },
        };

        public static readonly WowRoute TELDRASSIL_LEVEL_4_TO_7_ROUTE = new WowRoute
        {
            Name = "TELDRASSIL_LEVEL_4_TO_7_ROUTE",
            Zone = WowZone.Teldrassil,
            Faction = WowFaction.ALLIANCE,
            TraversalMethod = WowRoute.WaypointTraversalMethod.LINEAR,

            Waypoints = new List<Vector2>
            {
                new Vector2(60.11f, 42.64f),
                new Vector2(60.68f, 43.75f),
                new Vector2(61.37f, 44.98f),
                new Vector2(61.22f, 46.40f),
                new Vector2(61.14f, 47.13f),
                new Vector2(61.26f, 48.05f),
                new Vector2(61.43f, 49.88f),
                new Vector2(61.50f, 50.74f),
                new Vector2(63.13f, 52.32f),
            },
        };

        public static readonly WowRoute TELDRASSIL_LEVEL_7_TO_DOLANAAR = new WowRoute
        {
            Name = "TELDRASSIL_LEVEL_7_TO_DOLANAAR",
            Zone = WowZone.Teldrassil,
            Faction = WowFaction.ALLIANCE,
            TraversalMethod = WowRoute.WaypointTraversalMethod.LINEAR,

            Waypoints = new List<Vector2>
            {
                new Vector2(64.46f, 55.66f),
                new Vector2(63.36f, 53.79f),
                new Vector2(62.12f, 55.31f),
                new Vector2(61.16f, 56.40f),
                new Vector2(59.74f, 57.34f),
                new Vector2(58.35f, 57.54f),
                new Vector2(57.32f, 57.64f),
                new Vector2(56.32f, 57.80f),
                new Vector2(55.85f, 58.32f),
            },
        };

        public static readonly WowRoute DOLANAAR_TO_WARRIOR_TRAINER = new WowRoute
        {
            Name = "DOLANAAR_TO_WARRIOR_TRAINER",
            Zone = WowZone.Teldrassil,
            Faction = WowFaction.ALLIANCE,
            TraversalMethod = WowRoute.WaypointTraversalMethod.LINEAR,

            Waypoints = new List<Vector2>
            {
                new Vector2(55.85f, 58.32f),
                new Vector2(55.95f, 58.78f),
                new Vector2(56.22f, 59.20f),
            },
        };

        public static readonly WowRoute DOLANAAR_TO_ROGUE_TRAINER = new WowRoute
        {
            Name = "DOLANAAR_TO_ROGUE_TRAINER",
            Zone = WowZone.Teldrassil,
            Faction = WowFaction.ALLIANCE,
            TraversalMethod = WowRoute.WaypointTraversalMethod.LINEAR,

            Waypoints = new List<Vector2>
            {
                new Vector2(55.85f, 58.32f),
                new Vector2(55.95f, 58.78f),
                new Vector2(56.38f, 60.14f),
            },
        };

        public static readonly WowRoute DOLANAAR_TO_DRUID_TRAINER = new WowRoute
        {
            Name = "DOLANAAR_TO_DRUID_TRAINER",
            Zone = WowZone.Teldrassil,
            Faction = WowFaction.ALLIANCE,
            TraversalMethod = WowRoute.WaypointTraversalMethod.LINEAR,

            Waypoints = new List<Vector2>
            {
                new Vector2(55.85f, 58.32f),
                new Vector2(56.39f, 58.35f),
                new Vector2(56.99f, 58.81f),
                new Vector2(57.24f, 60.10f),
                new Vector2(56.80f, 61.22f),
                new Vector2(55.94f, 61.57f),
            },
        };

        public static readonly WowRoute DOLANAAR_TO_MERCHANT = new WowRoute
        {
            Name = "DOLANAAR_TO_MERCHANT",
            Zone = WowZone.Teldrassil,
            Faction = WowFaction.ALLIANCE,
            TraversalMethod = WowRoute.WaypointTraversalMethod.LINEAR,

            Waypoints = new List<Vector2>
            {
                new Vector2(55.85f, 58.32f),
                new Vector2(55.95f, 58.78f),
                new Vector2(55.89f, 59.21f),
            },
        };

        #endregion

        #region Dun Morogh

        public static readonly WowRoute LEVEL_1_DUN_MOROGH_TO_MERCHANT = new WowRoute
        {
            Name = "LEVEL_1_DUN_MOROGH_TO_MERCHANT",
            Zone = WowZone.DunMorogh,
            Faction = WowFaction.ALLIANCE,
            TraversalMethod = WowRoute.WaypointTraversalMethod.LINEAR,

            Waypoints = new List<Vector2>
            {
                new Vector2(28.92f, 72.71f),
                new Vector2(29.72f, 72.27f),
                new Vector2(29.98f, 71.52f),
                new Vector2(30.09f, 71.55f),
            },
        };

        public static readonly WowRoute LEVEL_1_DUN_MOROGH_TO_LEVEL_4 = new WowRoute
        {
            Name = "LEVEL_1_DUN_MOROGH_TO_LEVEL_4",
            Zone = WowZone.DunMorogh,
            Faction = WowFaction.ALLIANCE,
            TraversalMethod = WowRoute.WaypointTraversalMethod.LINEAR,

            Waypoints = new List<Vector2>
            {
                new Vector2(27.07f, 76.01f),
                new Vector2(26.55f, 77.74f),
            },
        };

        public static readonly WowRoute DUN_MOROGH_COLDRIDGE_TO_KHARANOS = new WowRoute
        {
            Name = "DUN_MOROGH_COLDRIDGE_TO_KHARANOS",
            Zone = WowZone.DunMorogh,
            Faction = WowFaction.ALLIANCE,
            TraversalMethod = WowRoute.WaypointTraversalMethod.LINEAR,

            Waypoints = new List<Vector2>
            {
                new Vector2(46.54f, 54.91f),
                new Vector2(46.85f, 56.03f),
                new Vector2(47.06f, 57.56f),
                new Vector2(46.97f, 59.65f),
                new Vector2(46.42f, 60.48f),
                new Vector2(46.33f, 61.48f),
                new Vector2(45.98f, 62.07f),
                new Vector2(44.95f, 63.51f),
                new Vector2(43.80f, 64.53f),
                new Vector2(42.21f, 65.07f),
                new Vector2(40.86f, 64.47f),
                new Vector2(39.41f, 63.72f),
                new Vector2(38.27f, 62.82f),
                new Vector2(37.07f, 62.26f),
                new Vector2(35.70f, 61.86f),
                new Vector2(35.36f, 62.27f),
                new Vector2(35.31f, 64.11f),
                new Vector2(35.58f, 65.45f),
                new Vector2(35.80f, 66.66f),
                new Vector2(35.50f, 67.32f),
                new Vector2(35.65f, 67.86f),
                new Vector2(35.22f, 67.99f),
                new Vector2(34.94f, 68.26f),
                new Vector2(35.38f, 69.34f),
                new Vector2(34.76f, 69.48f),
                new Vector2(34.22f, 69.30f),
                new Vector2(33.89f, 69.82f),
                new Vector2(33.98f, 70.35f),
                new Vector2(34.39f, 70.78f),
                new Vector2(34.02f, 71.75f),
                new Vector2(33.66f, 71.68f),
                new Vector2(32.97f, 72.50f),
                new Vector2(32.06f, 72.75f),
                new Vector2(31.06f, 72.34f),
                new Vector2(30.26f, 72.54f),
                new Vector2(29.93f, 71.49f),
            },
        };

        #endregion

        // Every route above, via reflection.
        private static readonly Lazy<IReadOnlyList<WowRoute>> ALL_WALKING_ROUTES_LAZY = StaticConfigCollector.LazyCollect<WowRoute>(typeof(WowWalkingRoutes));
        public static IReadOnlyList<WowRoute> ALL_WALKING_ROUTES => ALL_WALKING_ROUTES_LAZY.Value;
    }
}
