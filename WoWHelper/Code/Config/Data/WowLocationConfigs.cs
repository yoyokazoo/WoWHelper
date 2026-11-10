using System.Collections.Generic;
using System.Numerics;
using WoWHelper.Code.Gameplay;
using static WoWHelper.Code.WorldState.WowLocationConfiguration;

namespace WoWHelper.Code.WorldState
{
    public static class WowLocationConfigs
    {
        #region Levels 50-60

        public static readonly WowLocationConfiguration LEVEL_58_SILITHUS_RUMBLERS = new WowLocationConfiguration
        {
            Route = WowRoutes.LEVEL_58_SILITHUS_RUMBLERS_ROUTE,
            TargetMacroMobNames = "Desert Rumbler",
            AllMobsInZoneAreNatureImmune = true,
        };

        public static readonly WowLocationConfiguration LEVEL_57_WINTERSPRING_YETIS = new WowLocationConfiguration
        {
            Route = WowRoutes.LEVEL_57_WINTERSPRING_YETIS_ROUTE,
            TargetMacroMobNames = "Ice Thistle",
        };

        public static readonly WowLocationConfiguration LEVEL_56_DALTONS_TEARS_FRONTSIDE_WPL = new WowLocationConfiguration
        {
            Route = WowRoutes.LEVEL_56_DALTONS_TEARS_FRONTSIDE_WPL_ROUTE,
            TargetMacroMobNames = "Blighted,Rotting",
            EngageMethod = EngagementMethod.Pull,
        };

        // Water elementals spawn here during invasion, bot will log off if they are seen.
        // Supposedly they only show up once every 48 hours or so
        public static readonly WowLocationConfiguration LEVEL_55_WINTERSPRING_LAKE = new WowLocationConfiguration
        {
            Route = WowRoutes.LEVEL_55_WINTERSPRING_LAKE_ROUTE,
            TargetMacroMobNames = "Suffering,Anguished,Watery",
        };

        // Legashi Hellcallers stand still and cast.  Only use this camp if you have handling
        // for LongRangeCasters set up (Earth shock, etc.)
        // FWIW I was getting stuck on geometry a lot at this specific one, so probably only run supervised
        public static readonly WowLocationConfiguration LEVEL_53_ASZHARA_SATYR_CIRCLE = new WowLocationConfiguration
        {
            Route = WowRoutes.LEVEL_53_ASZHARA_SATYR_CIRCLE_ROUTE,
            TargetMacroMobNames = "Legashi",
        };

        public static readonly WowLocationConfiguration LEVEL_53_NORTH_FELWOOD = new WowLocationConfiguration
        {
            Route = WowRoutes.LEVEL_53_NORTH_FELWOOD_ROUTE,
            TargetMacroMobNames = "Ironbeak,Angerclaw,Warpwood,Felpaw",
            ChaseOutOfRangeTargets = false, // Tall cliffs nearby, can't risk it
        };

        public static readonly WowLocationConfiguration LEVEL_50_FELWOOD_SOUTH = new WowLocationConfiguration
        {
            Route = WowRoutes.LEVEL_50_FELWOOD_SOUTH_ROUTE,
            TargetMacroMobNames = "Ironbeak,Angerclaw,Warpwood,Felpaw",
        };

        #endregion

        #region Levels 40-50

        public static readonly WowLocationConfiguration LEVEL_46_FERALAS_HIPPOGRYPHS = new WowLocationConfiguration
        {
            Route = WowRoutes.LEVEL_46_FERALAS_HIPPOGRYPHS_ROUTE,
            TargetMacroMobNames = "Ironfur,Cursed,Wandering,Frayfeather",
        };

