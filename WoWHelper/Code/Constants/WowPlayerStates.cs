namespace WoWHelper.Code
{
    public static class WowPlayerStates
    {
        // Outside of the game state, AKA getting focus etc.
        public enum PlayerMetaState
        {
            WAITING_TO_FOCUS_ON_WINDOW,
            EXECUTING_GOAL,
            EXITING
        }

        // Which goal the player is currently trying to execute
        public enum PlayerGoal
        {
            NONE,
            FIGHT,
            FIND_ENEMY_TARGET,
            LOG_OUT,
            REPAIR,
            SELL,
            TRAIN,
            TRAVEL,
            SET_HEARTH,
            RECOVER_FROM_BATTLE
        }

        // Where we are in battle recovery
        public enum BattleRecoveryState
        {
            LOOT_ATTEMPT_ONE,
            LOOT_ATTEMPT_TWO,
            SKIN_ATTEMPT_ONE,
            SKIN_ATTEMPT_TWO,
            STARTING_RECOVERY,
            WAITING_FOR_RECOVERY,
        }

        // Movement substate to try and recover from being stuck
        public enum StuckRecoveryState
        {
            NOT_STUCK,
            JUMPED,
            WIGGLED_LEFT,
            WIGGLED_RIGHT,
            GAVE_UP
        }

        // >_>
        public enum LogoutState
        {
            STARTING_LOGOUT,
            WAITING_FOR_LOGOUT,
        }

        // Run around and find enemy to fight
        public enum FindEnemyTargetState
        {
            PICK_FARMING_LOCATION_CONFIG,
            BUILD_ROUTE_TO_FARMING_LOCATION,
            NAVIGATE_TO_FARMING_LOCATION,
            PICK_NEXT_WAYPOINT,
            FACE_WAYPOINT,
            WALK_TO_WAYPOINT,
            WALK_TO_TARGETED_ENEMY,
            START_ENGAGE_TARGETED_ENEMY,
            EXECUTE_ENGAGE_TARGETED_ENEMY
        }

        // Navigate from start of route to end of route
        public enum NavigateToEndOfCurrentRouteState
        {
            PICK_NEXT_WAYPOINT,
            FACE_WAYPOINT,
            WALK_TO_WAYPOINT,
            CROSSING_ZONE,
            CROSSED_ZONE,
            ARRIVED_AT_END_OF_ROUTE
        }

        // Find, move to, and interact with a merchant/trainer/etc.
        public enum InteractWithNPCState
        {
            PICK_NPC_TO_INTERACT_WITH,
            BUILD_ROUTE_TO_NPC,
            NAVIGATE_ROUTE_TO_NPC,
            INTERACT_WITH_NPC,
            DONE_INTERACTING_WITH_NPC
        }
    }
}
