using System.Collections.Generic;
using System.Numerics;

namespace WoWHelper.Code.WorldState
{
    // Every WowRoute (waypoint path + how to walk it + which zone it's in), one per
    // WowLocationConfigs entry -- each WowLocationConfiguration points at its own
    // FOO_ROUTE here via its Route property. Declared in its own static class, so
    // WowLocationConfigs' static initializers can reference these freely (a static
    // class's fields are all initialized before first access from another class).
    public static class WowRoutes
    {
        #region Durotar

        public static readonly WowRoute LEVEL_11_DUROTAR_COAST_ROUTE = new WowRoute
        {
            Zone = WowZone.Durotar,
            TraversalMethod = WowRoute.WaypointTraversalMethod.LINEAR,

            Waypoints = new List<Vector2>
            {
                new Vector2(37.52f, 22.80f),
                new Vector2(37.47f, 24.77f),
                new Vector2(37.07f, 27.55f),
                new Vector2(36.30f, 31.59f),
                new Vector2(36.03f, 33.09f),
                new Vector2(36.19f, 35.34f),
                new Vector2(36.28f, 39.20f),
                new Vector2(36.57f, 43.56f),
                new Vector2(36.60f, 47.55f),
            },
        };

        public static readonly WowRoute LEVEL_9_DUROTAR_SKULL_ROCK_COAST_ROUTE = new WowRoute
        {
            Zone = WowZone.Durotar,

            Waypoints = new List<Vector2>
            {
                new Vector2(52.93f, 17.36f),
                new Vector2(54.78f, 18.67f),
                new Vector2(55.12f, 19.66f),
                new Vector2(55.82f, 21.31f),
                new Vector2(56.17f, 23.66f),
                new Vector2(56.20f, 24.87f),
                new Vector2(56.92f, 25.92f),

                new Vector2(56.30f, 27.25f),
                new Vector2(56.18f, 28.78f),
                new Vector2(56.57f, 30.35f),
                new Vector2(58.30f, 27.74f),
                new Vector2(58.88f, 23.79f),

                new Vector2(57.34f, 24.08f),
                new Vector2(56.11f, 23.68f),
                new Vector2(55.94f, 21.49f),
                new Vector2(54.90f, 18.83f),
                new Vector2(53.57f, 17.68f)
            },
        };

        public static readonly WowRoute LEVEL_6_DUROTAR_BOAR_RAZOR_HILL_LOOP_ROUTE = new WowRoute
        {
            Zone = WowZone.Durotar,

            Waypoints = new List<Vector2>
            {
                new Vector2(51.82f, 66.99f),
                new Vector2(53.14f, 66.34f),
                new Vector2(53.31f, 63.53f),
                new Vector2(54.53f, 61.59f),
                new Vector2(54.19f, 61.04f),
                new Vector2(54.00f, 59.60f),
                new Vector2(53.94f, 58.08f),
                new Vector2(53.59f, 55.13f),
                new Vector2(52.96f, 53.19f),
                new Vector2(53.94f, 50.60f),
                new Vector2(53.74f, 47.89f),
                new Vector2(51.56f, 48.48f),
                new Vector2(52.21f, 50.67f),
                new Vector2(52.67f, 52.46f),
                new Vector2(51.94f, 54.02f),
                new Vector2(52.02f, 55.96f),
                new Vector2(52.69f, 57.70f),
                new Vector2(53.28f, 60.76f),
                new Vector2(53.22f, 62.94f),
                new Vector2(52.44f, 64.81f),
            },
        };

        public static readonly WowRoute LEVEL_4_DUROTAR_IMPS_ROUTE = new WowRoute
        {
            Zone = WowZone.Durotar,

            Waypoints = new List<Vector2>
            {
                new Vector2(46.78f, 57.33f),
                new Vector2(46.40f, 59.10f),
                new Vector2(44.91f, 59.08f),
                new Vector2(43.54f, 58.77f),
                new Vector2(43.88f, 56.90f),
                new Vector2(45.27f, 57.40f),
            },
        };

