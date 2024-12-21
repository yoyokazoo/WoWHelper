using WindowsGameAutomationTools.ImageDetection;

namespace WoWHelper
{
    public static class WoWWorldStateImageConstants
    {

        #region HpResourcePercents

        public const int HP_RESOURCE_PERCENT_BOX_WIDTH = 50;
        public const int HP_RESOURCE_PERCENT_BOX_HEIGHT = 20;

        public static readonly ImageMatchTextArea HP_PERCENT_POSITION = new ImageMatchTextArea(
            223, 76, HP_RESOURCE_PERCENT_BOX_WIDTH, HP_RESOURCE_PERCENT_BOX_HEIGHT
        );

        public static readonly ImageMatchTextArea RESOURCE_PERCENT_POSITION = new ImageMatchTextArea(
            223, 95, HP_RESOURCE_PERCENT_BOX_WIDTH, HP_RESOURCE_PERCENT_BOX_HEIGHT
        );

        #endregion
    }
}
