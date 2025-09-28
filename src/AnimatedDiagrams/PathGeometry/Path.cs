using AnimatedDiagrams.Models;

namespace AnimatedDiagrams.PathGeometry;

public static class Path
{

    /// <summary>
    /// Moves all points in an SVG path string by (dx, dy), preserving all command types and structure.
    /// </summary>
    public static string MovePathBy(string d, double dx, double dy)
    {
        if (string.IsNullOrWhiteSpace(d)) return d;
        // Parse the path and offset only absolute coordinates, preserving original command structure
        var sb = new System.Text.StringBuilder();
        int i = 0;
        double cx = 0, cy = 0; // current point
        double subpathStartX = 0, subpathStartY = 0;
        char lastCmd = 'M';
        bool firstRelMove = true;
        while (i < d.Length)
        {
            while (i < d.Length && char.IsWhiteSpace(d[i])) i++;
            if (i >= d.Length) break;
            char cmd = d[i];
            if (!char.IsLetter(cmd)) { i++; continue; }
            char absCmd = char.ToUpper(cmd);
            bool isRel = char.IsLower(cmd);
            i++;
            // Parse numbers after command
            List<double> nums = new();
            while (i < d.Length)
            {
                while (i < d.Length && (char.IsWhiteSpace(d[i]) || d[i] == ',')) i++;
                if (i < d.Length && (char.IsDigit(d[i]) || d[i] == '-' || d[i] == '.'))
                {
                    int start = i;
                    while (i < d.Length && (char.IsDigit(d[i]) || d[i] == '-' || d[i] == '.' || d[i] == 'e' || d[i] == 'E')) i++;
                    if (double.TryParse(d.Substring(start, i - start), out var val))
                        nums.Add(val);
                }
                else break;
            }
            int n = 0;
            sb.Append(cmd);
            switch (absCmd)
            {
                case 'M': // moveto
                    while (n + 1 < nums.Count)
                    {
                        double x = nums[n], y = nums[n + 1];
                        if (isRel)
                        {
                            if (firstRelMove)
                            {
                                // Offset the initial relative moveto
                                x += dx; y += dy;
                                firstRelMove = false;
                            }
                            // else: do not offset subsequent relative moves
                            sb.Append($" {x:0.##} {y:0.##}");
                            cx += x; cy += y;
                        }
                        else
                        {
                            x += dx; y += dy;
                            sb.Append($" {x:0.##} {y:0.##}");
                            cx = x; cy = y;
                        }
                        subpathStartX = cx; subpathStartY = cy;
                        n += 2;
                        if (n + 1 < nums.Count) sb.Append(" ");
                        absCmd = 'L';
                    }
                    break;
                case 'L': // lineto
                case 'T': // smooth quadratic curveto
                    while (n + 1 < nums.Count)
                    {
                        double x = nums[n], y = nums[n + 1];
                        if (isRel)
                        {
                            sb.Append($" {x:0.##} {y:0.##}");
                            cx += x; cy += y;
                        }
                        else
                        {
                            x += dx; y += dy;
                            sb.Append($" {x:0.##} {y:0.##}");
                            cx = x; cy = y;
                        }
                        n += 2;
                        if (n + 1 < nums.Count) sb.Append(" ");
                    }
                    break;
                case 'H': // horizontal lineto
                    while (n < nums.Count)
                    {
                        double x = nums[n];
                        if (isRel)
                        {
                            sb.Append($" {x:0.##}");
                            cx += x;
                        }
                        else
                        {
                            x += dx;
                            sb.Append($" {x:0.##}");
                            cx = x;
                        }
                        n++;
                        if (n < nums.Count) sb.Append(" ");
                    }
                    break;
                case 'V': // vertical lineto
                    while (n < nums.Count)
                    {
                        double y = nums[n];
                        if (isRel)
                        {
                            sb.Append($" {y:0.##}");
                            cy += y;
                        }
                        else
                        {
                            y += dy;
                            sb.Append($" {y:0.##}");
                            cy = y;
                        }
                        n++;
                        if (n < nums.Count) sb.Append(" ");
                    }
                    break;
                case 'C': // cubic Bezier curveto
                    while (n + 5 < nums.Count)
                    {
                        double x1 = nums[n], y1 = nums[n + 1], x2 = nums[n + 2], y2 = nums[n + 3], x = nums[n + 4], y = nums[n + 5];
                        if (isRel)
                        {
                            sb.Append($" {x1:0.##} {y1:0.##} {x2:0.##} {y2:0.##} {x:0.##} {y:0.##}");
                            cx += x; cy += y;
                        }
                        else
                        {
                            x1 += dx; y1 += dy; x2 += dx; y2 += dy; x += dx; y += dy;
                            sb.Append($" {x1:0.##} {y1:0.##} {x2:0.##} {y2:0.##} {x:0.##} {y:0.##}");
                            cx = x; cy = y;
                        }
                        n += 6;
                        if (n + 5 < nums.Count) sb.Append(" ");
                    }
                    break;
                case 'S': // smooth cubic Bezier curveto
                    while (n + 3 < nums.Count)
                    {
                        double x2 = nums[n], y2 = nums[n + 1], x = nums[n + 2], y = nums[n + 3];
                        if (isRel)
                        {
                            sb.Append($" {x2:0.##} {y2:0.##} {x:0.##} {y:0.##}");
                            cx += x; cy += y;
                        }
                        else
                        {
                            x2 += dx; y2 += dy; x += dx; y += dy;
                            sb.Append($" {x2:0.##} {y2:0.##} {x:0.##} {y:0.##}");
                            cx = x; cy = y;
                        }
                        n += 4;
                        if (n + 3 < nums.Count) sb.Append(" ");
                    }
                    break;
                case 'Q': // quadratic Bezier curveto
                    while (n + 3 < nums.Count)
                    {
                        double x1 = nums[n], y1 = nums[n + 1], x = nums[n + 2], y = nums[n + 3];
                        if (isRel)
                        {
                            sb.Append($" {x1:0.##} {y1:0.##} {x:0.##} {y:0.##}");
                            cx += x; cy += y;
                        }
                        else
                        {
                            x1 += dx; y1 += dy; x += dx; y += dy;
                            sb.Append($" {x1:0.##} {y1:0.##} {x:0.##} {y:0.##}");
                            cx = x; cy = y;
                        }
                        n += 4;
                        if (n + 3 < nums.Count) sb.Append(" ");
                    }
                    break;
                case 'A': // elliptical arc
                    while (n + 6 < nums.Count)
                    {
                        double rx = nums[n], ry = nums[n + 1], angle = nums[n + 2], laf = nums[n + 3], sf = nums[n + 4], x = nums[n + 5], y = nums[n + 6];
                        if (isRel)
                        {
                            sb.Append($" {rx:0.##} {ry:0.##} {angle:0.##} {laf:0} {sf:0} {x:0.##} {y:0.##}");
                            cx += x; cy += y;
                        }
                        else
                        {
                            x += dx; y += dy;
                            sb.Append($" {rx:0.##} {ry:0.##} {angle:0.##} {laf:0} {sf:0} {x:0.##} {y:0.##}");
                            cx = x; cy = y;
                        }
                        n += 7;
                        if (n + 6 < nums.Count) sb.Append(" ");
                    }
                    break;
                case 'Z': // closepath
                    sb.Append("");
                    cx = subpathStartX; cy = subpathStartY;
                    break;
            }
            lastCmd = absCmd;
            if (i < d.Length) sb.Append(" ");
        }
        return sb.ToString().Trim();
    }

