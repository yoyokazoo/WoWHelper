using System.Collections.Generic;
using System.Numerics;
using WoWHelper.Code.Gameplay;
using static WoWHelper.Code.WorldState.WowLocationConfiguration;

namespace WoWHelper.Code.WorldState
{
    public static class WowLocationConfigs
    {
        #region Levels 50-60

        /*
/target Desert Rumbler
        */
        public static readonly WowLocationConfiguration LEVEL_58_SILITHUS_RUMBLERS = new WowLocationConfiguration
        {
            Route = WowRoutes.LEVEL_58_SILITHUS_RUMBLERS_ROUTE,
            ExpectedMobNames = new List<string> { "Desert Rumbler" },
        };

        /*
/target Ice Thistle
        */
        public static readonly WowLocationConfiguration LEVEL_57_WINTERSPRING_YETIS = new WowLocationConfiguration
        {
            Route = WowRoutes.LEVEL_57_WINTERSPRING_YETIS_ROUTE,
        };

        /*
/target Blighted
/target Rotting
        */
        public static readonly WowLocationConfiguration LEVEL_56_DALTONS_TEARS_FRONTSIDE_WPL = new WowLocationConfiguration
        {
            Route = WowRoutes.LEVEL_56_DALTONS_TEARS_FRONTSIDE_WPL_ROUTE,
            EngageMethod = EngagementMethod.Pull,
        };

        /*
/target Suffering
/target Anguished
/target Watery
        */
        // Water elementals spawn here during invasion, bot will log off if they are seen.
        // Supposedly they only show up once every 48 hours or so
        public static readonly WowLocationConfiguration LEVEL_55_WINTERSPRING_LAKE = new WowLocationConfiguration
        {
            Route = WowRoutes.LEVEL_55_WINTERSPRING_LAKE_ROUTE,
        };

        /*
/target Legashi
        */
        // Legashi Hellcallers stand still and cast.  Only use this camp if you have handling
        // for LongRangeCasters set up (Earth shock, etc.)
        // FWIW I was getting stuck on geometry a lot at this specific one, so probably only run supervised
        public static readonly WowLocationConfiguration LEVEL_53_ASZHARA_SATYR_CIRCLE = new WowLocationConfiguration
        {
            Route = WowRoutes.LEVEL_53_ASZHARA_SATYR_CIRCLE_ROUTE,
        };

        /*
/target Ironbeak
/target Angerclaw
/target Warpwood
/target Felpaw
        */
        public static readonly WowLocationConfiguration LEVEL_53_NORTH_FELWOOD = new WowLocationConfiguration
        {
            Route = WowRoutes.LEVEL_53_NORTH_FELWOOD_ROUTE,
            ChaseOutOfRangeTargets = false, // Tall cliffs nearby, can't risk it
        };

        /*
/target Ironbeak
/target Angerclaw
/target Warpwood
/target Felpaw
/target Warpwood
        */
        public static readonly WowLocationConfiguration LEVEL_50_FELWOOD_SOUTH = new WowLocationConfiguration
        {
            Route = WowRoutes.LEVEL_50_FELWOOD_SOUTH_ROUTE,
        };

        #endregion

        #region Levels 40-50

        /*
/target Ironfur
/target Cursed
/target Wandering
/target Frayfeather
        */
        public static readonly WowLocationConfiguration LEVEL_46_FERALAS_HIPPOGRYPHS = new WowLocationConfiguration
        {
            Route = WowRoutes.LEVEL_46_FERALAS_HIPPOGRYPHS_ROUTE,
        };

        /*
/target Steeljaw
        */
        public static readonly WowLocationConfiguration LEVEL_41_TANARIS_TURTLES = new WowLocationConfiguration
        {
            MaximumLevel = 48,
            Route = WowRoutes.LEVEL_41_TANARIS_TURTLES_ROUTE,

            /*
            MerchantConfig = new WowMerchantConfiguration
            {
                Name = "Muuran",
                Waypoints = new List<Vector2>
                {
                    new Vector2(53.65f, 57.29f),
                    new Vector2(54.63f, 57.79f),
                    new Vector2(55.48f, 58.22f),
                    new Vector2(55.88f, 57.25f),
                    new Vector2(55.60f, 56.51f),
                },
            },
            */
        };

