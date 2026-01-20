using System.Collections.Generic;
using System.Numerics;
using WoWHelper.Code.Gameplay;
using static WoWHelper.Code.WorldState.WowLocationConfiguration;

namespace WoWHelper.Code.WorldState
{
    public static class WowLocationConfigs
    {
        public static readonly WowLocationConfiguration LEVEL_58_SILITHUS_RUMBLERS = new WowLocationConfiguration
        {
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
                new Vector2(22.92f, 17.88f),
            },
            TraversalMethod = WowLocationConfiguration.WaypointTraversalMethod.LINEAR,
            TargetFindMethod = WowLocationConfiguration.WaypointTargetFindMethod.ALTERNATE,
            DistanceTolerance = 0.1f,
            EngageMethod = EngagementMethod.Charge,
            UseRend = false,
            TooManyAttackersThreshold = 3
        };

        public static readonly WowLocationConfiguration LEVEL_56_DALTONS_TEARS_BACKSIDE_WPL = new WowLocationConfiguration
        {
            Waypoints = new List<Vector2>
            {
                new Vector2(48.41f, 51.75f),
                new Vector2(48.72f, 50.66f),
                new Vector2(48.49f, 49.79f),
                new Vector2(47.96f, 49.01f),
                new Vector2(47.38f, 48.65f),
                new Vector2(46.41f, 48.94f),
                new Vector2(45.86f, 49.10f),
                new Vector2(45.38f, 49.72f),
                new Vector2(45.04f, 50.33f),
                new Vector2(44.67f, 51.01f),
            },
            TraversalMethod = WowLocationConfiguration.WaypointTraversalMethod.LINEAR,
            TargetFindMethod = WowLocationConfiguration.WaypointTargetFindMethod.ALTERNATE,
            DistanceTolerance = 0.05f,
            EngageMethod = EngagementMethod.Shoot,
            UseRend = true,
            TooManyAttackersThreshold = 4
        };

        public static readonly WowLocationConfiguration LEVEL_56_DALTONS_TEARS_FRONTSIDE_WPL = new WowLocationConfiguration
        {
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
            TraversalMethod = WowLocationConfiguration.WaypointTraversalMethod.CIRCULAR,
            TargetFindMethod = WowLocationConfiguration.WaypointTargetFindMethod.ALTERNATE,
            DistanceTolerance = 0.1f,
            EngageMethod = EngagementMethod.Shoot,
            UseRend = false,
            TooManyAttackersThreshold = 4
        };

        /*
        // Water elementals spawn here during invasion, not really safe
        public static readonly WowWaypointConfiguration LEVEL_55_WINTERSPRING_LAKE = new WowWaypointConfiguration
        {
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
            TraversalMethod = WowWaypointConfiguration.WaypointTraversalMethod.CIRCULAR,
            TargetFindMethod = WowWaypointConfiguration.WaypointTargetFindMethod.ALTERNATE,
            DistanceTolerance = 0.06f
        };
        */

        public static readonly WowLocationConfiguration LEVEL_53_NORTH_FELWOOD = new WowLocationConfiguration
        {
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
            TraversalMethod = WowLocationConfiguration.WaypointTraversalMethod.LINEAR,
            TargetFindMethod = WowLocationConfiguration.WaypointTargetFindMethod.ALTERNATE,
            DistanceTolerance = 0.06f
        };

        /*
        // Level 55 pats through this area
        public static readonly WowWaypointConfiguration LEVEL_53_FELWOOD_IRONTREE_STOMPERS = new WowWaypointConfiguration
        {
            Waypoints = new List<Vector2>
            {
                new Vector2(51.68f, 26.10f),
                new Vector2(51.15f, 25.17f),
                new Vector2(51.70f, 24.43f),
                new Vector2(52.49f, 24.58f),
                new Vector2(53.23f, 23.66f),
                new Vector2(53.23f, 22.23f),
                new Vector2(52.72f, 21.38f),
                new Vector2(51.73f, 21.43f),
                new Vector2(50.62f, 20.74f),
                new Vector2(50.17f, 19.39f),
                new Vector2(50.29f, 18.11f),
                new Vector2(49.50f, 17.45f),
                new Vector2(49.13f, 19.06f),
                new Vector2(48.72f, 19.83f),
                new Vector2(48.81f, 21.12f),
            },
            TraversalMethod = WowWaypointConfiguration.WaypointTraversalMethod.LINEAR,
            TargetFindMethod = WowWaypointConfiguration.WaypointTargetFindMethod.ALTERNATE,
            DistanceTolerance = 0.06f
        };
        */

