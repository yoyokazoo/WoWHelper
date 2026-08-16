namespace WoWHelper.Code
{
    public static class WowPlayerConstants
    {
        public const long TIME_BETWEEN_WORLDSTATE_UPDATES = 250; // 250ms
        public const long TIME_BETWEEN_FIND_TARGET_MILLIS = 500; // 0.5 seconds
        public const long TIME_BETWEEN_JUMPS_MILLIS = 8 * 1000; // 8 seconds
        public const long FARM_TIME_LIMIT_MILLIS = 10 * 60 * 60 * 1000; // 10 hours

        public const int STOP_RESTING_HP_THRESHOLD = 94;
        public const int STOP_RESTING_MP_THRESHOLD = 98;

        public const int EAT_FOOD_HP_THRESHOLD = 90;
        public const int DRINK_WATER_MP_THRESHOLD = 95;
        
        public const int OH_SHIT_RETAL_HP_THRESHOLD = 35;
        public const int PETRI_ALTF4_HP_THRESHOLD = 20;

        public const int REND_HP_THRESHOLD = 75;

        public const int ENGAGE_ROTATION_ATTEMPTS = 60;

        // How far (in the same 0-100 map-percent units as Waypoints/MapX/MapY) the player is
        // allowed to be from the closest waypoint in the current route when starting, before
        // we conclude they started in the wrong spot and log out. Set to the single largest
        // adjacent-waypoint gap found across all of WowLocationConfigs.cs (currently
        // LEVEL_1_MULGORE_PLAINSTRIDERS, ~5.77) rather than per-config, so it's one constant --
        // deliberately the full gap, not half of it, since the player is starting manually and
        // can be given some wiggle room. If a future route's own gaps exceed this, widen it.
        public const float MAX_DISTANCE_FROM_ROUTE_WAYPOINT = 5.8f;
    }
}
