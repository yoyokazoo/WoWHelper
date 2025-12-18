using InputManager;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WoWHelper.Code
{
    public static class WowInput
    {
        public const Keys FIND_TARGET_MACRO = Keys.D1;
        public const Keys CHARGE_KEY = Keys.D2;
        public const Keys HEROIC_STRIKE_KEY = Keys.D3;
        public const Keys EAT_FOOD_KEY = Keys.D4;
        public const Keys DYNAMITE_KEY = Keys.D5;
        public const Keys HEALING_POTION_KEY = Keys.D6;
        public const Keys BATTLE_SHOUT_KEY = Keys.D7;
        public const Keys SHOOT_MACRO = Keys.D8;
        public const Keys OVERPOWER_KEY = Keys.D9;
        public const Keys CLEAR_TARGET_MACRO = Keys.D0;
        public const Keys REND_KEY = Keys.OemMinus;
        public const Keys RETALIATION_KEY = Keys.Oemplus;

        public const Keys SHIFT_DANGEROUS_TARGET_MACRO = Keys.D1;
        public const Keys SHIFT_SWEEPING_STRIKES_MACRO = Keys.D2;
        public const Keys SHIFT_WHIRLWIND_MACRO = Keys.D3;
        public const Keys SHIFT_CLEAVE_MACRO = Keys.D4;
        public const Keys SHIFT_LOGOUT_MACRO = Keys.D5;
        public const Keys SHIFT_UNASSIGNED1 = Keys.D6;
        public const Keys SHIFT_UNASSIGNED2 = Keys.D7;
        public const Keys SHIFT_UNASSIGNED3 = Keys.D8;
        public const Keys SHIFT_UNASSIGNED4 = Keys.D9;
        public const Keys SHIFT_UNASSIGNED5 = Keys.D0;

        public const Keys TURN_LEFT = Keys.A;
        public const Keys TURN_RIGHT = Keys.D;

        public const Keys STRAFE_LEFT = Keys.Q;
        public const Keys STRAFE_RIGHT = Keys.E;

        public const Keys MOVE_FORWARD = Keys.W;
        public const Keys MOVE_BACK = Keys.S;

        public const Keys TAB_TARGET = Keys.Tab;
        public const Keys JUMP = Keys.Space;

        public static async Task PressKeyWithShift(Keys key)
        {
            Keyboard.KeyDown(Keys.LShiftKey);
            await Task.Delay(5);
            Keyboard.KeyDown(key);
            await Task.Delay(5);
            Keyboard.KeyUp(key);
            await Task.Delay(5);
            Keyboard.KeyUp(Keys.LShiftKey);
        }
    }
}
