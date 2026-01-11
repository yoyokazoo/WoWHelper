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

        public const int SWEEPING_STRIKES_RAGE_COST = 30;
        public const int WHIRLWIND_RAGE_COST = 25;
        public const int CLEAVE_RAGE_COST = 20;

        public const int MORTAL_STRIKE_BLOODTHIRST_RAGE_COST = 30;

        public const int HEALING_POTION_HP_THRESHOLD = 50;
        public const int EXECUTE_HP_THRESHOLD = 20;

        // Mage
        public const int ARCANE_INTELLECT_LEVEL = 6;
        public const int CONJURE_WATER_LEVEL = 8;
        public const int CONJURE_FOOD_LEVEL = 20;
        public const int FIREBLAST_LEVEL = 12;
        public const int ARCANE_EXPLOSION_LEVEL = 14;
        public const int MANA_GEM_LEVEL = 20;

        public const int FROSTBOLT_MANA_COST = 45;

        public const int MANA_GEM_MP_THRESHOLD = 20;
        public const int MANA_LOW_ALERT_THRESHOLD = 30;

        // The healing trinket isn't very good, so spam it to keep the run going faster
        public const int HEALING_TRINKET_HP_THRESHOLD = 91;

        public const int DYNAMITE_COOLDOWN_MILLIS = 1 * 60 * 1000;
        public const int POTION_COOLDOWN_MILLIS = 2 * 60 * 1000;
        public const int HEALING_TRINKET_COOLDOWN_MILLIS = 5 * 60 * 1000;
        public const int DIAMOND_FLASK_COOLDOWN_MILLIS = 6 * 60 * 1000;
        public const int BERSERKER_RAGE_COOLDOWN_MILLIS = 30 * 1000;

        // Shared
        public const int DYNAMITE_LEVEL = 6;
    }
}
