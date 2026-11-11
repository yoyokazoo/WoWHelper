using InputManager;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WoWHelper.Code
{
    public static class WowInput
    {
        // Using Shift Keys built into the macros instead of binding a bar to the shift keys,
        // as it screws up regular bars if we bind a bar to shift

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
        */
        public const Keys EAT_FOOD = Keys.D8;
        public const Keys SHIFT_DRINK_WATER = Keys.D8;
        public const Keys CTRL_TARGET_MERCHANT = Keys.D8;

        /*
         * 9 Pot
#showtooltip Healing Potion
/use Healing Potion
        */
        public const Keys HEALING_POTION = Keys.D9;
        public const Keys SHIFT_9 = Keys.D9;

        /*
         * 0 Targ -- body is rewritten per route by the addon's /yytarget command (see
         * WowLocationConfiguration.TargetMacroMobNames); the macro just has to exist.
/cleartarget
/stopmacro [mod:shift]
/target Mottled
        */
        public const Keys FIND_TARGET_MACRO = Keys.D0;
        public const Keys SHIFT_CLEAR_TARGET = Keys.D0;

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

        // Tracked so ReleaseAllInputs knows which modified keys might still be held down
        public static Keys LatestShiftKey;
        public static Keys LatestControlKey;
        public static async Task PressKeyWithModifier(Keys key, Keys modifier)
        {
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
            LatestControlKey = key;

            await PressKeyWithModifier(key, Keys.LControlKey);
        }

        #endregion

        #region Chat Typing

        // Types a line into chat and sends it, e.g. TypeChatLineTask("/yytarget Mottled,Scorpid").
        // InputManager has no "type this text" call, so each character is mapped to the key
        // that produces it on a US keyboard layout (uppercase via shift). Throws on any
        // character that isn't mapped, rather than silently typing something else.
        public static async Task TypeChatLineTask(string text)
        {
            var keys = new (Keys key, bool shift)[text.Length];
            for (int i = 0; i < text.Length; i++)
            {
                keys[i] = GetKeyForChar(text[i]);
            }

            await PressKey(Keys.Enter);
            await Task.Delay(100); // let the chat box open before typing into it

            foreach (var (key, shift) in keys)
            {
                if (shift)
                {
                    await PressKeyWithModifier(key, Keys.LShiftKey);
                }
                else
                {
                    await PressKey(key);
                }
                await Task.Delay(15);
            }

            await PressKey(Keys.Enter);
        }

        private static (Keys key, bool shift) GetKeyForChar(char c)
        {
            if (c >= 'a' && c <= 'z') return (Keys.A + (c - 'a'), false);
            if (c >= 'A' && c <= 'Z') return (Keys.A + (c - 'A'), true);
            if (c >= '0' && c <= '9') return (Keys.D0 + (c - '0'), false);

            switch (c)
            {
                case ' ': return (Keys.Space, false);
                case '/': return (Keys.OemQuestion, false);
                case ',': return (Keys.Oemcomma, false);
                case '.': return (Keys.OemPeriod, false);
                case '-': return (Keys.OemMinus, false);
                case '\'': return (Keys.OemQuotes, false);
                default: throw new System.ArgumentException($"Can't type character '{c}' -- add it to WowInput.GetKeyForChar");
            }
        }

        #endregion

        #region Cleanup

        // Releases every key/button the bot may be holding down, so nothing is left
        // stuck pressed (e.g. when quitting mid-movement with ESC).
        public static void ReleaseAllInputs()
        {
            Keyboard.KeyUp(MOVE_FORWARD);
            Keyboard.KeyUp(MOVE_BACK);
            Keyboard.KeyUp(TURN_LEFT);
            Keyboard.KeyUp(TURN_RIGHT);
            Keyboard.KeyUp(JUMP);
            Keyboard.KeyUp(STRAFE_LEFT);
            Keyboard.KeyUp(STRAFE_RIGHT);
            Keyboard.KeyUp(LatestShiftKey);
            Keyboard.KeyUp(LatestControlKey);
            Keyboard.KeyUp(Keys.LShiftKey);
            Mouse.ButtonUp(Mouse.MouseKeys.Right);
        }

        #endregion
    }
}
