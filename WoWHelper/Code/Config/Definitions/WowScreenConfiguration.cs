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

        // Pixel-row index 0 (see AddonLoadedPosition below): fixed sentinel painted by
        // the addon, exact match. Present (this exact color) means the addon is loaded
        // and the rest of the row is real; anything else -- including whatever's really
        // at this screen position when the addon isn't rendering, e.g. the login screen
        // -- means OnLoginScreen. Replaces the old text/UI pixel-signature match against
        // login-screen-specific colors.
        public static readonly Color ADDON_LOADED_COLOR = Color.FromArgb(96, 255, 117);

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

        // Region to crop Slack alert screenshots (e.g. unseen whisper) down
        // to, instead of sending the whole screen. Null means "not
        // configured for this resolution" -- callers should fall back to
        // the full screen in that case (see SlackFileUploadWorkaround).
        public Rectangle? SlackScreenshotCropRegion { get; set; }

        // Error text detections
        public ImageMatchColorPositions FacingWrongWayPositions { get; set; }
        public ImageMatchColorPositions TooFarAwayPositions { get; set; }
        public ImageMatchColorPositions TargetNeedsToBeInFrontPositions { get; set; }
        public ImageMatchColorPositions InvalidTargetPositions { get; set; }
        public ImageMatchColorPositions OutOfRangePositions { get; set; }

        // Breath bar detections
        public ImageMatchColorPositions BreathBarScreenPositions { get; set; }

        // Trade window
        public ImageMatchColorPositions TradeWindowScreenPositions { get; set; }
        public ImageMatchColorPositions TradeWindowAcceptedScreenPositions { get; set; }
        public ImageMatchColorPositions TradeWindowConfirmationScreenPositions { get; set; }
        public ImageMatchTextArea TradeWindowRecipientTextArea { get; set; }

        // Readback points for the machine-readable pixel row the Lua addon draws
        // via InitializePixelRow() (UIFunctions.lua): a row of PixelSize x
        // PixelSize swatches pinned to the screen's literal top-left corner.
        // Fixed and resolution-independent -- no per-resolution calibration
        // needed. We read the CENTER pixel of each swatch (not its top-left
        // corner) for margin against edge blur/anti-aliasing. PixelSize here
        // MUST match Lua's PIXEL_SIZE constant, and the indices passed to
        // PixelRowPoint() must match the AddSwatch(index, ...) calls there.
        //
        // The row is condensed to exactly the pixels actually read below --
        // no MultiBoolTwo/ClassBoolTwo/ClassIntOne Points exist because
        // nothing currently decodes them. Add one back here (and a matching
        // swatch in InitializePixelRow()) if/when a field needs it.
        private const int PixelSize = 3;
        private const int PixelCenterOffset = PixelSize / 2;

        private static Point PixelRowPoint(int index) =>
            new Point(index * PixelSize + PixelCenterOffset, PixelCenterOffset);

        // Index 0: fixed sentinel (see ADDON_LOADED_COLOR above), exact-color match --
        // not decoded via GetFloatFromColor/DecodeByte like the rest of the row.
        public Point AddonLoadedPosition => PixelRowPoint(0);

        public Point MapXPosition => PixelRowPoint(1);
        public Point MapYPosition => PixelRowPoint(2);
        public Point FacingDegreesPosition => PixelRowPoint(3);

        public Point MultiBoolOnePosition => PixelRowPoint(4);

        public Point MultiIntOnePosition => PixelRowPoint(5);
        public Point MultiIntTwoPosition => PixelRowPoint(6);

        public Point ClassBoolOnePosition => PixelRowPoint(7);

        // Bounding rectangle covering every pixel anything in this codebase reads off a
        // captured screen bitmap: the pixel row (top-left corner), the red-error-text/
        // breath-bar cluster, and the trade-window matchers/OCR area (used by
        // CupidTradeLoopTask, WowManagementTasks.cs). Computed once from the fields below and
        // cached (this instance is a static readonly singleton per resolution, so the cache
        // lives for the process). Not every resolution defines every field (e.g. trade window
        // positions are only configured for 2560x1600 today) -- null entries are skipped rather
        // than expanding the rectangle.
        //
        // Add any new Point, ImageMatchColorPositions, or ImageMatchTextArea field read off a
        // captured bitmap to the lists in ComputeCaptureRectangle() below, or the capture will
        // silently clip it -- same class of one-sided-change trap as the pixel row itself.
        private const int CaptureRectangleMargin = 5;
        private Rectangle? _captureRectangle;

        public Rectangle CaptureRectangle =>
            (_captureRectangle ?? (_captureRectangle = ComputeCaptureRectangle())).Value;

        private Rectangle ComputeCaptureRectangle()
        {
            var points = new[]
            {
                AddonLoadedPosition, MapXPosition, MapYPosition, FacingDegreesPosition,
                MultiBoolOnePosition, MultiIntOnePosition, MultiIntTwoPosition, ClassBoolOnePosition,
            };

            var clusters = new[]
            {
                FacingWrongWayPositions, TooFarAwayPositions, TargetNeedsToBeInFrontPositions,
                InvalidTargetPositions, OutOfRangePositions, BreathBarScreenPositions,
                TradeWindowScreenPositions, TradeWindowAcceptedScreenPositions, TradeWindowConfirmationScreenPositions,
            };

            int maxX = 0;
            int maxY = 0;

            foreach (var point in points)
            {
                maxX = Math.Max(maxX, point.X);
                maxY = Math.Max(maxY, point.Y);
            }

            foreach (var cluster in clusters)
            {
                if (cluster == null)
                {
                    continue; // not configured for this resolution -- skip rather than blow up
                }

                foreach (var position in cluster.ColorPositions)
                {
                    maxX = Math.Max(maxX, position.X);
                    maxY = Math.Max(maxY, position.Y);
                }
            }

            var textAreas = new[]
            {
                TradeWindowRecipientTextArea,
            };

            foreach (var textArea in textAreas)
            {
                if (textArea == null)
                {
                    continue; // not configured for this resolution -- skip rather than blow up
                }

                maxX = Math.Max(maxX, textArea.X + textArea.Width);
                maxY = Math.Max(maxY, textArea.Y + textArea.Height);
            }

            return new Rectangle(0, 0, maxX + CaptureRectangleMargin, maxY + CaptureRectangleMargin);
        }
    }
}
