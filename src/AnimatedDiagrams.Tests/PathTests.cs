using System;
using System.Collections.Generic;
using AnimatedDiagrams.Models;
using AnimatedDiagrams.PathGeometry;
using Xunit;

namespace AnimatedDiagrams.Tests
{

    public class PathTests
    {

    [Fact]
    public void MovePathBy_OffsetsRelativeCommands_PreservesRelative()
    {
        // A square using only relative commands
        string d = "m 10 10 l 20 0 l 0 20 l -20 0 z";
        string moved = Path.MovePathBy(d, 5, 7);
        // The initial moveto should be offset
        Assert.StartsWith("m 15 17", moved.Trim());
        // All other commands should remain lowercase (relative)
        Assert.Contains("l 20 0", moved);
        Assert.Contains("l 0 20", moved);
        Assert.Contains("l -20 0", moved);
        Assert.Contains("z", moved.ToLower());
    }

    [Fact]
    public void MovePathBy_OffsetsMixedAbsoluteAndRelative()
    {
        // Path with both absolute and relative commands
        string d = "M 0 0 l 10 0 L 20 10 l 0 10 Z";
        string moved = Path.MovePathBy(d, 3, 4);
        // Absolute M and L should be offset
        Assert.Contains("M 3 4", moved);
        Assert.Contains("L 23 14", moved);
        // Relative l commands should remain lowercase and unchanged in value
        Assert.Contains("l 10 0", moved);
        Assert.Contains("l 0 10", moved);
        Assert.Contains("Z", moved);
    }
        [Fact]
        public void MovePathBy_OffsetsAllPoints_PreservesCommands()
            {
                string d = "M 0 0 L 10 0 Q 20 10 30 0 C 40 0 50 10 60 0 A 5 5 0 0 1 70 0 Z";
                string moved = Path.MovePathBy(d, 5, 10);
                Assert.Contains("M 5 10", moved);
                Assert.Contains("L 15 10", moved);
                Assert.Contains("Q 25 20 35 10", moved);
                Assert.Contains("C 45 10 55 20 65 10", moved);
                Assert.Contains("A 5 5 0 0 1 75 10", moved);
                Assert.Contains("Z", moved);
            }

            [Fact]
            public void IsNearPoint_WorksForArcAndCubic()
            {
                var arc = new SvgPathItem { D = "M 0 0 A 10 10 0 0 1 20 0" };
                // Test a point exactly on the arc end
                Assert.True(Path.IsNearPoint(20, 0, arc));
                var cubic = new SvgPathItem { D = "M 0 0 C 10 10 20 10 30 0" };
                // Test a point exactly on the cubic end
                Assert.True(Path.IsNearPoint(30, 0, cubic));
            }

            [Fact]
            public void MovePathBy_DoesNotConvertCurvesToLines()
            {
                string d = "M 0 0 C 10 10 20 10 30 0";
                string moved = Path.MovePathBy(d, 5, 5);
                // Should still contain 'C' command, not just 'L'
                Assert.Contains("C", moved);
                Assert.DoesNotContain("L 15 15 25 15 35 5", moved); // Should not be linearized
            }
        [Fact]
        public void DistanceToSegment_ReturnsZero_WhenPointOnSegment()
        {
            double d = Path.DistanceToSegment(1, 1, 0, 0, 2, 2);
            Assert.True(Math.Abs(d) < 1e-8);
        }

        [Fact]
        public void IsNearPoint_ReturnsTrue_WhenPointNearPath()
        {
            var path = new SvgPathItem { D = "M 0 0 L 10 0" };
            Assert.True(Path.IsNearPoint(5, 1, path));
        }

        [Fact]
        public void EstimatedLength_CalculatesLength_ForSimplePath()
        {
            string d = "M 0 0 L 3 4";
            double len = Path.EstimatedLength(d);
            Assert.Equal(5, len, 3);
        }

        [Fact]
        public void GetPathNodesWithType_ParsesNodesAndControls()
        {
            var path = new SvgPathItem { D = "M 0 0 Q 1 2 3 4" };
            var nodes = Path.GetPathNodesWithType(path);
            Assert.Equal(3, nodes.Count);
            Assert.True(nodes[1].isControl);
        }

        [Fact]
        public void GetPoints_ParsesAllPoints()
        {
            var path = new SvgPathItem { D = "M 0 0 L 1 1 Q 2 2 3 3" };
            var pts = Path.GetPoints(path);
            Assert.Contains((1, 1), pts);
            Assert.Contains((2, 2), pts);
            Assert.Contains((3, 3), pts);
        }

        [Fact]
        public void IntersectsCircle_ReturnsTrue_WhenPathNearCircle()
        {
            var path = new SvgPathItem { D = "M 0 0 L 10 0" };
            Assert.True(Path.IntersectsCircle(path, 5, 0, 2));
        }

        [Fact]
        public void SegmentIntersectsCircle_ReturnsTrue_WhenSegmentNearCircle()
        {
            Assert.True(Path.SegmentIntersectsCircle(0, 0, 10, 0, 5, 0, 2));
        }

        [Fact]
        public void CircleIntersectsCircle_ReturnsTrue_WhenOverlap()
        {
            var c = new SvgCircleItem { Cx = 0, Cy = 0, R = 5 };
            Assert.True(Path.CircleIntersectsCircle(c, 7, 0, 3));
        }

        [Fact]
        public void IntersectsRect_ReturnsTrue_WhenPathPointInRect()
        {
            var path = new SvgPathItem { D = "M 5 5 L 10 10" };
            path.Bounds = Path.GetBounds(path.D);
            // Full geometry mode
            Assert.True(Path.IntersectsRect(path, 0, 0, 6, 6));
            // Fast bounds-only mode (should also be true, as bounds overlap)
            Assert.True(Path.IntersectsRect(path, 0, 0, 6, 6));

            // Path outside rect
            var path2 = new SvgPathItem { D = "M 20 20 L 30 30" };
            path2.Bounds = Path.GetBounds(path2.D);
            Assert.False(Path.IntersectsRect(path2, 0, 0, 6, 6));
            Assert.False(Path.IntersectsRect(path2, 0, 0, 6, 6));
        }

        [Fact]
        public void CircleIntersectsRect_ReturnsTrue_WhenOverlap()
        {
            var c = new SvgCircleItem { Cx = 5, Cy = 5, R = 3 };
            Assert.True(Path.CircleIntersectsRect(c, 0, 0, 6, 6));
        }

        [Fact]
        public void IsPointInCircle_ReturnsTrue_WhenInside()
        {
            var c = new SvgCircleItem { Cx = 0, Cy = 0, R = 2 };
            Assert.True(Path.IsPointInCircle(1, 1, c));
        }

        [Fact]
        public void OffsetPathD_OffsetsAllPoints()
        {
            string d = "M 0 0 L 1 1 Q 2 2 3 3";
            string result = Path.OffsetPathD(d, (10, 20));
            Assert.Contains("M 10 20", result);
            Assert.Contains("L 11 21", result);
            Assert.Contains("Q 12 22 13 23", result);
        }
    }
}
