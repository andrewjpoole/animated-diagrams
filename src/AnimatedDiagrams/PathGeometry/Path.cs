using AnimatedDiagrams.Models;

namespace AnimatedDiagrams.PathGeometry;

public static class Path
{
    // Returns true if any point or segment of the path is within threshold of (x,y)
    public static bool IsNearPoint(double x, double y, SvgPathItem path)
    {
        var tokens = path.D.Replace("M", " ").Replace("Q", " ").Replace("L", " ").Split(' ', StringSplitOptions.RemoveEmptyEntries);
        const double threshold = 5;
        if (tokens.Length < 4) return false;
        for (int i = 0; i + 3 < tokens.Length; i += 2)
        {
            if (double.TryParse(tokens[i], out var x1) && double.TryParse(tokens[i + 1], out var y1) &&
                double.TryParse(tokens[i + 2], out var x2) && double.TryParse(tokens[i + 3], out var y2))
            {
                var dist = DistanceToSegment(x, y, x1, y1, x2, y2);
                if (dist < threshold) return true;
            }
        }
        return false;
    }

    // Distance from point (px,py) to segment (x1,y1)-(x2,y2)
    public static double DistanceToSegment(double px, double py, double x1, double y1, double x2, double y2)
    {
        var dx = x2 - x1; var dy = y2 - y1;
        if (dx == 0 && dy == 0) return Math.Sqrt((px - x1) * (px - x1) + (py - y1) * (py - y1));
        var t = ((px - x1) * dx + (py - y1) * dy) / (dx * dx + dy * dy);
        t = Math.Max(0, Math.Min(1, t));
        var projX = x1 + t * dx;
        var projY = y1 + t * dy;
        return Math.Sqrt((px - projX) * (px - projX) + (py - projY) * (py - projY));
    }

    // Simple path length estimation (for speed hints)
    public static double EstimatedLength(string d)
    {
        // Simple estimation: sum of segment lengths for M/L commands
        var tokens = d.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        double length = 0;
        double? lastX = null, lastY = null;
        int i = 0;
        while (i < tokens.Length)
        {
            var cmd = tokens[i];
            if (cmd == "M" || cmd == "L")
            {
                if (i + 2 < tokens.Length && double.TryParse(tokens[i + 1], out var x) && double.TryParse(tokens[i + 2], out var y))
                {
                    if (lastX.HasValue && lastY.HasValue)
                    {
                        var dx = x - lastX.Value;
                        var dy = y - lastY.Value;
                        length += Math.Sqrt(dx * dx + dy * dy);
                    }
                    lastX = x; lastY = y;
                    i += 3;
                }
                else i++;
            }
            else if (cmd == "Q")
            {
                // Quadratic: estimate as straight line for fallback
                if (i + 4 < tokens.Length && double.TryParse(tokens[i + 3], out var x) && double.TryParse(tokens[i + 4], out var y))
                {
                    if (lastX.HasValue && lastY.HasValue)
                    {
                        var dx = x - lastX.Value;
                        var dy = y - lastY.Value;
                        length += Math.Sqrt(dx * dx + dy * dy);
                    }
                    lastX = x; lastY = y;
                    i += 5;
                }
                else i++;
            }
            else if (cmd == "C")
            {
                // Cubic: estimate as straight line for fallback
                if (i + 6 < tokens.Length && double.TryParse(tokens[i + 5], out var x) && double.TryParse(tokens[i + 6], out var y))
                {
                    if (lastX.HasValue && lastY.HasValue)
                    {
                        var dx = x - lastX.Value;
                        var dy = y - lastY.Value;
                        length += Math.Sqrt(dx * dx + dy * dy);
                    }
                    lastX = x; lastY = y;
                    i += 7;
                }
                else i++;
            }
            else i++;
        }
        return length;
    }

