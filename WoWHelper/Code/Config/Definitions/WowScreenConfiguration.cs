using System;
using System.Collections.Generic;
using System.Drawing;
using WindowsGameAutomationTools.ImageDetection;

namespace WoWHelper
{
    public class WowScreenConfiguration
    {
        #region Constants

        public static readonly Color TRADE_SCREEN_COLOR_ONE = Color.FromArgb(93, 88, 86);
        public static readonly Color TRADE_SCREEN_COLOR_TWO = Color.FromArgb(167, 165, 161);
        public static readonly Color TRADE_SCREEN_COLOR_THREE = Color.FromArgb(212, 177, 42);

        public static readonly Color TRADE_SCREEN_ACCEPTED_COLOR_ONE = Color.FromArgb(38, 64, 33);
        public static readonly Color TRADE_SCREEN_ACCEPTED_COLOR_TWO = Color.FromArgb(140, 223, 0);

        public static readonly Color TRADE_SCREEN_CONFIRMATION_COLOR_ONE = Color.FromArgb(68, 66, 64);
        public static readonly Color TRADE_SCREEN_CONFIRMATION_COLOR_TWO = Color.FromArgb(233, 181, 43);
        public static readonly Color TRADE_SCREEN_CONFIRMATION_COLOR_THREE = Color.FromArgb(87, 0, 0);

        // Sentinel color painted onto the current target's nameplate (a single CENTER-anchored
        // texture -- see UIFunctions.lua's target-marker section) so it can be found via an
        // ordinary screen-capture pixel search instead of a restricted frame-measurement API
        // (UnitPosition/C_Map.GetPlayerMapPosition/nameplate :GetCenter() are all confirmed
        // blocked for an arbitrary target in this client). Keep in sync with
        // UIFunctions.lua's NAMEPLATE_MARKER_COLOR.
        public static readonly Color TARGET_MARKER_COLOR = Color.FromArgb(255, 0, 255);

        #endregion

        // /console cameraDistanceMaxZoomFactor 2.6
        // TODO: init instead of set would be nice. What would it take to migrate?
        public string Name { get; set; }
        public Size Resolution { get; set; }

        public int DynamiteAndDummyX => (Resolution.Width / 2);
        public int DynamiteAndDummyY => (Resolution.Height / 2);

        public int LootHeatmapX { get; set; }
        public int LootHeatmapY { get; set; }
        public int LootHeatmapWidth { get; set; }
        public int LootHeatmapHeight { get; set; }

        public int LootDefaultX => (Resolution.Width / 2);
        public int LootDefaultY => (Resolution.Height / 2) - (Resolution.Height / 12);

        public int LootHeatmapIgnoreX { get; set; }
        public int LootHeatmapIgnoreY { get; set; }
        public int LootHeatmapIgnoreWidth { get; set; }
        public int LootHeatmapIgnoreHeight { get; set; }

        // Region to crop Slack alert screenshots (e.g. unseen whisper) down
        // to, instead of sending the whole screen. Null means "not
        // configured for this resolution" -- callers should fall back to
        // the full screen in that case (see SlackFileUploadWorkaround).
        public Rectangle? SlackScreenshotCropRegion { get; set; }

        // Distance in pixels from the center of the screen
        public int MeleeRangeTargetMarkerDistance { get; set; }
        public int TooCloseToTurnWhileWalkingTargetMarkerDistance { get; set; }

        // Trade window
        public ImageMatchColorPositions TradeWindowScreenPositions { get; set; }
        public ImageMatchColorPositions TradeWindowAcceptedScreenPositions { get; set; }
        public ImageMatchColorPositions TradeWindowConfirmationScreenPositions { get; set; }
        public ImageMatchTextArea TradeWindowRecipientTextArea { get; set; }

        // Bounding rectangle covering every pixel anything in this codebase reads off a
        // captured screen bitmap: the pixel rows (top-left corner, see WowPixelRows) and
        // the trade-window matchers/OCR area (used by
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
                WowPixelRows.FarthestReadPixel,
            };

            var clusters = new[]
            {
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
