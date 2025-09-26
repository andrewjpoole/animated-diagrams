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
            Assert.True(Path.IntersectsRect(path, 0, 0, 6, 6));
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
