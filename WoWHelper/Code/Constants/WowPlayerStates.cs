namespace WoWHelper.Code
{
    public static class WowPlayerStates
    {
        public enum PlayerState
        {
            WAITING_TO_FOCUS,
            FOCUSED_ON_WINDOW,
            WAIT_UNTIL_BATTLE_READY,
            CHECK_FOR_VALID_TARGET,
            TRY_TO_CHARGE_TARGET,
            IN_CORE_COMBAT_LOOP,
            TARGET_DEFEATED,

            WALK_WAYPOINTS,

            ESC_KEY_SEEN,
            CHECK_FOR_LOGOUT,
            LOGGING_OUT,
            LOGGED_OUT,
            EXITING_CORE_GAMEPLAY_LOOP,
        }

        public enum PathfindingState
        {
            PICKING_NEXT_WAYPOINT,
            MOVING_TOWARDS_WAYPOINT,
            ARRIVED_AT_WAYPOINT
        }
    }
}
