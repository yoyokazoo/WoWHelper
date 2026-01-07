using System.Collections.Generic;
using System.Drawing;
using WindowsGameAutomationTools.ImageDetection;

namespace WoWHelper
{
    public static class WowImageConstants
    {
        public const int WIDTH_OF_SCREEN_TO_SLICE = 1700;
        public const int HEIGHT_OF_SCREEN_TO_SLICE = 800;

        // Center of color box in text column
        public const int TEXT_LEFT_COORD = 75;
        public const int TEXT_TOP_COORD = 200;
        public const int TEXT_BOX_HEIGHT = 40;
        public const int TEXT_BOX_WIDTH = 270;

        public const int BOOL_LEFT_COORD = TEXT_LEFT_COORD + TEXT_BOX_WIDTH;
        public const int BOOL_TOP_COORD = TEXT_TOP_COORD;
        public const int BOOL_SECTION_HEIGHT = TEXT_BOX_HEIGHT;

        private static readonly Color ERROR_TEXT_COLOR = Color.FromArgb(255, 25, 25);

        public static readonly ImageMatchColorPositions FACING_WRONG_WAY_POSITIONS = new ImageMatchColorPositions(0, 0, new List<ColorPosition> {
            new ColorPosition(1529, 217, ERROR_TEXT_COLOR),
            new ColorPosition(1606, 219, ERROR_TEXT_COLOR),
            new ColorPosition(1647, 218, ERROR_TEXT_COLOR),
        });

        public static readonly ImageMatchColorPositions TOO_FAR_AWAY_POSITIONS = new ImageMatchColorPositions(0, 0, new List<ColorPosition> {
            new ColorPosition(1589, 217, ERROR_TEXT_COLOR),
            new ColorPosition(1641, 222, ERROR_TEXT_COLOR),
            new ColorPosition(1666, 220, ERROR_TEXT_COLOR),
        });

        public static readonly ImageMatchColorPositions TARGET_NEEDS_TO_BE_IN_FRONT_POSITIONS = new ImageMatchColorPositions(0, 0, new List<ColorPosition> {
            new ColorPosition(1506, 216, ERROR_TEXT_COLOR),
            new ColorPosition(1524, 218, ERROR_TEXT_COLOR),
            new ColorPosition(1531, 219, ERROR_TEXT_COLOR),
        });

        private static readonly Color LOGIN_SCREEN_COLOR_ONE = Color.FromArgb(241, 221, 175);
        private static readonly Color LOGIN_SCREEN_COLOR_TWO = Color.FromArgb(52, 51, 60);
        private static readonly Color LOGIN_SCREEN_COLOR_THREE = Color.FromArgb(229, 38, 39);

        public static readonly ImageMatchColorPositions ON_LOGIN_SCREEN_POSITIONS = new ImageMatchColorPositions(0, 0, new List<ColorPosition> {
            new ColorPosition(581, 109, LOGIN_SCREEN_COLOR_ONE),
            new ColorPosition(705, 64, LOGIN_SCREEN_COLOR_TWO),
            new ColorPosition(579, 248, LOGIN_SCREEN_COLOR_THREE),
        });

        private static readonly Color BREATH_BAR_COLOR_ONE = Color.FromArgb(0, 77, 155);
        private static readonly Color BREATH_BAR_COLOR_TWO = Color.FromArgb(0, 31, 62);

        public static readonly ImageMatchColorPositions BREATH_BAR_SCREEN_POSITIONS = new ImageMatchColorPositions(0, 0, new List<ColorPosition> {
            new ColorPosition(1568, 173, BREATH_BAR_COLOR_ONE),
            new ColorPosition(1561, 173, BREATH_BAR_COLOR_TWO)
        });

        public static readonly Point MAP_X_POSITION = new Point(TEXT_LEFT_COORD, TEXT_TOP_COORD + (TEXT_BOX_HEIGHT * 3));
        public static readonly Point MAP_Y_POSITION = new Point(TEXT_LEFT_COORD, TEXT_TOP_COORD + (TEXT_BOX_HEIGHT * 4));
        public static readonly Point FACING_DEGREES_POSITION = new Point(TEXT_LEFT_COORD, TEXT_TOP_COORD + (TEXT_BOX_HEIGHT * 5));

        public static readonly Point MULTI_BOOL_ONE_POSITION = new Point(BOOL_LEFT_COORD, BOOL_TOP_COORD + (BOOL_SECTION_HEIGHT * 4));
        public static readonly Point MULTI_BOOL_TWO_POSITION = new Point(BOOL_LEFT_COORD, BOOL_TOP_COORD + (BOOL_SECTION_HEIGHT * 5));

        public static readonly Point MULTI_INT_ONE_POSITION = new Point(BOOL_LEFT_COORD, BOOL_TOP_COORD + (BOOL_SECTION_HEIGHT * 6));
        public static readonly Point MULTI_INT_TWO_POSITION = new Point(BOOL_LEFT_COORD, BOOL_TOP_COORD + (BOOL_SECTION_HEIGHT * 7));
    }
}