    // Efficiently compute bounds for a path D string (minX, minY, width, height)
    public static (double x, double y, double w, double h) GetBounds(string d)
    {
        double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
        int i = 0;
        double cx = 0, cy = 0; // current point
        double startX = 0, startY = 0;
        double prevCtrlX = 0, prevCtrlY = 0; // for smooth curves
        char prevCmd = 'M';
        List<(double x, double y)> coords = new();
        while (i < d.Length)
        {
            // Skip whitespace
            while (i < d.Length && char.IsWhiteSpace(d[i])) i++;
            if (i >= d.Length) break;
            char cmd = d[i];
            if (!char.IsLetter(cmd)) { i++; continue; }
            i++;
            // Parse numbers after command
            List<double> nums = new();
            while (i < d.Length)
            {
                while (i < d.Length && (char.IsWhiteSpace(d[i]) || d[i] == ',')) i++;
                if (i < d.Length && (char.IsDigit(d[i]) || d[i] == '-' || d[i] == '.'))
                {
                    int start = i;
                    while (i < d.Length && (char.IsDigit(d[i]) || d[i] == '-' || d[i] == '.')) i++;
                    if (double.TryParse(d.Substring(start, i - start), out var val))
                        nums.Add(val);
                }
                else break;
            }
            int n = 0;
            switch (char.ToUpper(cmd))
            {
                case 'M': // moveto
                    while (n + 1 < nums.Count)
                    {
                        double nx = nums[n], ny = nums[n + 1];
                        if (char.IsLower(cmd)) { nx += cx; ny += cy; }
                        cx = nx; cy = ny;
                        startX = cx; startY = cy;
                        coords.Add((cx, cy));
                        minX = Math.Min(minX, cx);
                        minY = Math.Min(minY, cy);
                        maxX = Math.Max(maxX, cx);
                        maxY = Math.Max(maxY, cy);
                        n += 2;
                    }
                    break;
                case 'L': // lineto
                    while (n + 1 < nums.Count)
                    {
                        double nx = nums[n], ny = nums[n + 1];
                        if (char.IsLower(cmd)) { nx += cx; ny += cy; }
                        cx = nx; cy = ny;
                        coords.Add((cx, cy));
                        minX = Math.Min(minX, cx);
                        minY = Math.Min(minY, cy);
                        maxX = Math.Max(maxX, cx);
                        maxY = Math.Max(maxY, cy);
                        n += 2;
                    }
                    break;
                case 'H': // horizontal lineto
                    while (n < nums.Count)
                    {
                        double nx = nums[n];
                        if (char.IsLower(cmd)) nx += cx;
                        cx = nx;
                        coords.Add((cx, cy));
                        minX = Math.Min(minX, cx);
                        maxX = Math.Max(maxX, cx);
                        n++;
                    }
                    break;
                case 'V': // vertical lineto
                    while (n < nums.Count)
                    {
                        double ny = nums[n];
                        if (char.IsLower(cmd)) ny += cy;
                        cy = ny;
                        coords.Add((cx, cy));
                        minY = Math.Min(minY, cy);
                        maxY = Math.Max(maxY, cy);
                        n++;
                    }
                    break;
                case 'C': // cubic Bezier curveto
                    while (n + 5 < nums.Count)
                    {
                        double x1 = nums[n], y1 = nums[n + 1], x2 = nums[n + 2], y2 = nums[n + 3], x3 = nums[n + 4], y3 = nums[n + 5];
                        if (char.IsLower(cmd))
                        {
                            x1 += cx; y1 += cy; x2 += cx; y2 += cy; x3 += cx; y3 += cy;
                        }
                        // For bounds, use all control points and end point
                        double[] bx = { cx, x1, x2, x3 };
                        double[] by = { cy, y1, y2, y3 };
                        foreach (var xx in bx) { minX = Math.Min(minX, xx); maxX = Math.Max(maxX, xx); }
                        foreach (var yy in by) { minY = Math.Min(minY, yy); maxY = Math.Max(maxY, yy); }
                        cx = x3; cy = y3;
                        coords.Add((cx, cy));
                        prevCtrlX = x2; prevCtrlY = y2;
                        n += 6;
                    }
                    break;
                case 'S': // smooth cubic Bezier curveto
                    while (n + 3 < nums.Count)
                    {
                        double x2 = nums[n], y2 = nums[n + 1], x3 = nums[n + 2], y3 = nums[n + 3];
                        double x1 = cx, y1 = cy;
                        if (char.ToUpper(prevCmd) == 'C' || char.ToUpper(prevCmd) == 'S')
                        {
                            x1 = 2 * cx - prevCtrlX;
                            y1 = 2 * cy - prevCtrlY;
                        }
                        if (char.IsLower(cmd))
                        {
                            x2 += cx; y2 += cy; x3 += cx; y3 += cy;
                        }
                        double[] bx = { cx, x1, x2, x3 };
                        double[] by = { cy, y1, y2, y3 };
                        foreach (var xx in bx) { minX = Math.Min(minX, xx); maxX = Math.Max(maxX, xx); }
                        foreach (var yy in by) { minY = Math.Min(minY, yy); maxY = Math.Max(maxY, yy); }
                        cx = x3; cy = y3;
                        coords.Add((cx, cy));
                        prevCtrlX = x2; prevCtrlY = y2;
                        n += 4;
                    }
                    break;
                case 'Q': // quadratic Bezier curveto
                    while (n + 3 < nums.Count)
                    {
                        double x1 = nums[n], y1 = nums[n + 1], x2 = nums[n + 2], y2 = nums[n + 3];
                        if (char.IsLower(cmd))
                        {
                            x1 += cx; y1 += cy; x2 += cx; y2 += cy;
                        }
                        double[] bx = { cx, x1, x2 };
                        double[] by = { cy, y1, y2 };
                        foreach (var xx in bx) { minX = Math.Min(minX, xx); maxX = Math.Max(maxX, xx); }
                        foreach (var yy in by) { minY = Math.Min(minY, yy); maxY = Math.Max(maxY, yy); }
                        cx = x2; cy = y2;
                        coords.Add((cx, cy));
                        prevCtrlX = x1; prevCtrlY = y1;
                        n += 4;
                    }
                    break;
                case 'T': // smooth quadratic curveto
                    while (n + 1 < nums.Count)
                    {
                        double x1 = cx, y1 = cy;
                        if (char.ToUpper(prevCmd) == 'Q' || char.ToUpper(prevCmd) == 'T')
                        {
                            x1 = 2 * cx - prevCtrlX;
                            y1 = 2 * cy - prevCtrlY;
                        }
                        double x2 = nums[n], y2 = nums[n + 1];
                        if (char.IsLower(cmd)) { x2 += cx; y2 += cy; }
                        double[] bx = { cx, x1, x2 };
                        double[] by = { cy, y1, y2 };
                        foreach (var xx in bx) { minX = Math.Min(minX, xx); maxX = Math.Max(maxX, xx); }
                        foreach (var yy in by) { minY = Math.Min(minY, yy); maxY = Math.Max(maxY, yy); }
                        cx = x2; cy = y2;
                        coords.Add((cx, cy));
                        prevCtrlX = x1; prevCtrlY = y1;
                        n += 2;
                    }
                    break;
                case 'A': // elliptical arc
                    while (n + 6 < nums.Count)
                    {
                        double rx = nums[n], ry = nums[n + 1], angle = nums[n + 2], largeArc = nums[n + 3], sweep = nums[n + 4], x2 = nums[n + 5], y2 = nums[n + 6];
                        if (char.IsLower(cmd)) { x2 += cx; y2 += cy; }
                        // For bounds, just use the endpoint (not accurate for arcs)
                        cx = x2; cy = y2;
                        coords.Add((cx, cy));
                        minX = Math.Min(minX, cx);
                        minY = Math.Min(minY, cy);
                        maxX = Math.Max(maxX, cx);
                        maxY = Math.Max(maxY, cy);
                        n += 7;
                    }
                    break;
                case 'Z': // closepath
                    cx = startX; cy = startY;
                    coords.Add((cx, cy));
                    minX = Math.Min(minX, cx);
                    minY = Math.Min(minY, cy);
                    maxX = Math.Max(maxX, cx);
                    maxY = Math.Max(maxY, cy);
                    break;
            }
            prevCmd = cmd;
        }
        if (minX == double.MaxValue) minX = minY = maxX = maxY = 0;
        Console.WriteLine($"[Debug] GetBounds: d=\"{d}\" coords=[{string.Join(", ", coords.Select(c => $"({c.x},{c.y})"))}] bounds=({minX},{minY},{maxX-minX},{maxY-minY})");
        return (minX, minY, maxX - minX, maxY - minY);
    }

