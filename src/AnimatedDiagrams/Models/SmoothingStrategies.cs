using System.Collections.Generic;

namespace AnimatedDiagrams.Models;

public enum SmoothingType
{
    Linear,
    CubicBezier,
    CatmullRom,
    SimplifiedBezier,
    SimplifyWithCubicBeziers,
    LinearReduced
}

public static class SmoothingStrategies
{   
    /// <summary>
    /// Builds an SVG path string using the selected smoothing strategy and adaptive step size.
    /// </summary>
    public static string BuildPath(List<(double x, double y)> pts, SmoothingType type, int baseStep = 2)
    {
        return type switch
        {
            SmoothingType.CubicBezier => CubicBezier(pts, baseStep),
            SmoothingType.CatmullRom => CatmullRom(pts),
            SmoothingType.SimplifiedBezier => SimplifyWithBeziers(pts),
            SmoothingType.SimplifyWithCubicBeziers => SimplifyWithCubicBeziers(pts),
            SmoothingType.LinearReduced => LinearReduced(pts, 10.0),
            _ => Linear(pts)
        };
    }

    /// <summary>
    /// Cubic Bézier smoothing: uses cubic curves between points, skipping by step size.
    /// Each segment uses two control points for smoothness.
    /// </summary>
    public static string CubicBezier(List<(double x, double y)> pts, int step = 2)
    {
        if (pts.Count < 4) return Linear(pts);
        // Simplify points first (tolerance can be tuned)
        var simplified = RamerDouglasPeucker(pts, 2.0); // 2.0 px tolerance
        if (simplified.Count < 4) return Linear(simplified);
        var sb = new System.Text.StringBuilder();
        sb.Append($"M {simplified[0].x:0.##} {simplified[0].y:0.##}");
        for (int i = 0; i < simplified.Count - 1; i++)
        {
            var p0 = i == 0 ? simplified[0] : simplified[i - 1];
            var p1 = simplified[i];
            var p2 = simplified[i + 1];
            var p3 = (i + 2 < simplified.Count) ? simplified[i + 2] : simplified[i + 1];
            // Catmull-Rom to Cubic Bézier conversion
            var c1x = p1.x + (p2.x - p0.x) / 6.0;
            var c1y = p1.y + (p2.y - p0.y) / 6.0;
            var c2x = p2.x - (p3.x - p1.x) / 6.0;
            var c2y = p2.y - (p3.y - p1.y) / 6.0;
            sb.Append($" C {c1x:0.##} {c1y:0.##} {c2x:0.##} {c2y:0.##} {p2.x:0.##} {p2.y:0.##}");
        }
        return sb.ToString();
    }

    /// <summary>
    /// Linear: no smoothing, connects all points with straight lines.
    /// </summary>
    public static string Linear(List<(double x, double y)> pts)
    {
        if (pts.Count < 2) return "";
        var sb = new System.Text.StringBuilder();
        sb.Append($"M {pts[0].x:0.##} {pts[0].y:0.##}");
        for (int i = 1; i < pts.Count; i++)
            sb.Append($" L {pts[i].x:0.##} {pts[i].y:0.##}");
        return sb.ToString();
    }

    /// <summary>
    /// Catmull-Rom smoothing: uses Catmull-Rom splines for smooth curves
    /// </summary>
    public static string CatmullRom(List<(double x, double y)> pts)
    {
        if (pts.Count < 4) return Linear(pts);
        var sb = new System.Text.StringBuilder();
        sb.Append($"M {pts[0].x:0.##} {pts[0].y:0.##}");
        for (int i = 1; i < pts.Count - 2; i++)
        {
            var p0 = pts[i - 1];
            var p1 = pts[i];
            var p2 = pts[i + 1];
            var p3 = pts[i + 2];
            var c1x = p1.x + (p2.x - p0.x) / 6.0;
            var c1y = p1.y + (p2.y - p0.y) / 6.0;
            var c2x = p2.x - (p3.x - p1.x) / 6.0;
            var c2y = p2.y - (p3.y - p1.y) / 6.0;
            sb.Append($" C {c1x:0.##} {c1y:0.##} {c2x:0.##} {c2y:0.##} {p2.x:0.##} {p2.y:0.##}");
        }
        sb.Append($" L {pts[^1].x:0.##} {pts[^1].y:0.##}");
        return sb.ToString();
    }

    /// <summary>
    /// Simplifies a path using Ramer-Douglas-Peucker and fits quadratic Bezier segments.
    /// </summary>
    public static string SimplifyWithBeziers(List<(double x, double y)> pts, double tolerance = 2.0)
    {
        var simplified = RamerDouglasPeucker(pts, tolerance);
        if (simplified.Count < 3) return "";
        var sb = new System.Text.StringBuilder();
        sb.Append($"M {simplified[0].x} {simplified[0].y} ");
        for (int i = 0; i < simplified.Count - 2; i += 2)
        {
            var p0 = simplified[i];
            var p1 = simplified[i + 1];
            var p2 = simplified[Math.Min(i + 2, simplified.Count - 1)];
            sb.Append($"Q {p1.x} {p1.y} {p2.x} {p2.y} ");
        }
        return sb.ToString().Trim();
    }


