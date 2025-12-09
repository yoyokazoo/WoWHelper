using System.Drawing;
using System.Numerics;
using WindowsGameAutomationTools.ImageDetection;

namespace WoWHelper
{
    public static class WoWWorldStateImageConstants
    {
        // Center of color box in text column
        public const int TEXT_LEFT_COORD = 75;
        public const int TEXT_TOP_COORD = 200;
        public const int TEXT_BOX_HEIGHT = 40;
        public const int TEXT_BOX_WIDTH = 270;

        public const int BOOL_LEFT_COORD = TEXT_LEFT_COORD + TEXT_BOX_WIDTH;
        public const int BOOL_TOP_COORD = TEXT_TOP_COORD;
        public const int BOOL_SECTION_HEIGHT = TEXT_BOX_HEIGHT;

        public static readonly Point PLAYER_HP_PERCENT_POSITION = new Point(TEXT_LEFT_COORD, TEXT_TOP_COORD + (TEXT_BOX_HEIGHT * 0));
        public static readonly Point RESOURCE_PERCENT_POSITION = new Point(TEXT_LEFT_COORD, TEXT_TOP_COORD + (TEXT_BOX_HEIGHT * 1));
        public static readonly Point TARGET_PERCENT_POSITION = new Point(TEXT_LEFT_COORD, TEXT_TOP_COORD + (TEXT_BOX_HEIGHT * 2));
        public static readonly Point MAP_X_POSITION = new Point(TEXT_LEFT_COORD, TEXT_TOP_COORD + (TEXT_BOX_HEIGHT * 3));
        public static readonly Point MAP_Y_POSITION = new Point(TEXT_LEFT_COORD, TEXT_TOP_COORD + (TEXT_BOX_HEIGHT * 4));
        public static readonly Point FACING_DEGREES_POSITION = new Point(TEXT_LEFT_COORD, TEXT_TOP_COORD + (TEXT_BOX_HEIGHT * 5));

        public static readonly Point IS_IN_RANGE_POSITION = new Point(BOOL_LEFT_COORD, BOOL_TOP_COORD + (BOOL_SECTION_HEIGHT * 0));
        public static readonly Point IS_IN_COMBAT_POSITION = new Point(BOOL_LEFT_COORD, BOOL_TOP_COORD + (BOOL_SECTION_HEIGHT * 1));
        public static readonly Point CAN_CHARGE_TARGET_POSITION = new Point(BOOL_LEFT_COORD, BOOL_TOP_COORD + (BOOL_SECTION_HEIGHT * 2));
        public static readonly Point HEROIC_STRIKE_QUEUED_POSITION = new Point(BOOL_LEFT_COORD, BOOL_TOP_COORD + (BOOL_SECTION_HEIGHT * 3));
    }
}