        public static readonly WowLocationConfiguration LEVEL_51_FELWOOD_SOUTH = new WowLocationConfiguration
        {
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
            TraversalMethod = WowLocationConfiguration.WaypointTraversalMethod.CIRCULAR,
            TargetFindMethod = WowLocationConfiguration.WaypointTargetFindMethod.ALTERNATE,
            DistanceTolerance = 0.06f
        };

        public static readonly WowLocationConfiguration LEVEL_48_FERALAS_HIPPOGRYPHS = new WowLocationConfiguration
        {
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
            TraversalMethod = WowLocationConfiguration.WaypointTraversalMethod.CIRCULAR,
            TargetFindMethod = WowLocationConfiguration.WaypointTargetFindMethod.ALTERNATE,
            DistanceTolerance = 0.06f
        };

        public static readonly WowLocationConfiguration TANARIS_TEST_PATHFINDING = new WowLocationConfiguration
        {
            Waypoints = new List<Vector2>
            {
                new Vector2(68.42f, 34.13f),
                new Vector2(67.07f, 34.12f),
                new Vector2(67.16f, 34.75f),
                new Vector2(67.02f, 35.20f),
                new Vector2(67.81f, 35.26f),
            },
            TraversalMethod = WowLocationConfiguration.WaypointTraversalMethod.CIRCULAR,
            TargetFindMethod = WowLocationConfiguration.WaypointTargetFindMethod.ALTERNATE,
            DistanceTolerance = 0.06f
        };

        public static readonly WowLocationConfiguration LEVEL_42_TANARIS_TURTLES = new WowLocationConfiguration
        {
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
            TraversalMethod = WowLocationConfiguration.WaypointTraversalMethod.LINEAR,
            TargetFindMethod = WowLocationConfiguration.WaypointTargetFindMethod.ALTERNATE,
            DistanceTolerance = 0.2f
        };

