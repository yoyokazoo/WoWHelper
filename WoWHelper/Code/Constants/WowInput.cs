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
        //public const Keys WARRIOR_FIND_TARGET_MACRO = Keys.D1;
        public const Keys WARRIOR_CHARGE_KEY = Keys.D2;
        public const Keys WARRIOR_MORTALSTRIKE_BLOODTHIRST_MACRO = Keys.D3;
        public const Keys WARRIOR_HEROIC_STRIKE_KEY = Keys.D4;
        //public const Keys WARRIOR_DYNAMITE_KEY = Keys.D5;
        //public const Keys WARRIOR_HEALING_POTION_KEY = Keys.D6;
        public const Keys WARRIOR_BATTLE_SHOUT_KEY = Keys.D7;
        public const Keys WARRIOR_SHOOT_MACRO = Keys.D8;
        public const Keys WARRIOR_OVERPOWER_KEY = Keys.D9;
        public const Keys WARRIOR_CLEAR_TARGET_MACRO = Keys.D0;
        public const Keys WARRIOR_REND_KEY = Keys.OemMinus;
        public const Keys WARRIOR_EXECUTE_KEY = Keys.Oemplus;

        public const Keys WARRIOR_SHIFT_RETALIATION_KEY = Keys.D1;
        public const Keys WARRIOR_SHIFT_BERSERKER_RAGE_MACRO = Keys.D2;
        public const Keys WARRIOR_SHIFT_WHIRLWIND_MACRO = Keys.D3;
        public const Keys WARRIOR_SHIFT_CLEAVE_MACRO = Keys.D4;
        //public const Keys WARRIOR_SHIFT_LOGOUT_MACRO = Keys.D5;
        public const Keys WARRIOR_SHIFT_SHIELD_WALL = Keys.D6;
        public const Keys WARRIOR_SHIFT_HEALING_TRINKET = Keys.D7;
        //public const Keys WARRIOR_SHIFT_TARGET_DUMMY = Keys.D8;
        //public const Keys WARRIOR_SHIFT_PETRIFICATION_FLASK = Keys.D9;
        public const Keys WARRIOR_SHIFT_EAT_FOOD_KEY = Keys.D0;
        #endregion

        #region Mage
        // Mage Input

        public const Keys MAGE_WAND = Keys.D1;
        public const Keys MAGE_SHIFT_1 = Keys.D1;

        public const Keys MAGE_FROSTBOLT = Keys.D2;
        public const Keys MAGE_SHIFT_2 = Keys.D2;

        public const Keys MAGE_FIREBLAST = Keys.D3;
        public const Keys MAGE_SHIFT_CONE_OF_COLD = Keys.D3;

        public const Keys MAGE_ARCANE_EXPLOSION = Keys.D4;
        public const Keys MAGE_SHIFT_4 = Keys.D4;

        public const Keys MAGE_5 = Keys.D5;
        public const Keys MAGE_SHIFT_5 = Keys.D5;

        /*
#showtooltip [mod:shift] Arcane Intellect; Frost Armor
/cast [nomod] Frost Armor
/stopmacro [nomod]
/cleartarget
/cast Arcane Intellect
        */
        public const Keys MAGE_FROST_ARMOR = Keys.D6;
        public const Keys MAGE_SHIFT_ARCANE_INTELLECT = Keys.D6;

        /*
#showtooltip [mod:shift] Conjure Food; Conjure Water
/cast [nomod] Conjure Water
/cast [mod:shift] Conjure Food
        */
        public const Keys MAGE_CONJURE_WATER = Keys.D7;
        public const Keys MAGE_SHIFT_CONJURE_FOOD = Keys.D7;



        #endregion

        #region Shaman

        public const Keys SHAMAN_LIGHTNING_BOLT = Keys.D2;
        public const Keys SHAMAN_SHIFT_2 = Keys.D2;

        public const Keys SHAMAN_SHOCK = Keys.D3;
        public const Keys SHAMAN_SHIFT_3 = Keys.D3;

        /*
#showtooltip [mod:shift] Rockbiter Weapon; Lightning Shield
/use [nomod] Lightning Shield
/use [mod:shift] Rockbiter Weapon
        */
        public const Keys SHAMAN_LIGHTNING_SHIELD = Keys.D4;
        public const Keys SHAMAN_SHIFT_ROCKBITER_WEAPON = Keys.D4;

        public const Keys SHAMAN_5 = Keys.D5;
        public const Keys SHAMAN_SHIFT_5 = Keys.D5;

        public const Keys SHAMAN_6 = Keys.D6;
        public const Keys SHAMAN_SHIFT_6 = Keys.D6;

        public const Keys SHAMAN_7 = Keys.D7;
        public const Keys SHAMAN_SHIFT_7 = Keys.D7;

        #endregion

        #region Common
        // Common Input
        // For the sake of sharing tasks, forcing these common keys to be shared

        public const Keys START_ATTACK = Keys.D1;
        public const Keys SHIFT_1 = Keys.D1;

        /*
#showtooltip [mod:shift] Conjured Fresh Water; Conjured Bread
/use [nomod] Conjured Bread
/use [mod:shift] Conjured Fresh Water
        */
        public const Keys EAT_FOOD = Keys.D8;
        public const Keys SHIFT_DRINK_WATER = Keys.D8;

        public const Keys CLEAR_TARGET_MACRO = Keys.D9;
        public const Keys SHIFT_9 = Keys.D9;

        /*
#showtooltip [mod:shift] Healing Potion; Target
/use [mod:shift] Healing Potion
/stopmacro [mod:shift]
/cleartarget
/target Fleeting
        */
        public const Keys FIND_TARGET_MACRO = Keys.D0;
        public const Keys SHIFT_HEALING_POTION = Keys.D0;

        /*
#showtooltip [mod:shift] Target Dummy; Rough Dynamite
/use [nomod] Rough Dynamite
/use [mod:shift] Target Dummy
        */
        public const Keys THROW_DYNAMITE = Keys.OemMinus;
        public const Keys SHIFT_TARGET_DUMMY = Keys.OemMinus;

        /*
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
        #endregion
    }
}
