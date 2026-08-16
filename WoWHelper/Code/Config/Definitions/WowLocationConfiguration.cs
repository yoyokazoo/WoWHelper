using System.Collections.Generic;
using System.Numerics;

namespace WoWHelper.Code.WorldState
{
    // Zones WowLocationConfigs.cs's routes are known to live in. Numeric values are
    // explicit and MUST stay in sync with the ZONE_NAME_TO_ID table in WoWFunctions.lua
    // (GetCurrentZoneId()) -- that's what packs a zone into MultiIntTwo's B channel for
    // WowWorldState.CurrentZone to decode. This is a WowHelper-defined ID scheme, not
    // Blizzard's internal map/zone ID (which doesn't fit in a single byte channel).
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

        Unknown = 255, // current zone doesn't match any known entry
    }

    public class WowLocationConfiguration
    {
        public enum WaypointTraversalMethod
        {
            CIRCULAR, // go from start -> end, then restart at start
            LINEAR // go from start -> end -> start
        }

        public enum WaypointTargetFindMethod
        {
            TAB, // only use tab, only gets a narrow cone in front
            MACRO, // only use macro, often gets stuck re-picking a bad target
            ALTERNATE // alternate between the two
        }

        public enum EngagementMethod
        {
            Charge, // charge
            Shoot, // shoot bow, shoot gun
            Spellcast // frost bolt, lightning bolt, etc.
        }

        // Human-readable name, including the minimum level, e.g. "Durotar Imps (Level 4+)".
        public string Title { get; set; }
        public int MinimumLevel { get; set; } // for validation -- character should be at least this level before starting
        public WowZone Zone { get; set; } // for validation -- character should be in this zone before starting

        public List<Vector2> Waypoints { get; set; }
        public WaypointTraversalMethod TraversalMethod { get; set; }
        public WaypointTargetFindMethod TargetFindMethod { get; set; }
        public float DistanceTolerance { get; set; }

        public EngagementMethod EngageMethod { get; set; }
        public bool UseRend { get; set; } // some mobs are immune to bleed
        public bool PreemptFear { get; set; } // if fighting mobs that Fear, start each fight with Berserker Rage
        public bool HasRunners { get; set; } // mobs in this route flee at low HP -- defaults to false, set true per-config to tweak combat logic accordingly
        public int TooManyAttackersThreshold { get; set; } // how many mobs to panic at (sometimes mobs spawn tiny bugs or something that will get counted)
        public int LogoffLevel { get; set; } // Level to log off at (mostly for low level areas, or if we're going to be learning a spell that the bot will expect to know)

        public WowLocationConfiguration()
        {
            TraversalMethod = WaypointTraversalMethod.CIRCULAR;
            TargetFindMethod = WaypointTargetFindMethod.ALTERNATE;
            DistanceTolerance = 0.2f;
            TooManyAttackersThreshold = 3;

            LogoffLevel = 61;

            // Default to Unknown, not the implicit Durotar (enum value 0) -- a config that
            // forgets to set Zone should fail loudly/obviously, not silently claim Durotar.
            Zone = WowZone.Unknown;
        }
    }
}