        public static readonly WowRoute LEVEL_1_DUROTAR_BOARS_AND_SCORPS_ROUTE = new WowRoute
        {
            Zone = WowZone.Durotar,

            Waypoints = new List<Vector2>
            {
                new Vector2(44.19f, 66.23f),
                new Vector2(43.31f, 64.90f),
                new Vector2(41.03f, 64.58f),
                new Vector2(41.22f, 63.01f),
                new Vector2(42.96f, 62.43f),
                new Vector2(44.29f, 62.22f),
                new Vector2(45.49f, 64.54f),
            },
        };

        #endregion

        #region Mulgore

        public static readonly WowRoute LEVEL_10_MULGORE_MIXED_BEASTS_ROUTE = new WowRoute
        {
            Zone = WowZone.Mulgore,

            Waypoints = new List<Vector2>
            {
                new Vector2(49.82f, 40.08f),
                new Vector2(49.82f, 36.88f),
                new Vector2(49.82f, 33.47f),
                new Vector2(52.35f, 33.47f),
                new Vector2(52.07f, 37.26f),
                new Vector2(51.60f, 39.78f),
            },
        };

        public static readonly WowRoute LEVEL_8_MULGORE_MIXED_BEASTS_ROUTE = new WowRoute
        {
            Zone = WowZone.Mulgore,

            Waypoints = new List<Vector2>
            {
                new Vector2(43.44f, 66.44f),
                new Vector2(40.87f, 70.93f),
                new Vector2(38.80f, 70.32f),
                new Vector2(36.35f, 71.71f),
                new Vector2(36.80f, 67.05f),
                new Vector2(40.00f, 65.46f),
                new Vector2(41.77f, 65.69f),
            },
        };

        public static readonly WowRoute LEVEL_6_MULGORE_BATTLEBOARS_ROUTE = new WowRoute
        {
            Zone = WowZone.Mulgore,

            Waypoints = new List<Vector2>
            {
                new Vector2(55.20f, 75.76f),
                new Vector2(55.27f, 79.62f),
                new Vector2(54.18f, 80.59f),
                new Vector2(53.69f, 81.98f),
                new Vector2(56.43f, 84.50f),
                new Vector2(58.30f, 88.36f),
                new Vector2(59.84f, 88.61f),
            },
        };

        public static readonly WowRoute LEVEL_4_MULGORE_MOUNTAIN_COUGARS_ROUTE = new WowRoute
        {
            Zone = WowZone.Mulgore,

            Waypoints = new List<Vector2>
            {
                new Vector2(46.54f, 88.68f),
                new Vector2(46.45f, 90.95f),
                new Vector2(44.38f, 93.00f),
                new Vector2(42.82f, 89.71f),
                new Vector2(41.21f, 88.86f),
                new Vector2(42.75f, 86.92f),
                new Vector2(43.54f, 88.74f),
            },
        };

        public static readonly WowRoute LEVEL_1_MULGORE_PLAINSTRIDERS_ROUTE = new WowRoute
        {
            Zone = WowZone.Mulgore,

            Waypoints = new List<Vector2>
            {
                new Vector2(46.26f, 76.52f),
                new Vector2(48.83f, 76.44f),
                new Vector2(51.83f, 75.29f),
                new Vector2(49.42f, 80.53f),
                new Vector2(46.44f, 78.21f),
            },
        };

        #endregion

        #region The Barrens

        public static readonly WowRoute LEVEL_17_NORTHERN_BARRENS_ROUTE = new WowRoute
        {
            Zone = WowZone.TheBarrens,
            TraversalMethod = WowRoute.WaypointTraversalMethod.LINEAR,

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
        };

        public static readonly WowRoute LEVEL_13_BARRENS_ENTRANCE_ROUTE = new WowRoute
        {
            Zone = WowZone.TheBarrens,

            Waypoints = new List<Vector2>
            {
                new Vector2(55.00f, 21.13f),
                new Vector2(56.91f, 19.55f),
                new Vector2(58.95f, 20.13f),
                new Vector2(60.42f, 20.46f),
                new Vector2(61.11f, 22.33f),
                new Vector2(59.88f, 21.84f),
                new Vector2(56.29f, 22.47f),
                new Vector2(56.60f, 22.00f)
            },
        };

