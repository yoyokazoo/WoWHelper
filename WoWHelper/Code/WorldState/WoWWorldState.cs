using System;
using System.Collections.Generic;
using System.Drawing;
using WoWHelper.Shared;

namespace WoWHelper
{
    public class WoWWorldState
    {
        public bool Initialized { get; private set; }
        public int HpPercent { get; private set; }
        public int ResourcePercent { get; private set; }

        public Dictionary<string, int> WorldStateUpdateFailures { get; private set; }

        public WoWWorldState()
        {
            Initialized = false;
            HpPercent = -1;
            ResourcePercent = -1;
            WorldStateUpdateFailures = new Dictionary<string, int>();

            WorldStateUpdateFailures[nameof(HpPercent)] = 0;
            WorldStateUpdateFailures[nameof(ResourcePercent)] = 0;

            TesseractEngineSingleton.Instance.SetVariable("tessedit_char_whitelist", "0123456789");
        }

        public void UpdateFromBitmap(Bitmap bmp)
        {
            Initialized = true;

            UpdateHpPercent(bmp);
            UpdateResourcePercent(bmp);
        }

        public void UpdateHpPercent(Bitmap bmp)
        {
            string text = WoWWorldStateImageConstants.HP_PERCENT_POSITION.GetText(TesseractEngineSingleton.Instance, bmp);
            string textTrimmed = Trim(text);
            var success = int.TryParse(textTrimmed, out int hpPercent);

            if (success)
            {
                HpPercent = hpPercent;
                WorldStateUpdateFailures[nameof(HpPercent)] = 0;
            }
            else
            {
                Console.WriteLine($"Unable to parse {text} (trimmed: {textTrimmed}) to an int.  Perhaps the Trim method needs a new character?");
                WorldStateUpdateFailures[nameof(HpPercent)]++;
            }
        }

        public void UpdateResourcePercent(Bitmap bmp)
        {
            string text = WoWWorldStateImageConstants.RESOURCE_PERCENT_POSITION.GetText(TesseractEngineSingleton.Instance, bmp);
            string textTrimmed = Trim(text);
            var success = int.TryParse(textTrimmed, out int resourcePercent);

            if (success)
            {
                ResourcePercent = resourcePercent;
                WorldStateUpdateFailures[nameof(ResourcePercent)] = 0;
            }
            else
            {
                Console.WriteLine($"Unable to parse {text} (trimmed: {textTrimmed}) to an int.  Perhaps the Trim method needs a new character?");
                WorldStateUpdateFailures[nameof(ResourcePercent)]++;
            }
        }

        // TODO: performance test various methods, optimize for CPU speed
        public string Trim(string untrimmedString)
        {
            return untrimmedString.Trim();
            //return untrimmedString.Trim(' ', '\t', '\n', '(', ')', '%', '\'', '‘');

            /*
             * string RemoveNonIntegers(string input)
                {
                    return new string(input.Where(char.IsDigit).ToArray());
                }
             * */

            /* 
             * string RemoveNonIntegers(string input)
            {
            var sb = new StringBuilder();

            foreach (char c in input)
            {
            if (char.IsDigit(c))
            {
            sb.Append(c);
            }
            }

            return sb.ToString();
            }
            }
            }
            */

            /*
             * string RemoveNonIntegers(string input)
                {
                    return Regex.Replace(input, @"\D", ""); // Replace all non-digit characters (\D) with an empty string
                }
             * */
        }
    }
}
