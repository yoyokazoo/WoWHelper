using System;
using System.Collections.Generic;
using System.Drawing;
using WindowsGameAutomationTools.ImageDetection;

namespace WoWHelper
{
    public class WowScreenConfiguration
    {
        #region Constants

        public static readonly Color ERROR_TEXT_COLOR = Color.FromArgb(255, 25, 25);

        public static readonly Color BREATH_BAR_COLOR_ONE = Color.FromArgb(0, 77, 155);
        public static readonly Color BREATH_BAR_COLOR_TWO = Color.FromArgb(0, 31, 62);

        public static readonly Color LOGIN_SCREEN_COLOR_ONE = Color.FromArgb(254, 246, 246);
        public static readonly Color LOGIN_SCREEN_COLOR_TWO = Color.FromArgb(214, 33, 33);
        public static readonly Color LOGIN_SCREEN_COLOR_THREE = Color.FromArgb(8, 12, 8);

        public static readonly Color TRADE_SCREEN_COLOR_ONE = Color.FromArgb(93, 88, 86);
        public static readonly Color TRADE_SCREEN_COLOR_TWO = Color.FromArgb(167, 165, 161);
        public static readonly Color TRADE_SCREEN_COLOR_THREE = Color.FromArgb(212, 177, 42);

        public static readonly Color TRADE_SCREEN_ACCEPTED_COLOR_ONE = Color.FromArgb(38, 64, 33);
        public static readonly Color TRADE_SCREEN_ACCEPTED_COLOR_TWO = Color.FromArgb(140, 223, 0);

        public static readonly Color TRADE_SCREEN_CONFIRMATION_COLOR_ONE = Color.FromArgb(68, 66, 64);
        public static readonly Color TRADE_SCREEN_CONFIRMATION_COLOR_TWO = Color.FromArgb(233, 181, 43);
        public static readonly Color TRADE_SCREEN_CONFIRMATION_COLOR_THREE = Color.FromArgb(87, 0, 0);

        #endregion

        // /console cameraDistanceMaxZoomFactor 2.6
        // TODO: init instead of set would be nice. What would it take to migrate?
        public string Name { get; set; }
        public Size Resolution { get; set; }

        // TODO: instead of grabbing a giant chunk, modify addon to be smaller, only grab that chunk and the text notification space chunk
        // Note: we don't need to include the heatmap in this slice, it's handled on its own
        public int WidthOfScreenToSlice { get; set; }
        public int HeightOfScreenToSlice { get; set; }

        public int DynamiteAndDummyX { get; set; }
        public int DynamiteAndDummyY { get; set; }

        public int LootHeatmapX { get; set; }
        public int LootHeatmapY { get; set; }
        public int LootHeatmapWidth { get; set; }
        public int LootHeatmapHeight { get; set; }

        public int LootHeatmapIgnoreX { get; set; }
        public int LootHeatmapIgnoreY { get; set; }
        public int LootHeatmapIgnoreWidth { get; set; }
        public int LootHeatmapIgnoreHeight { get; set; }

        /// <summary>
        /// X of the center of the top left color box
        /// </summary>
        public int TextLeftCoord { get; set; }
        public int TextTopCoord { get; set; }

        /// <summary>
        /// Vertical distance between the centers of the color boxes
        /// </summary>
        public int TextBoxHeight { get; set; }
        public int TextBoxWidth { get; set; }

        public int BoolLeftCoord => TextLeftCoord + TextBoxWidth;
        public int BoolTopCoord => TextTopCoord;
        public int BoolSectionHeight => TextBoxHeight;

        // Error text detections
        public ImageMatchColorPositions FacingWrongWayPositions { get; set; }
        public ImageMatchColorPositions TooFarAwayPositions { get; set; }
        public ImageMatchColorPositions TargetNeedsToBeInFrontPositions { get; set; }
        public ImageMatchColorPositions InvalidTargetPositions { get; set; }
        public ImageMatchColorPositions OutOfRangePositions { get; set; }

        // Login screen detections
        public ImageMatchColorPositions OnLoginScreenPositions { get; set; }

        // Breath bar detections
        public ImageMatchColorPositions BreathBarScreenPositions { get; set; }

        // Trade window
        public ImageMatchColorPositions TradeWindowScreenPositions { get; set; }
        public ImageMatchColorPositions TradeWindowAcceptedScreenPositions { get; set; }
        public ImageMatchColorPositions TradeWindowConfirmationScreenPositions { get; set; }
        public ImageMatchTextArea TradeWindowRecipientTextArea { get; set; }

        // Text readback points (computed)
        public Point MapXPosition => new Point(TextLeftCoord, TextTopCoord + (TextBoxHeight * 3));
        public Point MapYPosition => new Point(TextLeftCoord, TextTopCoord + (TextBoxHeight * 4));
        public Point FacingDegreesPosition => new Point(TextLeftCoord, TextTopCoord + (TextBoxHeight * 5));

        // Multi bool readback points (computed)
        public Point MultiBoolOnePosition => new Point(BoolLeftCoord, BoolTopCoord + (BoolSectionHeight * 4));
        public Point MultiBoolTwoPosition => new Point(BoolLeftCoord, BoolTopCoord + (BoolSectionHeight * 5));

        // Multi int readback points (computed)
        public Point MultiIntOnePosition => new Point(BoolLeftCoord, BoolTopCoord + (BoolSectionHeight * 6));
        public Point MultiIntTwoPosition => new Point(BoolLeftCoord, BoolTopCoord + (BoolSectionHeight * 7));
    }
}
