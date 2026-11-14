using System.Collections.Generic;
using System.Numerics;
using WoWHelper.Code.Gameplay;
using static WoWHelper.Code.WorldState.WowFarmingConfiguration;

namespace WoWHelper.Code.WorldState
{
    public static class WowFarmingConfigs
    {
        #region Levels 50-60

        public static readonly WowFarmingConfiguration LEVEL_58_SILITHUS_RUMBLERS = new WowFarmingConfiguration
        {
            Route = WowFarmingRoutes.LEVEL_58_SILITHUS_RUMBLERS_ROUTE,
            TargetMacroMobNames = "Desert Rumbler",
            AllMobsInZoneAreNatureImmune = true,
        };

        public static readonly WowFarmingConfiguration LEVEL_57_WINTERSPRING_YETIS = new WowFarmingConfiguration
        {
            Route = WowFarmingRoutes.LEVEL_57_WINTERSPRING_YETIS_ROUTE,
            TargetMacroMobNames = "Ice Thistle",
        };

        public static readonly WowFarmingConfiguration LEVEL_56_DALTONS_TEARS_FRONTSIDE_WPL = new WowFarmingConfiguration
        {
            Route = WowFarmingRoutes.LEVEL_56_DALTONS_TEARS_FRONTSIDE_WPL_ROUTE,
            TargetMacroMobNames = "Blighted,Rotting",
            EngageMethod = EngagementMethod.Pull,
        };

        // Water elementals spawn here during invasion, bot will log off if they are seen.
        // Supposedly they only show up once every 48 hours or so
        public static readonly WowFarmingConfiguration LEVEL_55_WINTERSPRING_LAKE = new WowFarmingConfiguration
        {
            Route = WowFarmingRoutes.LEVEL_55_WINTERSPRING_LAKE_ROUTE,
            TargetMacroMobNames = "Suffering,Anguished,Watery",
        };

        // Legashi Hellcallers stand still and cast.  Only use this camp if you have handling
        // for LongRangeCasters set up (Earth shock, etc.)
        // FWIW I was getting stuck on geometry a lot at this specific one, so probably only run supervised
        public static readonly WowFarmingConfiguration LEVEL_53_ASZHARA_SATYR_CIRCLE = new WowFarmingConfiguration
        {
            Route = WowFarmingRoutes.LEVEL_53_ASZHARA_SATYR_CIRCLE_ROUTE,
            TargetMacroMobNames = "Legashi",
        };

        public static readonly WowFarmingConfiguration LEVEL_53_NORTH_FELWOOD = new WowFarmingConfiguration
        {
            Route = WowFarmingRoutes.LEVEL_53_NORTH_FELWOOD_ROUTE,
            TargetMacroMobNames = "Ironbeak,Angerclaw,Warpwood,Felpaw",
            ChaseOutOfRangeTargets = false, // Tall cliffs nearby, can't risk it
        };

        public static readonly WowFarmingConfiguration LEVEL_50_FELWOOD_SOUTH = new WowFarmingConfiguration
        {
            Route = WowFarmingRoutes.LEVEL_50_FELWOOD_SOUTH_ROUTE,
            TargetMacroMobNames = "Ironbeak,Angerclaw,Warpwood,Felpaw",
        };

        #endregion

        #region Levels 40-50

        public static readonly WowFarmingConfiguration LEVEL_46_FERALAS_HIPPOGRYPHS = new WowFarmingConfiguration
        {
            Route = WowFarmingRoutes.LEVEL_46_FERALAS_HIPPOGRYPHS_ROUTE,
            TargetMacroMobNames = "Ironfur,Cursed,Wandering,Frayfeather",
        };

