using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

public static class BitmapDifferenceVisualizer
{
    public static Bitmap BuildDifferenceHeatmap(List<Point> points, int width, int height, int ignoreXMin, int ignoreXMax, int ignoreYMin, int ignoreYMax)
    {
        Bitmap output = new Bitmap(width, height);

        foreach(var point in points)
        {
            output.SetPixel(point.X, point.Y, Color.Red);
        }

        return output;
    }

    // Samples every `step`th pixel (outside the ignore rectangle) and returns the ones whose
    // color changes at any point across the frames.
    public static List<Point> FindHotspots(IReadOnlyList<Bitmap> bitmaps, int ignoreXMin, int ignoreXMax, int ignoreYMin, int ignoreYMax, int step = 3)
    {
        int width = bitmaps[0].Width;
        int height = bitmaps[0].Height;

        int[][] frames = new int[bitmaps.Count][];
        for (int i = 0; i < bitmaps.Count; i++)
        {
            frames[i] = CopyPixelsArgb(bitmaps[i]);
        }

        List<Point> output = new List<Point>();

        for (int x = 0; x < width; x += step)
        {
            for (int y = 0; y < height; y += step)
            {
                if (x >= ignoreXMin && x <= ignoreXMax && y >= ignoreYMin && y <= ignoreYMax)
                {
                    continue;
                }

                int index = y * width + x;
                int firstColor = frames[0][index];
                for (int i = 1; i < frames.Length; i++)
                {
                    if (frames[i][index] != firstColor)
                    {
                        output.Add(new Point(x, y));
                        break;
                    }
                }
            }
        }

        return output;
    }

    // Copies a bitmap's pixels into a row-major int[] (index y * width + x), one ARGB int per
    // pixel, comparable to Color.ToArgb(). Reading via LockBits is ~10-30x faster than
    // Bitmap.GetPixel; locking as Format32bppArgb makes GDI+ convert if the bitmap is in some
    // other format, and copying row by row skips any stride padding.
    private static int[] CopyPixelsArgb(Bitmap bmp)
    {
        int width = bmp.Width;
        int height = bmp.Height;
        int[] pixels = new int[width * height];

        BitmapData data = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        try
        {
            for (int y = 0; y < height; y++)
            {
                Marshal.Copy(data.Scan0 + y * data.Stride, pixels, y * width, width);
            }
        }
        finally
        {
            bmp.UnlockBits(data);
        }

        return pixels;
    }

    /// <summary>
    /// Finds offsetX/offsetY (top-left / min corner) of an axis-aligned square of side length L
    /// that contains the most points. Assumes integer point coordinates in [0..maxX],[0..maxY].
    /// Square is inclusive: x in [ox, ox+L], y in [oy, oy+L].
    /// </summary>
    public static (int offsetX, int offsetY, int count) FindBestSquareOffset(
        IReadOnlyList<Point> points,
        int maxX,
        int maxY,
        int L)
    {
        if (points == null) throw new ArgumentNullException(nameof(points));
        if (maxX < 0 || maxY < 0) throw new ArgumentOutOfRangeException("maxX/maxY must be >= 0.");
        if (L < 0) throw new ArgumentOutOfRangeException(nameof(L), "L must be >= 0.");

        // If L is bigger than the entire range, the best is trivially at (0,0).
        int oxMax = Math.Max(0, maxX - L);
        int oyMax = Math.Max(0, maxY - L);

        // We build a (maxX+1) x (maxY+1) grid, then prefix with +1 padding for simpler sums.
        // Prefix dimensions: (maxX+2) x (maxY+2)
        int w = maxX + 2;
        int h = maxY + 2;

        // Using int[,] is convenient; for very large grids you may prefer a flat int[] for speed.
        var prefix = new int[w, h];

        // Accumulate point counts into prefix buffer at (x+1, y+1)
        foreach (var p in points)
        {
            if ((uint)p.X > (uint)maxX || (uint)p.Y > (uint)maxY)
                continue; // ignore out-of-bounds

            prefix[p.X + 1, p.Y + 1] += 1;
        }

        // Build 2D prefix sum:
        // prefix[x,y] = grid sum over [1..x],[1..y] (in shifted coords)
        for (int x = 1; x < w; x++)
        {
            int rowRunning = 0;
            for (int y = 1; y < h; y++)
            {
                rowRunning += prefix[x, y];
                prefix[x, y] = prefix[x - 1, y] + rowRunning;
            }
        }

        int bestCount = -1;
        int bestOx = 0, bestOy = 0;

        // Query count in inclusive square [ox..ox+L], [oy..oy+L]
        // Convert to prefix coords with +1 shift:
        // x1 = ox+1, y1 = oy+1, x2 = (ox+L)+1, y2 = (oy+L)+1
        for (int ox = 0; ox <= oxMax; ox++)
        {
            int x1 = ox + 1;
            int x2 = ox + L + 1;

            for (int oy = 0; oy <= oyMax; oy++)
            {
                int y1 = oy + 1;
                int y2 = oy + L + 1;

                int count =
                    prefix[x2, y2]
                    - prefix[x1 - 1, y2]
                    - prefix[x2, y1 - 1]
                    + prefix[x1 - 1, y1 - 1];

                if (count > bestCount)
                {
                    bestCount = count;
                    bestOx = ox;
                    bestOy = oy;
                }
            }
        }

        return (bestOx, bestOy, bestCount < 0 ? 0 : bestCount);
    }

    public static int ConvertColorToInt(Color color)
    {
        return (color.R * 255 * 255) + (color.G * 255) + color.B;
    }

    /// <summary>
    /// Scans a bitmap for pixels exactly matching markerColor and returns the centroid
    /// (average position) of every match, or null if none found. Used to locate the
    /// sentinel-colored target marker UIFunctions.lua paints onto the current target's
    /// nameplate (see WowScreenConfiguration.TARGET_MARKER_COLOR) -- since the marker is a
    /// solid NxN square, a handful of exact-match hits toward its interior is expected even
    /// stepping across the bitmap rather than checking every pixel (cheaper over a
    /// near-full-screen capture, same tradeoff FindHotspots above makes).
    ///
    /// Reads pixels via LockBits like CopyPixelsArgb, but copies out only the sampled rows,
    /// one at a time into a reused buffer -- this runs on a full-screen capture every few
    /// hundred ms, so it avoids allocating a whole-screen array each call.
    /// </summary>
    public static Point? FindColorCentroid(Bitmap bmp, Color markerColor, int step = 4)
    {
        long sumX = 0, sumY = 0;
        int count = 0;
        int width = bmp.Width;
        int height = bmp.Height;
        int markerArgb = markerColor.ToArgb();

        BitmapData data = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        try
        {
            int[] row = new int[width];
            for (int y = 0; y < height; y += step)
            {
                Marshal.Copy(data.Scan0 + y * data.Stride, row, 0, width);
                for (int x = 0; x < width; x += step)
                {
                    if (row[x] == markerArgb)
                    {
                        sumX += x;
                        sumY += y;
                        count++;
                    }
                }
            }
        }
        finally
        {
            bmp.UnlockBits(data);
        }

        if (count == 0)
        {
            return null;
        }

        return new Point((int)(sumX / count), (int)(sumY / count));
    }
}