    /// <summary>
    /// Linear smoothing with node reduction: removes nodes that are closer than minDistance to the previous node.
    /// Especially useful for cleaning up dense endpoints from slow pointer movement.
    /// </summary>
    public static string LinearReduced(List<(double x, double y)> pts, double minDistance = 4.0)
    {
        if (pts.Count < 2) return "";
        var reduced = new List<(double x, double y)>();
        reduced.Add(pts[0]);
        for (int i = 1; i < pts.Count; i++)
        {
            var prev = reduced[^1];
            var curr = pts[i];
            double dx = curr.x - prev.x;
            double dy = curr.y - prev.y;
            double dist = Math.Sqrt(dx * dx + dy * dy);
            if (dist >= minDistance || i == pts.Count - 1) // Always keep last node
            {
                reduced.Add(curr);
            }
        }
        if (reduced.Count < 2) return "";
        var sb = new System.Text.StringBuilder();
        sb.Append($"M {reduced[0].x:0.##} {reduced[0].y:0.##}");
        for (int i = 1; i < reduced.Count; i++)
            sb.Append($" L {reduced[i].x:0.##} {reduced[i].y:0.##}");
        return sb.ToString();
    }

    /// <summary>
    /// Simplifies a path using Ramer-Douglas-Peucker and fits cubic Bézier segments.
    /// Returns an SVG path string.
    /// </summary>
    public static string SimplifyWithCubicBeziers(List<(double x, double y)> pts, double tolerance = 2.0)
    {
        var simplified = RamerDouglasPeucker(pts, tolerance);
        if (simplified.Count < 4) return Linear(simplified);
        var sb = new System.Text.StringBuilder();
        sb.Append($"M {simplified[0].x:0.##} {simplified[0].y:0.##}");
        for (int i = 0; i < simplified.Count - 1; i++)
        {
            var p0 = i == 0 ? simplified[0] : simplified[i - 1];
            var p1 = simplified[i];
            var p2 = simplified[i + 1];
            var p3 = (i + 2 < simplified.Count) ? simplified[i + 2] : simplified[i + 1];
            // Catmull-Rom to Cubic Bézier conversion
            var c1x = p1.x + (p2.x - p0.x) / 6.0;
            var c1y = p1.y + (p2.y - p0.y) / 6.0;
            var c2x = p2.x - (p3.x - p1.x) / 6.0;
            var c2y = p2.y - (p3.y - p1.y) / 6.0;
            sb.Append($" C {c1x:0.##} {c1y:0.##} {c2x:0.##} {c2y:0.##} {p2.x:0.##} {p2.y:0.##}");
        }
        return sb.ToString();
    }

    // Ramer-Douglas-Peucker algorithm for path simplification
    private static List<(double x, double y)> RamerDouglasPeucker(List<(double x, double y)> pts, double epsilon)
    {
        if (pts.Count < 3) return new List<(double x, double y)>(pts);
        int index = -1;
        double maxDist = 0;
        for (int i = 1; i < pts.Count - 1; i++)
        {
            double dist = PerpendicularDistance(pts[i], pts[0], pts[^1]);
            if (dist > maxDist)
            {
                index = i;
                maxDist = dist;
            }
        }
        if (maxDist > epsilon)
        {
            var left = RamerDouglasPeucker(pts.GetRange(0, index + 1), epsilon);
            var right = RamerDouglasPeucker(pts.GetRange(index, pts.Count - index), epsilon);
            left.RemoveAt(left.Count - 1);
            left.AddRange(right);
            return left;
        }
        else
        {
            return new List<(double x, double y)> { pts[0], pts[^1] };
        }
    }

    // Helper for RDP: perpendicular distance from point to line
    private static double PerpendicularDistance((double x, double y) pt, (double x, double y) lineStart, (double x, double y) lineEnd)
    {
        double dx = lineEnd.x - lineStart.x;
        double dy = lineEnd.y - lineStart.y;
        if (dx == 0 && dy == 0)
            return Math.Sqrt((pt.x - lineStart.x) * (pt.x - lineStart.x) + (pt.y - lineStart.y) * (pt.y - lineStart.y));
        double t = ((pt.x - lineStart.x) * dx + (pt.y - lineStart.y) * dy) / (dx * dx + dy * dy);
        double projX = lineStart.x + t * dx;
        double projY = lineStart.y + t * dy;
        return Math.Sqrt((pt.x - projX) * (pt.x - projX) + (pt.y - projY) * (pt.y - projY));
    }
}