        #endregion

        #region Levels 30-40

        /*
/target Aged
/target Dying
/target Ancient
/target Carrion
        */
        public static readonly WowLocationConfiguration LEVEL_36_KODO_GRAVEYARD = new WowLocationConfiguration
        {
            MaximumLevel = 43,
            Route = WowRoutes.LEVEL_36_KODO_GRAVEYARD_ROUTE,
            EngageMethod = EngagementMethod.Pull,

            MerchantConfig = new WowMerchantConfiguration
            {
                Name = "Muuran",
                Waypoints = new List<Vector2>
                {
                    new Vector2(53.65f, 57.29f),
                    new Vector2(54.63f, 57.79f),
                    new Vector2(55.48f, 58.22f),
                    new Vector2(55.88f, 57.25f),
                    new Vector2(55.60f, 56.51f),
                },
            },
        };

        /*
/target Scorpid
/target Swirling
/target Saltstone
/target Sparkleshell
/target Ironeye
        */
        public static readonly WowLocationConfiguration LEVEL_34_SHIMMERING_FLATS = new WowLocationConfiguration
        {
            MaximumLevel = 38,
            Route = WowRoutes.LEVEL_34_SHIMMERING_FLATS_ROUTE,
            ChaseOutOfRangeTargets = false, // the ship wreck here is easy to get stuck on
        };

        #endregion

        #region Levels 20-30

        /*
/target Snapjaw
        */
        public static readonly WowLocationConfiguration LEVEL_29_HILLSBRAD_RIVER = new WowLocationConfiguration
        {
            MaximumLevel = 35,
            Route = WowRoutes.LEVEL_29_HILLSBRAD_RIVER_ROUTE,
            TargetFindMethod = WowLocationConfiguration.WaypointTargetFindMethod.MACRO,
            ChaseOutOfRangeTargets = false, // elite dragon nearby if we get dragged too far

            // TODO: stubbed -- confirm this is actually a vendor and tune the tight final
            // approach point once tested live.
            MerchantConfig = new WowMerchantConfiguration
            {
                Name = "Derak Nightfall",
                Waypoints = new List<Vector2>
                {
                    new Vector2(67.82f, 17.99f), // must match LEVEL_29_HILLSBRAD_RIVER_ROUTE.Waypoints[2] exactly
                    new Vector2(66.43f, 18.23f),
                    new Vector2(64.79f, 18.04f),
                    new Vector2(63.26f, 17.25f),
                    new Vector2(63.09f, 19.39f),
                },
            },
        };

        /*
/target Rogue
/target Blackened
/target Bloodfury
        */
        public static readonly WowLocationConfiguration LEVEL_26_STONETALON_CHARRED_FOREST = new WowLocationConfiguration
        {
            MaximumLevel = 29,
            Route = WowRoutes.LEVEL_26_STONETALON_CHARRED_FOREST_ROUTE,
        };

        /*
/target Antlered
/target Sap
        */
        public static readonly WowLocationConfiguration LEVEL_23_STONETALON = new WowLocationConfiguration
        {
            MaximumLevel = 28,
            Route = WowRoutes.LEVEL_23_STONETALON_ROUTE,
        };

        /*
/target Wild
/target Ghostpaw
        */
        public static readonly WowLocationConfiguration LEVEL_20_ZORAMGAR = new WowLocationConfiguration
        {
            MaximumLevel = 25,
            Route = WowRoutes.LEVEL_20_ZORAMGAR_ROUTE,
        };

        #endregion

        #region Levels 10-20

        /*
/target Zhevra
/target Savannah
/target Hecklefang
/target Ornery
/target Sunscale
        */
        public static readonly WowLocationConfiguration LEVEL_17_NORTHERN_BARRENS = new WowLocationConfiguration
        {
            MaximumLevel = 21,
            Route = WowRoutes.LEVEL_17_NORTHERN_BARRENS_ROUTE,
        };

