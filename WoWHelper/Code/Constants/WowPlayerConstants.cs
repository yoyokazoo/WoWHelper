using System.Drawing;

namespace WoWHelper.Code
{
    // Constants that don't relate to combat/gameplay logic but more about the operation of the bot
    public static partial class WowConstants
    {
        public const long TIME_BETWEEN_WORLDSTATE_UPDATES = 200;
        public const long TIME_BETWEEN_FIND_TARGET_MILLIS = 800;
        public const long TIME_BETWEEN_JUMPS_MILLIS = 8 * 1000;
        public const long FARM_TIME_LIMIT_MILLIS = 10 * 60 * 60 * 1000; // 10 hours

        // Wiggle so we don't get AFK DC'ed
        public const long WORLD_BUFF_WAIT_MILLIS = 5 * 60 * 1000; // 5 minutes

        // How long the combat-stalemate branch in EveryWorldStateUpdateTasks
        // (WowManagementTasks.cs) waits for the login screen after pressing logout before
        // giving up and returning to the combat loop. Comfortably past WoW's 20-second
        // logout timer.
        public const long COMBAT_STALEMATE_LOGOUT_WAIT_MILLIS = 45 * 1000;

        // Number of attackers to start clicking our "oh shit" buttons and logging out since we should never pull this many
        // See ATTACKER_COUNT_IGNORED_MOB_NAMES to ignore spawns that we don't want to contribute to this count,
        // like the worms that spawn from zombies when they're killed
        public const int TOO_MANY_ATTACKERS_THRESHOLD = 3;

        // Number of times we attempt to engage before logging out
        public const int ENGAGE_ROTATION_ATTEMPTS = 60;

        // If we keep trying and failing to engage a target, back off for this many seconds to walk around before trying to engage again
        public const long LINE_OF_SIGHT_RETARGET_SUPPRESS_MILLIS = 10 * 1000;

        // Max distance away from a known waypoint to consider running to safely
        public const float MAX_DISTANCE_FROM_ROUTE_WAYPOINT = 3.0f;

        // How long to spend trying to run across a zone line before giving up
        public const long ZONE_CROSSING_TIMEOUT_MILLIS = 10 * 1000;

        // Need to stand right on top of the NPC
        public const float NPC_FINAL_WAYPOINT_TOLERANCE = 0.02f;

        // How long to wait for auto-sell/train/repair
        public const long NPC_INTERACT_WAIT_MILLIS = 15 * 1000;

        // Pink Target Marker box on nameplates
        public static readonly Color TARGET_MARKER_COLOR = Color.FromArgb(255, 0, 255);

    }
}
