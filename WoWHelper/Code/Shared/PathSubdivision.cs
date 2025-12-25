using System;
using System.Collections.Generic;
using System.Numerics;

public static class PathSubdivision
{
    /// <summary>
    /// Subdivides a polyline so that no adjacent points are more than maxDistance apart.
    /// The first input point is preserved, the last input point is preserved,
    /// and intermediate points are inserted as needed.
    /// </summary>
    public static List<Vector2> Subdivide(
        IReadOnlyList<Vector2> points,
        float maxDistance)
    {
        if (points == null)
            throw new ArgumentNullException(nameof(points));

        if (points.Count < 2)
            throw new ArgumentException("At least two points are required.", nameof(points));

        if (maxDistance <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxDistance));

        var result = new List<Vector2>();

        // Always start with the first point
        result.Add(points[0]);

        for (int i = 0; i < points.Count - 1; i++)
        {
            Vector2 start = points[i];
            Vector2 end = points[i + 1];

            float distance = Vector2.Distance(start, end);

            if (distance <= maxDistance)
            {
                // Just add the end point (avoid duplicating start)
                result.Add(end);
                continue;
            }

            int segments = (int)Math.Ceiling(distance / maxDistance);

            // Start from 1 to avoid re-adding 'start'
            for (int s = 1; s <= segments; s++)
            {
                float t = (float)s / segments;
                Vector2 point = Vector2.Lerp(start, end, t);
                result.Add(point);
            }
        }

        return result;
    }

    /// <summary>
    /// Original two-point version (unchanged, kept for reuse or testing)
    /// </summary>
    public static List<Vector2> Subdivide(
        Vector2 start,
        Vector2 end,
        float maxDistance)
    {
        return Subdivide(new[] { start, end }, maxDistance);
    }
}
