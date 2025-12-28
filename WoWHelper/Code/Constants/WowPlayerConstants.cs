namespace WoWHelper.Code
{
    public static class WowPlayerConstants
    {
        public const long TIME_BETWEEN_WORLDSTATE_UPDATES = 250; // 250ms
        public const long TIME_BETWEEN_FIND_TARGET_MILLIS = 1 * 1000; // 1 seconds
        public const long TIME_BETWEEN_JUMPS_MILLIS = 8 * 1000; // 8 seconds
        public const long FARM_TIME_LIMIT_MILLIS = 7 * 60 * 60 * 1000; // 7 hours

        public const int STOP_RESTING_HP_THRESHOLD = 94;
        public const int EAT_FOOD_HP_THRESHOLD = 90;
        public const int OH_SHIT_RETAL_HP_THRESHOLD = 35;
        public const int PETRI_ALTF4_HP_THRESHOLD = 20;

        public const int REND_HP_THRESHOLD = 75;

        public const int ENGAGE_ROTATION_ATTEMPTS = 30;
    }
}
