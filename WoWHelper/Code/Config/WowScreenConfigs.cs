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

        public static readonly WowScreenConfiguration RESOLUTION_1920_X_1080 = new WowScreenConfiguration
        {
            Name = "1920x1080",
            Resolution = new Size(1920, 1080),

            WidthOfScreenToSlice = 1700,
            HeightOfScreenToSlice = 800,

            DynamiteAndDummyX = 960,
            DynamiteAndDummyY = 500,

            TextLeftCoord = 57,
            TextTopCoord = 151,
            TextBoxHeight = 30,
            TextBoxWidth = 203,

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
                    new ColorPosition(1532, 215, WowScreenConfiguration.ERROR_TEXT_COLOR),
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
    }
}
