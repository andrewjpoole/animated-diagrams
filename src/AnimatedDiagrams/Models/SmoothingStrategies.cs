using System.Collections.Generic;

namespace AnimatedDiagrams.Models
{
    public enum SmoothingType
    {
        QuadraticBezier,
        Linear,
        CatmullRom,
        SimplifiedBezier
    }

    public static class SmoothingStrategies
    {
        /// <summary>
        /// Computes an adaptive smoothing step based on the average change of direction (angle) between segments.
        /// Curvy lines use a low step (2-4), only very straight lines get a high step (up to maxStep).
        /// </summary>
        public static int ComputeAdaptiveStep(List<(double x, double y)> pts, int baseStep = 2, int maxStep = 20)
        {
            if (pts.Count < 3) return baseStep;
            double totalAngle = 0;
            for (int i = 1; i < pts.Count - 1; i++)
            {
                var a = pts[i - 1];
                var b = pts[i];
                var c = pts[i + 1];
                var v1x = b.x - a.x; var v1y = b.y - a.y;
                var v2x = c.x - b.x; var v2y = c.y - b.y;
                double dot = v1x * v2x + v1y * v2y;
                double mag1 = Math.Sqrt(v1x * v1x + v1y * v1y);
                double mag2 = Math.Sqrt(v2x * v2x + v2y * v2y);
                if (mag1 > 0 && mag2 > 0)
                {
                    double angle = Math.Acos(Math.Clamp(dot / (mag1 * mag2), -1.0, 1.0));
                    totalAngle += angle;
                }
            }
            double avgAngle = totalAngle / (pts.Count - 2);
            // If avgAngle is small, path is straight; if large, path is curvy
            // Use a threshold: below 0.1 rad (very straight), use maxStep; above 0.3 rad (curvy), use baseStep
            if (avgAngle < 0.1) return maxStep;
            if (avgAngle < 0.2) return Math.Max(baseStep, maxStep / 2);
            return baseStep; // curvy lines
        }

        /// <summary>
        /// Builds an SVG path string using the selected smoothing strategy and adaptive step size.
        /// </summary>
        public static string BuildPath(List<(double x, double y)> pts, SmoothingType type, int baseStep = 2, int maxStep = 20)
        {
            int step = ComputeAdaptiveStep(pts, baseStep, maxStep);
            return type switch
            {
                SmoothingType.QuadraticBezier => QuadraticBezier(pts, step),
                SmoothingType.Linear => Linear(pts),
                SmoothingType.CatmullRom => CatmullRom(pts),
                SmoothingType.SimplifiedBezier => SimplifyWithBeziers(pts),
                _ => QuadraticBezier(pts, step)
            };
        }

        /// <summary>
        /// Quadratic Bézier smoothing: uses quadratic curves between points, skipping by step size.
        /// </summary>
        public static string QuadraticBezier(List<(double x, double y)> pts, int step = 2)
        {
            if (pts.Count < 3) return $"M {pts[0].x:0.##} {pts[0].y:0.##} L {pts[^1].x:0.##} {pts[^1].y:0.##}";
            var sb = new System.Text.StringBuilder();
            sb.Append($"M {pts[0].x:0.##} {pts[0].y:0.##}");
            for (int i = step; i < pts.Count - step; i += step)
            {
                var p0 = pts[i - step];
                var p1 = pts[i];
                var p2 = pts[Math.Min(i + step, pts.Count - 1)];
                var cx = (p0.x + p2.x) / 2; var cy = (p0.y + p2.y) / 2;
                sb.Append($" Q {p1.x:0.##} {p1.y:0.##} {cx:0.##} {cy:0.##}");
            }
            sb.Append($" L {pts[^1].x:0.##} {pts[^1].y:0.##}");
            return sb.ToString();
        }

        /// <summary>
        /// Linear smoothing: connects all points with straight lines (no smoothing).
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
        /// Catmull-Rom smoothing: uses Catmull-Rom splines for smooth curves (step size not used).
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
        /// Returns an SVG path string.
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
}