    // Get all nodes (points) from a path, marking control points
    public static List<(Node node, bool isControl)> GetPathNodesWithType(SvgPathItem path)
    {
        var result = new List<(Node, bool)>();
        var tokens = path.D.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        int i = 0;
        while (i < tokens.Length)
        {
            var cmd = tokens[i];
            if (cmd == "M" || cmd == "L")
            {
                if (i + 2 < tokens.Length && double.TryParse(tokens[i + 1], out var x) && double.TryParse(tokens[i + 2], out var y))
                {
                    result.Add((new Node { X = x, Y = y }, false));
                    i += 3;
                }
                else i++;
            }
            else if (cmd == "Q")
            {
                // Q x1 y1 x y (x1,y1 is control, x,y is endpoint)
                if (i + 4 < tokens.Length && double.TryParse(tokens[i + 1], out var cx) && double.TryParse(tokens[i + 2], out var cy)
                    && double.TryParse(tokens[i + 3], out var x) && double.TryParse(tokens[i + 4], out var y))
                {
                    result.Add((new Node { X = cx, Y = cy }, true)); // control point
                    result.Add((new Node { X = x, Y = y }, false));   // endpoint
                    i += 5;
                }
                else i++;
            }
            else if (cmd == "C")
            {
                // C x1 y1 x2 y2 x y (cubic: two controls, one endpoint)
                if (i + 6 < tokens.Length && double.TryParse(tokens[i + 1], out var cx1) && double.TryParse(tokens[i + 2], out var cy1)
                    && double.TryParse(tokens[i + 3], out var cx2) && double.TryParse(tokens[i + 4], out var cy2)
                    && double.TryParse(tokens[i + 5], out var x) && double.TryParse(tokens[i + 6], out var y))
                {
                    result.Add((new Node { X = cx1, Y = cy1 }, true));
                    result.Add((new Node { X = cx2, Y = cy2 }, true));
                    result.Add((new Node { X = x, Y = y }, false));
                    i += 7;
                }
                else i++;
            }
            else i++;
        }
        return result;
    }

    // Get all points from a path (for drag-move)
    public static List<(double x, double y)> GetPoints(SvgPathItem path)
    {
        var pts = new List<(double x, double y)>();
        var tokens = path.D.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        int i = 0;
        while (i < tokens.Length)
        {
            var cmd = tokens[i];
            if (cmd == "M" || cmd == "L")
            {
                if (i + 2 < tokens.Length && double.TryParse(tokens[i + 1], out var x) && double.TryParse(tokens[i + 2], out var y))
                {
                    pts.Add((x, y));
                    i += 3;
                }
                else i++;
            }
            else if (cmd == "Q")
            {
                if (i + 4 < tokens.Length && double.TryParse(tokens[i + 1], out var cx) && double.TryParse(tokens[i + 2], out var cy)
                    && double.TryParse(tokens[i + 3], out var x) && double.TryParse(tokens[i + 4], out var y))
                {
                    pts.Add((cx, cy));
                    pts.Add((x, y));
                    i += 5;
                }
                else i++;
            }
            else if (cmd == "C")
            {
                if (i + 6 < tokens.Length && double.TryParse(tokens[i + 1], out var cx1) && double.TryParse(tokens[i + 2], out var cy1)
                    && double.TryParse(tokens[i + 3], out var cx2) && double.TryParse(tokens[i + 4], out var cy2)
                    && double.TryParse(tokens[i + 5], out var x) && double.TryParse(tokens[i + 6], out var y))
                {
                    pts.Add((cx1, cy1));
                    pts.Add((cx2, cy2));
                    pts.Add((x, y));
                    i += 7;
                }
                else i++;
            }
            else i++;
        }
        return pts;
    }

    // Returns true if any point or segment of the path is within radius of (cx, cy)
    public static bool IntersectsCircle(SvgPathItem path, double cx, double cy, double radius)
    {
        var pts = GetPoints(path);
        double r2 = radius * radius;
        for (int i = 0; i < pts.Count; i++)
        {
            var (x, y) = pts[i];
            double dx = x - cx, dy = y - cy;
            if (dx * dx + dy * dy <= r2)
                return true;
            // Check segment distance if not first point
            if (i > 0)
            {
                var (x0, y0) = pts[i - 1];
                if (SegmentIntersectsCircle(x0, y0, x, y, cx, cy, radius))
                    return true;
            }
        }
        return false;
    }

    // Returns true if the segment (x0,y0)-(x1,y1) is within radius of (cx,cy)
    public static bool SegmentIntersectsCircle(double x0, double y0, double x1, double y1, double cx, double cy, double radius)
    {
        // Closest point on segment to circle center
        double dx = x1 - x0, dy = y1 - y0;
        double l2 = dx * dx + dy * dy;
        if (l2 == 0) return false;
        double t = ((cx - x0) * dx + (cy - y0) * dy) / l2;
        t = Math.Max(0, Math.Min(1, t));
        double px = x0 + t * dx, py = y0 + t * dy;
        double dist2 = (px - cx) * (px - cx) + (py - cy) * (py - cy);
        return dist2 <= radius * radius;
    }

