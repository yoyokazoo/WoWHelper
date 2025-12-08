using System.Drawing;
using System.Numerics;
using WindowsGameAutomationTools.ImageDetection;

namespace WoWHelper
{
    public static class WoWWorldStateImageConstants
    {
        public static readonly Color FALSE_COLOR = Color.FromArgb(255, 0, 0);
        public static readonly Color TRUE_COLOR = Color.FromArgb(0, 255, 0);

        public const int TEXT_LEFT_COORD = 59;
        public const int TEXT_TOP_COORD = 187;

        public const int TEXT_BOX_WIDTH = 100;
        public const int TEXT_BOX_HEIGHT = 40;

        public const int BOOL_LEFT_COORD = 260;
        public const int BOOL_TOP_COORD = 200;
        public const int BOOL_SECTION_HEIGHT = 70;

        public static readonly ImageMatchTextArea PLAYER_HP_PERCENT_POSITION = new ImageMatchTextArea(
            TEXT_LEFT_COORD, TEXT_TOP_COORD + (TEXT_BOX_HEIGHT * 0), TEXT_BOX_WIDTH, TEXT_BOX_HEIGHT
        );

        public static readonly ImageMatchTextArea RESOURCE_PERCENT_POSITION = new ImageMatchTextArea(
            TEXT_LEFT_COORD, TEXT_TOP_COORD + (TEXT_BOX_HEIGHT * 1), TEXT_BOX_WIDTH, TEXT_BOX_HEIGHT
        );

        public static readonly ImageMatchTextArea TARGET_PERCENT_POSITION = new ImageMatchTextArea(
            TEXT_LEFT_COORD, TEXT_TOP_COORD + (TEXT_BOX_HEIGHT * 2), TEXT_BOX_WIDTH, TEXT_BOX_HEIGHT
        );

        public static readonly ImageMatchTextArea MAP_X_POSITION = new ImageMatchTextArea(
            TEXT_LEFT_COORD, TEXT_TOP_COORD + (TEXT_BOX_HEIGHT * 3), TEXT_BOX_WIDTH, TEXT_BOX_HEIGHT
        );

        public static readonly ImageMatchTextArea MAP_Y_POSITION = new ImageMatchTextArea(
            TEXT_LEFT_COORD, TEXT_TOP_COORD + (TEXT_BOX_HEIGHT * 4), TEXT_BOX_WIDTH, TEXT_BOX_HEIGHT
        );

        public static readonly ImageMatchTextArea FACING_DEGREES_POSITION = new ImageMatchTextArea(
            TEXT_LEFT_COORD, TEXT_TOP_COORD + (TEXT_BOX_HEIGHT * 5), TEXT_BOX_WIDTH, TEXT_BOX_HEIGHT
        );

        public static readonly Point IS_IN_RANGE_POSITION = new Point(BOOL_LEFT_COORD, BOOL_TOP_COORD + (BOOL_SECTION_HEIGHT * 0));
        public static readonly Point IS_IN_COMBAT_POSITION = new Point(BOOL_LEFT_COORD, BOOL_TOP_COORD + (BOOL_SECTION_HEIGHT * 1));
        public static readonly Point CAN_CHARGE_TARGET_POSITION = new Point(BOOL_LEFT_COORD, BOOL_TOP_COORD + (BOOL_SECTION_HEIGHT * 2));
    }
}
