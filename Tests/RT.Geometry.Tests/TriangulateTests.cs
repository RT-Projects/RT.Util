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
        testDelaunayEdges([
            new PointD(0.41586468, 45.06283759),
            new PointD(0.11000000000000001, 45.08910442),
            new PointD(0.11, 44.1),
            new PointD(0.11000000000000001, 43.11012299),
            new PointD(0.42641961, 43.11399106),
            new PointD(0.41956433, 44.92768723),
        ], [(3, 2), (2, 1), (0, 1), (5, 1), (4, 3), (5, 0), (2, 5), (4, 2), (4, 5)]);

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

        // negative coordinates
        testDelaunayEdges([
            new PointD(-50, 50),
            new PointD(50, 50),
        ], [(0, 1)]);
    }

    [Test]
    public void TestPolygonTriangles()
    {
        //m 5,0 1,3 h 4 L 7,5 8,9 5,7 2,9 3,5 0,3 H 4 Z M 4.5,3.5 5.3,4.8 3.3,7.1 Z
        var starOutside = new PolygonD(new PointD(5, 0), new PointD(6, 3), new PointD(10, 3), new PointD(7, 5), new PointD(8, 9),
            new PointD(5, 7), new PointD(2, 9), new PointD(3, 5), new PointD(0, 3), new PointD(4, 3));
        var starHole = new PolygonD(new PointD(4.5, 3.5), new PointD(5.3, 4.8), new PointD(3.3, 7.1));
        var (vs, es, pes) = Triangulate.DelaunayEdgesConstrained([starOutside, starHole]);
        Assert.AreEqual(15, vs.Count);
        var ixs1 = vs.IndexOf((vs[10] + vs[12]) / 2); // left split
        var ixs2 = vs.IndexOf((vs[11] + vs[12]) / 2); // right split
        assertEdges(es, [(0, 1), (0, 9), (1, 2), (1, 3), (1, 11), (1, 10), (1, 9), (2, 3), (3, 4), (3, 5), (3, 11), (4, 5), (5, 6), (5, 12), (5, ixs2), (5, 11),
            (6, 7), (6, 12), (7, 8), (7, 9), (7, 10), (7, ixs1), (7, 12), (8, 9), (9, 10), (10, 11), (10, ixs1), (11, ixs2), (12, ixs1), (12, ixs2), // border and internal edges
            (0, 2), (0, 8), (2, 4), (4, 6), (6, 8), (11, ixs1), (ixs1, ixs2)]); // internal edges
        var triangles = Triangulate.DelaunayTriangles([starOutside, starHole]);
        assertTriangles(triangles, vs, [(0, 1, 9), (1, 2, 3), (1, 3, 11), (1, 11, 10), (1, 10, 9), (3, 4, 5), (3, 5, 11),
            (5, 6, 12), (5, 12, ixs2), (5, ixs2, 11), (6, 7, 12), (7, 8, 9), (7, 9, 10), (7, 10, ixs1), (7, ixs1, 12)]);

        // Differences to the above test which all require different handling:
        // - the edges of the hole are not split
        // - inner hole is just one triangle
        // - the convex hull of the points is a triangle
        var triOutside = new PolygonD(new PointD(5, 0), new PointD(10, 10), new PointD(0, 10));
        var triHole = new PolygonD(new PointD(5, 6), new PointD(4, 4), new PointD(6, 4));
        (vs, es, pes) = Triangulate.DelaunayEdgesConstrained([triOutside, triHole]);
        Assert.AreEqual(6, vs.Count);
        assertEdges(es, [(0, 1), (1, 2), (2, 0), (3, 4), (4, 5), (5, 3), (0, 4), (0, 5), (1, 3), (1, 5), (2, 3), (2, 4)]);
        triangles = Triangulate.DelaunayTriangles([triOutside, triHole]);
        assertTriangles(triangles, vs, [(0, 1, 5), (0, 5, 4), (0, 4, 2), (1, 2, 3), (1, 3, 5), (2, 4, 3)]);
        triangles = Triangulate.DelaunayTriangles([triOutside, triHole], true);
        assertTriangles(triangles, vs, [(0, 5, 1), (0, 4, 5), (0, 2, 4), (1, 3, 2), (1, 5, 3), (2, 3, 4)]);

        // two "vertical" diamonds, not connected to each other
        var pts = new[] { (2, 1), (3, 2), (2, 3), (1, 2), (2 + 5, 1), (3 + 5, 2), (2 + 5, 3), (1 + 5, 2) }.Select(p => new PointD(p.Item1, p.Item2)).ToArray();
        var edges = new[] { (0, 1), (1, 2), (2, 3), (3, 0), (3, 1), (4, 5), (5, 6), (6, 7), (7, 4), (7, 5) };
        var tris = Triangulate.TrianglesFromEdges(pts, edges);
        assertTriangles(tris, pts, [(0, 1, 3), (1, 2, 3), (4, 5, 7), (5, 6, 7)]);
        // same but now there's an edge connecting them - possible if triangulate edges got filtered before being converted to triangles
        edges = new[] { (0, 1), (1, 2), (2, 3), (3, 0), (3, 1), (4, 5), (5, 6), (6, 7), (7, 4), (7, 5), (1, 7) };
        tris = Triangulate.TrianglesFromEdges(pts, edges);
        assertTriangles(tris, pts, [(0, 1, 3), (1, 2, 3), (4, 5, 7), (5, 6, 7)]); // no change to triangles expected

        void assertEdges(IEnumerable<(int v1, int v2)> edges, (int v1, int v2)[] expected)
        {
            Assert.AreEqual(expected.Length, edges.Count());
            foreach (var expectedEdge in expected)
                Assert.IsTrue(edges.Contains(expectedEdge) || edges.Contains((expectedEdge.v2, expectedEdge.v1)));
        }
        void assertTriangles(IEnumerable<TriangleD> triangles, IList<PointD> vertices, (int v1, int v2, int v3)[] expected)
        {
            Assert.AreEqual(expected.Length, triangles.Count());
            foreach (var exT in expected)
            {
                Assert.IsTrue(triangles.Any(t =>
                    (t.V1 == vertices[exT.v1] && t.V2 == vertices[exT.v2] && t.V3 == vertices[exT.v3]) ||
                    (t.V1 == vertices[exT.v2] && t.V2 == vertices[exT.v3] && t.V3 == vertices[exT.v1]) ||
                    (t.V1 == vertices[exT.v3] && t.V2 == vertices[exT.v1] && t.V3 == vertices[exT.v2])));
            }
        }
    }
}

