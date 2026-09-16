using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WindowsGameAutomationTools.ImageDetection;

namespace WoWHelper.Code.Config
{
    public class WowScreenConfigs
    {
        private const int RESOLUTION_MATCH_TOLERANCE = 5;

        public static WowScreenConfiguration GetForBitmap(Bitmap bitmap)
        {
            var candidates = new[]
            {
                RESOLUTION_3440_X_1440,
                RESOLUTION_2560_X_1600,
                RESOLUTION_1920_X_1080,
            };

            foreach (var config in candidates)
            {
                if (Math.Abs(bitmap.Width - config.Resolution.Width) <= RESOLUTION_MATCH_TOLERANCE &&
                    Math.Abs(bitmap.Height - config.Resolution.Height) <= RESOLUTION_MATCH_TOLERANCE)
                    return config;
            }

            throw new Exception($"No screen config for bitmap resolution {bitmap.Width}x{bitmap.Height}");
        }

        public static readonly WowScreenConfiguration RESOLUTION_3440_X_1440 = new WowScreenConfiguration
        {
            Name = "3440x1440",
            Resolution = new Size(3440, 1440),

            DynamiteAndDummyX = 1720,
            DynamiteAndDummyY = 720,

            LootHeatmapX = 1495,
            LootHeatmapY = 516,
            LootHeatmapWidth = 426,
            LootHeatmapHeight = 341,

            // Does this account for /console cameraDistanceMaxZoomFactor 3.5 ??
            LootHeatmapIgnoreX = 1635,
            LootHeatmapIgnoreY = 655,
            LootHeatmapIgnoreWidth = 160,
            LootHeatmapIgnoreHeight = 145,

            SlackScreenshotCropRegion = new Rectangle(0, 1005, 790, 261),

            FacingWrongWayPositions = new ImageMatchColorPositions(0, 0, new List<ColorPosition>
                {
                    new ColorPosition(1529, 217, WowScreenConfiguration.ERROR_TEXT_COLOR),
                    new ColorPosition(1606, 219, WowScreenConfiguration.ERROR_TEXT_COLOR),
                    new ColorPosition(1647, 218, WowScreenConfiguration.ERROR_TEXT_COLOR),
                }),

            TooFarAwayPositions = new ImageMatchColorPositions(0, 0, new List<ColorPosition>
                {
                    new ColorPosition(1589, 217, WowScreenConfiguration.ERROR_TEXT_COLOR),
                    new ColorPosition(1641, 222, WowScreenConfiguration.ERROR_TEXT_COLOR),
                    new ColorPosition(1666, 220, WowScreenConfiguration.ERROR_TEXT_COLOR),
                }),

            TargetNeedsToBeInFrontPositions = new ImageMatchColorPositions(0, 0, new List<ColorPosition>
                {
                    new ColorPosition(1506, 216, WowScreenConfiguration.ERROR_TEXT_COLOR),
                    new ColorPosition(1524, 218, WowScreenConfiguration.ERROR_TEXT_COLOR),
                    // new(1531, 219, errorText), // spotty.  Sometimes when the message spams it switches X position by a pixel.  Need to pick pixels where this can be ignored
                }),

            InvalidTargetPositions = new ImageMatchColorPositions(0, 0, new List<ColorPosition>
                {
                    new ColorPosition(1637, 217, WowScreenConfiguration.ERROR_TEXT_COLOR),
                    new ColorPosition(1654, 221, WowScreenConfiguration.ERROR_TEXT_COLOR),
                    new ColorPosition(1662, 216, WowScreenConfiguration.ERROR_TEXT_COLOR),
                }),

            OutOfRangePositions = new ImageMatchColorPositions(0, 0, new List<ColorPosition>
                {
                    new ColorPosition(1644, 208, WowScreenConfiguration.ERROR_TEXT_COLOR),
                    new ColorPosition(1665, 225, WowScreenConfiguration.ERROR_TEXT_COLOR),
                    new ColorPosition(1677, 214, WowScreenConfiguration.ERROR_TEXT_COLOR),
                }),

            NotInLineOfSightPositions = new ImageMatchColorPositions(0, 0, new List<ColorPosition>
                {
                    new ColorPosition(1563, 216, WowScreenConfiguration.ERROR_TEXT_COLOR),
                    new ColorPosition(1601, 225, WowScreenConfiguration.ERROR_TEXT_COLOR),
                    new ColorPosition(1631, 220, WowScreenConfiguration.ERROR_TEXT_COLOR),
                }),

            BreathBarScreenPositions = new ImageMatchColorPositions(0, 0, new List<ColorPosition>
                {
                    new ColorPosition(1568, 173, WowScreenConfiguration.BREATH_BAR_COLOR_ONE),
                    new ColorPosition(1561, 173, WowScreenConfiguration.BREATH_BAR_COLOR_TWO),
                }),
        };

