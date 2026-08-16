using System.Diagnostics.SymbolStore;
using System.Drawing;
using System.Numerics;
using WindowsGameAutomationTools.ImageDetection;
using WindowsGameAutomationTools.Images;
using WoWHelper.Code.Config;
using WoWHelper.Code.WorldState;

namespace WoWHelper
{
    public class WowWorldState
    {
        public bool Initialized { get; private set; }
        
        public float MapX { get; private set; }
        public float MapY { get; private set; }
        public Vector2 PlayerLocation { get; private set; }
        public float FacingDegrees { get; private set; }

        // Multi Int One
        public int PlayerHpPercent { get; private set; }
        public int ResourcePercent { get; private set; }
        public int TargetHpPercent { get; private set; }

        // Multi Int Two
        public int AttackerCount { get; private set; }
        public int PlayerLevel { get; private set; }
        public WowZone CurrentZone { get; private set; }

        public bool IsInCombat { get; private set; }

        public bool IsAutoAttacking { get; private set; }
        public bool LowOnHealthPotions { get; private set; }
        public bool LowOnDynamite { get; private set; }
        public bool LowOnAmmo { get; private set; }
        public bool GCDCooledDown { get; private set; }
        public bool BagsAreFull { get; private set; }
        public bool IsPlayerPetrified { get; private set; }
        public bool HasUnseenWhisper { get; private set; }
        public bool IsInMeleeRange { get; private set; }
        public bool IsCurrentlyCasting { get; private set; }
        public bool EnemyNameplatesAreTurnedOn { get; private set; }
        public bool CurrentTargetInCombatWithUs { get; private set; }

        public bool FacingWrongWay { get; private set; }
        public bool TooFarAway { get; private set; }
        public bool TargetNeedsToBeInFront { get; private set; }
        public bool InvalidTarget { get; private set; }
        public bool OutOfRange { get; private set; }
        public bool OnLoginScreen { get; private set; }
        public bool Underwater { get; private set; }

        public Bitmap Bmp { get; private set; }

        public WowScreenConfiguration ScreenConfig { get; private set; }

        public WowWorldState(WowScreenConfiguration screenConfig)
        {
            Initialized = false;
            PlayerHpPercent = -1;
            ResourcePercent = -1;
            TargetHpPercent = -1;
            MapX = -1;
            MapY = -1;
            PlayerLocation = Vector2.Zero;
            FacingDegrees = -1;
            AttackerCount = -1;
            PlayerLevel = -1;
            ScreenConfig = screenConfig;

            //TesseractEngineSingleton.Instance.SetVariable("tessedit_char_whitelist", "0123456789-.");
        }

        public static WowWorldState GetWoWWorldState(WowScreenConfiguration screenConfig)
        {
            WowWorldState currentState = new WowWorldState(screenConfig);

            currentState.Bmp = ScreenCapture.CaptureBitmapFromDesktopAndRectangle(
                new Rectangle(0, 0, screenConfig.WidthOfScreenToSlice, screenConfig.WidthOfScreenToSlice));
            currentState.UpdateFromBitmap(currentState.Bmp);
            //wowBitmap.Dispose(); // TODO: Implement IDisposable

            return currentState;
        }

        public void UpdateFromBitmap(Bitmap bmp)
        {
            Initialized = true;

            // Checked first (pixel-row index 0) -- if the addon isn't rendering, the rest
            // of the row is garbage, so this is the one decode everything else implicitly
            // depends on being meaningful.
            UpdateOnLoginScreen(bmp);

            UpdateMapX(bmp);
            UpdateMapY(bmp);
            PlayerLocation = new Vector2(MapX, MapY);
            UpdateFacingDegrees(bmp);

            UpdateMultiBoolOne(bmp);
            UpdateMultiBoolTwo(bmp);
            UpdateMultiIntOne(bmp);
            UpdateMultiIntTwo(bmp);

            UpdateRedErrorTextMessages(bmp);

            UpdateBreathBar(bmp);
        }

        // Returns the R component of the color
        public static int GetIntFromColor(Color color)
        {
            return color.R * 255 + color.G;
        }

        // Returns the R component as the whole number part, and the G component as the fractional part.
        // Only works for numbers <= 255.99
        public static float GetFloatFromColor(Color color)
        {
            return color.R * 255.0f + color.G + (color.B / 255.0f);
        }

        // Return true if color is exactly green, false otherwise
        public static bool GetBoolFromColor(Color color)
        {
            return color.R == 0 && color.G == 255 && color.B == 0;
        }

        public static void DecodeByte(
            byte value,
            out bool b1, out bool b2, out bool b3, out bool b4,
            out bool b5, out bool b6, out bool b7, out bool b8)
        {
            b1 = (value & 1) != 0;
            b2 = (value & 2) != 0;
            b3 = (value & 4) != 0;
            b4 = (value & 8) != 0;
            b5 = (value & 16) != 0;
            b6 = (value & 32) != 0;
            b7 = (value & 64) != 0;
            b8 = (value & 128) != 0;
        }

        public void UpdateMapX(Bitmap bmp)
        {
            Color color = bmp.GetPixel(ScreenConfig.MapXPosition.X, ScreenConfig.MapXPosition.Y);
            MapX = GetFloatFromColor(color);
        }

