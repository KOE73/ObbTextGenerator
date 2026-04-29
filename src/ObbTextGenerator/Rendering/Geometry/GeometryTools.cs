using SkiaSharp;

namespace ObbTextGenerator;

public static class GeometryTools
{
    /// <summary>
    /// Checks if two convex polygons intersect using the Separating Axis Theorem (SAT).
    /// </summary>
    public static bool PolygonsIntersect(SKPoint[] poly1, SKPoint[] poly2)
    {
        return PolygonsIntersectInternal(poly1, poly2) && PolygonsIntersectInternal(poly2, poly1);
    }

    private static bool PolygonsIntersectInternal(SKPoint[] poly1, SKPoint[] poly2)
    {
        for (int i = 0; i < poly1.Length; i++)
        {
            // Get the edge
            SKPoint p1 = poly1[i];
            SKPoint p2 = poly1[(i + 1) % poly1.Length];

            // Get the normal to the edge (axis)
            SKPoint axis = new SKPoint(-(p2.Y - p1.Y), p2.X - p1.X);

            // Project both polygons onto the axis
            float min1, max1, min2, max2;
            ProjectPolygon(poly1, axis, out min1, out max1);
            ProjectPolygon(poly2, axis, out min2, out max2);

            // If there's a gap, they don't intersect
            if (max1 < min2 || max2 < min1)
                return false;
        }
        return true;
    }

    private static void ProjectPolygon(SKPoint[] poly, SKPoint axis, out float min, out float max)
    {
        min = float.MaxValue;
        max = float.MinValue;
        foreach (var p in poly)
        {
            float projection = p.X * axis.X + p.Y * axis.Y;
            if (projection < min) min = projection;
            if (projection > max) max = projection;
        }
    }

    /// <summary>
    /// Rotates a rect points around a center.
    /// </summary>
    public static SKPoint[] GetRotatedRectPoints(float centerX, float centerY, float width, float height, float rotation)
    {
        float angleRad = (float)(rotation * Math.PI / 180.0);
        float cos = (float)Math.Cos(angleRad);
        float sin = (float)Math.Sin(angleRad);

        float dx = width / 2;
        float dy = height / 2;

        SKPoint[] points = new SKPoint[4];
        points[0] = RotatePoint(-dx, -dy, cos, sin, centerX, centerY);
        points[1] = RotatePoint(dx, -dy, cos, sin, centerX, centerY);
        points[2] = RotatePoint(dx, dy, cos, sin, centerX, centerY);
        points[3] = RotatePoint(-dx, dy, cos, sin, centerX, centerY);

        return points;
    }

    private static SKPoint RotatePoint(float px, float py, float cos, float sin, float cx, float cy)
    {
        return new SKPoint(
            cx + (px * cos - py * sin),
            cy + (px * sin + py * cos)
        );
    }
}
