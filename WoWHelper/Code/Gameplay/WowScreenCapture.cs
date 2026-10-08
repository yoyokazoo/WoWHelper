using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using WindowsGameAutomationTools.Images;
using WoWHelper.Code;
using WoWHelper.Code.Gameplay;
using WoWHelper.Code.WorldState;

namespace WoWHelper
{
    // Screen-scraping helpers that capture beyond the small fixed pixel-row crop
    // WowWorldState decodes every tick. Stateless -- each takes the screen config it
    // needs and returns what it found, leaving it to WowPlayer to store the result.
    public static class WowScreenCapture
    {
        // Captures a full-screen screenshot and searches it for the sentinel-colored target
        // marker UIFunctions.lua paints onto the current target's nameplate (see
        // WowScreenConfiguration.TARGET_MARKER_COLOR) -- a full-resolution capture, not the
        // tiny fixed pixel-row crop WorldState normally reads, since the marker can be
        // anywhere on screen. Returns its screen position, or null if not found (marker not
        // created yet, target occluded, or no target at all). Shared by TargetMarkerDebugTask
        // and WowMovementTasks.TurnToFaceTargetMarkerTask.
        public static Point? FindTargetMarkerOnScreen(WowScreenConfiguration screenConfig)
        {
            var resolution = screenConfig.Resolution;
            var fullScreenRect = new Rectangle(0, 0, resolution.Width, resolution.Height);

            using (Bitmap fullBmp = ScreenCapture.CaptureBitmapFromDesktopAndRectangle(fullScreenRect))
            {
                Point? centroid = BitmapDifferenceVisualizer.FindColorCentroid(fullBmp, WowScreenConfiguration.TARGET_MARKER_COLOR);

                // TEMP DEBUG (WalkIntoMeleeRangeTask troubleshooting): confirm whether the
                // marker is actually being found at all, and where -- remove once resolved.
                Console.WriteLine(centroid == null
                    ? $"DEBUG FindTargetMarkerOnScreen: marker NOT found (color {WowScreenConfiguration.TARGET_MARKER_COLOR}, resolution {resolution})"
                    : $"DEBUG FindTargetMarkerOnScreen: marker found at {centroid.Value}");

                return centroid;
            }
        }

        // Captures the loot heatmap region repeatedly, diffs the frames to find where the
        // loot sparkle is, and returns the absolute screen point to click to loot it.
        public static async Task<Point> CreateHeatmapForLooting(WowScreenConfiguration screenConfig, bool saveBitmaps = false)
        {
            List<Bitmap> screenChunks = new List<Bitmap>();

            var lootHeatmapRectangle = new Rectangle(
                        screenConfig.LootHeatmapX,
                        screenConfig.LootHeatmapY,
                        screenConfig.LootHeatmapWidth,
                        screenConfig.LootHeatmapHeight);

            for (int i = 0; i < 20; i++)
            {
                Bitmap bmp = ScreenCapture.CaptureBitmapFromDesktopAndRectangle(lootHeatmapRectangle);
                screenChunks.Add(bmp);
                await Task.Delay(100);
            }

            // convert from absolute coords to relative to the snippet we took
            int ignoreXMin = screenConfig.LootHeatmapIgnoreX - screenConfig.LootHeatmapX;
            int ignoreXMax = ignoreXMin + screenConfig.LootHeatmapIgnoreWidth;
            int ignoreYMin = screenConfig.LootHeatmapIgnoreY - screenConfig.LootHeatmapY;
            int ignoreYMax = ignoreYMin + screenConfig.LootHeatmapIgnoreHeight;

            int squareSize = 40;
            int halfSquareSize = squareSize / 2;

            var points = BitmapDifferenceVisualizer.FindHotspots(screenChunks, ignoreXMin, ignoreXMax, ignoreYMin, ignoreYMax);
            var bestSquareOffset = BitmapDifferenceVisualizer.FindBestSquareOffset(points, screenConfig.LootHeatmapWidth, screenConfig.LootHeatmapHeight, squareSize);
            var heatmap = BitmapDifferenceVisualizer.BuildDifferenceHeatmap(points, screenConfig.LootHeatmapWidth, screenConfig.LootHeatmapHeight, ignoreXMin, ignoreXMax, ignoreYMin, ignoreYMax);

            Console.WriteLine($"Best Offset = {bestSquareOffset}, click at {new Point(bestSquareOffset.offsetX + halfSquareSize, bestSquareOffset.offsetY + halfSquareSize)}");
            var lootPoint = new Point(
                screenConfig.LootHeatmapX + bestSquareOffset.offsetX + halfSquareSize,
                screenConfig.LootHeatmapY + bestSquareOffset.offsetY + halfSquareSize);

            Bitmap example = ScreenCapture.CaptureBitmapFromDesktopAndRectangle(lootHeatmapRectangle);

            if (saveBitmaps)
            {
                ScreenCapture.SaveBitmapToFile(heatmap, "Heatmap.bmp");
                ScreenCapture.SaveBitmapToFile(example, "Example.bmp");

                using (Bitmap exampleWithIgnore = new Bitmap(example))
                using (Graphics graphics = Graphics.FromImage(exampleWithIgnore))
                {
                    graphics.FillRectangle(Brushes.Black, ignoreXMin, ignoreYMin, ignoreXMax - ignoreXMin, ignoreYMax - ignoreYMin);
                    ScreenCapture.SaveBitmapToFile(exampleWithIgnore, "ExampleWithIgnore.bmp");
                }
            }

            foreach (Bitmap bmp in screenChunks)
            {
                bmp.Dispose();
            }
            heatmap.Dispose();
            example.Dispose();

            return lootPoint;
        }
    }
}
