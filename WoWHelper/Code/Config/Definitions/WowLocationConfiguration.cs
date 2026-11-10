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

    public class WowLocationConfiguration
    {
        public enum WaypointTargetFindMethod
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

        public EngagementMethod EngageMethod { get; set; }
        public WaypointTargetFindMethod TargetFindMethod { get; set; }

        // Whether PathfindingLoopTask is allowed to leave the waypoint route to walk
        // towards a targeted mob that's out of pull range (the target-marker chase -- see
        // WalkTowardsTargetMarkerTask in WowMovementTasks.cs). On by default; turn it off
        // for routes where straying off the waypoints is dangerous -- geometry to get
        // caught on, or higher-level/elite mobs just past the safe path -- since the chase
        // walks a straight line at the target with no idea what's in between.
        public bool ChaseOutOfRangeTargets { get; set; }

        public bool AllMobsInZoneAreNatureImmune { get; set; }

        // Comma-separated /target names for the "0 Targ" target-finding macro, e.g.
        // "Mottled,Scorpid" -> "/target Mottled" + "/target Scorpid". Typed into chat as
        // "/yytarget Mottled,Scorpid" once this route is picked,
        // and the addon rewrites the macro to match. Partial-name substrings, NOT
        // ExpectedMobNames' exact roster. Null/empty leaves the macro as-is.
        public string TargetMacroMobNames { get; set; }

        // Name, waypoints, traversal method, distance tolerance, zone and minimum level -- see WowRoutes.cs.
        public WowRoute Route { get; set; }

        public WowLocationConfiguration()
        {
            TargetFindMethod = WaypointTargetFindMethod.ALTERNATE;

            EngageMethod = EngagementMethod.Charge;
            ChaseOutOfRangeTargets = true;

            AllMobsInZoneAreNatureImmune = false;
        }
    }
}
