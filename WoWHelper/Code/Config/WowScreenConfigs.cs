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
        public static readonly WowScreenConfiguration RESOLUTION_3440_X_1440 = new WowScreenConfiguration
        {
            Name = "3440x1440",
            Resolution = new Size(3440, 1440),

            WidthOfScreenToSlice = 1700,
            HeightOfScreenToSlice = 800,

            DynamiteAndDummyX = 1720,
            DynamiteAndDummyY = 690,

            LootHeatmapX = 1495,
            LootHeatmapY = 516,
            LootHeatmapWidth = 426,
            LootHeatmapHeight = 341,

            LootHeatmapIgnoreX = 1655,
            LootHeatmapIgnoreY = 655,
            LootHeatmapIgnoreWidth = 130,
            LootHeatmapIgnoreHeight = 135,

            TextLeftCoord = 75,
            TextTopCoord = 200,
            TextBoxHeight = 40,
            TextBoxWidth = 270,

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

            OnLoginScreenPositions = new ImageMatchColorPositions(0, 0, new List<ColorPosition>
                {
                    new ColorPosition(581, 109, WowScreenConfiguration.LOGIN_SCREEN_COLOR_ONE),
                    new ColorPosition(705, 64,  WowScreenConfiguration.LOGIN_SCREEN_COLOR_TWO),
                    new ColorPosition(579, 248, WowScreenConfiguration.LOGIN_SCREEN_COLOR_THREE),
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

            WidthOfScreenToSlice = 0,
            HeightOfScreenToSlice = 0,

            DynamiteAndDummyX = 1273,
            DynamiteAndDummyY = 739,

            LootHeatmapX = 1081,
            LootHeatmapY = 650,
            LootHeatmapWidth = 407,
            LootHeatmapHeight = 268,

            LootHeatmapIgnoreX = 1207,
            LootHeatmapIgnoreY = 731,
            LootHeatmapIgnoreWidth = 143,
            LootHeatmapIgnoreHeight = 138,

            TextLeftCoord = 84,
            TextTopCoord = 224,
            TextBoxHeight = 45,
            TextBoxWidth = 300,

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

            OnLoginScreenPositions = new ImageMatchColorPositions(0, 0, new List<ColorPosition>
                {
                    new ColorPosition(103, 78, WowScreenConfiguration.LOGIN_SCREEN_COLOR_ONE),
                    new ColorPosition(116, 164,  WowScreenConfiguration.LOGIN_SCREEN_COLOR_TWO),
                    new ColorPosition(147, 48, WowScreenConfiguration.LOGIN_SCREEN_COLOR_THREE),
                }),

            BreathBarScreenPositions = new ImageMatchColorPositions(0, 0, new List<ColorPosition>
                {
                    new ColorPosition(846, 130, WowScreenConfiguration.BREATH_BAR_COLOR_ONE),
                    new ColorPosition(845, 134, WowScreenConfiguration.BREATH_BAR_COLOR_TWO),
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

            WidthOfScreenToSlice = 950,
            HeightOfScreenToSlice = 200,

            DynamiteAndDummyX = 960,
            DynamiteAndDummyY = 500,

            LootHeatmapX = 812,
            LootHeatmapY = 395,
            LootHeatmapWidth = 303,
            LootHeatmapHeight = 231,

            LootHeatmapIgnoreX = 919,
            LootHeatmapIgnoreY = 499,
            LootHeatmapIgnoreWidth = 77,
            LootHeatmapIgnoreHeight = 75,

            TextLeftCoord = 57,
            TextTopCoord = 151,
            TextBoxHeight = 30,
            TextBoxWidth = 203,

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

            OnLoginScreenPositions = new ImageMatchColorPositions(0, 0, new List<ColorPosition>
                {
                    new ColorPosition(103, 78, WowScreenConfiguration.LOGIN_SCREEN_COLOR_ONE),
                    new ColorPosition(116, 164,  WowScreenConfiguration.LOGIN_SCREEN_COLOR_TWO),
                    new ColorPosition(147, 48, WowScreenConfiguration.LOGIN_SCREEN_COLOR_THREE),
                }),

            BreathBarScreenPositions = new ImageMatchColorPositions(0, 0, new List<ColorPosition>
                {
                    new ColorPosition(846, 130, WowScreenConfiguration.BREATH_BAR_COLOR_ONE),
                    new ColorPosition(845, 134, WowScreenConfiguration.BREATH_BAR_COLOR_TWO),
                }),
        };
    }
}