        #endregion

        #region Ashenvale

        public static readonly WowRoute LEVEL_20_ZORAMGAR_ROUTE = new WowRoute
        {
            Zone = WowZone.Ashenvale,

            Waypoints = new List<Vector2>
            {
                new Vector2(16.67f, 28.15f),
                new Vector2(16.07f, 29.94f),
                new Vector2(17.30f, 30.17f),
                new Vector2(18.18f, 31.92f),
                new Vector2(18.50f, 32.45f),
                new Vector2(19.11f, 33.88f),
                new Vector2(18.30f, 35.24f),
                new Vector2(17.71f, 36.24f),
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
                new Vector2(18.08f, 31.35f),
                new Vector2(17.68f, 30.43f),
                new Vector2(16.72f, 29.32f),
            },
        };

        #endregion

        #region Stonetalon Mountains

        public static readonly WowRoute LEVEL_26_STONETALON_CHARRED_FOREST_ROUTE = new WowRoute
        {
            Zone = WowZone.StonetalonMountains,

            Waypoints = new List<Vector2>
            {
                new Vector2(37.13f, 46.76f),
                new Vector2(37.82f, 48.43f),
                new Vector2(37.62f, 50.48f),
                new Vector2(37.58f, 51.35f),
                new Vector2(36.36f, 53.21f),
                new Vector2(35.28f, 54.04f),
                new Vector2(35.47f, 51.74f),
                new Vector2(36.22f, 50.04f),
                new Vector2(36.07f, 49.98f),
                new Vector2(35.79f, 47.97f),
            },
        };

        public static readonly WowRoute LEVEL_23_STONETALON_ROUTE = new WowRoute
        {
            Zone = WowZone.StonetalonMountains,

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
        };

        #endregion

        #region Hillsbrad Foothills

        public static readonly WowRoute LEVEL_29_HILLSBRAD_RIVER_ROUTE = new WowRoute
        {
            Zone = WowZone.HillsbradFoothills,
            TraversalMethod = WowRoute.WaypointTraversalMethod.LINEAR,
            DistanceTolerance = 0.3f,

            Waypoints = new List<Vector2>
            {
                new Vector2(70.12f, 11.00f),
                new Vector2(68.84f, 13.74f),
                new Vector2(67.82f, 17.99f),
                new Vector2(67.78f, 21.78f),
                new Vector2(67.94f, 22.98f),
                new Vector2(67.97f, 25.64f),
                new Vector2(67.35f, 30.65f),
                new Vector2(67.71f, 35.17f),
                new Vector2(65.68f, 38.19f),
                new Vector2(64.12f, 40.35f),
            },
        };

        #endregion

        #region Thousand Needles

        public static readonly WowRoute LEVEL_34_SHIMMERING_FLATS_ROUTE = new WowRoute
        {
            Zone = WowZone.ThousandNeedles,
            TraversalMethod = WowRoute.WaypointTraversalMethod.LINEAR,

            Waypoints = new List<Vector2>
            {
                new Vector2(78.68f, 55.01f),
                new Vector2(78.16f, 52.21f),
                new Vector2(79.70f, 52.30f),
                new Vector2(81.79f, 52.12f),
                new Vector2(82.08f, 53.51f),
                new Vector2(81.60f, 55.03f),
                new Vector2(82.47f, 56.14f),
                new Vector2(83.00f, 54.67f),
                new Vector2(82.92f, 54.00f),
                new Vector2(84.13f, 56.00f),
            },
        };

        #endregion

        #region Desolace