        public static readonly WowScreenConfiguration RESOLUTION_2560_X_1600 = new WowScreenConfiguration
        {
            Name = "2560x1600",
            Resolution = new Size(2560, 1600),

            DynamiteAndDummyX = 1280,
            DynamiteAndDummyY = 800,

            LootHeatmapX = 1081,
            LootHeatmapY = 650,
            LootHeatmapWidth = 407,
            LootHeatmapHeight = 268,

            LootHeatmapIgnoreX = 1207,
            LootHeatmapIgnoreY = 731,
            LootHeatmapIgnoreWidth = 143,
            LootHeatmapIgnoreHeight = 138,

            SlackScreenshotCropRegion = new Rectangle(52, 1162, 827, 239),

            FacingWrongWayPositions = new ImageMatchColorPositions(0, 0, new List<ColorPosition>
                {
                    new ColorPosition(1071, 242, WowScreenConfiguration.ERROR_TEXT_COLOR),
                    new ColorPosition(1102, 244, WowScreenConfiguration.ERROR_TEXT_COLOR),
                    new ColorPosition(1127, 249, WowScreenConfiguration.ERROR_TEXT_COLOR),
                }),

            TooFarAwayPositions = new ImageMatchColorPositions(0, 0, new List<ColorPosition>
                {
                    new ColorPosition(1137, 245, WowScreenConfiguration.ERROR_TEXT_COLOR),
                    new ColorPosition(1161, 245, WowScreenConfiguration.ERROR_TEXT_COLOR),
                    new ColorPosition(1172, 252, WowScreenConfiguration.ERROR_TEXT_COLOR),
                }),

            TargetNeedsToBeInFrontPositions = new ImageMatchColorPositions(0, 0, new List<ColorPosition>
                {
                    new ColorPosition(1048, 243, WowScreenConfiguration.ERROR_TEXT_COLOR),
                    new ColorPosition(1060, 250, WowScreenConfiguration.ERROR_TEXT_COLOR),
                    new ColorPosition(1093, 259, WowScreenConfiguration.ERROR_TEXT_COLOR),
                }),

            InvalidTargetPositions = new ImageMatchColorPositions(0, 0, new List<ColorPosition>
                {
                    new ColorPosition(1187, 235, WowScreenConfiguration.ERROR_TEXT_COLOR),
                    new ColorPosition(1205, 240, WowScreenConfiguration.ERROR_TEXT_COLOR),
                    new ColorPosition(1231, 248, WowScreenConfiguration.ERROR_TEXT_COLOR),
                }),

            OutOfRangePositions = new ImageMatchColorPositions(0, 0, new List<ColorPosition>
                {
                    new ColorPosition(1200, 233, WowScreenConfiguration.ERROR_TEXT_COLOR),
                    new ColorPosition(1225, 246, WowScreenConfiguration.ERROR_TEXT_COLOR),
                    new ColorPosition(1234, 246, WowScreenConfiguration.ERROR_TEXT_COLOR),
                }),

            NotInLineOfSightPositions = new ImageMatchColorPositions(0, 0, new List<ColorPosition>
                {
                    new ColorPosition(0, 0, WowScreenConfiguration.ERROR_TEXT_COLOR),
                    new ColorPosition(0, 0, WowScreenConfiguration.ERROR_TEXT_COLOR),
                    new ColorPosition(0, 0, WowScreenConfiguration.ERROR_TEXT_COLOR),
                }),

            BreathBarScreenPositions = new ImageMatchColorPositions(0, 0, new List<ColorPosition>
                {
                    new ColorPosition(1141, 190, WowScreenConfiguration.BREATH_BAR_COLOR_ONE),
                    new ColorPosition(1149, 186, WowScreenConfiguration.BREATH_BAR_COLOR_THREE),
                }),

            TradeWindowScreenPositions = new ImageMatchColorPositions(0, 0, new List<ColorPosition>
                {
                    new ColorPosition(31, 328, WowScreenConfiguration.TRADE_SCREEN_COLOR_ONE),
                    new ColorPosition(169, 220, WowScreenConfiguration.TRADE_SCREEN_COLOR_TWO),
                    new ColorPosition(169, 350, WowScreenConfiguration.TRADE_SCREEN_COLOR_THREE),
                }),

            TradeWindowAcceptedScreenPositions = new ImageMatchColorPositions(0, 0, new List<ColorPosition>
                {
                    new ColorPosition(360, 377, WowScreenConfiguration.TRADE_SCREEN_ACCEPTED_COLOR_ONE),
                    new ColorPosition(360, 382, WowScreenConfiguration.TRADE_SCREEN_ACCEPTED_COLOR_TWO),
                }),

            TradeWindowConfirmationScreenPositions = new ImageMatchColorPositions(0, 0, new List<ColorPosition>
                {
                    new ColorPosition(889, 290, WowScreenConfiguration.TRADE_SCREEN_CONFIRMATION_COLOR_ONE),
                    new ColorPosition(947, 329, WowScreenConfiguration.TRADE_SCREEN_CONFIRMATION_COLOR_TWO),
                    new ColorPosition(1051, 400, WowScreenConfiguration.TRADE_SCREEN_CONFIRMATION_COLOR_THREE),
                }),

            TradeWindowRecipientTextArea = new ImageMatchTextArea(464, 222, 158, 33),
        };