    // Returns true if the circle item overlaps the hit circle
    public static bool CircleIntersectsCircle(SvgCircleItem c, double cx, double cy, double radius)
    {
        double dx = c.Cx - cx, dy = c.Cy - cy;
        double dist = Math.Sqrt(dx * dx + dy * dy);
        return dist <= (c.R + radius);
    }

    // Returns true if any point of the path is inside the rect
    public static bool IntersectsRect(SvgPathItem path, double minX, double minY, double maxX, double maxY)
    {
        var tokens = path.D.Replace("M", " ").Replace("Q", " ").Replace("L", " ").Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i + 1 < tokens.Length; i += 2)
        {
            if (double.TryParse(tokens[i], out var px) && double.TryParse(tokens[i + 1], out var py))
            {
                if (px >= minX && px <= maxX && py >= minY && py <= maxY)
                    return true;
            }
        }
        return false;
    }

    // Returns true if the circle item overlaps the rect
    public static bool CircleIntersectsRect(SvgCircleItem c, double minX, double minY, double maxX, double maxY)
    {
        // Check if circle center is inside rect, or if rect overlaps circle
        if (c.Cx >= minX && c.Cx <= maxX && c.Cy >= minY && c.Cy <= maxY)
            return true;
        // Check if any rect corner is inside the circle
        var corners = new[] { (minX, minY), (maxX, minY), (minX, maxY), (maxX, maxY) };
        foreach (var (x, y) in corners)
        {
            var dx = x - c.Cx;
            var dy = y - c.Cy;
            if ((dx * dx + dy * dy) <= (c.R * c.R)) return true;
        }
        // Check if circle overlaps any edge of the rect
        if (c.Cx + c.R >= minX && c.Cx - c.R <= maxX && c.Cy + c.R >= minY && c.Cy - c.R <= maxY)
            return true;
        return false;
    }

    // Hit test for circles
    public static bool IsPointInCircle(double x, double y, SvgCircleItem c)
    {
        var dx = x - c.Cx;
        var dy = y - c.Cy;
        return (dx * dx + dy * dy) <= (c.R * c.R);
    }

    // Returns a new path D string offset by (offset.x, offset.y)
    public static string OffsetPathD(string d, (double x, double y) offset)
    {
        var tokens = d.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var sb = new System.Text.StringBuilder();
        int i = 0;
        while (i < tokens.Length)
        {
            var cmd = tokens[i];
            if (cmd == "M" || cmd == "L")
            {
                if (i + 2 < tokens.Length &&
                    double.TryParse(tokens[i + 1], out var x) &&
                    double.TryParse(tokens[i + 2], out var y))
                {
                    sb.Append($"{cmd} {x + offset.x} {y + offset.y} ");
                    i += 3;
                }
                else i++;
            }
            else if (cmd == "Q")
            {
                if (i + 4 < tokens.Length &&
                    double.TryParse(tokens[i + 1], out var cx) &&
                    double.TryParse(tokens[i + 2], out var cy) &&
                    double.TryParse(tokens[i + 3], out var x) &&
                    double.TryParse(tokens[i + 4], out var y))
                {
                    sb.Append($"{cmd} {cx + offset.x} {cy + offset.y} {x + offset.x} {y + offset.y} ");
                    i += 5;
                }
                else i++;
            }
            else if (cmd == "C")
            {
                if (i + 6 < tokens.Length &&
                    double.TryParse(tokens[i + 1], out var cx1) &&
                    double.TryParse(tokens[i + 2], out var cy1) &&
                    double.TryParse(tokens[i + 3], out var cx2) &&
                    double.TryParse(tokens[i + 4], out var cy2) &&
                    double.TryParse(tokens[i + 5], out var x) &&
                    double.TryParse(tokens[i + 6], out var y))
                {
                    sb.Append($"{cmd} {cx1 + offset.x} {cy1 + offset.y} {cx2 + offset.x} {cy2 + offset.y} {x + offset.x} {y + offset.y} ");
                    i += 7;
                }
                else i++;
            }
            else
            {
                sb.Append(cmd + " ");
                i++;
            }
        }
        return sb.ToString().Trim();
    }
}
