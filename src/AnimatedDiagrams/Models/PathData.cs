using System;
using System.Collections.Generic;
using System.Globalization;

namespace AnimatedDiagrams.Models
{
    public static class PathData
    {
        /// <summary>
        /// Parses a simple SVG path string (M/L/Q/C commands) into a list of (x, y) points.
        /// Only extracts endpoint coordinates, not control points.
        /// </summary>
        public static List<(double x, double y)> ToPoints(string d)
        {
            var points = new List<(double x, double y)>();
            if (string.IsNullOrWhiteSpace(d)) return points;
            var tokens = d.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            int i = 0;
            while (i < tokens.Length)
            {
                var cmd = tokens[i];
                if (cmd == "M" || cmd == "L")
                {
                    if (i + 2 < tokens.Length &&
                        double.TryParse(tokens[i + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out var x) &&
                        double.TryParse(tokens[i + 2], NumberStyles.Float, CultureInfo.InvariantCulture, out var y))
                    {
                        points.Add((x, y));
                        i += 3;
                    }
                    else i++;
                }
                else if (cmd == "Q")
                {
                    // Q cx cy x y
                    if (i + 4 < tokens.Length &&
                        double.TryParse(tokens[i + 3], NumberStyles.Float, CultureInfo.InvariantCulture, out var x) &&
                        double.TryParse(tokens[i + 4], NumberStyles.Float, CultureInfo.InvariantCulture, out var y))
                    {
                        points.Add((x, y));
                        i += 5;
                    }
                    else i++;
                }
                else if (cmd == "C")
                {
                    // C c1x c1y c2x c2y x y
                    if (i + 6 < tokens.Length &&
                        double.TryParse(tokens[i + 5], NumberStyles.Float, CultureInfo.InvariantCulture, out var x) &&
                        double.TryParse(tokens[i + 6], NumberStyles.Float, CultureInfo.InvariantCulture, out var y))
                    {
                        points.Add((x, y));
                        i += 7;
                    }
                    else i++;
                }
                else i++;
            }
            return points;
        }
    }
}
