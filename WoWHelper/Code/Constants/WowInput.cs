using InputManager;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WoWHelper.Code
{
    public static class WowInput
    {
        public const Keys FIND_TARGET_MACRO = Keys.D1;
        public const Keys CHARGE_KEY = Keys.D2;
        public const Keys MORTALSTRIKE_BLOODTHIRST_MACRO = Keys.D3;
        public const Keys HEROIC_STRIKE_KEY = Keys.D4;
        public const Keys DYNAMITE_KEY = Keys.D5;
        public const Keys HEALING_POTION_KEY = Keys.D6;
        public const Keys BATTLE_SHOUT_KEY = Keys.D7;
        public const Keys SHOOT_MACRO = Keys.D8;
        public const Keys OVERPOWER_KEY = Keys.D9;
        public const Keys CLEAR_TARGET_MACRO = Keys.D0;
        public const Keys REND_KEY = Keys.OemMinus;
        public const Keys EXECUTE_KEY = Keys.Oemplus;

        public const Keys SHIFT_RETALIATION_KEY = Keys.D1;
        public const Keys SHIFT_BERSERKER_RAGE_MACRO = Keys.D2;
        public const Keys SHIFT_WHIRLWIND_MACRO = Keys.D3;
        public const Keys SHIFT_CLEAVE_MACRO = Keys.D4;
        public const Keys SHIFT_LOGOUT_MACRO = Keys.D5;
        public const Keys SHIFT_SHIELD_WALL = Keys.D6;
        public const Keys SHIFT_HEALING_TRINKET = Keys.D7;
        public const Keys SHIFT_TARGET_DUMMY = Keys.D8;
        public const Keys SHIFT_PETRIFICATION_FLASK = Keys.D9;
        public const Keys SHIFT_EAT_FOOD_KEY = Keys.D0;

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
