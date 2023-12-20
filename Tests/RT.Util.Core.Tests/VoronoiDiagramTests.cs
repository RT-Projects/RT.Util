using NUnit.Framework;
using RT.KitchenSink.Geometry;
using RT.Util.Geometry;

namespace RT.Util;

[TestFixture]
public sealed class VoronoiDiagramTests
{
    static void assertHasVertex(PolygonD polygon, double x, double y)
    {
        Assert.IsTrue(polygon.Vertices.Any(v => Math.Abs(v.X - x) < 1e-9 && Math.Abs(v.Y - y) < 1e-9));
    }

    [Test]
    public static void TestVoronoiBasic()
    {
        var vd = VoronoiDiagram.GenerateVoronoiDiagram(new[] { new PointD(1, 1), new PointD(9, 9) }, 10, 10, VoronoiDiagramFlags.IncludeEdgePolygons);
        Assert.AreEqual(2, vd.Polygons.Length);
        assertHasVertex(vd.Polygons[0], 0, 0);
        assertHasVertex(vd.Polygons[0], 10, 0);
        assertHasVertex(vd.Polygons[0], 0, 10);
        assertHasVertex(vd.Polygons[1], 10, 10);
        assertHasVertex(vd.Polygons[1], 10, 0);
        assertHasVertex(vd.Polygons[1], 0, 10);
    }

    [Test]
    public static void TestVoronoiSinglePoint()
    {
        var vd = VoronoiDiagram.GenerateVoronoiDiagram(new[] { new PointD(1, 1) }, 10, 10, VoronoiDiagramFlags.IncludeEdgePolygons);
        assertHasVertex(vd.Polygons[0], 0, 0);
        assertHasVertex(vd.Polygons[0], 10, 0);
        assertHasVertex(vd.Polygons[0], 0, 10);
        assertHasVertex(vd.Polygons[0], 10, 10);
    }

    [Test]
    public static void TestVoronoiEdgePolygonCornerCase()
    {
        var sites = new[] {
            new PointD(1.58, 13.93),
            new PointD(1.03, 14.24),
            new PointD(1.29, 14.04),
            new PointD(1.89, 14.08),
        };
        var vd = VoronoiDiagram.GenerateVoronoiDiagram(sites, 14, 15, VoronoiDiagramFlags.IncludeEdgePolygons);
        Assert.AreEqual(4, vd.Polygons.Length);
    }
}