        public static readonly WowFarmingConfiguration LEVEL_41_TANARIS_TURTLES = new WowFarmingConfiguration
        {
            Route = WowFarmingRoutes.LEVEL_41_TANARIS_TURTLES_ROUTE,
            TargetMacroMobNames = "Steeljaw",

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

        public static readonly WowFarmingConfiguration LEVEL_36_KODO_GRAVEYARD = new WowFarmingConfiguration
        {
            Route = WowFarmingRoutes.LEVEL_36_KODO_GRAVEYARD_ROUTE,
            TargetMacroMobNames = "Aged,Dying,Ancient,Carrion",
            EngageMethod = EngagementMethod.Pull,
        };

        public static readonly WowFarmingConfiguration LEVEL_34_SHIMMERING_FLATS = new WowFarmingConfiguration
        {
            Route = WowFarmingRoutes.LEVEL_34_SHIMMERING_FLATS_ROUTE,
            TargetMacroMobNames = "Scorpid,Swirling,Saltstone,Sparkleshell,Ironeye",
            ChaseOutOfRangeTargets = false, // the ship wreck here is easy to get stuck on
        };

        #endregion

        #region Levels 20-30

        public static readonly WowFarmingConfiguration LEVEL_29_HILLSBRAD_RIVER = new WowFarmingConfiguration
        {
            Route = WowFarmingRoutes.LEVEL_29_HILLSBRAD_RIVER_ROUTE,
            TargetMacroMobNames = "Snapjaw",
            TargetFindMethod = TargetFindMethod.MACRO,
            ChaseOutOfRangeTargets = false, // elite dragon nearby if we get dragged too far
        };

        public static readonly WowFarmingConfiguration LEVEL_26_STONETALON_CHARRED_FOREST = new WowFarmingConfiguration
        {
            Route = WowFarmingRoutes.LEVEL_26_STONETALON_CHARRED_FOREST_ROUTE,
            TargetMacroMobNames = "Rogue,Blackened,Bloodfury",
        };

        public static readonly WowFarmingConfiguration LEVEL_23_STONETALON = new WowFarmingConfiguration
        {
            Route = WowFarmingRoutes.LEVEL_23_STONETALON_ROUTE,
            TargetMacroMobNames = "Antlered,Sap",
        };

        public static readonly WowFarmingConfiguration LEVEL_20_ZORAMGAR = new WowFarmingConfiguration
        {
            Route = WowFarmingRoutes.LEVEL_20_ZORAMGAR_ROUTE,
            TargetMacroMobNames = "Wild,Ghostpaw",
        };

        #endregion

        #region Levels 10-20

        public static readonly WowFarmingConfiguration LEVEL_17_NORTHERN_BARRENS = new WowFarmingConfiguration
        {
            Route = WowFarmingRoutes.LEVEL_17_NORTHERN_BARRENS_ROUTE,
            TargetMacroMobNames = "Zhevra,Savannah,Hecklefang,Ornery,Sunscale",
        };

        public static readonly WowFarmingConfiguration LEVEL_13_BARRENS_ENTRANCE = new WowFarmingConfiguration
        {
            Route = WowFarmingRoutes.LEVEL_13_BARRENS_ENTRANCE_ROUTE,
            TargetMacroMobNames = "Fleeting,Zhevra,Sunscale",
        };

        public static readonly WowFarmingConfiguration LEVEL_11_DUROTAR_COAST = new WowFarmingConfiguration
        {
            Route = WowFarmingRoutes.LEVEL_11_DUROTAR_COAST_ROUTE,
            TargetMacroMobNames = "Venom,Elder,Blood,Corrupted",
        };

        #endregion

        #region Levels 1-10 Durotar

        public static readonly WowFarmingConfiguration LEVEL_9_DUROTAR_SKULL_ROCK_COAST = new WowFarmingConfiguration
        {
            Route = WowFarmingRoutes.LEVEL_9_DUROTAR_SKULL_ROCK_COAST_ROUTE,
            TargetMacroMobNames = "Elder,Venom,Armored,Blood",
            ChaseOutOfRangeTargets = true,
        };

        public static readonly WowFarmingConfiguration LEVEL_6_DUROTAR_BOAR_RAZOR_HILL_LOOP = new WowFarmingConfiguration
        {
            Route = WowFarmingRoutes.LEVEL_6_DUROTAR_BOAR_RAZOR_HILL_LOOP_ROUTE,
            TargetMacroMobNames = "Dire,Clattering",
            ChaseOutOfRangeTargets = true,
        };

        public static readonly WowFarmingConfiguration LEVEL_4_DUROTAR_IMPS = new WowFarmingConfiguration
        {
            Route = WowFarmingRoutes.LEVEL_4_DUROTAR_IMPS_ROUTE,
            TargetMacroMobNames = "Scorpid,Vile",
            ChaseOutOfRangeTargets = true,
        };

        public static readonly WowFarmingConfiguration LEVEL_4_DUROTAR_SCORPS = new WowFarmingConfiguration
        {
            Route = WowFarmingRoutes.LEVEL_4_DUROTAR_SCORPS_ROUTE,
            TargetMacroMobNames = "Scorpid",
        };

        public static readonly WowFarmingConfiguration LEVEL_1_DUROTAR_BOARS_AND_SCORPS = new WowFarmingConfiguration
        {
            Route = WowFarmingRoutes.LEVEL_1_DUROTAR_BOARS_AND_SCORPS_ROUTE,
            TargetMacroMobNames = "Mottled,Scorpid",
        };

        #endregion

        #region Levels 1-10 Mulgore

        public static readonly WowFarmingConfiguration LEVEL_10_MULGORE_MIXED_BEASTS = new WowFarmingConfiguration
        {
            Route = WowFarmingRoutes.LEVEL_10_MULGORE_MIXED_BEASTS_ROUTE,
            TargetMacroMobNames = "Swoop,Flatland,Prairie",
            TargetFindMethod = TargetFindMethod.MACRO, // Kodo packs wandering around
        };

        public static readonly WowFarmingConfiguration LEVEL_8_MULGORE_MIXED_BEASTS = new WowFarmingConfiguration
        {
            Route = WowFarmingRoutes.LEVEL_8_MULGORE_MIXED_BEASTS_ROUTE,
            TargetFindMethod = TargetFindMethod.MACRO, // Kodo packs wandering around
        };

        public static readonly WowFarmingConfiguration LEVEL_6_MULGORE_BATTLEBOARS = new WowFarmingConfiguration
        {
            Route = WowFarmingRoutes.LEVEL_6_MULGORE_BATTLEBOARS_ROUTE,
            TargetMacroMobNames = "Battleboar",
        };

        public static readonly WowFarmingConfiguration LEVEL_4_MULGORE_MOUNTAIN_COUGARS = new WowFarmingConfiguration
        {
            Route = WowFarmingRoutes.LEVEL_4_MULGORE_MOUNTAIN_COUGARS_ROUTE,
            TargetMacroMobNames = "Mountain",
        };

        public static readonly WowFarmingConfiguration LEVEL_1_MULGORE_PLAINSTRIDERS = new WowFarmingConfiguration
        {
            Route = WowFarmingRoutes.LEVEL_1_MULGORE_PLAINSTRIDERS_ROUTE,
            TargetMacroMobNames = "Plains",
        };

        #endregion

        #region Levels 1-10 Tirisfal

        public static readonly WowFarmingConfiguration LEVEL_6_TIRISFAL_ZOMBIES = new WowFarmingConfiguration
        {
            Route = WowFarmingRoutes.LEVEL_6_TIRISFAL_ZOMBIES_ROUTE,
            TargetMacroMobNames = "Decrepit,Greater,Rotting,Ravaged",
        };

        public static readonly WowFarmingConfiguration LEVEL_4_TIRISFAL_ANIMALS = new WowFarmingConfiguration
        {
            Route = WowFarmingRoutes.LEVEL_4_TIRISFAL_ANIMALS_ROUTE,
            TargetMacroMobNames = "Mangy,Ragged",
        };

        public static readonly WowFarmingConfiguration LEVEL_1_TIRISFAL_UNDEAD = new WowFarmingConfiguration
        {
            Route = WowFarmingRoutes.LEVEL_1_TIRISFAL_UNDEAD_ROUTE,
            TargetMacroMobNames = "Young,Duskbat,Mindless,Wretched,Rattlecage",
        };

        #endregion

        #region Levels 1-10 Teldrassil

        public static readonly WowFarmingConfiguration LEVEL_1_TELDRASSIL_ANIMALS = new WowFarmingConfiguration
        {
            Route = WowFarmingRoutes.LEVEL_1_TELDRASSIL_ANIMALS_ROUTE,
            TargetMacroMobNames = "Young,Grellkin,Mangy,Thistle",
        };

        public static readonly WowFarmingConfiguration LEVEL_4_TELDRASSIL_ANIMALS = new WowFarmingConfiguration
        {
            Route = WowFarmingRoutes.LEVEL_1_TELDRASSIL_ANIMALS_ROUTE,
            TargetMacroMobNames = "Strigid,Nightsaber",
        };

        #endregion

        public static readonly List<WowFarmingConfiguration> ALL_LOCATIONS = new List<WowFarmingConfiguration>
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
            LEVEL_10_MULGORE_MIXED_BEASTS,
            LEVEL_8_MULGORE_MIXED_BEASTS,
            LEVEL_6_MULGORE_BATTLEBOARS,
            LEVEL_4_MULGORE_MOUNTAIN_COUGARS,
            LEVEL_4_TIRISFAL_ANIMALS,
            LEVEL_4_DUROTAR_IMPS,
            LEVEL_4_DUROTAR_SCORPS,
            LEVEL_1_MULGORE_PLAINSTRIDERS,
            LEVEL_1_DUROTAR_BOARS_AND_SCORPS,
            LEVEL_1_TIRISFAL_UNDEAD,
            LEVEL_1_TELDRASSIL_ANIMALS,
            LEVEL_4_TELDRASSIL_ANIMALS
        };
    }
}