        public static readonly WowLocationConfiguration LEVEL_37_KODO_GRAVEYARD = new WowLocationConfiguration
        {
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
                new Vector2(50.17f, 57.92f),
                new Vector2(49.45f, 58.84f),
                new Vector2(49.31f, 59.66f),
                new Vector2(48.09f, 60.46f),
                new Vector2(47.00f, 60.92f),
                new Vector2(47.64f, 58.37f),
            },
            TraversalMethod = WowLocationConfiguration.WaypointTraversalMethod.LINEAR,
            TargetFindMethod = WowLocationConfiguration.WaypointTargetFindMethod.ALTERNATE,
            DistanceTolerance = 0.2f
        };

        public static readonly WowLocationConfiguration LEVEL_34_SHIMMERING_FLATS_WAYPOINTS_ALTERNATE = new WowLocationConfiguration
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
            TraversalMethod = WowLocationConfiguration.WaypointTraversalMethod.LINEAR,
            TargetFindMethod = WowLocationConfiguration.WaypointTargetFindMethod.MACRO,
            DistanceTolerance = 0.2f
        };

        public static readonly WowLocationConfiguration LEVEL_34_SHIMMERING_FLATS_WAYPOINTS = new WowLocationConfiguration
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
            TraversalMethod = WowLocationConfiguration.WaypointTraversalMethod.LINEAR,
            TargetFindMethod = WowLocationConfiguration.WaypointTargetFindMethod.MACRO,
            DistanceTolerance = 0.2f
        };

        // Don't recommend this one without WBs
        public static readonly WowLocationConfiguration LEVEL_27_NORTH_ASHENVALE_WAYPOINTS = new WowLocationConfiguration
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
            TraversalMethod = WowLocationConfiguration.WaypointTraversalMethod.CIRCULAR,
            TargetFindMethod = WowLocationConfiguration.WaypointTargetFindMethod.TAB,
            DistanceTolerance = 0.2f
        };

        public static readonly WowLocationConfiguration LEVEL_29_HILLSBRAD_RIVER_WAYPOINTS = new WowLocationConfiguration
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
            TraversalMethod = WowLocationConfiguration.WaypointTraversalMethod.LINEAR,
            TargetFindMethod = WowLocationConfiguration.WaypointTargetFindMethod.MACRO,
            DistanceTolerance = 0.3f
        };

        public static readonly WowLocationConfiguration LEVEL_24_STONETALON_WAYPOINTS = new WowLocationConfiguration
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
            TraversalMethod = WowLocationConfiguration.WaypointTraversalMethod.CIRCULAR,
            TargetFindMethod = WowLocationConfiguration.WaypointTargetFindMethod.ALTERNATE,
            DistanceTolerance = 0.2f
        };

        public static readonly WowLocationConfiguration LEVEL_21_ZORAMGAR_WAYPOINTS = new WowLocationConfiguration
        {
            Waypoints = new List<Vector2>
            {
                new Vector2(16.67f, 28.15f),
                new Vector2(16.07f, 29.94f),
                new Vector2(17.30f, 30.17f),
                new Vector2(18.18f, 31.92f),
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
                new Vector2(17.68f, 30.43f),
                new Vector2(16.72f, 29.32f),
            },
            EngageMethod = EngagementMethod.Spellcast, // TODO how to define this in combat config when location specific?
            LogoffLevel = 24,
        };

        public static readonly WowLocationConfiguration LEVEL_17_NORTHERN_BARRENS_WAYPOINTS = new WowLocationConfiguration
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
            TraversalMethod = WowLocationConfiguration.WaypointTraversalMethod.LINEAR,
            EngageMethod = EngagementMethod.Spellcast, // TODO how to define this in combat config when location specific?
            LogoffLevel = 21,
        };

        public static readonly WowLocationConfiguration LEVEL_13_BARRENS_ENTRANCE_WAYPOINTS = new WowLocationConfiguration
        {
            // /target Fleeting
            // /target Zhevra
            // /target Sunscale
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
            EngageMethod = EngagementMethod.Spellcast, // TODO how to define this in combat config when location specific?
            LogoffLevel = 18,
        };

        public static readonly WowLocationConfiguration LEVEL_11_DUROTAR_COAST_WAYPOINTS = new WowLocationConfiguration
        {
            // /target Venom
            // /target Elder
            // /target Blood
            // /target Corrupted
            Waypoints = new List<Vector2>
            { 
                //new Vector2(38.13f, 16.10f),
                new Vector2(37.52f, 22.80f),
                new Vector2(37.47f, 24.77f),
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
            TraversalMethod = WowLocationConfiguration.WaypointTraversalMethod.LINEAR,
            EngageMethod = EngagementMethod.Spellcast, // TODO how to define this in combat config when location specific?
            LogoffLevel = 14
        };

        public static readonly WowLocationConfiguration LEVEL_9_DUROTAR_SKULL_ROCK_COAST_WAYPOINTS = new WowLocationConfiguration
        {
            /*
/target Elder
/target Venom
/target Armored
/target Blood
            */
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
            EngageMethod = EngagementMethod.Spellcast, // TODO how to define this in combat config when location specific?
            LogoffLevel = 12
        };

        public static readonly WowLocationConfiguration LEVEL_6_DUROTAR_BOAR_RAZOR_HILL_LOOP = new WowLocationConfiguration
        {
            // /target Dire
            // /target Clattering
            Waypoints = new List<Vector2>
            {
                new Vector2(51.82f, 66.99f),
                //new Vector2(53.86f, 67.21f),
                //new Vector2(54.40f, 64.83f),
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
                //new Vector2(51.30f, 63.76f),
                //new Vector2(52.09f, 65.87f),
                new Vector2(52.44f, 64.81f),
            },
            EngageMethod = EngagementMethod.Spellcast, // TODO how to define this in combat config when location specific?
            LogoffLevel = 10,
        };

        public static readonly WowLocationConfiguration LEVEL_4_DUROTAR_IMPS = new WowLocationConfiguration
        {
            // /target Scorpid
            // /target Vile
            Waypoints = new List<Vector2>
            {
                new Vector2(46.78f, 57.33f),
                new Vector2(46.40f, 59.10f),
                new Vector2(44.91f, 59.08f),
                new Vector2(43.54f, 58.77f),
                new Vector2(43.88f, 56.90f),
                new Vector2(45.27f, 57.40f),
            },
            EngageMethod = EngagementMethod.Spellcast, // TODO how to define this in combat config when location specific?
            TooManyAttackersThreshold = 3,
            LogoffLevel = 7,
        };

        public static readonly WowLocationConfiguration LEVEL_10_MULGORE_MIXED_BEASTS = new WowLocationConfiguration
        {
            // /target Swoop
            // /target Flatland
            // /target Prairie
            Waypoints = new List<Vector2>
            {
                new Vector2(49.82f, 40.08f),
                new Vector2(49.82f, 36.88f),
                new Vector2(49.82f, 33.47f),
                new Vector2(52.35f, 33.47f),
                new Vector2(52.07f, 37.26f),
                new Vector2(51.60f, 39.78f),
            },
            TargetFindMethod = WaypointTargetFindMethod.MACRO, // Kodo packs wandering around
            EngageMethod = EngagementMethod.Spellcast, // TODO how to define this in combat config when location specific?
            TooManyAttackersThreshold = 4,
            LogoffLevel = 13,
        };

        public static readonly WowLocationConfiguration LEVEL_8_MULGORE_MIXED_BEASTS = new WowLocationConfiguration
        {
            // /target Battleboar
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
            TargetFindMethod = WaypointTargetFindMethod.MACRO, // Kodo packs wandering around
            EngageMethod = EngagementMethod.Spellcast, // TODO how to define this in combat config when location specific?
            LogoffLevel = 10,
        };

        public static readonly WowLocationConfiguration LEVEL_6_MULGORE_BATTLEBOARS = new WowLocationConfiguration
        {
            // /target Battleboar
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
            TraversalMethod = WaypointTraversalMethod.LINEAR,
            EngageMethod = EngagementMethod.Spellcast, // TODO how to define this in combat config when location specific?
            LogoffLevel = 8,
        };

        public static readonly WowLocationConfiguration LEVEL_4_MULGORE_MOUNTAIN_COUGARS = new WowLocationConfiguration
        {
            // /target Mountain
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
            EngageMethod = EngagementMethod.Spellcast, // TODO how to define this in combat config when location specific?
            TooManyAttackersThreshold = 2,
            LogoffLevel = 6,
        };

        public static readonly WowLocationConfiguration LEVEL_1_MULGORE_PLAINSTRIDERS = new WowLocationConfiguration
        {
            // /target Plains
            Waypoints = new List<Vector2>
            {
                new Vector2(46.26f, 76.52f),
                new Vector2(48.83f, 76.44f),
                new Vector2(51.83f, 75.29f),
                new Vector2(49.42f, 80.53f),
                new Vector2(46.44f, 78.21f),
            },
            EngageMethod = EngagementMethod.Spellcast, // TODO how to define this in combat config when location specific?
            TooManyAttackersThreshold = 2,
            LogoffLevel = 4,
        };

        public static readonly WowLocationConfiguration LEVEL_1_DUROTAR_BOARS_AND_SCORPS = new WowLocationConfiguration
        {
            // /target Mottled
            // /target Scorpid
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
            EngageMethod = EngagementMethod.Spellcast, // TODO how to define this in combat config when location specific?
            TooManyAttackersThreshold = 2,
            LogoffLevel = 4,
        };
    }
}