        public static readonly WowRoute LEVEL_36_KODO_GRAVEYARD_ROUTE = new WowRoute
        {
            Zone = WowZone.Desolace,
            TraversalMethod = WowRoute.WaypointTraversalMethod.LINEAR,

            Waypoints = new List<Vector2>
            {
                new Vector2(54.05f, 61.73f),
                new Vector2(53.45f, 59.77f),
                new Vector2(53.93f, 58.53f),
                new Vector2(53.65f, 57.29f),
                new Vector2(52.62f, 57.50f),
                new Vector2(51.40f, 58.10f),
                new Vector2(51.08f, 57.06f),
                new Vector2(50.60f, 56.78f),

                new Vector2(50.06f, 57.92f),
                new Vector2(50.52f, 59.66f),
                new Vector2(50.13f, 59.83f),
                new Vector2(50.13f, 59.83f),
                new Vector2(48.63f, 59.99f),
                new Vector2(47.36f, 60.77f),
                new Vector2(46.59f, 60.81f),
                new Vector2(46.70f, 59.10f),
                new Vector2(47.12f, 57.79f),
            },
        };

        #endregion

        #region Tanaris

        public static readonly WowRoute LEVEL_41_TANARIS_TURTLES_ROUTE = new WowRoute
        {
            Zone = WowZone.Tanaris,
            TraversalMethod = WowRoute.WaypointTraversalMethod.LINEAR,

            Waypoints = new List<Vector2>
            {
                new Vector2(68.87f, 39.97f),
                new Vector2(68.44f, 39.65f),
                new Vector2(67.62f, 39.21f),
                new Vector2(67.26f, 38.69f),
                new Vector2(67.35f, 37.68f),
                new Vector2(67.88f, 36.57f),
                new Vector2(67.86f, 35.27f),
                new Vector2(68.54f, 34.02f),
            },
        };

        #endregion

        #region Feralas

        public static readonly WowRoute LEVEL_46_FERALAS_HIPPOGRYPHS_ROUTE = new WowRoute
        {
            Zone = WowZone.Feralas,
            DistanceTolerance = 0.06f,

            Waypoints = new List<Vector2>
            {
                new Vector2(55.06f, 64.41f),
                new Vector2(54.26f, 65.30f),
                new Vector2(53.84f, 66.35f),
                new Vector2(53.80f, 68.20f),
                new Vector2(53.54f, 69.29f),
                new Vector2(53.74f, 70.95f),
                new Vector2(54.38f, 72.55f),
                new Vector2(55.22f, 74.12f),
                new Vector2(55.71f, 74.88f),
                new Vector2(56.07f, 72.88f),
                new Vector2(55.73f, 71.73f),
                new Vector2(54.72f, 70.25f),
                new Vector2(54.84f, 68.00f),
                new Vector2(55.38f, 67.00f),
                new Vector2(55.49f, 66.57f),
                new Vector2(55.00f, 65.00f),
            },
        };

        #endregion

        #region Felwood

        public static readonly WowRoute LEVEL_53_NORTH_FELWOOD_ROUTE = new WowRoute
        {
            Zone = WowZone.Felwood,
            TraversalMethod = WowRoute.WaypointTraversalMethod.LINEAR,
            DistanceTolerance = 0.06f,

            Waypoints = new List<Vector2>
            {
                new Vector2(50.88f, 15.57f),
                new Vector2(51.81f, 15.38f),
                new Vector2(52.31f, 16.10f),
                new Vector2(54.02f, 15.57f),
                new Vector2(54.78f, 15.88f),
                new Vector2(55.23f, 16.49f),
                new Vector2(54.43f, 17.08f),
                new Vector2(55.02f, 17.69f),
                new Vector2(54.85f, 19.46f),
                new Vector2(55.85f, 20.75f),
                new Vector2(55.48f, 22.18f),
                new Vector2(55.43f, 23.45f),
                new Vector2(54.72f, 24.82f),
                new Vector2(54.30f, 26.32f),
                new Vector2(53.57f, 28.18f),
            },
        };

