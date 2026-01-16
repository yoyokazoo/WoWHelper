using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Net;
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

    public static List<Point> FindHotspots(IReadOnlyList<Bitmap> bitmaps, int ignoreXMin, int ignoreXMax, int ignoreYMin, int ignoreYMax)
    {
        int width = bitmaps[0].Width;
        int height = bitmaps[0].Height;

        int widthStep = 3;// 5;
        int heightStep = 3;// 5;

        int count = bitmaps.Count;

        List<Point> output = new List<Point>();

        Color firstColor = Color.White;
        for (int x = 0; x < width; x += widthStep)
        {
            for (int y = 0; y < height; y += heightStep)
            {
                if (x >= ignoreXMin && x <= ignoreXMax && y >= ignoreYMin && y <= ignoreYMax)
                {
                    continue;
                }

                for (int bmpIndex = 0; bmpIndex < count; bmpIndex++)
                {
                    if (bmpIndex == 0)
                    {
                        firstColor = bitmaps[bmpIndex].GetPixel(x, y);
                    }
                    else
                    {
                        if (firstColor != bitmaps[bmpIndex].GetPixel(x, y))
                        {
                            output.Add(new Point(x, y));
                            break;
                        }
                    }
                }
            }
        }

        return output;
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
}
