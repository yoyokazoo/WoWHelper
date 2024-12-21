using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tesseract;
using WindowsGameAutomationTools.ImageDetection;
using WoWHelper.Shared;

namespace WoWHelper
{
    public class WoWWorldState
    {
        public bool Initialized { get; private set; } = false;
        public int HpPercent { get; private set; } = -1;

        public Dictionary<string, int> WorldStateUpdateFailures { get; private set; } = new Dictionary<string, int>();

        /*
        public static readonly ImageMatchTextArea HP_PERCENT_POSITION = new ImageMatchTextArea(
            150, 75, 200, 20
        );
        */

        /*
        public static readonly ImageMatchTextArea HP_PERCENT_POSITION = new ImageMatchTextArea(
            200, 75, 38, 22
        );
        */

        public static readonly ImageMatchTextArea HP_PERCENT_POSITION = new ImageMatchTextArea(
            221, 72, 33, 29
        );

        public WoWWorldState() { }

        public void UpdateFromBitmap(Bitmap bmp)
        {
            Initialized = true;

            string text = HP_PERCENT_POSITION.GetText(TesseractEngineSingleton.Instance, bmp);
            string textTrimmed = text.Trim(' ', '\t', '\n', '(', ')', '%', '\'');
            var success = int.TryParse(textTrimmed, out int hpPercent);

            if (success)
            {
                HpPercent = hpPercent;
            }
            else
            {
                // don't update HpPercent
                // count failures in a row, if it exceeds a number log an error?  How to do this in an extensible fashion?
                Console.WriteLine($"Unable to parse {text} (trimmed: {textTrimmed}) to an int.  Perhaps the Trim method needs a new character?");
            }


        }
    }
}
