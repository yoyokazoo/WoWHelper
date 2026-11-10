using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WindowsGameAutomationTools.ImageDetection;

namespace WoWHelper.Code.Config
{
    public class WowScreenConfigs
    {
        private const int RESOLUTION_MATCH_TOLERANCE = 20;

        public static WowScreenConfiguration GetForPrimaryScreen()
        {
            int width = Screen.PrimaryScreen.Bounds.Width;
            int height = Screen.PrimaryScreen.Bounds.Height;

            if (width == 1920 && height == 1080) return RESOLUTION_1920_X_1080;
            if (width == 3440 && height == 1440) return RESOLUTION_3440_X_1440;
            if (width == 2560 && height == 1600) return RESOLUTION_2560_X_1600;

            throw new Exception($"No screen config for resolution {width}x{height}!");
        }

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

            LootHeatmapX = 1495,
            LootHeatmapY = 516,
            LootHeatmapWidth = 426,
            LootHeatmapHeight = 341,

            LootHeatmapIgnoreX = 1635,
            LootHeatmapIgnoreY = 655,
            LootHeatmapIgnoreWidth = 160,
            LootHeatmapIgnoreHeight = 145,

            SlackScreenshotCropRegion = new Rectangle(0, 1005, 790, 261),

            MeleeRangeTargetMarkerDistance = 120,
            TooCloseToTurnWhileWalkingTargetMarkerDistance = 200,
        };

        public static readonly WowScreenConfiguration RESOLUTION_2560_X_1600 = new WowScreenConfiguration
        {
            Name = "2560x1600",
            Resolution = new Size(2560, 1600),

            LootHeatmapX = 1081,
            LootHeatmapY = 650,
            LootHeatmapWidth = 407,
            LootHeatmapHeight = 268,

            LootHeatmapIgnoreX = 1207,
            LootHeatmapIgnoreY = 731,
            LootHeatmapIgnoreWidth = 143,
            LootHeatmapIgnoreHeight = 138,

            SlackScreenshotCropRegion = new Rectangle(52, 1162, 827, 239),

            MeleeRangeTargetMarkerDistance = 140,
            TooCloseToTurnWhileWalkingTargetMarkerDistance = 240,

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

            LootHeatmapX = 812,
            LootHeatmapY = 395,
            LootHeatmapWidth = 303,
            LootHeatmapHeight = 231,

            LootHeatmapIgnoreX = 919,
            LootHeatmapIgnoreY = 499,
            LootHeatmapIgnoreWidth = 77,
            LootHeatmapIgnoreHeight = 75,

            MeleeRangeTargetMarkerDistance = 120,
            TooCloseToTurnWhileWalkingTargetMarkerDistance = 200,

            SlackScreenshotCropRegion = new Rectangle(39, 786, 552, 161),
        };
    }
}
