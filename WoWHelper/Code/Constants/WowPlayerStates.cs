namespace WoWHelper.Code
{
    public static class WowPlayerStates
    {
        public enum PlayerMetaState
        {
            WAITING_TO_FOCUS_ON_WINDOW,
            RESOLVE_FARMING_CONFIGURATION,
            RUNNING
        }

        public enum PlayerState
        {
            WAITING_TO_FOCUS_ON_WINDOW,
            RESOLVE_FARMING_CONFIGURATION,
            START_BATTLE_READY_RECOVERY,
            WAIT_UNTIL_BATTLE_READY,
            CHECK_FOR_VALID_TARGET,
            INITIATE_ENGAGE_TARGET,
            CONTINUE_TO_TRY_TO_ENGAGE,
            IN_CORE_COMBAT_LOOP,
            TARGET_DEFEATED,
            LOOT_ATTEMPT_TWO,
            SKIN_ATTEMPT,
            SKIN_ATTEMPT_TWO,

            WALK_WAYPOINTS,

            CHECK_FOR_LOGOUT,
            START_LOGGING_OUT,
            WAITING_TO_LOG_OUT,
            LOGGED_OUT,
            EXITING_CORE_GAMEPLAY_LOOP,
        }

        public enum PathfindingState
        {
            PICKING_NEXT_WAYPOINT,
            MOVING_TOWARDS_WAYPOINT,
            ARRIVED_AT_WAYPOINT
        }

        public enum TradeState
        {
            WAITING_FOR_TRADE_WINDOW,
            CHECKING_BLOCKLIST,
            POPULATING_TRADE_WINDOW,
            WAITING_FOR_TRADE_ACCEPTANCE,
            CONFIRMING_TRADE,
            CANCELING_TRADE
        }

        // Drives MerchantRunStepTask (WowMovementTasks.cs) -- the branch-off-to-sell detour
        // hijacked into PathfindingLoopTask's loop while WowPlayer.IsOnMerchantRun is true.
        // See WowLocationConfiguration.MerchantConfig for the branch-off contract.
        public enum MerchantRunPhase
        {
            WALKING_TO_MERCHANT,
            INTERACTING_WITH_MERCHANT,
            WAITING_FOR_AUTO_SELL,
            WALKING_BACK_TO_ROUTE
        }
    }
}
