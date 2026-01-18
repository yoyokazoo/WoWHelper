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

            // Your original file had OUT_OF_RANGE identical to INVALID_TARGET.
            // Keeping them separate in the config lets you diverge later without breaking callers.
            OutOfRangePositions = new ImageMatchColorPositions(0, 0, new List<ColorPosition>
                {
                    new ColorPosition(1637, 217, WowScreenConfiguration.ERROR_TEXT_COLOR),
                    new ColorPosition(1654, 221, WowScreenConfiguration.ERROR_TEXT_COLOR),
                    new ColorPosition(1662, 216, WowScreenConfiguration.ERROR_TEXT_COLOR),
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
