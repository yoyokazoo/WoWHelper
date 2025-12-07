using WindowsGameAutomationTools.ImageDetection;

namespace WoWHelper
{
    public static class WoWWorldStateImageConstants
    {

        #region HpResourcePercents

        public const int TEXT_RIGHT_COORD = 59;
        public const int TEXT_TOP_COORD = 187;

        public const int TEXT_BOX_WIDTH = 100;
        public const int TEXT_BOX_HEIGHT = 40;

        public static readonly ImageMatchTextArea PLAYER_HP_PERCENT_POSITION = new ImageMatchTextArea(
            TEXT_RIGHT_COORD, TEXT_TOP_COORD + (TEXT_BOX_HEIGHT * 0), TEXT_BOX_WIDTH, TEXT_BOX_HEIGHT
        );

        public static readonly ImageMatchTextArea RESOURCE_PERCENT_POSITION = new ImageMatchTextArea(
            TEXT_RIGHT_COORD, TEXT_TOP_COORD + (TEXT_BOX_HEIGHT * 1), TEXT_BOX_WIDTH, TEXT_BOX_HEIGHT
        );

        public static readonly ImageMatchTextArea TARGET_PERCENT_POSITION = new ImageMatchTextArea(
            TEXT_RIGHT_COORD, TEXT_TOP_COORD + (TEXT_BOX_HEIGHT * 2), TEXT_BOX_WIDTH, TEXT_BOX_HEIGHT
        );

        public static readonly ImageMatchTextArea MAP_X_POSITION = new ImageMatchTextArea(
            TEXT_RIGHT_COORD, TEXT_TOP_COORD + (TEXT_BOX_HEIGHT * 3), TEXT_BOX_WIDTH, TEXT_BOX_HEIGHT
        );

        public static readonly ImageMatchTextArea MAP_Y_POSITION = new ImageMatchTextArea(
            TEXT_RIGHT_COORD, TEXT_TOP_COORD + (TEXT_BOX_HEIGHT * 4), TEXT_BOX_WIDTH, TEXT_BOX_HEIGHT
        );

        public static readonly ImageMatchTextArea FACING_DEGREES_POSITION = new ImageMatchTextArea(
            TEXT_RIGHT_COORD, TEXT_TOP_COORD + (TEXT_BOX_HEIGHT * 5), TEXT_BOX_WIDTH, TEXT_BOX_HEIGHT
        );

        #endregion
    }
}
