using NUnit.Framework;

namespace RT.Geometry.Tests;

[TestFixture]
public sealed class PolygonDTests
{
    private const double Epsilon = 1e-10;

    [Test]
    public void TestContainsPoint()
    {
        // Simple inside
        var square = new PolygonD(new PointD(0, 0), new PointD(10, 0), new PointD(10, 10), new PointD(0, 10));
        Assert.IsTrue(square.ContainsPoint(new PointD(5, 5)));
        Assert.IsTrue(square.ContainsPoint(new PointD(1, 1)));
        Assert.IsTrue(square.ContainsPoint(new PointD(9, 9)));
        // Simple outside
        Assert.IsFalse(square.ContainsPoint(new PointD(-1, 5)));
        Assert.IsFalse(square.ContainsPoint(new PointD(11, 5)));
        Assert.IsFalse(square.ContainsPoint(new PointD(5, -1)));
        Assert.IsFalse(square.ContainsPoint(new PointD(5, 11)));
        Assert.IsFalse(square.ContainsPoint(new PointD(15, 15)));

        // On vertex
        var triangle = new PolygonD(new PointD(0, 0), new PointD(10, 0), new PointD(5, 10));
        Assert.IsTrue(triangle.ContainsPoint(new PointD(0, 0)));
        Assert.IsTrue(triangle.ContainsPoint(new PointD(10, 0)));
        Assert.IsTrue(triangle.ContainsPoint(new PointD(5, 10)));
        // On edge
        Assert.IsTrue(triangle.ContainsPoint(new PointD(5, 0)));
        Assert.IsTrue(triangle.ContainsPoint(new PointD(2.5, 5)));
        Assert.IsTrue(triangle.ContainsPoint(new PointD(7.5, 5)));

        // Concave
        var concave = new PolygonD(new PointD(0, 0), new PointD(6, 0), new PointD(6, 4), new PointD(3, 4), new PointD(3, 2), new PointD(0, 2));
        Assert.IsTrue(concave.ContainsPoint(new PointD(1, 1)));
        Assert.IsTrue(concave.ContainsPoint(new PointD(5, 2)));
        Assert.IsTrue(concave.ContainsPoint(new PointD(4, 3)));
        Assert.IsFalse(concave.ContainsPoint(new PointD(1, 3)));

        // L shape
        var Lshape = new PolygonD(new PointD(0, 0), new PointD(4, 0), new PointD(4, 2), new PointD(2, 2), new PointD(2, 4), new PointD(0, 4));
        Assert.IsTrue(Lshape.ContainsPoint(new PointD(1, 1)));
        Assert.IsTrue(Lshape.ContainsPoint(new PointD(3, 1)));
        Assert.IsTrue(Lshape.ContainsPoint(new PointD(1, 3)));
        Assert.IsFalse(Lshape.ContainsPoint(new PointD(3, 3)));

        // Horizontal edge
        var polygon1 = new PolygonD(new PointD(0, 0), new PointD(4, 0), new PointD(4, 2), new PointD(6, 2), new PointD(6, 4), new PointD(0, 4));
        Assert.IsTrue(polygon1.ContainsPoint(new PointD(2, 2)));
        Assert.IsTrue(polygon1.ContainsPoint(new PointD(5, 3)));
        Assert.IsFalse(polygon1.ContainsPoint(new PointD(5, 1)));

        // Vertical edge
        var polygon2 = new PolygonD(new PointD(0, 0), new PointD(2, 0), new PointD(2, 2), new PointD(4, 2), new PointD(4, 4), new PointD(0, 4));
        Assert.IsTrue(polygon2.ContainsPoint(new PointD(1, 1)));
        Assert.IsTrue(polygon2.ContainsPoint(new PointD(3, 3)));
        Assert.IsFalse(polygon2.ContainsPoint(new PointD(3, 1)));

        // Point on horizontal edge
        var rectangle1 = new PolygonD(new PointD(0, 0), new PointD(10, 0), new PointD(10, 5), new PointD(0, 5));
        Assert.IsTrue(rectangle1.ContainsPoint(new PointD(5, 0)));
        Assert.IsTrue(rectangle1.ContainsPoint(new PointD(5, 5)));
        Assert.IsTrue(rectangle1.ContainsPoint(new PointD(2, 0)));
        Assert.IsTrue(rectangle1.ContainsPoint(new PointD(8, 5)));

        // Collinear vertices
        var polygonWithCollinearVertices = new PolygonD(new PointD(0, 0), new PointD(5, 0), new PointD(10, 0), new PointD(10, 5), new PointD(5, 5), new PointD(0, 5));
        Assert.IsTrue(polygonWithCollinearVertices.ContainsPoint(new PointD(5, 2)));
        Assert.IsTrue(polygonWithCollinearVertices.ContainsPoint(new PointD(7, 0)));
        Assert.IsFalse(polygonWithCollinearVertices.ContainsPoint(new PointD(5, -1)));

        // Very small polygon
        var tiny = new PolygonD(new PointD(0, 0), new PointD(1e-6, 0), new PointD(1e-6, 1e-6), new PointD(0, 1e-6));
        Assert.IsTrue(tiny.ContainsPoint(new PointD(5e-7, 5e-7)));
        Assert.IsFalse(tiny.ContainsPoint(new PointD(2e-6, 2e-6)));

        // Very large polygon
        var largePoly = new PolygonD(new PointD(1e10, 1e10), new PointD(1e10 + 100, 1e10), new PointD(1e10 + 100, 1e10 + 100), new PointD(1e10, 1e10 + 100));
        Assert.IsTrue(largePoly.ContainsPoint(new PointD(1e10 + 50, 1e10 + 50)));
        Assert.IsFalse(largePoly.ContainsPoint(new PointD(1e10 - 1, 1e10 + 50)));

        // Degenerate: fewer than 3 points
        var empty = new PolygonD();
        Assert.IsFalse(empty.ContainsPoint(new PointD(0, 0)));
        var singlePoint = new PolygonD(new PointD(5, 5));
        Assert.IsFalse(singlePoint.ContainsPoint(new PointD(5, 5)));
        var twoPoints = new PolygonD(new PointD(0, 0), new PointD(10, 10));
        Assert.IsFalse(twoPoints.ContainsPoint(new PointD(5, 5)));

        // All vertices at same Y
        var horizontalLine = new PolygonD(new PointD(0, 5), new PointD(5, 5), new PointD(10, 5));
        Assert.IsFalse(horizontalLine.ContainsPoint(new PointD(5, 5)));
        Assert.IsFalse(horizontalLine.ContainsPoint(new PointD(5, 4)));
        Assert.IsFalse(horizontalLine.ContainsPoint(new PointD(5, 6)));

        // Negative coordinates
        var polygon = new PolygonD(new PointD(-5, -5), new PointD(5, -5), new PointD(5, 5), new PointD(-5, 5));
        Assert.IsTrue(polygon.ContainsPoint(new PointD(0, 0)));
        Assert.IsTrue(polygon.ContainsPoint(new PointD(-3, -3)));
        Assert.IsFalse(polygon.ContainsPoint(new PointD(-6, 0)));
        Assert.IsFalse(polygon.ContainsPoint(new PointD(0, 6)));

        // Star (no hole)
        var star = new PolygonD(new PointD(5, 0), new PointD(6, 3), new PointD(10, 3), new PointD(7, 5), new PointD(8, 9),
            new PointD(5, 7), new PointD(2, 9), new PointD(3, 5), new PointD(0, 3), new PointD(4, 3));
        Assert.IsTrue(star.ContainsPoint(new PointD(5, 0.02))); // just inside the top point
        Assert.IsTrue(star.ContainsPoint(new PointD(7.98, 8.97))); // just inside the bottom right point
        Assert.IsFalse(star.ContainsPoint(new PointD(3.8, 2.8))); // top left corner
        Assert.IsFalse(star.ContainsPoint(new PointD(7.2, 5.0))); // right corner

        // Star with hole (self-intersecting)
        var starWithHole = new PolygonD(
            new PointD(5, 0),    // top point
            new PointD(2, 8),    // bottom left inner
            new PointD(10, 3),   // right outer
            new PointD(0, 3),    // left outer
            new PointD(8, 8)    // bottom right inner
        );
        Assert.IsFalse(starWithHole.ContainsPoint(new PointD(5, 4))); // center hole
        Assert.IsFalse(starWithHole.ContainsPoint(new PointD(5, 5))); // center hole
        Assert.IsTrue(starWithHole.ContainsPoint(new PointD(1, 3))); // left point area
        Assert.IsTrue(starWithHole.ContainsPoint(new PointD(9, 3))); // right point area
        Assert.IsTrue(starWithHole.ContainsPoint(new PointD(3, 7))); // bottom left point area
        Assert.IsTrue(starWithHole.ContainsPoint(new PointD(7, 7))); // bottom right point area
    }
}
