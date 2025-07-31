using NUnit.Framework;

namespace RT.Geometry.Tests;

[TestFixture]
public sealed class TriangulateTests
{
    [Test]
    public static void TestTriangulateBasic()
    {
        void testDelaunayEdges(PointD[] vertices, (int, int)[] expectedEdges)
        {
            var edges = Triangulate.DelaunayEdges(vertices).ToArray();
            Assert.AreEqual(edges.Length, expectedEdges.Length);
            foreach (var expected in expectedEdges)
                Assert.IsTrue(edges.Contains(expected) || edges.Contains((expected.Item2, expected.Item1))); // unordered
        }
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

        void testTriangles(PointD[] vertices, (int, int)[] edges, (int V1, int V2, int V3)[] expected)
        {
            var triangles = Triangulate.TriangleVerticesFromEdges(vertices, edges).ToList();
            Assert.AreEqual(triangles.Count, expected.Length);
            foreach (var exT in expected)
            {
                // must contain in order 1,2,3, or 2,3,1, or 3,1,2
                Assert.IsTrue(triangles.Any(acT =>
                    (acT.V1 == exT.V1 && acT.V2 == exT.V2 && acT.V3 == exT.V3) ||
                    (acT.V1 == exT.V2 && acT.V2 == exT.V3 && acT.V3 == exT.V1) ||
                    (acT.V1 == exT.V3 && acT.V2 == exT.V1 && acT.V3 == exT.V2)));
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
}