        /*
/target Fleeting
/target Zhevra
/target Sunscale
        */
        public static readonly WowLocationConfiguration LEVEL_13_BARRENS_ENTRANCE = new WowLocationConfiguration
        {
            MaximumLevel = 18,
            Route = WowRoutes.LEVEL_13_BARRENS_ENTRANCE_ROUTE,
        };

        /*
/target Venom
/target Elder
/target Blood
/target Corrupted
        */
        public static readonly WowLocationConfiguration LEVEL_11_DUROTAR_COAST = new WowLocationConfiguration
        {
            MaximumLevel = 14,
            Route = WowRoutes.LEVEL_11_DUROTAR_COAST_ROUTE,
        };

        #endregion

        #region Levels 1-10 Durotar

        /*
/target Elder
/target Venom
/target Armored
/target Blood
        */
        public static readonly WowLocationConfiguration LEVEL_9_DUROTAR_SKULL_ROCK_COAST = new WowLocationConfiguration
        {
            MaximumLevel = 12,
            Route = WowRoutes.LEVEL_9_DUROTAR_SKULL_ROCK_COAST_ROUTE,
        };

        /*
/target Dire
/target Clattering
        */
        public static readonly WowLocationConfiguration LEVEL_6_DUROTAR_BOAR_RAZOR_HILL_LOOP = new WowLocationConfiguration
        {
            MaximumLevel = 10,
            Route = WowRoutes.LEVEL_6_DUROTAR_BOAR_RAZOR_HILL_LOOP_ROUTE,
        };

        /*
/target Scorpid
/target Vile
        */
        public static readonly WowLocationConfiguration LEVEL_4_DUROTAR_IMPS = new WowLocationConfiguration
        {
            MaximumLevel = 7,
            Route = WowRoutes.LEVEL_4_DUROTAR_IMPS_ROUTE,
        };

        public static readonly WowLocationConfiguration LEVEL_1_DUROTAR_BOARS_AND_SCORPS = new WowLocationConfiguration
        {
            MaximumLevel = 4,
            Route = WowRoutes.LEVEL_1_DUROTAR_BOARS_AND_SCORPS_ROUTE,
            TargetMacroMobNames = "Mottled,Scorpid",

            MerchantConfig = new WowMerchantConfiguration
            {
                Name = "Duokna",
                Waypoints = new List<Vector2>
                {
                    new Vector2(44.19f, 66.23f),
                    new Vector2(43.82f, 66.78f),
                    new Vector2(43.72f, 67.81f),
                    new Vector2(43.10f, 67.61f),
                    new Vector2(42.58f, 67.35f),
                },
            },
        };

        #endregion

        #region Levels 1-10 Mulgore

        /*
/target Swoop
/target Flatland
/target Prairie
        */
        public static readonly WowLocationConfiguration LEVEL_10_MULGORE_MIXED_BEASTS = new WowLocationConfiguration
        {
            MaximumLevel = 13,
            Route = WowRoutes.LEVEL_10_MULGORE_MIXED_BEASTS_ROUTE,
            TargetFindMethod = WaypointTargetFindMethod.MACRO, // Kodo packs wandering around
        };

        /*
//TODO
        */
        public static readonly WowLocationConfiguration LEVEL_8_MULGORE_MIXED_BEASTS = new WowLocationConfiguration
        {
            MaximumLevel = 10,
            Route = WowRoutes.LEVEL_8_MULGORE_MIXED_BEASTS_ROUTE,
            TargetFindMethod = WaypointTargetFindMethod.MACRO, // Kodo packs wandering around
        };

        /*
/target Battleboar
        */
        public static readonly WowLocationConfiguration LEVEL_6_MULGORE_BATTLEBOARS = new WowLocationConfiguration
        {
            MaximumLevel = 8,
            Route = WowRoutes.LEVEL_6_MULGORE_BATTLEBOARS_ROUTE,
        };