        public void UpdateMapY(Bitmap bmp)
        {
            Color color = bmp.GetPixel(ScreenConfig.MapYPosition.X, ScreenConfig.MapYPosition.Y);
            MapY = GetFloatFromColor(color);
        }

        public void UpdateFacingDegrees(Bitmap bmp)
        {
            Color color = bmp.GetPixel(ScreenConfig.FacingDegreesPosition.X, ScreenConfig.FacingDegreesPosition.Y);
            FacingDegrees = GetFloatFromColor(color);
        }

        // All class-agnostic bools are packed tightly into MultiBoolOne's R+G
        // bytes (see GetMultiBoolOne() in WoWFunctions.lua) -- MultiBoolTwo is
        // left fully reserved for the next class-agnostic field rather than
        // scattering a few bits across both pixels.
        public void UpdateMultiBoolOne(Bitmap bmp)
        {
            Color color = bmp.GetPixel(ScreenConfig.MultiBoolOnePosition.X, ScreenConfig.MultiBoolOnePosition.Y);
            DecodeByte(color.R, out var r1, out var r2, out var r3, out var r4, out var r5, out var r6, out var r7, out var r8);
            DecodeByte(color.G, out var g1, out var g2, out var g3, out var g4, out var g5, out var g6, out var g7, out var g8);

            IsAutoAttacking = r1;
            LowOnHealthPotions = r2;
            LowOnDynamite = r3;
            LowOnAmmo = r4;
            GCDCooledDown = r5;
            BagsAreFull = r6;
            IsInCombat = r7;
            IsPlayerPetrified = r8;

            HasUnseenWhisper = g1;
            IsInMeleeRange = g2;
            IsCurrentlyCasting = g3;
            EnemyNameplatesAreTurnedOn = g4;
            CurrentTargetInCombatWithUs = g5;
            // g6-g8 and the whole B byte are reserved/unused.
        }

        // MultiBoolTwo currently carries no decoded fields -- reserved for the
        // next class-agnostic flag (see GetMultiBoolTwo() in WoWFunctions.lua).
        public void UpdateMultiBoolTwo(Bitmap bmp)
        {
        }

        public void UpdateMultiIntOne(Bitmap bmp)
        {
            Color color = bmp.GetPixel(ScreenConfig.MultiIntOnePosition.X, ScreenConfig.MultiIntOnePosition.Y);
            
            PlayerHpPercent = color.R;
            ResourcePercent = color.G;
            TargetHpPercent = color.B;
        }

        public void UpdateMultiIntTwo(Bitmap bmp)
        {
            Color color = bmp.GetPixel(ScreenConfig.MultiIntTwoPosition.X, ScreenConfig.MultiIntTwoPosition.Y);

            AttackerCount = color.R;
            PlayerLevel = color.G;
            CurrentZone = (WowZone)color.B; // see GetCurrentZoneId() in WoWFunctions.lua
        }

        public void UpdateRedErrorTextMessages(Bitmap bmp)
        {
            FacingWrongWay = MatchesErrorTextColor(bmp, ScreenConfig.FacingWrongWayPositions);
            TooFarAway = MatchesErrorTextColor(bmp, ScreenConfig.TooFarAwayPositions);
            TargetNeedsToBeInFront = MatchesErrorTextColor(bmp, ScreenConfig.TargetNeedsToBeInFrontPositions);
            InvalidTarget = MatchesErrorTextColor(bmp, ScreenConfig.InvalidTargetPositions);
            OutOfRange = MatchesErrorTextColor(bmp, ScreenConfig.OutOfRangePositions);
        }

        // Index 0 of the pixel row is a fixed sentinel the addon paints, exactly
        // ADDON_LOADED_COLOR. If it's there, the addon is loaded and the rest of the row
        // is real; any other color -- including whatever's actually at this screen
        // position when the addon isn't rendering, e.g. the login screen -- means
        // OnLoginScreen. Replaces the old text/UI pixel-signature match.
        public void UpdateOnLoginScreen(Bitmap bmp)
        {
            Color color = bmp.GetPixel(ScreenConfig.AddonLoadedPosition.X, ScreenConfig.AddonLoadedPosition.Y);
            OnLoginScreen = !(color.R == WowScreenConfiguration.ADDON_LOADED_COLOR.R
                && color.G == WowScreenConfiguration.ADDON_LOADED_COLOR.G
                && color.B == WowScreenConfiguration.ADDON_LOADED_COLOR.B);
        }

        public void UpdateBreathBar(Bitmap bmp)
        {
            Underwater = ScreenConfig.BreathBarScreenPositions.MatchesSourceImage(bmp);
        }

        public bool MatchesErrorTextColor(Bitmap bmp, ImageMatchColorPositions positions)
        {
            foreach (var position in positions.ColorPositions)
            {
                var color = bmp.GetPixel(position.X, position.Y);
                if (color.R < WowScreenConfiguration.ERROR_TEXT_COLOR.R)
                {
                    return false;
                }

                if (color.G != color.B)
                {
                    return false;
                }

                if (color.G > color.R)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