        public static readonly WowLocationConfiguration LEVEL_41_TANARIS_TURTLES = new WowLocationConfiguration
        {
            Route = WowRoutes.LEVEL_41_TANARIS_TURTLES_ROUTE,
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

        public static readonly WowLocationConfiguration LEVEL_36_KODO_GRAVEYARD = new WowLocationConfiguration
        {
            Route = WowRoutes.LEVEL_36_KODO_GRAVEYARD_ROUTE,
            TargetMacroMobNames = "Aged,Dying,Ancient,Carrion",
            EngageMethod = EngagementMethod.Pull,
        };

        public static readonly WowLocationConfiguration LEVEL_34_SHIMMERING_FLATS = new WowLocationConfiguration
        {
            Route = WowRoutes.LEVEL_34_SHIMMERING_FLATS_ROUTE,
            TargetMacroMobNames = "Scorpid,Swirling,Saltstone,Sparkleshell,Ironeye",
            ChaseOutOfRangeTargets = false, // the ship wreck here is easy to get stuck on
        };

        #endregion

        #region Levels 20-30

        public static readonly WowLocationConfiguration LEVEL_29_HILLSBRAD_RIVER = new WowLocationConfiguration
        {
            Route = WowRoutes.LEVEL_29_HILLSBRAD_RIVER_ROUTE,
            TargetMacroMobNames = "Snapjaw",
            TargetFindMethod = TargetFindMethod.MACRO,
            ChaseOutOfRangeTargets = false, // elite dragon nearby if we get dragged too far
        };

        public static readonly WowLocationConfiguration LEVEL_26_STONETALON_CHARRED_FOREST = new WowLocationConfiguration
        {
            Route = WowRoutes.LEVEL_26_STONETALON_CHARRED_FOREST_ROUTE,
            TargetMacroMobNames = "Rogue,Blackened,Bloodfury",
        };

        public static readonly WowLocationConfiguration LEVEL_23_STONETALON = new WowLocationConfiguration
        {
            Route = WowRoutes.LEVEL_23_STONETALON_ROUTE,
            TargetMacroMobNames = "Antlered,Sap",
        };

        public static readonly WowLocationConfiguration LEVEL_20_ZORAMGAR = new WowLocationConfiguration
        {
            Route = WowRoutes.LEVEL_20_ZORAMGAR_ROUTE,
            TargetMacroMobNames = "Wild,Ghostpaw",
        };

        #endregion

        #region Levels 10-20

        public static readonly WowLocationConfiguration LEVEL_17_NORTHERN_BARRENS = new WowLocationConfiguration
        {
            Route = WowRoutes.LEVEL_17_NORTHERN_BARRENS_ROUTE,
            TargetMacroMobNames = "Zhevra,Savannah,Hecklefang,Ornery,Sunscale",
        };

        public static readonly WowLocationConfiguration LEVEL_13_BARRENS_ENTRANCE = new WowLocationConfiguration
        {
            Route = WowRoutes.LEVEL_13_BARRENS_ENTRANCE_ROUTE,
            TargetMacroMobNames = "Fleeting,Zhevra,Sunscale",
        };

        public static readonly WowLocationConfiguration LEVEL_11_DUROTAR_COAST = new WowLocationConfiguration
        {
            Route = WowRoutes.LEVEL_11_DUROTAR_COAST_ROUTE,
            TargetMacroMobNames = "Venom,Elder,Blood,Corrupted",
        };

        #endregion

        #region Levels 1-10 Durotar

        public static readonly WowLocationConfiguration LEVEL_9_DUROTAR_SKULL_ROCK_COAST = new WowLocationConfiguration
        {
            Route = WowRoutes.LEVEL_9_DUROTAR_SKULL_ROCK_COAST_ROUTE,
            TargetMacroMobNames = "Elder,Venom,Armored,Blood",
            ChaseOutOfRangeTargets = true,
        };

        public static readonly WowLocationConfiguration LEVEL_6_DUROTAR_BOAR_RAZOR_HILL_LOOP = new WowLocationConfiguration
        {
            Route = WowRoutes.LEVEL_6_DUROTAR_BOAR_RAZOR_HILL_LOOP_ROUTE,
            TargetMacroMobNames = "Dire,Clattering",
            ChaseOutOfRangeTargets = true,
        };

        public static readonly WowLocationConfiguration LEVEL_4_DUROTAR_IMPS = new WowLocationConfiguration
        {
            Route = WowRoutes.LEVEL_4_DUROTAR_IMPS_ROUTE,
            TargetMacroMobNames = "Scorpid,Vile",
            ChaseOutOfRangeTargets = true,
        };

        public static readonly WowLocationConfiguration LEVEL_4_DUROTAR_SCORPS = new WowLocationConfiguration
        {
            Route = WowRoutes.LEVEL_4_DUROTAR_SCORPS_ROUTE,
            TargetMacroMobNames = "Scorpid",
        };

        public static readonly WowLocationConfiguration LEVEL_1_DUROTAR_BOARS_AND_SCORPS = new WowLocationConfiguration
        {
            Route = WowRoutes.LEVEL_1_DUROTAR_BOARS_AND_SCORPS_ROUTE,
            TargetMacroMobNames = "Mottled,Scorpid",
        };

        #endregion

        #region Levels 1-10 Mulgore

        public static readonly WowLocationConfiguration LEVEL_10_MULGORE_MIXED_BEASTS = new WowLocationConfiguration
        {
            Route = WowRoutes.LEVEL_10_MULGORE_MIXED_BEASTS_ROUTE,
            TargetMacroMobNames = "Swoop,Flatland,Prairie",
            TargetFindMethod = TargetFindMethod.MACRO, // Kodo packs wandering around
        };

        public static readonly WowLocationConfiguration LEVEL_8_MULGORE_MIXED_BEASTS = new WowLocationConfiguration
        {
            Route = WowRoutes.LEVEL_8_MULGORE_MIXED_BEASTS_ROUTE,
            TargetFindMethod = TargetFindMethod.MACRO, // Kodo packs wandering around
        };

        public static readonly WowLocationConfiguration LEVEL_6_MULGORE_BATTLEBOARS = new WowLocationConfiguration
        {
            Route = WowRoutes.LEVEL_6_MULGORE_BATTLEBOARS_ROUTE,
            TargetMacroMobNames = "Battleboar",
        };

        public static readonly WowLocationConfiguration LEVEL_4_MULGORE_MOUNTAIN_COUGARS = new WowLocationConfiguration
        {
            Route = WowRoutes.LEVEL_4_MULGORE_MOUNTAIN_COUGARS_ROUTE,
            TargetMacroMobNames = "Mountain",
        };

        public static readonly WowLocationConfiguration LEVEL_1_MULGORE_PLAINSTRIDERS = new WowLocationConfiguration
        {
            Route = WowRoutes.LEVEL_1_MULGORE_PLAINSTRIDERS_ROUTE,
            TargetMacroMobNames = "Plains",
        };

        #endregion

        #region Levels 1-10 Tirisfal

        public static readonly WowLocationConfiguration LEVEL_6_TIRISFAL_ZOMBIES = new WowLocationConfiguration
        {
            Route = WowRoutes.LEVEL_6_TIRISFAL_ZOMBIES_ROUTE,
            TargetMacroMobNames = "Decrepit,Greater,Rotting,Ravaged",
        };

        public static readonly WowLocationConfiguration LEVEL_4_TIRISFAL_ANIMALS = new WowLocationConfiguration
        {
            Route = WowRoutes.LEVEL_4_TIRISFAL_ANIMALS_ROUTE,
            TargetMacroMobNames = "Mangy,Ragged",
        };

        public static readonly WowLocationConfiguration LEVEL_1_TIRISFAL_UNDEAD = new WowLocationConfiguration
        {
            Route = WowRoutes.LEVEL_1_TIRISFAL_UNDEAD_ROUTE,
            TargetMacroMobNames = "Young,Duskbat,Mindless,Wretched,Rattlecage",
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
        };
    }
}
