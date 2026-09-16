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

        public const int EXECUTE_HP_THRESHOLD = 20;

        // Shaman

        // The healing trinket isn't very good, so spam it to keep the run going faster
        public const int HEALING_TRINKET_HP_THRESHOLD = 91;

        public const int DYNAMITE_COOLDOWN_MILLIS = 1 * 60 * 1000;
        public const int POTION_COOLDOWN_MILLIS = 2 * 60 * 1000;
        public const int HEALING_TRINKET_COOLDOWN_MILLIS = 5 * 60 * 1000;
        public const int DIAMOND_FLASK_COOLDOWN_MILLIS = 6 * 60 * 1000;
        public const int BERSERKER_RAGE_COOLDOWN_MILLIS = 30 * 1000;

        // Warlock

        // Below this, a mob is expected to die from melee/Shadow Bolt damage before a
        // freshly-applied DoT would tick for much -- not worth the GCD unless one of the
        // WarlockShouldCastImmolate/Corruption overrides below applies (multiple attackers,
        // or we're low enough to want the mob dead by any means).
        public const int WARLOCK_DOT_TARGET_HP_THRESHOLD = 25;

        // The DoT debuff icon/CLASS_BOOL pixel takes a little while to actually show up
        // after casting, so WowWarlockClassState.ShouldCastImmolate/ShouldCastCorruption
        // can still read true for a few ticks after the cast already landed -- without this,
        // that reads as "no DoT yet" and re-casts the same DoT repeatedly. Suppress
        // re-casting a given DoT for this long after our own last cast of it, regardless of
        // what the (possibly stale) decoded bool says.
        // 2 seconds to account for pushback and time for the buff to update
        public const int WARLOCK_DOT_RECAST_SUPPRESS_MILLIS = 4000;

        // Shared
        public const int DYNAMITE_LEVEL = 6;
        public const int PETRIFICATION_FLASK_LEVEL = 50;

        public const int HEALING_POTION_HP_THRESHOLD = 45; // 50
    }
}