    // Returns true if any point or segment of the path is within threshold of (x,y)
    public static bool IsNearPoint(double x, double y, SvgPathItem path)
    {
        var d = path.D;
        int i = 0;
        double cx = 0, cy = 0; // current point
        double startX = 0, startY = 0;
        double prevCtrlX = 0, prevCtrlY = 0; // for smooth curves
        char prevCmd = 'M';
        const double threshold = 5;
        while (i < d.Length)
        {
            // Skip whitespace
            while (i < d.Length && char.IsWhiteSpace(d[i])) i++;
            if (i >= d.Length) break;
            char cmd = d[i];
            if (!char.IsLetter(cmd)) { i++; continue; }
            i++;
            // Parse numbers after command
            List<double> nums = new();
            while (i < d.Length)
            {
                while (i < d.Length && (char.IsWhiteSpace(d[i]) || d[i] == ',')) i++;
                if (i < d.Length && (char.IsDigit(d[i]) || d[i] == '-' || d[i] == '.'))
                {
                    int start = i;
                    while (i < d.Length && (char.IsDigit(d[i]) || d[i] == '-' || d[i] == '.')) i++;
                    if (double.TryParse(d.Substring(start, i - start), out var val))
                        nums.Add(val);
                }
                else break;
            }
            int n = 0;
            switch (char.ToUpper(cmd))
            {
                case 'M': // moveto
                    while (n + 1 < nums.Count)
                    {
                        double nx = nums[n], ny = nums[n + 1];
                        if (char.IsLower(cmd)) { nx += cx; ny += cy; }
                        cx = nx; cy = ny;
                        startX = cx; startY = cy;
                        n += 2;
                    }
                    break;
                case 'L': // lineto
                    while (n + 1 < nums.Count)
                    {
                        double nx = nums[n], ny = nums[n + 1];
                        if (char.IsLower(cmd)) { nx += cx; ny += cy; }
                        var dist = DistanceToSegment(x, y, cx, cy, nx, ny);
                        if (dist < threshold) return true;
                        cx = nx; cy = ny;
                        n += 2;
                    }
                    break;
                case 'H': // horizontal lineto
                    while (n < nums.Count)
                    {
                        double nx = nums[n];
                        if (char.IsLower(cmd)) nx += cx;
                        var dist = DistanceToSegment(x, y, cx, cy, nx, cy);
                        if (dist < threshold) return true;
                        cx = nx;
                        n++;
                    }
                    break;
                case 'V': // vertical lineto
                    while (n < nums.Count)
                    {
                        double ny = nums[n];
                        if (char.IsLower(cmd)) ny += cy;
                        var dist = DistanceToSegment(x, y, cx, cy, cx, ny);
                        if (dist < threshold) return true;
                        cy = ny;
                        n++;
                    }
                    break;
                case 'C': // cubic Bezier curveto
                    while (n + 5 < nums.Count)
                    {
                        double x1 = nums[n], y1 = nums[n + 1], x2 = nums[n + 2], y2 = nums[n + 3], x3 = nums[n + 4], y3 = nums[n + 5];
                        if (char.IsLower(cmd))
                        {
                            x1 += cx; y1 += cy; x2 += cx; y2 += cy; x3 += cx; y3 += cy;
                        }
                        double prevX = cx, prevY = cy;
                        for (int t = 1; t <= 10; t++)
                        {
                            double tt = t / 10.0;
                            double bx = Math.Pow(1 - tt, 3) * cx + 3 * Math.Pow(1 - tt, 2) * tt * x1 + 3 * (1 - tt) * tt * tt * x2 + Math.Pow(tt, 3) * x3;
                            double by = Math.Pow(1 - tt, 3) * cy + 3 * Math.Pow(1 - tt, 2) * tt * y1 + 3 * (1 - tt) * tt * tt * y2 + Math.Pow(tt, 3) * y3;
                            var dist = DistanceToSegment(x, y, prevX, prevY, bx, by);
                            if (dist < threshold) return true;
                            prevX = bx; prevY = by;
                        }
                        cx = x3; cy = y3;
                        prevCtrlX = x2; prevCtrlY = y2;
                        n += 6;
                    }
                    break;
                case 'S': // smooth cubic Bezier curveto
                    while (n + 3 < nums.Count)
                    {
                        double x2 = nums[n], y2 = nums[n + 1], x3 = nums[n + 2], y3 = nums[n + 3];
                        double x1 = cx, y1 = cy;
                        if (char.ToUpper(prevCmd) == 'C' || char.ToUpper(prevCmd) == 'S')
                        {
                            x1 = 2 * cx - prevCtrlX;
                            y1 = 2 * cy - prevCtrlY;
                        }
                        if (char.IsLower(cmd))
                        {
                            x2 += cx; y2 += cy; x3 += cx; y3 += cy;
                        }
                        double prevX = cx, prevY = cy;
                        for (int t = 1; t <= 10; t++)
                        {
                            double tt = t / 10.0;
                            double bx = Math.Pow(1 - tt, 3) * cx + 3 * Math.Pow(1 - tt, 2) * tt * x1 + 3 * (1 - tt) * tt * tt * x2 + Math.Pow(tt, 3) * x3;
                            double by = Math.Pow(1 - tt, 3) * cy + 3 * Math.Pow(1 - tt, 2) * tt * y1 + 3 * (1 - tt) * tt * tt * y2 + Math.Pow(tt, 3) * y3;
                            var dist = DistanceToSegment(x, y, prevX, prevY, bx, by);
                            if (dist < threshold) return true;
                            prevX = bx; prevY = by;
                        }
                        cx = x3; cy = y3;
                        prevCtrlX = x2; prevCtrlY = y2;
                        n += 4;
                    }
                    break;
                case 'Q': // quadratic Bezier curveto
                    while (n + 3 < nums.Count)
                    {
                        double x1 = nums[n], y1 = nums[n + 1], x2 = nums[n + 2], y2 = nums[n + 3];
                        if (char.IsLower(cmd))
                        {
                            x1 += cx; y1 += cy; x2 += cx; y2 += cy;
                        }
                        double prevX = cx, prevY = cy;
                        for (int t = 1; t <= 10; t++)
                        {
                            double tt = t / 10.0;
                            double bx = Math.Pow(1 - tt, 2) * cx + 2 * (1 - tt) * tt * x1 + Math.Pow(tt, 2) * x2;
                            double by = Math.Pow(1 - tt, 2) * cy + 2 * (1 - tt) * tt * y1 + Math.Pow(tt, 2) * y2;
                            var dist = DistanceToSegment(x, y, prevX, prevY, bx, by);
                            if (dist < threshold) return true;
                            prevX = bx; prevY = by;
                        }
                        cx = x2; cy = y2;
                        prevCtrlX = x1; prevCtrlY = y1;
                        n += 4;
                    }
                    break;
                case 'T': // smooth quadratic curveto
                    while (n + 1 < nums.Count)
                    {
                        double x1 = cx, y1 = cy;
                        if (char.ToUpper(prevCmd) == 'Q' || char.ToUpper(prevCmd) == 'T')
                        {
                            x1 = 2 * cx - prevCtrlX;
                            y1 = 2 * cy - prevCtrlY;
                        }
                        double x2 = nums[n], y2 = nums[n + 1];
                        if (char.IsLower(cmd)) { x2 += cx; y2 += cy; }
                        double prevX = cx, prevY = cy;
                        for (int t = 1; t <= 10; t++)
                        {
                            double tt = t / 10.0;
                            double bx = Math.Pow(1 - tt, 2) * cx + 2 * (1 - tt) * tt * x1 + Math.Pow(tt, 2) * x2;
                            double by = Math.Pow(1 - tt, 2) * cy + 2 * (1 - tt) * tt * y1 + Math.Pow(tt, 2) * y2;
                            var dist = DistanceToSegment(x, y, prevX, prevY, bx, by);
                            if (dist < threshold) return true;
                            prevX = bx; prevY = by;
                        }
                        cx = x2; cy = y2;
                        prevCtrlX = x1; prevCtrlY = y1;
                        n += 2;
                    }
                    break;
                case 'A': // elliptical arc
                    while (n + 6 < nums.Count)
                    {
                        double rx = nums[n], ry = nums[n + 1], angle = nums[n + 2], largeArc = nums[n + 3], sweep = nums[n + 4], x2 = nums[n + 5], y2 = nums[n + 6];
                        if (char.IsLower(cmd)) { x2 += cx; y2 += cy; }
                        // Sample the arc as a polyline for hit testing
                        int arcSegments = 20;
                        var arcPoints = SampleArc(cx, cy, rx, ry, angle, largeArc != 0, sweep != 0, x2, y2, arcSegments);
                        double prevX = arcPoints[0].Item1, prevY = arcPoints[0].Item2;
                        for (int j = 1; j < arcPoints.Count; j++)
                        {
                            double px = arcPoints[j].Item1, py = arcPoints[j].Item2;
                            var dist = DistanceToSegment(x, y, prevX, prevY, px, py);
                            if (dist < threshold) return true;
                            prevX = px; prevY = py;
                        }
                        cx = x2; cy = y2;
                        n += 7;
                    }
                    break;
        // Helper for arc sampling (SVG elliptical arc to polyline)
        static List<(double, double)> SampleArc(double x0, double y0, double rx, double ry, double angle, bool largeArc, bool sweep, double x, double y, int segments)
        {
            // Algorithm adapted from SVG spec: https://www.w3.org/TR/SVG/implnote.html#ArcImplementationNotes
            List<(double, double)> pts = new();
            if (rx == 0 || ry == 0)
            {
                pts.Add((x0, y0));
                pts.Add((x, y));
                return pts;
            }
            double rad = angle * Math.PI / 180.0;
            double cosA = Math.Cos(rad), sinA = Math.Sin(rad);
            double dx2 = (x0 - x) / 2.0, dy2 = (y0 - y) / 2.0;
            double x1p = cosA * dx2 + sinA * dy2;
            double y1p = -sinA * dx2 + cosA * dy2;
            double rx2 = rx * rx, ry2 = ry * ry, x1p2 = x1p * x1p, y1p2 = y1p * y1p;
            double lam = x1p2 / rx2 + y1p2 / ry2;
            if (lam > 1) { rx *= Math.Sqrt(lam); ry *= Math.Sqrt(lam); rx2 = rx * rx; ry2 = ry * ry; }
            double sign = (largeArc == sweep) ? -1 : 1;
            double sq = ((rx2 * ry2) - (rx2 * y1p2) - (ry2 * x1p2)) / ((rx2 * y1p2) + (ry2 * x1p2));
            sq = (sq < 0) ? 0 : sq;
            double coef = sign * Math.Sqrt(sq);
            double cxp = coef * (rx * y1p) / ry;
            double cyp = coef * -(ry * x1p) / rx;
            double cx_ = cosA * cxp - sinA * cyp + (x0 + x) / 2.0;
            double cy_ = sinA * cxp + cosA * cyp + (y0 + y) / 2.0;
            double theta1 = Math.Atan2((y1p - cyp) / ry, (x1p - cxp) / rx);
            double dtheta = Math.Atan2((-y1p - cyp) / ry, (-x1p - cxp) / rx) - theta1;
            if (sweep && dtheta < 0) dtheta += 2 * Math.PI;
            else if (!sweep && dtheta > 0) dtheta -= 2 * Math.PI;
            for (int i = 0; i <= segments; i++)
            {
                double t = theta1 + dtheta * i / segments;
                double px = cx_ + rx * Math.Cos(rad) * Math.Cos(t) - ry * Math.Sin(rad) * Math.Sin(t);
                double py = cy_ + rx * Math.Sin(rad) * Math.Cos(t) + ry * Math.Cos(rad) * Math.Sin(t);
                pts.Add((px, py));
            }
            return pts;
        }
                case 'Z': // closepath
                    var distZ = DistanceToSegment(x, y, cx, cy, startX, startY);
                    if (distZ < threshold) return true;
                    cx = startX; cy = startY;
                    break;
            }
            prevCmd = cmd;
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
        // Fast path: if bounds don't intersect, return false
        if (path.Bounds.HasValue)
        {
            var b = path.Bounds.Value;
            if (b.x > maxX || b.x + b.w < minX || b.y > maxY || b.y + b.h < minY)
            {
                Console.WriteLine($"[Debug] IntersectsRect: Path {path.Id} bounds=({b.x},{b.y},{b.w},{b.h}) does not intersect rect=({minX},{minY},{maxX},{maxY})");
                return false;
            }
        }        

        // Efficiently parse points
        var d = path.D;
        int i = 0;
        int len = d.Length;
        double px = 0, py = 0;
        while (i < len)
        {
            // Skip non-numeric chars
            while (i < len && (d[i] < '0' || d[i] > '9') && d[i] != '-' && d[i] != '.') i++;
            int start = i;
            while (i < len && (char.IsDigit(d[i]) || d[i] == '-' || d[i] == '.')) i++;
            if (i > start && double.TryParse(d.Substring(start, i - start), out px))
            {
                // Parse y
                while (i < len && (d[i] < '0' || d[i] > '9') && d[i] != '-' && d[i] != '.') i++;
                int startY = i;
                while (i < len && (char.IsDigit(d[i]) || d[i] == '-' || d[i] == '.')) i++;
                if (i > startY && double.TryParse(d.Substring(startY, i - startY), out py))
                {
                    if (px >= minX && px <= maxX && py >= minY && py <= maxY)
                    {
                        Console.WriteLine($"[Debug] IntersectsRect: Path {path.Id} point=({px},{py}) is inside rect=({minX},{minY},{maxX},{maxY})");
                        return true;
                    }
                }
            }
        }
        Console.WriteLine($"[Debug] IntersectsRect: Path {path.Id} does not intersect rect=({minX},{minY},{maxX},{maxY})");
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
