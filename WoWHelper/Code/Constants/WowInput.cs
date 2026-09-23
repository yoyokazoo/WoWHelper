using InputManager;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WoWHelper.Code
{
    public static class WowInput
    {
        // Using Shift Keys built into the macros instead of binding a bar to the shift keys,
        // as it screws up regular bars if we bind a bar to shift

        #region Warrior
        // Warrior Input

        /*
         * 2 Pull
#showtooltip [mod:shift] Shoot; Charge
/cast [nomod] Charge
/cast [mod:shift] Shoot
        */
        public const Keys WARRIOR_CHARGE = Keys.D2;
        public const Keys WARRIOR_SHIFT_SHOOT = Keys.D2;

        /*
         * 3 H/C
#showtooltip [mod:shift] Cleave; Heroic Strike
/cast [nomod] Heroic Strike
/cast [mod:shift] Cleave
        */
        public const Keys WARRIOR_HEROIC_STRIKE = Keys.D3;
        public const Keys WARRIOR_SHIFT_CLEAVE = Keys.D3;

        /*
         * 4 Buff
#showtooltip [mod:shift] Berserker Rage; [mod:ctrl] Sweeping Strikes; Battle Shout
/cast [nomod] Battle Shout
/cast [mod:shift] Berserker Rage
/cast [mod:ctrl] Sweeping Strikes
        */
        public const Keys WARRIOR_BATTLE_SHOUT = Keys.D4;
        // TODO: add stance dancing to macros??
        public const Keys WARRIOR_SHIFT_BERSERKER_RAGE = Keys.D4;
        public const Keys WARRIOR_CTRL_SWEEPING_STRIKES = Keys.D4;

        /*
         * 5 Dmg
#showtooltip [mod:shift] Sunder Armor; Mortal Strike
/cast [nomod] Mortal Strike
/cast [mod:shift] Sunder Armor
        */
        public const Keys WARRIOR_MORTALSTRIKE_BLOODTHIRST = Keys.D5;
        public const Keys WARRIOR_SHIFT_SUNDER_ARMOR = Keys.D5;

        /*
         * 6 Rend
#showtooltip [mod:shift] Execute; Mortal Strike
/cast [nomod] Mortal Strike
/cast [mod:shift] Execute
        */
        public const Keys WARRIOR_REND = Keys.D6;
        public const Keys WARRIOR_SHIFT_OVERPOWER = Keys.D6;

        /*
         * 7 shit
#showtooltip [mod:shift] Retaliation; Execute
/cast [nomod] Execute
/cast [mod:shift] Retaliation
        */
        public const Keys WARRIOR_EXECUTE = Keys.D7;
        public const Keys WARRIOR_SHIFT_RETALIATION = Keys.D7;

        #endregion

        #region Shaman

        /*
         * 2 Bolt
#showtooltip [mod:shift] Lightning Bolt(Rank 1); Lightning Bolt
/cast [nomod] Lightning Bolt
/cast [mod:shift] Lightning Bolt(Rank 1)
        */
        public const Keys SHAMAN_LIGHTNING_BOLT = Keys.D2;
        public const Keys SHAMAN_SHIFT_LIGHTNING_BOLT_RANK_1 = Keys.D2;

        /*
         * 3 Shock
#showtooltip [mod:shift] Flame Shock; Earth Shock
/use [nomod] Earth Shock
/use [mod:shift] Flame Shock
        */
        public const Keys SHAMAN_EARTH_SHOCK = Keys.D3;
        public const Keys SHAMAN_SHIFT_FLAME_SHOCK = Keys.D3;

        /*
         * 4 Buff
#showtooltip [mod:shift] Rockbiter Weapon; Lightning Shield
/use [nomod] Lightning Shield
/use [mod:shift] Rockbiter Weapon
        */
        public const Keys SHAMAN_LIGHTNING_SHIELD = Keys.D4;
        public const Keys SHAMAN_SHIFT_ROCKBITER_WEAPON = Keys.D4;

        /*
         * 5 Cure
#showtooltip [mod:shift] Cure Disease; Cure Poison
/use [nomod] Cure Poison
/use [mod:shift] Cure Disease
         */
        public const Keys SHAMAN_CURE_POISON = Keys.D5;
        public const Keys SHAMAN_SHIFT_CURE_DISEASE = Keys.D5;

        /*
         * 6 Frost Shock
#showtooltip Frost Shock
/cast Frost Shock
        */
        public const Keys SHAMAN_FROST_SHOCK = Keys.D6;
        public const Keys SHAMAN_SHIFT_6 = Keys.D6;

        public const Keys SHAMAN_7 = Keys.D7;
        public const Keys SHAMAN_SHIFT_7 = Keys.D7;

        #endregion

        #region Warlock
        // TODO: define Warlock keybinds/macros here once the rotation is designed,
        // same pattern as the Warrior/Shaman regions above (a const Keys per
        // in-game keybind/macro slot, referenced from WowWarlockTasks.cs).

        /*
         * 2 Bolt
#showtooltip Shadow Bolt
/cast Shadow Bolt
        */
        public const Keys WARLOCK_SHADOW_BOLT = Keys.D2;
        public const Keys WARLOCK_SHIFT_2 = Keys.D2;

        /*
         * 3 Dot
#showtooltip [mod:shift] Immolate; Corruption
/use [nomod] Corruption
/use [mod:shift] Immolate
         */
        public const Keys WARLOCK_CORRUPTION = Keys.D3;
        public const Keys WARLOCK_SHIFT_IMMOLATE = Keys.D3;

        /*
         * 4 Buff
#showtooltip [mod:shift] Demon Skin; Summon Imp
/use [nomod] Summon Imp
/use [mod:shift] Demon Skin
        */
        public const Keys WARLOCK_SUMMON_PET = Keys.D4;
        public const Keys WARLOCK_SHIFT_DEMON_ARMOR = Keys.D4;

        #endregion

        #region Common
        // Common Input
        // For the sake of sharing tasks, forcing these common keys to be shared

        // stopcasting so shamans who are pulling and interrupted by a wandering mob don't pull an extra
        /*
         * 1 Atk
/stopcasting
/startattack
        */
        public const Keys START_ATTACK = Keys.D1;
        public const Keys SHIFT_1 = Keys.D1;

        /*
         * 8 Eat
#showtooltip [mod:shift] Conjured Crystal Water; Raw Bristle Whisker Catfish
/use [nomod] Raw Bristle Whisker Catfish
/use [mod:shift] Conjured Crystal Water
/sit
/use Thick-shelled Clam
/stopmacro [mod:shift]
/stopmacro [nomod]
/target Derak Nightfall
        */
        public const Keys EAT_FOOD = Keys.D8;
        public const Keys SHIFT_DRINK_WATER = Keys.D8;
        public const Keys CTRL_TARGET_MERCHANT = Keys.D8;

        /*
         * 9 Clr
/cleartarget
        */
        public const Keys CLEAR_TARGET_MACRO = Keys.D9;
        public const Keys SHIFT_9 = Keys.D9;

        /*
         * 0 Targ
#showtooltip [mod:shift] Healing Potion; Target
/use [mod:shift] Healing Potion
/stopmacro [mod:shift]
/cleartarget
/target Fleeting
        */
        public const Keys FIND_TARGET_MACRO = Keys.D0;
        public const Keys SHIFT_HEALING_POTION = Keys.D0;

        /*
         * - Dyn
#showtooltip [mod:shift] Target Dummy; Rough Dynamite
/use [nomod, @cursor] Rough Dynamite
/use [mod:shift, @cursor] Target Dummy
        */
        public const Keys THROW_DYNAMITE = Keys.OemMinus;
        public const Keys SHIFT_TARGET_DUMMY = Keys.OemMinus;

        /*
         * = Log
#showtooltip [mod:shift] Flask of Petrification; Logout
/cast [mod:shift] Flask of Petrification
/stopmacro [mod:shift]
/logout
        */
        public const Keys LOGOUT_MACRO = Keys.Oemplus;
        public const Keys SHIFT_PETRIFICATION_FLASK = Keys.Oemplus;

        public const Keys ALT_FORCE_QUIT_KEY = Keys.F4;

        public const Keys TURN_LEFT = Keys.A;
        public const Keys TURN_RIGHT = Keys.D;

        public const Keys STRAFE_LEFT = Keys.Q;
        public const Keys STRAFE_RIGHT = Keys.E;

        public const Keys MOVE_FORWARD = Keys.W;
        public const Keys MOVE_BACK = Keys.S;

        public const Keys TAB_TARGET = Keys.Tab;
        public const Keys JUMP = Keys.Space;

        #endregion

        #region Shift/Alt Handling

        public static async Task PressKey(Keys key)
        {
            Keyboard.KeyPress(key);
            await Task.Delay(10);
        }

        // For when we exit the program with ESC, make sure we don't have any lingering keys pressed down
        public static Keys LatestShiftKey;
        public static async Task PressKeyWithModifier(Keys key, Keys modifier)
        {
            LatestShiftKey = key;

            Keyboard.KeyDown(modifier);
            await Task.Delay(15);
            Keyboard.KeyDown(key);
            await Task.Delay(15);
            Keyboard.KeyUp(key);
            await Task.Delay(15);
            Keyboard.KeyUp(modifier);
        }

        public static async Task PressKeyWithShift(Keys key)
        {
            LatestShiftKey = key;

            await PressKeyWithModifier(key, Keys.LShiftKey);
        }

        public static async Task PressKeyWithAlt(Keys key)
        {
            // Why is it not Keys.Alt, which exists? no one knows!
            await PressKeyWithModifier(key, Keys.Menu);
        }

        public static async Task PressKeyWithControl(Keys key)
        {
            await PressKeyWithModifier(key, Keys.LControlKey);
        }

        #endregion
    }
}
