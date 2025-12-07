using System;
using System.Collections.Generic;
using System.Drawing;
using WindowsGameAutomationTools.Images;
using WoWHelper.Shared;

namespace WoWHelper
{
    public class WoWWorldState
    {
        public bool Initialized { get; private set; }
        public int PlayerHpPercent { get; private set; }
        public int ResourcePercent { get; private set; }
        public int TargetHpPercent { get; private set; }
        public float MapX { get; private set; }
        public float MapY { get; private set; }
        public float FacingDegrees { get; private set; }

        public Dictionary<string, int> WorldStateUpdateFailures { get; private set; }

        public WoWWorldState()
        {
            Initialized = false;
            PlayerHpPercent = -1;
            ResourcePercent = -1;
            TargetHpPercent = -1;
            MapX = -1;
            MapY = -1;
            FacingDegrees = -1;
            WorldStateUpdateFailures = new Dictionary<string, int>();

            WorldStateUpdateFailures[nameof(PlayerHpPercent)] = 0;
            WorldStateUpdateFailures[nameof(ResourcePercent)] = 0;
            WorldStateUpdateFailures[nameof(MapX)] = 0;
            WorldStateUpdateFailures[nameof(MapY)] = 0;
            WorldStateUpdateFailures[nameof(FacingDegrees)] = 0;

            TesseractEngineSingleton.Instance.SetVariable("tessedit_char_whitelist", "0123456789-.");
        }

        public void UpdateFromBitmap(Bitmap bmp)
        {
            Initialized = true;

            UpdatePlayerHpPercent(bmp);
            UpdateResourcePercent(bmp);
            UpdateMapX(bmp);
            UpdateMapY(bmp);
            UpdateFacingDegrees(bmp);
        }

        public void UpdatePlayerHpPercent(Bitmap bmp)
        {
            //Bitmap bmpSnippet = new Bitmap()
            string text = WoWWorldStateImageConstants.PLAYER_HP_PERCENT_POSITION.GetText(TesseractEngineSingleton.Instance, bmp);
            string textTrimmed = Trim(text);
            var success = int.TryParse(textTrimmed, out int hpPercent);

            if (success)
            {
                PlayerHpPercent = hpPercent;
                WorldStateUpdateFailures[nameof(PlayerHpPercent)] = 0;
            }
            else
            {
                Console.WriteLine($"Unable to parse {text} (trimmed: {textTrimmed}) to an int.  Perhaps the Trim method needs a new character?");
                WorldStateUpdateFailures[nameof(PlayerHpPercent)]++;
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

        public void UpdateMapX(Bitmap bmp)
        {
            string text = WoWWorldStateImageConstants.MAP_X_POSITION.GetText(TesseractEngineSingleton.Instance, bmp);
            string textTrimmed = Trim(text);
            var success = float.TryParse(textTrimmed, out float mapX);

            if (success)
            {
                MapX = mapX;
                WorldStateUpdateFailures[nameof(MapX)] = 0;
            }
            else
            {
                Console.WriteLine($"Unable to parse {text} (trimmed: {textTrimmed}) to a float.  Perhaps the Trim method needs a new character?");
                WorldStateUpdateFailures[nameof(MapX)]++;
            }
        }

        public void UpdateMapY(Bitmap bmp)
        {
            string text = WoWWorldStateImageConstants.MAP_Y_POSITION.GetText(TesseractEngineSingleton.Instance, bmp);
            string textTrimmed = Trim(text);
            var success = float.TryParse(textTrimmed, out float mapY);

            if (success)
            {
                MapY = mapY;
                WorldStateUpdateFailures[nameof(MapY)] = 0;
            }
            else
            {
                Console.WriteLine($"Unable to parse {text} (trimmed: {textTrimmed}) to a float.  Perhaps the Trim method needs a new character?");
                WorldStateUpdateFailures[nameof(MapY)]++;
            }
        }

        public void UpdateFacingDegrees(Bitmap bmp)
        {
            string text = WoWWorldStateImageConstants.FACING_DEGREES_POSITION.GetText(TesseractEngineSingleton.Instance, bmp);
            string textTrimmed = Trim(text);
            var success = float.TryParse(textTrimmed, out float facingDegrees);

            if (success)
            {
                FacingDegrees = facingDegrees;
                WorldStateUpdateFailures[nameof(FacingDegrees)] = 0;
            }
            else
            {
                Console.WriteLine($"Unable to parse {text} (trimmed: {textTrimmed}) to a float.  Perhaps the Trim method needs a new character?");
                WorldStateUpdateFailures[nameof(FacingDegrees)]++;
            }
        }

        // TODO: performance test various methods, optimize for CPU speed
        public string Trim(string untrimmedString)
        {
            return untrimmedString.Trim();
        }
    }
}
