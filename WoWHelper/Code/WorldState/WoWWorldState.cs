using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using WindowsGameAutomationTools.ImageDetection;
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
        public Vector2 PlayerLocation { get; private set; }
        public float FacingDegrees { get; private set; }
        public int AttackerCount { get; private set; }

        public bool IsInRange { get; private set; }
        public bool IsInCombat { get; private set; }
        public bool CanChargeTarget { get; private set; }
        public bool HeroicStrikeQueued { get; private set; }

        public bool IsAutoAttacking { get; private set; }
        public bool BattleShoutActive { get; private set; }

        public bool FacingWrongWay { get; private set; }

        public WoWWorldState()
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

            IsInRange = false;
            IsInCombat = false;
            CanChargeTarget = false;
            HeroicStrikeQueued = false;

            FacingWrongWay = false;

            //TesseractEngineSingleton.Instance.SetVariable("tessedit_char_whitelist", "0123456789-.");
        }

        public static WoWWorldState GetWoWWorldState()
        {
            WoWWorldState currentState = new WoWWorldState();

            Bitmap wowBitmap = ScreenCapture.CaptureBitmapFromDesktopAndRectangle(new Rectangle(0, 0, WoWWorldStateImageConstants.WIDTH_OF_SCREEN_TO_SLICE, WoWWorldStateImageConstants.HEIGHT_OF_SCREEN_TO_SLICE));
            currentState.UpdateFromBitmap(wowBitmap);
            wowBitmap.Dispose();

            return currentState;
        }

        public void UpdateFromBitmap(Bitmap bmp)
        {
            Initialized = true;

            UpdatePlayerHpPercent(bmp);
            UpdateResourcePercent(bmp);
            UpdateTargetHpPercent(bmp);
            UpdateMapX(bmp);
            UpdateMapY(bmp);
            PlayerLocation = new Vector2(MapX, MapY);
            UpdateFacingDegrees(bmp);
            UpdateAttackerCount(bmp);

            UpdateIsInRange(bmp);
            UpdateIsInCombat(bmp);
            UpdateCanChargeTarget(bmp);
            UpdateHeroicStrikeQueued(bmp);
            UpdateMultiBoolOne(bmp);

            UpdateFacingWrongWay(bmp);
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

        public void UpdatePlayerHpPercent(Bitmap bmp)
        {
            Color color = bmp.GetPixel(WoWWorldStateImageConstants.PLAYER_HP_PERCENT_POSITION.X, WoWWorldStateImageConstants.PLAYER_HP_PERCENT_POSITION.Y);
            PlayerHpPercent = GetIntFromColor(color);
        }

        public void UpdateResourcePercent(Bitmap bmp)
        {
            Color color = bmp.GetPixel(WoWWorldStateImageConstants.RESOURCE_PERCENT_POSITION.X, WoWWorldStateImageConstants.RESOURCE_PERCENT_POSITION.Y);
            ResourcePercent = GetIntFromColor(color);
        }

        public void UpdateTargetHpPercent(Bitmap bmp)
        {
            Color color = bmp.GetPixel(WoWWorldStateImageConstants.TARGET_PERCENT_POSITION.X, WoWWorldStateImageConstants.TARGET_PERCENT_POSITION.Y);
            TargetHpPercent = GetIntFromColor(color);
        }

        public void UpdateMapX(Bitmap bmp)
        {
            Color color = bmp.GetPixel(WoWWorldStateImageConstants.MAP_X_POSITION.X, WoWWorldStateImageConstants.MAP_X_POSITION.Y);
            MapX = GetFloatFromColor(color);
        }

        public void UpdateMapY(Bitmap bmp)
        {
            Color color = bmp.GetPixel(WoWWorldStateImageConstants.MAP_Y_POSITION.X, WoWWorldStateImageConstants.MAP_Y_POSITION.Y);
            MapY = GetFloatFromColor(color);
        }

        public void UpdateFacingDegrees(Bitmap bmp)
        {
            Color color = bmp.GetPixel(WoWWorldStateImageConstants.FACING_DEGREES_POSITION.X, WoWWorldStateImageConstants.FACING_DEGREES_POSITION.Y);
            FacingDegrees = GetFloatFromColor(color);
        }

        public void UpdateAttackerCount(Bitmap bmp)
        {
            Color color = bmp.GetPixel(WoWWorldStateImageConstants.ATTACKER_COUNT_POSITION.X, WoWWorldStateImageConstants.ATTACKER_COUNT_POSITION.Y);
            AttackerCount = GetIntFromColor(color);
        }

        public void UpdateIsInRange(Bitmap bmp)
        {
            Color color = bmp.GetPixel(WoWWorldStateImageConstants.IS_IN_RANGE_POSITION.X, WoWWorldStateImageConstants.IS_IN_RANGE_POSITION.Y);
            IsInRange = GetBoolFromColor(color);
        }

        public void UpdateIsInCombat(Bitmap bmp)
        {
            Color color = bmp.GetPixel(WoWWorldStateImageConstants.IS_IN_COMBAT_POSITION.X, WoWWorldStateImageConstants.IS_IN_COMBAT_POSITION.Y);
            IsInCombat = GetBoolFromColor(color);
        }

        public void UpdateCanChargeTarget(Bitmap bmp)
        {
            Color color = bmp.GetPixel(WoWWorldStateImageConstants.CAN_CHARGE_TARGET_POSITION.X, WoWWorldStateImageConstants.CAN_CHARGE_TARGET_POSITION.Y);
            CanChargeTarget = GetBoolFromColor(color);
        }

        public void UpdateHeroicStrikeQueued(Bitmap bmp)
        {
            Color color = bmp.GetPixel(WoWWorldStateImageConstants.HEROIC_STRIKE_QUEUED_POSITION.X, WoWWorldStateImageConstants.HEROIC_STRIKE_QUEUED_POSITION.Y);
            HeroicStrikeQueued = GetBoolFromColor(color);
        }

        public void UpdateMultiBoolOne(Bitmap bmp)
        {
            Color color = bmp.GetPixel(WoWWorldStateImageConstants.MULTI_BOOL_ONE_POSITION.X, WoWWorldStateImageConstants.MULTI_BOOL_ONE_POSITION.Y);
            DecodeByte(color.R, out var r1, out var r2, out var r3, out var r4, out var r5, out var r6, out var r7, out var r8);

            IsAutoAttacking = r1;
            BattleShoutActive = r2;
        }

        public void UpdateFacingWrongWay(Bitmap bmp)
        {
            FacingWrongWay = WoWWorldStateImageConstants.FACING_WRONG_WAY_POSITIONS.MatchesSourceImage(bmp);
        }
    }
}