        /*
/target Mountain
        */
        public static readonly WowLocationConfiguration LEVEL_4_MULGORE_MOUNTAIN_COUGARS = new WowLocationConfiguration
        {
            MaximumLevel = 6,
            Route = WowRoutes.LEVEL_4_MULGORE_MOUNTAIN_COUGARS_ROUTE,
        };

        /*
/target Plains
        */
        public static readonly WowLocationConfiguration LEVEL_1_MULGORE_PLAINSTRIDERS = new WowLocationConfiguration
        {
            MaximumLevel = 4,
            Route = WowRoutes.LEVEL_1_MULGORE_PLAINSTRIDERS_ROUTE,
        };

        #endregion

        #region Levels 1-10 Tirisfal

        /*
/target Decrepit
/target Greater
/target Rotting
/target Ravaged
        */
        public static readonly WowLocationConfiguration LEVEL_6_TIRISFAL_ZOMBIES = new WowLocationConfiguration
        {
            MaximumLevel = 10,
            Route = WowRoutes.LEVEL_6_TIRISFAL_ZOMBIES_ROUTE,
        };

        /*
/target Mangy
/target Ragged
        */
        public static readonly WowLocationConfiguration LEVEL_4_TIRISFAL_ANIMALS = new WowLocationConfiguration
        {
            MaximumLevel = 7,
            Route = WowRoutes.LEVEL_4_TIRISFAL_ANIMALS_ROUTE,
        };

        /*
/target Young
/target Duskbat
/target Mindless
/target Wretched
/target Rattlecage
        */
        public static readonly WowLocationConfiguration LEVEL_1_TIRISFAL_UNDEAD = new WowLocationConfiguration
        {
            MaximumLevel = 4,
            Route = WowRoutes.LEVEL_1_TIRISFAL_UNDEAD_ROUTE,
        };

        #endregion

        public static readonly List<WowLocationConfiguration> ALL_LOCATIONS = new List<WowLocationConfiguration>
        {
            LEVEL_58_SILITHUS_RUMBLERS,
            LEVEL_57_WINTERSPRING_YETIS,
            LEVEL_56_DALTONS_TEARS_FRONTSIDE_WPL,
            LEVEL_55_WINTERSPRING_LAKE,
            LEVEL_53_ASZHARA_SATYR_CIRCLE,
            LEVEL_53_NORTH_FELWOOD,
            LEVEL_50_FELWOOD_SOUTH,
            LEVEL_46_FERALAS_HIPPOGRYPHS,
            LEVEL_41_TANARIS_TURTLES,
            LEVEL_36_KODO_GRAVEYARD,
            LEVEL_34_SHIMMERING_FLATS,
            LEVEL_29_HILLSBRAD_RIVER,
            LEVEL_26_STONETALON_CHARRED_FOREST,
            LEVEL_23_STONETALON,
            LEVEL_20_ZORAMGAR,
            LEVEL_17_NORTHERN_BARRENS,
            LEVEL_13_BARRENS_ENTRANCE,
            LEVEL_11_DUROTAR_COAST,
            LEVEL_9_DUROTAR_SKULL_ROCK_COAST,
            LEVEL_6_TIRISFAL_ZOMBIES,
            LEVEL_6_DUROTAR_BOAR_RAZOR_HILL_LOOP,
            LEVEL_4_DUROTAR_IMPS,
            LEVEL_10_MULGORE_MIXED_BEASTS,
            LEVEL_8_MULGORE_MIXED_BEASTS,
            LEVEL_6_MULGORE_BATTLEBOARS,
            LEVEL_4_MULGORE_MOUNTAIN_COUGARS,
            LEVEL_4_TIRISFAL_ANIMALS,
            LEVEL_1_MULGORE_PLAINSTRIDERS,
            LEVEL_1_DUROTAR_BOARS_AND_SCORPS,
            LEVEL_1_TIRISFAL_UNDEAD,
        };
    }
}
