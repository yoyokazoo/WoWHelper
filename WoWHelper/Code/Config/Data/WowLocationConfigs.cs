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
            Title = "Silithus Rumblers (Level 58+)",
            MinimumLevel = 58,
            Route = WowRoutes.LEVEL_58_SILITHUS_RUMBLERS_ROUTE,
            ExpectedMobNames = new List<string> { "Desert Rumbler" },
        };

        /*
/target Ice Thistle
        */
        public static readonly WowLocationConfiguration LEVEL_57_WINTERSPRING_YETIS = new WowLocationConfiguration
        {
            Title = "Winterspring Yetis (Level 57+)",
            MinimumLevel = 57,
            Route = WowRoutes.LEVEL_57_WINTERSPRING_YETIS_ROUTE,
        };

        /*
/target Blighted
/target Rotting
        */
        public static readonly WowLocationConfiguration LEVEL_56_DALTONS_TEARS_FRONTSIDE_WPL = new WowLocationConfiguration
        {
            Title = "Dalton's Tears Frontside, Western Plaguelands (Level 56+)",
            MinimumLevel = 56,
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
            Title = "Winterspring Lake (Level 56+)",
            MinimumLevel = 56,
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
            Title = "Azshara (Level 53+)",
            MinimumLevel = 53,
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
            Title = "North Felwood (Level 53+)",
            MinimumLevel = 53,
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
            Title = "South Felwood (Level 50+)",
            MinimumLevel = 50,
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
            Title = "Feralas Hippogryphs (Level 46+)",
            MinimumLevel = 46,
            Route = WowRoutes.LEVEL_46_FERALAS_HIPPOGRYPHS_ROUTE,
        };

        /*
/target Steeljaw
        */
        public static readonly WowLocationConfiguration LEVEL_41_TANARIS_TURTLES = new WowLocationConfiguration
        {
            Title = "Tanaris Turtles (Level 41+)",
            MinimumLevel = 41,
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
            Title = "Kodo Graveyard, Desolace (Level 36+)",
            MinimumLevel = 36,
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
            Title = "Shimmering Flats Alternate, Thousand Needles (Level 34+)",
            MinimumLevel = 34,
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
            Title = "Hillsbrad River (Level 29+)",
            MinimumLevel = 29,
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
            Title = "North Stonetalon (Level 26+)",
            MinimumLevel = 26,
            MaximumLevel = 29,
            Route = WowRoutes.LEVEL_26_STONETALON_CHARRED_FOREST_ROUTE,
        };

        /*
/target Antlered
/target Sap
        */
        public static readonly WowLocationConfiguration LEVEL_23_STONETALON = new WowLocationConfiguration
        {
            Title = "Stonetalon Mountains (Level 23+)",
            MinimumLevel = 23,
            MaximumLevel = 28,
            Route = WowRoutes.LEVEL_23_STONETALON_ROUTE,
        };

        /*
/target Wild
/target Ghostpaw
        */
        public static readonly WowLocationConfiguration LEVEL_20_ZORAMGAR = new WowLocationConfiguration
        {
            Title = "Zoram'gar, Ashenvale (Level 20+)",
            MinimumLevel = 20,
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
            Title = "Northern Barrens (Level 17+)",
            MinimumLevel = 17,
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
            Title = "Barrens Entrance (Level 13+)",
            MinimumLevel = 13,
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
            Title = "Durotar Coast (Level 11+)",
            MinimumLevel = 11,
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
            Title = "Durotar Skull Rock Coast (Level 9+)",
            MinimumLevel = 9,
            MaximumLevel = 12,
            Route = WowRoutes.LEVEL_9_DUROTAR_SKULL_ROCK_COAST_ROUTE,
        };

        /*
/target Dire
/target Clattering
        */
        public static readonly WowLocationConfiguration LEVEL_6_DUROTAR_BOAR_RAZOR_HILL_LOOP = new WowLocationConfiguration
        {
            Title = "Durotar Boar Razor Hill Loop (Level 6+)",
            MinimumLevel = 6,
            MaximumLevel = 10,
            Route = WowRoutes.LEVEL_6_DUROTAR_BOAR_RAZOR_HILL_LOOP_ROUTE,
        };

        /*
/target Scorpid
/target Vile
        */
        public static readonly WowLocationConfiguration LEVEL_4_DUROTAR_IMPS = new WowLocationConfiguration
        {
            Title = "Durotar Imps (Level 4+)",
            MinimumLevel = 4,
            MaximumLevel = 7,
            Route = WowRoutes.LEVEL_4_DUROTAR_IMPS_ROUTE,
        };

        public static readonly WowLocationConfiguration LEVEL_1_DUROTAR_BOARS_AND_SCORPS = new WowLocationConfiguration
        {
            Title = "Durotar Boars and Scorpions (Level 1+)",
            MinimumLevel = 1,
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
            Title = "Mulgore Mixed Beasts (Level 10+)",
            MinimumLevel = 10,
            MaximumLevel = 13,
            Route = WowRoutes.LEVEL_10_MULGORE_MIXED_BEASTS_ROUTE,
            TargetFindMethod = WaypointTargetFindMethod.MACRO, // Kodo packs wandering around
        };

        /*
//TODO
        */
        public static readonly WowLocationConfiguration LEVEL_8_MULGORE_MIXED_BEASTS = new WowLocationConfiguration
        {
            Title = "Mulgore Mixed Beasts (Level 8+)",
            MinimumLevel = 8,
            MaximumLevel = 10,
            Route = WowRoutes.LEVEL_8_MULGORE_MIXED_BEASTS_ROUTE,
            TargetFindMethod = WaypointTargetFindMethod.MACRO, // Kodo packs wandering around
        };

        /*
/target Battleboar
        */
        public static readonly WowLocationConfiguration LEVEL_6_MULGORE_BATTLEBOARS = new WowLocationConfiguration
        {
            Title = "Mulgore Battleboars (Level 6+)",
            MinimumLevel = 6,
            MaximumLevel = 8,
            Route = WowRoutes.LEVEL_6_MULGORE_BATTLEBOARS_ROUTE,
        };

        /*
/target Mountain
        */
        public static readonly WowLocationConfiguration LEVEL_4_MULGORE_MOUNTAIN_COUGARS = new WowLocationConfiguration
        {
            Title = "Mulgore Mountain Cougars (Level 4+)",
            MinimumLevel = 4,
            MaximumLevel = 6,
            Route = WowRoutes.LEVEL_4_MULGORE_MOUNTAIN_COUGARS_ROUTE,
        };

        /*
/target Plains
        */
        public static readonly WowLocationConfiguration LEVEL_1_MULGORE_PLAINSTRIDERS = new WowLocationConfiguration
        {
            Title = "Mulgore Plainstriders (Level 1+)",
            MinimumLevel = 1,
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
            Title = "Tirisfal Zombies (Level 6+)",
            MinimumLevel = 6,
            MaximumLevel = 10,
            Route = WowRoutes.LEVEL_6_TIRISFAL_ZOMBIES_ROUTE,
        };

        /*
/target Mangy
/target Ragged
        */
        public static readonly WowLocationConfiguration LEVEL_4_TIRISFAL_ANIMALS = new WowLocationConfiguration
        {
            Title = "Tirisfal Bats and Wolves (Level 4+)",
            MinimumLevel = 4,
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
            Title = "Tirisfal Zombies (Level 1+)",
            MinimumLevel = 1,
            MaximumLevel = 4,
            Route = WowRoutes.LEVEL_1_TIRISFAL_UNDEAD_ROUTE,
        };

        #endregion

        // Every location config eligible for WowPlayer.ResolveFarmingConfigurationTask's
        // automatic startup selection (matched by player level/zone/waypoint-proximity --
        // see that method). Deliberately an explicit list rather than reflecting over all
        // static fields on this class, so a route can be pulled out of auto-selection
        // (still being tuned, temporarily unsafe, etc.) without deleting its definition
        // above -- add new routes here once they're ready to be auto-picked. Declared last
        // in the class, after every field it references -- C# runs static field
        // initializers in declaration order, so this list would silently collect nulls if
        // it were declared before them.
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
