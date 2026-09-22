namespace WoWHelper.Code
{
    public static class WowPlayerConstants
    {
        public const long TIME_BETWEEN_WORLDSTATE_UPDATES = 200; // 200ms
        public const long TIME_BETWEEN_FIND_TARGET_MILLIS = 500; // 0.5 seconds
        public const long TIME_BETWEEN_JUMPS_MILLIS = 8 * 1000; // 8 seconds
        public const long FARM_TIME_LIMIT_MILLIS = 10 * 60 * 60 * 1000; // 10 hours

        // How long WaitForWorldBuffThenLogoffTask (WowManagementTasks.cs) sits idle, polling
        // for WorldState.HasDesiredWorldBuff, before it taps strafe-left/strafe-right briefly
        // to reset WoW's AFK kick timer and goes back to waiting.
        public const long WORLD_BUFF_WAIT_MILLIS = 5 * 60 * 1000; // 5 minutes

        // How long the combat-stalemate branch in EveryWorldStateUpdateTasks
        // (WowManagementTasks.cs) waits for the login screen after pressing logout before
        // giving up and returning to the combat loop. Comfortably past WoW's 20-second
        // logout timer.
        public const long COMBAT_STALEMATE_LOGOUT_WAIT_MILLIS = 45 * 1000;

        public const int STOP_RESTING_HP_THRESHOLD = 94;
        public const int STOP_RESTING_MP_THRESHOLD = 98;

        public const int EAT_FOOD_HP_THRESHOLD = 80;
        public const int DRINK_WATER_MP_THRESHOLD = 80;

        public const int EMERGENCY_HP_THRESHOLD = 29;
        public const int PETRI_ALTF4_HP_THRESHOLD = 20;

        public const int REND_HP_THRESHOLD = 60;
        // Below this the mob's dying soon enough that a Sunder's armor reduction won't pay
        // for its rage -- same reasoning as REND_HP_THRESHOLD above.
        public const int SUNDER_ARMOR_HP_THRESHOLD = 75;

        // How many simultaneous attackers to panic at (sometimes mobs spawn tiny bugs or
        // something that will get counted) -- used to be per-WowLocationConfiguration, but
        // every route used either the same default or a value close enough to it that a
        // single shared threshold was simpler than per-route tuning.
        public const int TOO_MANY_ATTACKERS_THRESHOLD = 3;

        public const int ENGAGE_ROTATION_ATTEMPTS = 60;

        // After bailing out of an engage attempt because the target was unreachable
        // (WorldState.TargetUnreachable -- "not in line of sight" or "no path available"),
        // suppress re-acquiring any target for this
        // long. Without this, PathfindingLoopTask's TAB_TARGET/FIND_TARGET_MACRO press
        // would immediately re-select the same unreachable mob (it's still the nearest/
        // next-in-tab-order target) and the bot would spin in the same stuck loop one
        // level up. We keep walking the waypoint route during the suppression window
        // instead, so by the time targeting resumes the player has usually physically
        // moved away from whatever was blocking line of sight.
        public const long LINE_OF_SIGHT_RETARGET_SUPPRESS_MILLIS = 10 * 1000;

        // How far (in the same 0-100 map-percent units as Waypoints/MapX/MapY) the player is
        // allowed to be from the closest waypoint in the current route when starting, before
        // we conclude they started in the wrong spot and log out. Set to the single largest
        // adjacent-waypoint gap found across all of WowLocationConfigs.cs (currently
        // LEVEL_1_MULGORE_PLAINSTRIDERS, ~5.77) rather than per-config, so it's one constant --
        // deliberately the full gap, not half of it, since the player is starting manually and
        // can be given some wiggle room. If a future route's own gaps exceed this, widen it.
        public const float MAX_DISTANCE_FROM_ROUTE_WAYPOINT = 5.8f;

        // How close two Vector2s (a route waypoint and WowMerchantConfiguration.Waypoints[0])
        // need to be to count as "the same point" -- matched by position, not waypoint index,
        // since a route's Waypoints list can reach that same point via more than one
        // index/direction. Small enough to require the config author actually meant the two
        // to line up, not so tight that an extra decimal digit of precision breaks the match.
        public const float MERCHANT_BRANCH_POINT_EPSILON = 0.01f;

        // Tolerance for every leg of a merchant run except the final approach -- same as
        // WowLocationConfiguration.DistanceTolerance's own default, since these are ordinary
        // waypoint-to-waypoint hops.
        public const float MERCHANT_INTERMEDIATE_WAYPOINT_TOLERANCE = 0.2f;

        // Deliberately much tighter than the intermediate tolerance above -- the final
        // waypoint is the merchant's exact standing spot, and CTRL_TARGET_MERCHANT + a
        // center-screen right-click only lands on the vendor's model if we actually stopped
        // right next to it, not just "in the neighborhood."
        public const float MERCHANT_FINAL_WAYPOINT_TOLERANCE = 0.02f;

        // How long MerchantRunStepTask's WAITING_FOR_AUTO_SELL phase waits for the addon's
        // existing MERCHANT_SHOW auto-sell handler (YoyokazooUI.lua) to empty the bags before
        // walking back. Interruptible -- see WaitUnlessInCombatTask.
        public const long MERCHANT_AUTO_SELL_WAIT_MILLIS = 15 * 1000;
    }
}
