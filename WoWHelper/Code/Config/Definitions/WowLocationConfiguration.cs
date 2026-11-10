using System.Collections.Generic;

namespace WoWHelper.Code.WorldState
{
    public enum WowZone
    {
        Durotar = 0,
        Mulgore = 1,
        TheBarrens = 2,
        Ashenvale = 3,
        StonetalonMountains = 4,
        HillsbradFoothills = 5,
        ThousandNeedles = 6,
        Desolace = 7,
        Tanaris = 8,
        Feralas = 9,
        Felwood = 10,
        WesternPlaguelands = 11,
        Silithus = 12,
        Azshara = 13,
        Winterspring = 14,
        TirisfalGlades = 15,
        Orgrimmar = 16,

        Unknown = 255, // current zone doesn't match any known entry
    }

    public enum TargetFindMethod
    {
        TAB, // only use tab, only gets a narrow cone in front
        MACRO, // only use macro, often gets stuck re-picking a bad target
        ALTERNATE // alternate between the two
    }

    public enum EngagementMethod
    {
        Charge, // melee gap-closer (Warrior only)
        Pull // shoot bow/gun (Warrior) or cast a pull spell -- lightning bolt, shadow bolt, etc. (Shaman/Warlock)
    }

    public class WowLocationConfiguration
    {
        public EngagementMethod EngageMethod { get; set; }
        public TargetFindMethod TargetFindMethod { get; set; }

        // Whether or not we should chase off-screen pink squares
        public bool ChaseOutOfRangeTargets { get; set; }

        // which mobs to add to /target
        public string TargetMacroMobNames { get; set; }

        // Name, waypoints, traversal method, distance tolerance, zone and minimum level -- see WowRoutes.cs.
        public WowRoute Route { get; set; }

        public bool AllMobsInZoneAreNatureImmune { get; set; }

        public WowLocationConfiguration()
        {
            EngageMethod = EngagementMethod.Charge;
            TargetFindMethod = TargetFindMethod.ALTERNATE;
            
            ChaseOutOfRangeTargets = true;

            AllMobsInZoneAreNatureImmune = false;
        }
    }
}
