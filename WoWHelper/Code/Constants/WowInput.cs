using InputManager;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WoWHelper.Code
{
    public static class WowInput
    {
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

        // Mage Input
        // Using Shift Keys built into the macros instead of binding a bar to the shift keys,
        // as it screws up regular bars if we bind a bar to shift

        public const Keys MAGE_WAND = Keys.D1;
        public const Keys MAGE_SHIFT_1 = Keys.D1;

        public const Keys MAGE_FROSTBOLT = Keys.D2;
        public const Keys MAGE_SHIFT_2 = Keys.D2;

        public const Keys MAGE_FIREBLAST = Keys.D3;
        public const Keys MAGE_SHIFT_CONE_OF_COLD = Keys.D3;

        public const Keys MAGE_ARCANE_EXPLOSION = Keys.D4;
        public const Keys MAGE_SHIFT_THROW_DYNAMITE = Keys.D4;

        public const Keys MAGE_5 = Keys.D5;
        public const Keys MAGE_SHIFT_5 = Keys.D5;

        public const Keys MAGE_6 = Keys.D6;
        public const Keys MAGE_SHIFT_6 = Keys.D6;

        public const Keys MAGE_7 = Keys.D7;
        public const Keys MAGE_SHIFT_7 = Keys.D7;

        public const Keys MAGE_FROST_ARMOR = Keys.D8;
        public const Keys MAGE_SHIFT_ARCANE_INTELLECT = Keys.D8;

        public const Keys MAGE_CONJURE_WATER = Keys.D9;
        public const Keys MAGE_SHIFT_CONJURE_FOOD = Keys.D9;

        public const Keys MAGE_DRINK_WATER = Keys.D0;
        public const Keys MAGE_SHIFT_EAT_FOOD = Keys.D0;

        public const Keys MAGE_HEALTH_POTION = Keys.OemMinus;
        public const Keys MAGE_SHIFT_MINUS = Keys.OemMinus;

        public const Keys MAGE_FIND_TARGET_MACRO = Keys.Oemplus;
        public const Keys MAGE_SHIFT_PLUS = Keys.Oemplus;


        // Common Input
        // For the sake of sharing tasks, forcing these common keys to be shared
        public const Keys FIND_TARGET_MACRO = Keys.D0;
        public const Keys SHIFT_HEALING_POTION = Keys.D0;

        public const Keys THROW_DYNAMITE = Keys.OemMinus;
        public const Keys SHIFT_TARGET_DUMMY = Keys.OemMinus;

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
    }
}