        public static readonly WowRoute LEVEL_50_FELWOOD_SOUTH_ROUTE = new WowRoute
        {
            Zone = WowZone.Felwood,
            DistanceTolerance = 0.06f,

            Waypoints = new List<Vector2>
            {
                new Vector2(51.06f, 82.12f),
                new Vector2(52.63f, 82.56f),
                new Vector2(53.50f, 83.02f),
                new Vector2(53.42f, 84.33f),
                new Vector2(55.01f, 85.73f),
                new Vector2(55.91f, 87.25f),
                new Vector2(56.02f, 89.37f),
                new Vector2(56.36f, 90.48f),
                new Vector2(55.99f, 92.01f),
                new Vector2(54.44f, 91.97f),
                new Vector2(53.87f, 90.58f),
                new Vector2(54.63f, 89.08f),
                new Vector2(53.33f, 87.21f),
                new Vector2(52.76f, 87.69f),
                new Vector2(51.87f, 86.53f),
                new Vector2(50.59f, 87.53f),
                new Vector2(51.31f, 86.30f),
                new Vector2(51.31f, 84.80f),

                new Vector2(49.78f, 84.04f),
                new Vector2(49.16f, 85.59f),
                new Vector2(48.39f, 83.93f),
                new Vector2(47.47f, 83.91f),
                new Vector2(47.24f, 83.18f),
                new Vector2(47.87f, 81.42f),
                new Vector2(48.51f, 81.87f),
                new Vector2(49.23f, 82.05f),
                new Vector2(50.47f, 81.94f),
            },
        };

        #endregion

        #region Western Plaguelands

        public static readonly WowRoute LEVEL_56_DALTONS_TEARS_FRONTSIDE_WPL_ROUTE = new WowRoute
        {
            Zone = WowZone.WesternPlaguelands,
            DistanceTolerance = 0.1f,

            Waypoints = new List<Vector2>
            {
                new Vector2(45.96f, 56.01f),
                new Vector2(45.5f, 55.25f),
                new Vector2(45.04f, 54.49f),
                new Vector2(44.58f, 53.73f),
                new Vector2(45.3525f, 53.625f),
                new Vector2(46.125f, 53.52f),
                new Vector2(46.8975f, 53.415f),
                new Vector2(47.67f, 53.31f),
                new Vector2(47.2425f, 53.985f),
                new Vector2(46.815f, 54.66f),
                new Vector2(46.3875f, 55.335f),
                new Vector2(45.96f, 56.01f),
            },
        };

        #endregion

        #region Silithus

        public static readonly WowRoute LEVEL_58_SILITHUS_RUMBLERS_ROUTE = new WowRoute
        {
            Zone = WowZone.Silithus,
            TraversalMethod = WowRoute.WaypointTraversalMethod.LINEAR,
            DistanceTolerance = 0.1f,

            Waypoints = new List<Vector2>
            {
                new Vector2(27.20f, 11.69f),
                new Vector2(25.52f, 10.55f),
                new Vector2(25.65f, 13.44f),
                new Vector2(24.75f, 14.44f),
                new Vector2(22.60f, 11.84f),
                new Vector2(21.31f, 11.96f),
                new Vector2(22.60f, 11.84f),
                new Vector2(22.50f, 14.59f),
                new Vector2(21.30f, 15.79f),
                new Vector2(22.04f, 17.74f),
                //new Vector2(22.92f, 17.88f), // too close to air elementals for comfort
            },
        };

        #endregion

        #region Azshara

        public static readonly WowRoute LEVEL_53_ASZHARA_SATYR_CIRCLE_ROUTE = new WowRoute
        {
            Zone = WowZone.Azshara,
            TraversalMethod = WowRoute.WaypointTraversalMethod.LINEAR,
            DistanceTolerance = 0.06f,

            Waypoints = new List<Vector2>
            {
                new Vector2(51.20f, 21.02f),
                new Vector2(50.66f, 21.21f),
                new Vector2(49.95f, 20.04f),
                new Vector2(49.53f, 19.12f),
                new Vector2(49.58f, 17.95f),
                new Vector2(50.30f, 17.77f),
                new Vector2(50.30f, 17.77f),
                new Vector2(50.87f, 17.79f),
                new Vector2(51.57f, 18.13f),
                new Vector2(52.22f, 18.27f),
                new Vector2(51.39f, 19.74f),
            },
        };

        #endregion

        #region Winterspring

