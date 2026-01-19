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

        public static readonly Color LOGIN_SCREEN_COLOR_ONE = Color.FromArgb(241, 221, 175);
        public static readonly Color LOGIN_SCREEN_COLOR_TWO = Color.FromArgb(52, 51, 60);
        public static readonly Color LOGIN_SCREEN_COLOR_THREE = Color.FromArgb(229, 38, 39);

        #endregion

        // /console cameraDistanceMaxZoomFactor 2.6
        // TODO: init instead of set would be nice. What would it take to migrate?
        public string Name { get; set; }
        public Size Resolution { get; set; }

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

        public int TextLeftCoord { get; set; }
        public int TextTopCoord { get; set; }
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
