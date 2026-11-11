using System;
using System.Collections.Generic;
using System.Drawing;
using WindowsGameAutomationTools.ImageDetection;

namespace WoWHelper
{
    public class WowScreenConfiguration
    {
        #region Constants

        public static readonly Color TARGET_MARKER_COLOR = Color.FromArgb(255, 0, 255);

        #endregion

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

            int maxX = 0;
            int maxY = 0;

            foreach (var point in points)
            {
                maxX = Math.Max(maxX, point.X);
                maxY = Math.Max(maxY, point.Y);
            }

            return new Rectangle(0, 0, maxX + CaptureRectangleMargin, maxY + CaptureRectangleMargin);
        }
    }
}
