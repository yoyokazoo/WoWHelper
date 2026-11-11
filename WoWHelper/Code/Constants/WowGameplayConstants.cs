namespace WoWHelper.Code.WorldState
{
    public class WowGameplayConstants
    {
        // Warrior costs
        public const int HEROIC_STRIKE_RAGE_COST = 15;
        public const int BATTLE_SHOUT_RAGE_COST = 10;
        public const int REND_RAGE_COST = 10;
        public const int OVERPOWER_RAGE_COST = 5;
        public const int EXECUTE_RAGE_COST = 15;
        public const int SUNDER_ARMOR_RAGE_COST = 15;

        public const int SWEEPING_STRIKES_RAGE_COST = 30;
        public const int WHIRLWIND_RAGE_COST = 25;
        public const int CLEAVE_RAGE_COST = 20;

        public const int MORTAL_STRIKE_BLOODTHIRST_RAGE_COST = 30;

        public const int EXECUTE_HP_THRESHOLD = 20;
        public const int REND_HP_THRESHOLD = 60;
        public const int SUNDER_ARMOR_HP_THRESHOLD = 75;

        public const int BERSERKER_RAGE_COOLDOWN_MILLIS = 30 * 1000;    // 30 seconds
        public const int BATTLE_SHOUT_DURATION_MILLIS = 3 * 60 * 1000;  // 3 minutes

        // Shaman

        // Warlock

        public const int WARLOCK_DOT_TARGET_HP_THRESHOLD = 25;
        // was having issues where it would recast the dot immediately before seeing the debuff on the mob
        // probably just needs a bug fix instead of this
        public const int WARLOCK_DOT_RECAST_SUPPRESS_MILLIS = 4000;

        // Shared

        public const int DYNAMITE_LEVEL = 6;
        public const int DYNAMITE_COOLDOWN_MILLIS = 1 * 60 * 1000;      // 1 minute

        public const int HEALING_POTION_HP_THRESHOLD = 45;
        public const int POTION_COOLDOWN_MILLIS = 2 * 60 * 1000;        // 2 minutes
        
        public const int PETRIFICATION_FLASK_LEVEL = 50;

        public const int STOP_RESTING_HP_THRESHOLD = 94;
        public const int STOP_RESTING_MP_THRESHOLD = 98;

        public const int EAT_FOOD_HP_THRESHOLD = 80;
        public const int DRINK_WATER_MP_THRESHOLD = 80;

        public const int EMERGENCY_HP_THRESHOLD = 35;
        public const int PETRI_ALTF4_HP_THRESHOLD = 20;

        
    }
}
