using System;
using System.Collections.Generic;
using System.Drawing;
using WindowsGameAutomationTools.ImageDetection;

namespace WoWHelper
{
    public class WowScreenConfiguration
    {
        #region Constants

        public static readonly Color BREATH_BAR_COLOR_ONE = Color.FromArgb(0, 77, 155);
        public static readonly Color BREATH_BAR_COLOR_TWO = Color.FromArgb(0, 31, 62);
        public static readonly Color BREATH_BAR_COLOR_THREE = Color.FromArgb(0, 34, 69);

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

        // Right-click-drag mouse turning rate (WowPathfinding.GetMouseDragPixelsForDegrees),
        // measured per-resolution by WowPlayer.MouseTurnRateSweepTask
        // (WowTurnCalibrationTasks.cs) -- confirmed resolution-dependent, not a shared
        // constant: 3440x1440 measured exactly linear through the origin, while 1920x1080
        // measured a real, non-zero-intercept affine relationship in its steady-state range
        // (roughly pixel 16 and up: degrees ~= 0.6937*pixels - 5.72, refit by linear
        // regression off a 1px-step sweep 1-122px -- see MouseDragOffsetDegrees below for how
        // that intercept is applied). This is just the slope (degrees-per-pixel, inverted);
        // it alone reproduces 3440x1440 exactly but needs MouseDragOffsetDegrees alongside it
        // to reproduce 1920x1080. Defaults to the originally 3440x1440-only measured value for
        // any resolution that hasn't been swept yet.
        public float MouseDragPixelsPerDegree { get; set; } = 4.1f;

        // Extra degrees-equivalent added to the requested turn before converting to pixels,
        // to compensate for a resolution whose measured degrees-vs-pixels line has a non-zero
        // intercept (see MouseDragPixelsPerDegree above) -- 0 for a resolution (3440x1440)
        // whose line passes through the origin. Below the steady-state range this was fit
        // from (1920x1080: roughly pixel 16 and under), the 1px-step sweep shows the real
        // relationship curving above this affine line rather than continuing to follow it
        // (e.g. pixel 10 measured ~2.58 degrees actual turn vs. ~1.3 degrees predicted) --
        // that residual is left uncorrected since RotateToDirectionTask's turn-then-verify
        // loop already re-turns on overshoot, and the residual is at most a couple degrees.
        public float MouseDragOffsetDegrees { get; set; } = 0f;

        // Drags shorter than this don't turn the character at all (TurnByMouseDragTask skips
        // them rather than risk a right-click-with-no-drag registering as a click on whatever's
        // under the cursor). Measured via a fine 1px-step sweep on both 3440x1440 and
        // 1920x1080 -- confirmed different (4px vs 5px), so this is per-resolution too.
        public int MouseDragMinEffectivePixels { get; set; } = 4;

        // Distance in pixels from the center of the screen
        public int MeleeRangeTargetMarkerDistance { get; set; }
        public int TooCloseToTurnWhileWalkingTargetMarkerDistance { get; set; }

        // Breath bar detections
        public ImageMatchColorPositions BreathBarScreenPositions { get; set; }

        // Trade window
        public ImageMatchColorPositions TradeWindowScreenPositions { get; set; }
        public ImageMatchColorPositions TradeWindowAcceptedScreenPositions { get; set; }
        public ImageMatchColorPositions TradeWindowConfirmationScreenPositions { get; set; }
        public ImageMatchTextArea TradeWindowRecipientTextArea { get; set; }

        // Bounding rectangle covering every pixel anything in this codebase reads off a
        // captured screen bitmap: the pixel rows (top-left corner, see WowPixelRows), the
        // breath bar, and the trade-window matchers/OCR area (used by
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
                BreathBarScreenPositions,
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