        public static readonly WowScreenConfiguration RESOLUTION_1920_X_1080 = new WowScreenConfiguration
        {
            Name = "1920x1080",
            Resolution = new Size(1920, 1080),

            DynamiteAndDummyX = 960,
            DynamiteAndDummyY = 540,

            LootHeatmapX = 812,
            LootHeatmapY = 395,
            LootHeatmapWidth = 303,
            LootHeatmapHeight = 231,

            LootHeatmapIgnoreX = 919,
            LootHeatmapIgnoreY = 499,
            LootHeatmapIgnoreWidth = 77,
            LootHeatmapIgnoreHeight = 75,

            SlackScreenshotCropRegion = new Rectangle(39, 786, 552, 161),

            FacingWrongWayPositions = new ImageMatchColorPositions(0, 0, new List<ColorPosition>
                {
                    new ColorPosition(818, 163, WowScreenConfiguration.ERROR_TEXT_COLOR),
                    new ColorPosition(830, 161, WowScreenConfiguration.ERROR_TEXT_COLOR),
                    new ColorPosition(842, 170, WowScreenConfiguration.ERROR_TEXT_COLOR),
                }),

            TooFarAwayPositions = new ImageMatchColorPositions(0, 0, new List<ColorPosition>
                {
                    new ColorPosition(863, 165, WowScreenConfiguration.ERROR_TEXT_COLOR),
                    new ColorPosition(875, 161, WowScreenConfiguration.ERROR_TEXT_COLOR),
                    new ColorPosition(887, 170, WowScreenConfiguration.ERROR_TEXT_COLOR),
                }),

            TargetNeedsToBeInFrontPositions = new ImageMatchColorPositions(0, 0, new List<ColorPosition>
                {
                    new ColorPosition(802, 157, WowScreenConfiguration.ERROR_TEXT_COLOR),
                    new ColorPosition(812, 161, WowScreenConfiguration.ERROR_TEXT_COLOR),
                    new ColorPosition(820, 163, WowScreenConfiguration.ERROR_TEXT_COLOR),
                }),

            InvalidTargetPositions = new ImageMatchColorPositions(0, 0, new List<ColorPosition>
                {
                    new ColorPosition(897, 162, WowScreenConfiguration.ERROR_TEXT_COLOR),
                    new ColorPosition(908, 161, WowScreenConfiguration.ERROR_TEXT_COLOR),
                    new ColorPosition(919, 169, WowScreenConfiguration.ERROR_TEXT_COLOR),
                }),

            OutOfRangePositions = new ImageMatchColorPositions(0, 0, new List<ColorPosition>
                {
                    new ColorPosition(904, 157, WowScreenConfiguration.ERROR_TEXT_COLOR),
                    new ColorPosition(919, 170, WowScreenConfiguration.ERROR_TEXT_COLOR),
                    new ColorPosition(929, 161, WowScreenConfiguration.ERROR_TEXT_COLOR),
                }),

            NotInLineOfSightPositions = new ImageMatchColorPositions(0, 0, new List<ColorPosition>
                {
                    new ColorPosition(843, 161, WowScreenConfiguration.ERROR_TEXT_COLOR),
                    new ColorPosition(857, 170, WowScreenConfiguration.ERROR_TEXT_COLOR),
                    new ColorPosition(872, 161, WowScreenConfiguration.ERROR_TEXT_COLOR),
                }),

            BreathBarScreenPositions = new ImageMatchColorPositions(0, 0, new List<ColorPosition>
                {
                    new ColorPosition(846, 130, WowScreenConfiguration.BREATH_BAR_COLOR_ONE),
                    new ColorPosition(845, 134, WowScreenConfiguration.BREATH_BAR_COLOR_TWO),
                }),
        };
    }
}