        public static readonly WowRoute LEVEL_57_WINTERSPRING_YETIS_ROUTE = new WowRoute
        {
            Zone = WowZone.Winterspring,
            DistanceTolerance = 0.1f,

            Waypoints = new List<Vector2>
            {
                new Vector2(64.33f, 40.78f),
                new Vector2(64.84f, 41.54f),
                new Vector2(65.06f, 41.94f),
                new Vector2(65.39f, 42.88f),
                new Vector2(65.73f, 43.53f),
                new Vector2(65.96f, 44.94f),
                new Vector2(66.28f, 45.71f),
                new Vector2(66.88f, 45.57f),
                new Vector2(66.92f, 45.41f),
                new Vector2(67.09f, 44.50f),
                new Vector2(66.06f, 43.82f),
                new Vector2(65.87f, 41.89f),
                new Vector2(65.09f, 40.48f),
            },
        };

        public static readonly WowRoute LEVEL_55_WINTERSPRING_LAKE_ROUTE = new WowRoute
        {
            Zone = WowZone.Winterspring,
            DistanceTolerance = 0.06f,

            Waypoints = new List<Vector2>
            {
                new Vector2(53.60f, 39.54f),
                new Vector2(53.58f, 40.99f),
                new Vector2(52.96f, 41.78f),
                new Vector2(52.07f, 42.04f),
                new Vector2(51.51f, 41.22f),
                new Vector2(50.84f, 41.76f),
                new Vector2(51.25f, 42.36f),
                new Vector2(50.71f, 43.01f),
                new Vector2(52.22f, 43.96f),
                new Vector2(52.97f, 43.92f),
                new Vector2(53.44f, 43.40f),
                new Vector2(53.54f, 42.67f),
                new Vector2(54.59f, 43.87f),
                new Vector2(55.23f, 42.98f),
                new Vector2(54.28f, 42.10f),
                new Vector2(53.96f, 40.54f),
            },
        };

        #endregion

        #region Tirisfal Glades

        public static readonly WowRoute LEVEL_6_TIRISFAL_ZOMBIES_ROUTE = new WowRoute
        {
            Zone = WowZone.TirisfalGlades,

            Waypoints = new List<Vector2>
            {
                new Vector2(55.43f, 51.36f),
                new Vector2(54.76f, 50.26f),
                new Vector2(53.97f, 49.84f),
                new Vector2(53.69f, 51.11f),
                new Vector2(53.01f, 50.36f),
                new Vector2(52.21f, 49.69f),
                new Vector2(51.10f, 51.23f),
                new Vector2(51.52f, 53.00f),
                new Vector2(51.81f, 53.65f),
                new Vector2(53.19f, 52.08f),
                new Vector2(54.55f, 52.90f),
                new Vector2(56.45f, 53.12f),
                new Vector2(56.34f, 51.48f),
            },
        };

        public static readonly WowRoute LEVEL_4_TIRISFAL_ANIMALS_ROUTE = new WowRoute
        {
            Zone = WowZone.TirisfalGlades,

            Waypoints = new List<Vector2>
            {
                new Vector2(36.99f, 56.80f),
                new Vector2(37.66f, 58.31f),
                new Vector2(37.39f, 60.60f),
                new Vector2(35.67f, 61.43f),
                new Vector2(34.89f, 61.68f),
                new Vector2(35.33f, 59.87f),
                new Vector2(34.60f, 58.76f),
                new Vector2(36.14f, 57.73f),
            },
        };

        public static readonly WowRoute LEVEL_1_TIRISFAL_UNDEAD_ROUTE = new WowRoute
        {
            Zone = WowZone.TirisfalGlades,

            Waypoints = new List<Vector2>
            {
                new Vector2(31.51f, 63.69f),
                new Vector2(30.16f, 63.75f),
                new Vector2(29.95f, 61.80f),
                new Vector2(30.81f, 61.16f),
                new Vector2(32.20f, 60.61f),
                new Vector2(32.41f, 61.90f),
                new Vector2(31.90f, 62.98f),
            },
        };

        #endregion
    }
}
