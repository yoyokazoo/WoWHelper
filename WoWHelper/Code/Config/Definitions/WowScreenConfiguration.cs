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
        public static readonly Color BREATH_BAR_COLOR_THREE = Color.FromArgb(0, 34, 69);

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

        public int DynamiteAndDummyX { get; set; }
        public int DynamiteAndDummyY { get; set; }

        public int LootHeatmapX { get; set; }
        public int LootHeatmapY { get; set; }
        public int LootHeatmapWidth { get; set; }
        public int LootHeatmapHeight { get; set; }

        public int LootDefaultX => (Resolution.Width / 2);
        public int LootDefaultY => (Resolution.Height / 2) - (Resolution.Height / 6);

        // Estimates how far away the current target is, from 0.0 (as close as the target
        // marker -- see WowPlayer.FindTargetMarkerOnScreen -- ever gets, i.e. melee range
        // immediately in front of the player) to 1.0 (as far as it's ever calibrated to be
        // seen), by linearly interpolating the marker's Y coordinate between
        // TargetMarkerNearY (0.0) and TargetMarkerFarY (1.0). Only the marker's Y matters
        // here, not X -- this assumes the caller already turned to face the target (see
        // WowMovementTasks.TurnToFaceTargetMarkerTask), so the marker should already be
        // roughly dead-ahead and its horizontal position isn't a distance signal. Clamped to
        // [0,1] since a marker slightly outside the calibrated range (anti-aliasing, a target
        // farther than anything this was calibrated against) shouldn't extrapolate to a
        // nonsense value. Throws if this resolution hasn't been calibrated (TargetMarkerFarY/
        // NearY still -1) -- silently returning a made-up distance would be worse than failing
        // loudly here.
        public float DistanceFromTarget(Point targetMarkerPosition)
        {
            if (TargetMarkerFarY < 0 || TargetMarkerNearY < 0)
            {
                throw new InvalidOperationException(
                    $"{nameof(DistanceFromTarget)}: {Name} isn't calibrated yet (TargetMarkerFarY/TargetMarkerNearY are still -1).");
            }

            float raw = (TargetMarkerNearY - targetMarkerPosition.Y) / (float)(TargetMarkerNearY - TargetMarkerFarY);
            return Math.Max(0f, Math.Min(1f, raw));
        }

        public int LootHeatmapIgnoreX { get; set; }
        public int LootHeatmapIgnoreY { get; set; }
        public int LootHeatmapIgnoreWidth { get; set; }
        public int LootHeatmapIgnoreHeight { get; set; }

        // Region to crop Slack alert screenshots (e.g. unseen whisper) down
        // to, instead of sending the whole screen. Null means "not
        // configured for this resolution" -- callers should fall back to
        // the full screen in that case (see SlackFileUploadWorkaround).
        public Rectangle? SlackScreenshotCropRegion { get; set; }

        // Calibration for DistanceFromTarget() below: the target marker's screen Y
        // coordinate (see WowPlayer.FindTargetMarkerOnScreen) at the two ends of the range
        // this bot ever walks a target through, assuming the player is already facing it
        // (camera pitched straight down, so Y alone -- not X -- tracks distance). -1 means
        // "not calibrated for this resolution yet" -- only 1920x1080 is calibrated so far
        // (see RESOLUTION_1920_X_1080 in WowScreenConfigs.cs), same "null/-1 until someone
        // measures it" pattern as SlackScreenshotCropRegion/NotInLineOfSightPositions above.
        public int TargetMarkerFarY { get; set; } = -1;
        public int TargetMarkerNearY { get; set; } = -1;

        // Error text detections
        public ImageMatchColorPositions FacingWrongWayPositions { get; set; }
        public ImageMatchColorPositions TooFarAwayPositions { get; set; }
        public ImageMatchColorPositions TargetNeedsToBeInFrontPositions { get; set; }
        public ImageMatchColorPositions InvalidTargetPositions { get; set; }
        public ImageMatchColorPositions OutOfRangePositions { get; set; }
        public ImageMatchColorPositions NotInLineOfSightPositions { get; set; }

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
        // no ClassIntOne Point exists because nothing currently decodes it
        // (MultiBoolTwo/ClassBoolTwo each got a Point once a field needed
        // one -- IsTargetLongRangeCaster and IsInEarthShockRange
        // respectively). Add one back here (and a matching swatch in
        // InitializePixelRow()) if/when a field needs it.
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

        public Point MultiBoolTwoPosition => PixelRowPoint(8);

        public Point ClassBoolTwoPosition => PixelRowPoint(9);

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
                MultiBoolTwoPosition, ClassBoolTwoPosition,
            };

            var clusters = new[]
            {
                FacingWrongWayPositions, TooFarAwayPositions, TargetNeedsToBeInFrontPositions,
                InvalidTargetPositions, OutOfRangePositions, NotInLineOfSightPositions, BreathBarScreenPositions,
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
