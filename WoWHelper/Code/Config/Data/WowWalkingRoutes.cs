using System.Collections.Generic;
using System.Numerics;

namespace WoWHelper.Code.WorldState
{
    public static class WowWalkingRoutes
    {
        #region Durotar

        public static readonly WowRoute BOARS_AND_SCORPS_TO_MERCHANT_ROUTE = new WowRoute
        {
            Name = "Durotar Boars and Scorpions to Merchant",
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
            Name = "Durotar Boars and Scorpions to Warrior Trainer",
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
            Name = "Durotar Boars and Scorpions to Imps",
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
            Name = "Durotar Boars and Scorpions to Imps",
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
            Name = "Valley of Trials to Razor Hill Loop",
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
            Name = "Razor Hill Loop To Razor Hill",
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
            Name = "Razor Hill To Merchant",
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
            Name = "Razor Hill To Warrior Trainer",
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
            Name = "Razor Hill To Org Entrance",
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
            Name = "Org Entrance To Durotar Skull Rock Coast",
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
            Name = "Org Entrance to Skinning Trainer",
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
            Name = "Orgrimmar Entrance",
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
            Name = "Animals to Merchant Route",
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

        public static readonly WowRoute TELDRASSIL_MERCHANT_TO_WARRIOR_TRAINER_ROUTE = new WowRoute
        {
            Name = "Ad Hoc 1794627159741 Reversed",
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
                new Vector2(59.64f, 38.44f),
            },
        };

        public static readonly WowRoute TELDRASSIL_LEVEL_1_TO_4_ROUTE = new WowRoute
        {
            Name = "Ad Hoc 1794630607988 Reversed",
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
            Name = "TELDRASSIL_LEVEL_1_TO_4_ROUTE",
            Zone = WowZone.Teldrassil,
            Faction = WowFaction.ALLIANCE,
            TraversalMethod = WowRoute.WaypointTraversalMethod.LINEAR,

            Waypoints = new List<Vector2>
            {
                new Vector2(60.05f, 42.53f),
                new Vector2(60.68f, 43.75f),
                new Vector2(61.37f, 44.98f),
                new Vector2(61.22f, 46.40f),
                new Vector2(61.14f, 47.13f),
                new Vector2(61.26f, 48.05f),
                new Vector2(61.43f, 49.88f),
                new Vector2(61.50f, 50.74f),
                new Vector2(63.22f, 52.26f),
            },
        };

        #endregion

        public static readonly List<WowRoute> ALL_WALKING_ROUTES = new List<WowRoute>
        {
            BOARS_AND_SCORPS_TO_MERCHANT_ROUTE,
            BOARS_AND_SCORPS_TO_WARRIOR_TRAINER_ROUTE,
            BOARS_AND_SCORPS_TO_IMPS,
            BOARS_AND_SCORPS_TO_SCORPS,
            VALLEY_OF_TRIALS_TO_RAZOR_HILL_LOOP,
            RAZOR_HILL_LOOP_TO_RAZOR_HILL,
            RAZOR_HILL_TO_MERCHANT,
            RAZOR_HILL_TO_WARRIOR_TRAINER,
            RAZOR_HILL_TO_ORG_ENTRANCE,
            ORG_ENTRANCE_TO_DUROTAR_SKULL_ROCK_COAST,
            ORGRIMMAR_ENTRANCE_ROUTE,
            TELDRASSIL_ANIMALS_TO_MERCHANT_ROUTE,
            TELDRASSIL_MERCHANT_TO_WARRIOR_TRAINER_ROUTE,
            TELDRASSIL_LEVEL_1_TO_4_ROUTE,
            TELDRASSIL_LEVEL_4_TO_7_ROUTE
        };
    }
}
