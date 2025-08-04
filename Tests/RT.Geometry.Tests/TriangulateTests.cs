using NUnit.Framework;

namespace RT.Geometry.Tests;

[TestFixture]
public sealed class TriangulateTests
{
    static void testDelaunayEdges(PointD[] vertices, (int, int)[] expectedEdges)
    {
        var edges = Triangulate.DelaunayEdges(vertices).ToArray();
        Assert.AreEqual(expectedEdges.Length, edges.Length);
        foreach (var expected in expectedEdges)
            Assert.IsTrue(edges.Contains(expected) || edges.Contains((expected.Item2, expected.Item1))); // unordered
    }

    [Test]
    public static void TestTriangulateBasic()
    {
        testDelaunayEdges([], []);
        testDelaunayEdges([new PointD(300, 700)], []);
        testDelaunayEdges([new PointD(300, 700), new PointD(600, 800)], [(0, 1)]);
        testDelaunayEdges([new PointD(300, 700), new PointD(600, 800), new PointD(200, 900)], [(0, 1), (1, 2), (0, 2)]);
        // 4th point inside the triangle:
        testDelaunayEdges([new PointD(300, 700), new PointD(600, 800), new PointD(200, 900), new PointD(400, 800)],
            [(0, 1), (1, 2), (0, 2), (3, 0), (3, 1), (3, 2)]);
        // 5th point outside the triangle, breaking 0-1 link
        testDelaunayEdges([new PointD(300, 700), new PointD(600, 800), new PointD(200, 900), new PointD(400, 800), new PointD(400, 700)],
            [(0, 4), (0, 3), (0, 2), (1, 4), (1, 3), (3, 4), (2, 3), (2, 1)]);
        // requires intersection finder to return an intersection that isn't between the parabolas
        testDelaunayEdges([new PointD(0.42, 0.85), new PointD(0.30, 0.9), new PointD(0.11, 0.1)],
            [(0, 1), (1, 2), (2, 0)]);


        void testTriangles(PointD[] vertices, (int, int)[] edges, (int v1, int v2, int v3)[] expected)
        {
            var triangles = Triangulate.TriangleVerticesFromEdges(vertices, edges).ToList();
            Assert.AreEqual(triangles.Count, expected.Length);
            foreach (var exT in expected)
            {
                // must contain in order 1,2,3, or 2,3,1, or 3,1,2
                Assert.IsTrue(triangles.Any(acT =>
                    (acT.v1 == exT.v1 && acT.v2 == exT.v2 && acT.v3 == exT.v3) ||
                    (acT.v1 == exT.v2 && acT.v2 == exT.v3 && acT.v3 == exT.v1) ||
                    (acT.v1 == exT.v3 && acT.v2 == exT.v1 && acT.v3 == exT.v2)));
            }
        }

        testTriangles([], [], expected: []);
        testTriangles([new PointD(300, 700)], [], expected: []);
        testTriangles([new PointD(300, 700), new PointD(600, 800)], [(0, 1)], expected: []);
        testTriangles([new PointD(300, 700), new PointD(600, 800), new PointD(200, 900)], [(0, 1), (1, 2), (0, 2)], expected: [(0, 1, 2)]);
        // reverse points:
        testTriangles([new PointD(300, 700), new PointD(200, 900), new PointD(600, 800)], [(0, 1), (1, 2), (0, 2)], expected: [(0, 2, 1)]);
        // 5 point graph from above:
        testTriangles([new PointD(300, 700), new PointD(600, 800), new PointD(200, 900), new PointD(400, 800), new PointD(400, 700)],
            [(0, 4), (0, 3), (0, 2), (1, 4), (1, 3), (3, 4), (2, 3), (2, 1)],
            expected: [(0, 4, 3), (4, 1, 3), (1, 2, 3), (0, 3, 2)]);
    }

    [Test]
    public static void TestNearlyVertical()
    {
        // this produces a numerically bad quadratic root in the original implementation, thus missing an edge
        testDelaunayEdges([
            new PointD(0.41586468, 11.063),
            new PointD(47.110000000000007, 11.089),
            new PointD(47.11, 10.1),
        ], [(0, 1), (1, 2), (2, 0)]);

        // similar to above, but the points are the first two points in the scan
        testDelaunayEdges([
            new PointD(0.41586468, 1.063),
            new PointD(0.11000000000000001, 1.089),
            new PointD(0.11, 0.1),
        ], [(0, 1), (1, 2), (2, 0)]);

        // bigger variants of the above
        testDelaunayEdges([
            new PointD(0.41586468, 45.06283759),
            new PointD(0.11000000000000001, 45.08910442),
            new PointD(0.11, 44.1),
            new PointD(0.41956433, 44.92768723),
        ], [(2, 1), (0, 1), (3, 1), (3, 0), (2, 3)]);

        // Timwi's cursed polygon
        //testDelaunayEdges([
        //    new PointD(0.41586468, 45.06283759),
        //    new PointD(0.11000000000000001, 45.08910442),
        //    new PointD(0.11, 44.1),
        //    new PointD(0.11000000000000001, 43.11012299),
        //    new PointD(0.42641961, 43.11399106),
        //    new PointD(0.41956433, 44.92768723),
        //], [(3, 2), (2, 1), (0, 1), (5, 1), (4, 3), (5, 0), (2, 5), (4, 2), (4, 5)]);

        // these tests mirror the Voronoi test but also verifies resulting adjacency
        testDelaunayEdges([
            new PointD(0.415, 45.06),
            new PointD(0.11000000000000001, 45.08),
            new PointD(0.11, 44.1)
        ], [(0, 1), (1, 2), (2, 0)]);

        testDelaunayEdges([
            new PointD(0.11, 44.1),
            new PointD(0.11000000000000001, 43.11),
            new PointD(0.419, 44.92),
        ], [(0, 1), (1, 2), (2, 0)]);
    }
}
