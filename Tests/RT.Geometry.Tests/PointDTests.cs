using NUnit.Framework;

namespace RT.Geometry.Tests;

[TestFixture]
public sealed class PointDTests
{
    private const double Epsilon = 1e-10;

    [Test]
    public void TestConstructors()
    {
        var p1 = new PointD(3, 4);
        Assert.AreEqual(3, p1.X);
        Assert.AreEqual(4, p1.Y);

        var p2 = new PointD(Math.PI / 2);
        Assert.AreEqual(0, p2.X, Epsilon);
        Assert.AreEqual(1, p2.Y, Epsilon);
    }

    [Test]
    public void TestEquality()
    {
        var p1 = new PointD(3, 4);
        var p2 = new PointD(3, 4);
        var p3 = new PointD(4, 3);

        Assert.IsTrue(p1.Equals(p2));
        Assert.IsTrue(p1 == p2);
        Assert.IsFalse(p1.Equals(p3));
        Assert.IsFalse(p1 == p3);
        Assert.IsTrue(p1 != p3);
    }

    [Test]
    public void TestOperators()
    {
        var p1 = new PointD(3, 4);
        var p2 = new PointD(1, 2);

        var pAdd = p1 + p2;
        Assert.AreEqual(4, pAdd.X, Epsilon);
        Assert.AreEqual(6, pAdd.Y, Epsilon);

        var pNeg = -p1;
        Assert.AreEqual(-3, pNeg.X, Epsilon);
        Assert.AreEqual(-4, pNeg.Y, Epsilon);

        var pSub = p1 - p2;
        Assert.AreEqual(2, pSub.X, Epsilon);
        Assert.AreEqual(2, pSub.Y, Epsilon);

        var pMul = p1 * 2;
        Assert.AreEqual(6, pMul.X, Epsilon);
        Assert.AreEqual(8, pMul.Y, Epsilon);

        var pMul2 = 2 * p1;
        Assert.AreEqual(6, pMul2.X, Epsilon);
        Assert.AreEqual(8, pMul2.Y, Epsilon);

        var pDiv = p1 / 2;
        Assert.AreEqual(1.5, pDiv.X, Epsilon);
        Assert.AreEqual(2, pDiv.Y, Epsilon);
    }

    [Test]
    public void TestProperties()
    {
        var p = new PointD(3, 4);
        Assert.AreEqual(5, p.Length, Epsilon);
        Assert.AreEqual(25, p.SquareLength, Epsilon);
        Assert.AreEqual(Math.Atan2(4, 3), p.Angle, Epsilon);

        var normalized = p.Normalized;
        Assert.AreEqual(0.6, normalized.X, Epsilon);
        Assert.AreEqual(0.8, normalized.Y, Epsilon);
        Assert.AreEqual(1, normalized.Length, Epsilon);
    }

    [Test]
    public void TestMethods()
    {
        var p1 = new PointD(3, 4);
        var p2 = new PointD(-1, 2);

        Assert.AreEqual(5, p1.Dot(p2), Epsilon);
        Assert.AreEqual(10, p1.CrossZ(p2), Epsilon);

        var pRot90 = p1.Rotate90();
        Assert.AreEqual(-4, pRot90.X, Epsilon);
        Assert.AreEqual(3, pRot90.Y, Epsilon);

        var pRotNeg90 = p1.RotateNeg90();
        Assert.AreEqual(4, pRotNeg90.X, Epsilon);
        Assert.AreEqual(-3, pRotNeg90.Y, Epsilon);

        var pRot = p1.Rotate(Math.PI / 2);
        Assert.AreEqual(-4, pRot.X, Epsilon);
        Assert.AreEqual(3, pRot.Y, Epsilon);

        var pRotDeg = p1.RotateDeg(90);
        Assert.AreEqual(-4, pRotDeg.X, Epsilon);
        Assert.AreEqual(3, pRotDeg.Y, Epsilon);

        var pRotAbout = new PointD(4, 6).RotateDeg(90, new PointD(3, 6));
        Assert.AreEqual(3, pRotAbout.X, Epsilon);
        Assert.AreEqual(7, pRotAbout.Y, Epsilon);

        Assert.AreEqual(Math.Sqrt(20), p1.Distance(p2), Epsilon);
    }

    [Test]
    public void TestProjection()
    {
        var p1 = new PointD(3, 4);
        var p2 = new PointD(1, 0);

        Assert.AreEqual(3, p1.LengthProjectedOnto(p2), Epsilon);
        var projected = p1.ProjectedOnto(p2);
        Assert.AreEqual(3, projected.X, Epsilon);
        Assert.AreEqual(0, projected.Y, Epsilon);

        p1.DecomposeAlong(p2, out var lenAlong, out var lenNormal);
        Assert.AreEqual(3, lenAlong, Epsilon);
        Assert.AreEqual(-4, lenNormal, Epsilon);

        var recomposed = p2.RecomposeVector(lenAlong, lenNormal);
        Assert.AreEqual(p1.X, recomposed.X, Epsilon);
        Assert.AreEqual(p1.Y, recomposed.Y, Epsilon);
    }

    [Test]
    public void TestReflection()
    {
        var p = new PointD(3, 4);
        var reflectedX = p.ReflectedAboutXAxis;
        Assert.AreEqual(3, reflectedX.X, Epsilon);
        Assert.AreEqual(-4, reflectedX.Y, Epsilon);

        var reflectedY = p.ReflectedAboutYAxis;
        Assert.AreEqual(-3, reflectedY.X, Epsilon);
        Assert.AreEqual(4, reflectedY.Y, Epsilon);

        var axis1 = new PointD(1, 1);
        var axis2 = new PointD(2, 2);
        var reflected = new PointD(1, 2).Reflected(axis1, axis2);
        Assert.AreEqual(2, reflected.X, Epsilon);
        Assert.AreEqual(1, reflected.Y, Epsilon);
    }
}
