namespace WoWHelper.Code.WorldState
{
    public class WowGameplayConstants
    {
        public const int HEROIC_STRIKE_RAGE_COST = 15;
        public const int BATTLE_SHOUT_RAGE_COST = 10;
        public const int REND_RAGE_COST = 10;
        public const int OVERPOWER_RAGE_COST = 5;

        public const int SWEEPING_STRIKES_RAGE_COST = 30;
        public const int WHIRLWIND_RAGE_COST = 25;
        public const int CLEAVE_RAGE_COST = 20;

        public const int MORTAL_STRIKE_BLOODTHIRST_RAGE_COST = 30;

        public const int HEALING_POTION_HP_THRESHOLD = 50;

        // The healing trinket isn't very good, so spam it to keep the run going faster
        public const int HEALING_TRINKET_HP_THRESHOLD = 91;

        public const int DYNAMITE_COOLDOWN_MILLIS = 1 * 60 * 1000;
        public const int POTION_COOLDOWN_MILLIS = 2 * 60 * 1000;
        public const int HEALING_TRINKET_COOLDOWN_MILLIS = 5 * 60 * 1000;
    }
}
