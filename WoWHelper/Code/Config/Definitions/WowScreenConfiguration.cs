using System;
using System.Collections.Generic;
using System.Drawing;
using WindowsGameAutomationTools.ImageDetection;

namespace WoWHelper
{
    public class WowScreenConfiguration
    {
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

        public Rectangle? SlackScreenshotCropRegion { get; set; }

        // Distance in pixels from the center of the screen
        public int MeleeRangeTargetMarkerDistance { get; set; }
        public int TooCloseToTurnWhileWalkingTargetMarkerDistance { get; set; }

        // Dynamically calculated rectangle that captures enough of the screen to read everything we need
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
