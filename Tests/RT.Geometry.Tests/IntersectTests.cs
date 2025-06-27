using NUnit.Framework;

namespace RT.Geometry.Tests;

[TestFixture]
public sealed class IntersectTests
{
    /// <summary>
    /// Like Math.Min but with a total order on doubles.
    /// </summary>
    private double MinTO(double d1, double d2)
    {
        // -Inf < negatives < 0 < positives < +Inf < NaN
        if (double.IsNaN(d1))
            return d2;
        else if (double.IsNaN(d2))
            return d1;
        else if (d1 < d2)
            return d1;
        else
            return d2;
    }

    /// <summary>
    /// Like Math.Max but with a total order on doubles.
    /// </summary>
    private double MaxTO(double d1, double d2)
    {
        // -Inf < negatives < 0 < positives < +Inf < NaN
        if (double.IsNaN(d1))
            return d1;
        else if (double.IsNaN(d2))
            return d2;
        else if (d1 > d2)
            return d1;
        else
            return d2;
    }

    private void AssertLineWithCircle(double frX, double frY, double toX, double toY, double cX, double cY, double cR, double expL1, double expL2)
    {
        EdgeD lin = new EdgeD(frX, frY, toX, toY);
        CircleD cir = new CircleD(cX, cY, cR);
        double l1, l2;
        Intersect.LineWithCircle(ref lin, ref cir, out l1, out l2);
        Assert.AreEqual(MinTO(expL1, expL2), MinTO(l1, l2), 0.001);
        Assert.AreEqual(MaxTO(expL1, expL2), MaxTO(l1, l2), 0.001);
    }

    private void AssertRayWithCircle(double frX, double frY, double toX, double toY, double cX, double cY, double cR, double expL1, double expL2)
    {
        EdgeD ray = new EdgeD(frX, frY, toX, toY);
        CircleD cir = new CircleD(cX, cY, cR);
        double l1, l2;
        Intersect.RayWithCircle(ref ray, ref cir, out l1, out l2);
        Assert.AreEqual(expL1, l1, 0.001);
        Assert.AreEqual(expL2, l2, 0.001);
    }

    [Test]
    public void LineWithCircle()
    {
        AssertLineWithCircle(0, 0, /**/ 1, 0, /*   */ 0, 0, 2.5, /*   */ 2.5, -2.5);
        AssertLineWithCircle(0, 0, /**/ -1, 0, /*   */ 0, 0, 2.6, /*   */ 2.6, -2.6);
        AssertLineWithCircle(0, 0, /**/ 0, 1, /*   */ 0, 0, 2.7, /*   */ 2.7, -2.7);
        AssertLineWithCircle(0, 0, /**/ 0, -1, /*   */ 0, 0, 2.8, /*   */ 2.8, -2.8);

        AssertLineWithCircle(0, 0, /**/ 1, 0, /*   */ 1, 0, 2.5, /*   */ 3.5, -1.5);
        AssertLineWithCircle(0, 0, /**/ -1, 0, /*   */ 1, 0, 2.6, /*   */ 1.6, -3.6);
        AssertLineWithCircle(0, 0, /**/ 0, 1, /*   */ 0, 1, 2.7, /*   */ 3.7, -1.7);
        AssertLineWithCircle(0, 0, /**/ 0, -1, /*   */ 0, 1, 2.8, /*   */ 1.8, -3.8);

        AssertLineWithCircle(0, 0, /**/ 2, 0, /*   */ 0, 0, 2.5, /*   */ 2.5 / 2, -2.5 / 2);
        AssertLineWithCircle(0, 0, /**/ -3, 0, /*   */ 0, 0, 2.6, /*   */ 2.6 / 3, -2.6 / 3);
        AssertLineWithCircle(0, 0, /**/ 0, 4, /*   */ 0, 0, 2.7, /*   */ 2.7 / 4, -2.7 / 4);
        AssertLineWithCircle(0, 0, /**/ 0, -5, /*   */ 0, 0, 2.8, /*   */ 2.8 / 5, -2.8 / 5);

        AssertLineWithCircle(-10, 0, /**/ -8, 0, /*   */ 0, 0, 3.0, /*   */ 7.0 / 2, 13.0 / 2);

        AssertLineWithCircle(0, -10, /**/ 0, -7, /*   */ 3, 2, 3.0, /*   */ 12.0 / 3, 12.0 / 3);

        AssertLineWithCircle(-10, 0, /**/ -8, 0, /*   */ 0, 4.0, 3.0, /*   */ double.NaN, double.NaN);

        // x1=8.5109,y1=14.0218       x2=13.089,y2=23.178    (dist to 8,20 is 6 for both)
        AssertLineWithCircle(3, 3, /**/ 4, 5, /*   */ 8, 20, 6.0, /*   */ 5.5109, 10.089);
    }

    [Test]
    public void RayWithCircle()
    {
        AssertRayWithCircle(0, 0, /**/ 1, 0, /*   */ 0, 0, 2.5, /*   */ 2.5, double.NaN);
        AssertRayWithCircle(0, 0, /**/ -1, 0, /*   */ 0, 0, 2.6, /*   */ 2.6, double.NaN);
        AssertRayWithCircle(0, 0, /**/ 0, 1, /*   */ 0, 0, 2.7, /*   */ 2.7, double.NaN);
        AssertRayWithCircle(0, 0, /**/ 0, -1, /*   */ 0, 0, 2.8, /*   */ 2.8, double.NaN);

        AssertRayWithCircle(0, 0, /**/ 2, 0, /*   */ 0, 0, 2.5, /*   */ 2.5 / 2, double.NaN);
        AssertRayWithCircle(0, 0, /**/ -3, 0, /*   */ 0, 0, 2.6, /*   */ 2.6 / 3, double.NaN);
        AssertRayWithCircle(0, 0, /**/ 0, 4, /*   */ 0, 0, 2.7, /*   */ 2.7 / 4, double.NaN);
        AssertRayWithCircle(0, 0, /**/ 0, -5, /*   */ 0, 0, 2.8, /*   */ 2.8 / 5, double.NaN);

        AssertRayWithCircle(-10, 0, /**/ -8, 0, /*   */ 0, 0, 3.0, /*   */ 7.0 / 2, 13.0 / 2);

        AssertRayWithCircle(0, -10, /**/ 0, -7, /*   */ 3, 2, 3.0, /*   */ 12.0 / 3, 12.0 / 3);

        AssertRayWithCircle(-10, 0, /**/ -8, 0, /*   */ 0, 4.0, 3.0, /*   */ double.NaN, double.NaN);
    }

}
