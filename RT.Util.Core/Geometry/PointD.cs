using System.Drawing;

namespace RT.Util.Geometry;

/// <summary>Encapsulates a double-precision point.</summary>
public struct PointD : IEquatable<PointD>
{
    /// <summary>X-co-ordinate of the point.</summary>
    public double X;
    /// <summary>Y-co-ordinate of the point.</summary>
    public double Y;

    /// <summary>Initialises a double-precision point with the specified co-ordinates.</summary>
    public PointD(double x, double y)
    {
        X = x;
        Y = y;
    }

    /// <summary>Initialises a double-precision point as a unit vector at a specified angle (in radians).</summary>
    public PointD(double angle)
    {
        X = Math.Cos(angle);
        Y = Math.Sin(angle);
    }

    /// <summary>
    ///     Compares two <see cref="PointD"/> objects for equality.</summary>
    /// <param name="obj">
    ///     Object to compare against.</param>
    /// <returns>
    ///     True if considered equal.</returns>
    public override readonly bool Equals(object obj) => obj is PointD p && p.X == X && p.Y == Y;

    /// <summary>
    ///     Compares two <see cref="PointD"/> objects for equality.</summary>
    /// <param name="other">
    ///     Object to compare against.</param>
    /// <returns>
    ///     True if considered equal.</returns>
    public readonly bool Equals(PointD other) => other.X == X && other.Y == Y;

    /// <summary>
    ///     Compares two <see cref="PointD"/> objects for equality.</summary>
    /// <param name="one">
    ///     First object to compare.</param>
    /// <param name="other">
    ///     Object to compare against.</param>
    /// <returns>
    ///     True if considered equal.</returns>
    public static bool operator ==(PointD one, PointD other) => one.Equals(other);

    /// <summary>
    ///     Compares two <see cref="PointD"/> objects for inequality.</summary>
    /// <param name="one">
    ///     First object to compare.</param>
    /// <param name="other">
    ///     Object to compare against.</param>
    /// <returns>
    ///     True if considered different.</returns>
    public static bool operator !=(PointD one, PointD other) => !one.Equals(other);

    /// <summary>
    ///     Performs unary vector negation (i.e. the resulting point is of the same length but pointing in the opposite
    ///     direction).</summary>
    public static PointD operator -(PointD vector) => new(-vector.X, -vector.Y);

    /// <summary>Performs vector addition, returning the result.</summary>
    public static PointD operator +(PointD one, PointD other) => new(one.X + other.X, one.Y + other.Y);

    /// <summary>Performs vector subtraction, returning the result.</summary>
    public static PointD operator -(PointD left, PointD right) => new(left.X - right.X, left.Y - right.Y);

    /// <summary>Scales a vector by a scalar.</summary>
    public static PointD operator *(double scalar, PointD vector) => new(scalar * vector.X, scalar * vector.Y);

    /// <summary>Scales a vector by a scalar.</summary>
    public static PointD operator *(PointD vector, double scalar) => new(scalar * vector.X, scalar * vector.Y);

    /// <summary>Scales a vector by 1 / scalar (i.e. performs scalar division).</summary>
    public static PointD operator /(PointD vector, double scalar) => new(vector.X / scalar, vector.Y / scalar);

    /// <summary>Returns a hash code for the current <see cref="PointD"/>.</summary>
    public override readonly int GetHashCode() => X.GetHashCode() * 31 + Y.GetHashCode();

    /// <summary>
    ///     Converts the current <see cref="PointD"/> object to a <see cref="PointF"/>. Note that doing so loses
    ///     precision.</summary>
    /// <returns>
    ///     Lower-precision <see cref="PointF"/>.</returns>
    public readonly PointF ToPointF() => new((float) X, (float) Y);

    /// <summary>Converts the provided <see cref="PointF"/> to a <see cref="PointD"/>.</summary>
    public PointD(PointF pointF)
    {
        X = pointF.X;
        Y = pointF.Y;
    }

    /// <summary>
    ///     Provides a string representation of the current <see cref="PointD"/>.</summary>
    /// <returns>
    ///     A string representation of the current <see cref="PointD"/>.</returns>
    public override readonly string ToString() => $"X={X:R}, Y={Y:R}";

    /// <summary>Returns the theta (angle) of the vector represented by this <see cref="PointD"/>.</summary>
    public readonly double Theta() => Math.Atan2(Y, X);

    /// <summary>Returns the unit vector in the same direction as this one.</summary>
    public readonly PointD Unit()
    {
        double len = Math.Sqrt(X * X + Y * Y);
        return new PointD(X / len, Y / len);
    }

    /// <summary>Returns the dot product of this vector with the specified one.</summary>
    public readonly double Dot(PointD other) => X * other.X + Y * other.Y;

    /// <summary>
    ///     Returns the Z-component of the cross product of this vector with <paramref name="other"/>. The Z-component is
    ///     equal to the product of: the lengths of the two vectors and the sin of the angle between them. Note that the X
    ///     and Y components of a cross product of 2D vectors are always zero.</summary>
    public readonly double CrossZ(PointD other) => X * other.Y - Y * other.X;

    /// <summary>Returns a vector normal to this one.</summary>
    public readonly PointD Normal() => new(Y, -X);

    /// <summary>
    ///     Decomposes this vector into components relative to another vector.</summary>
    /// <param name="vector">
    ///     Reference vector.</param>
    /// <param name="lenAlong">
    ///     Length of this vector along the reference vector.</param>
    /// <param name="lenNormal">
    ///     Length of this vector normal to the reference vector.</param>
    public readonly void DecomposeAlong(PointD vector, out double lenAlong, out double lenNormal)
    {
        lenAlong = LengthProjectedOnto(vector);
        lenNormal = LengthProjectedOnto(vector.Normal());
    }

    /// <summary>
    ///     Performs the inverse of <see cref="DecomposeAlong"/> using the current vector as the reference vector.</summary>
    /// <param name="lenAlong">
    ///     Length of this vector along the reference vector.</param>
    /// <param name="lenNormal">
    ///     Length of this vector normal to the reference vector.</param>
    public readonly PointD RecomposeVector(double lenAlong, double lenNormal)
    {
        PointD unitVector = Unit();
        PointD unitVectorNormal = unitVector.Normal();
        return lenAlong * unitVector + lenNormal * unitVectorNormal;
    }

    /// <summary>Returns the length of this vector's projection onto the specified vector.</summary>
    public readonly double LengthProjectedOnto(PointD vector)
    {
        return Dot(vector) / vector.Distance();
    }

    /// <summary>Returns the length of this vector's projection onto a unit vector at the specified angle.</summary>
    public readonly double LengthProjectedOnto(double angle)
    {
        // Simplifying the following, where vector is a unit vector with theta = angle
        // return Dot(vector) / vector.Abs();
        // return (X*vector.X + Y*vector.Y) / 1.0;
        return X * Math.Cos(angle) + Y * Math.Sin(angle);
    }

    /// <summary>
    ///     Returns a vector representing the projection (i.e. length and direction) of this vector onto the specified
    ///     vector.</summary>
    public readonly PointD ProjectedOnto(PointD vector)
    {
        PointD unitVector = vector.Unit();
        return Dot(unitVector) * unitVector;
    }

    /// <summary>
    ///     Returns a vector of the same length as this vector, but rotated by the specified <paramref name="angle"/>.</summary>
    /// <param name="angle">
    ///     The angle in radians.</param>
    /// <returns>
    ///     The rotated point.</returns>
    public readonly PointD Rotated(double angle)
    {
        var sina = Math.Sin(angle);
        var cosa = Math.Cos(angle);
        return new PointD(X * cosa + Y * sina, Y * cosa - X * sina);
    }

    /// <summary>Calculates the distance between this point and the origin — or, equivalently, the length of this vector.</summary>
    public readonly double Distance() => Math.Sqrt(X * X + Y * Y);

    /// <summary>Calculates the distance between this point and <paramref name="other"/>.</summary>
    public readonly double Distance(PointD other) => Math.Sqrt((X - other.X) * (X - other.X) + (Y - other.Y) * (Y - other.Y));

    /// <summary>Deconstructs this point into a tuple.</summary>
    public readonly void Deconstruct(out double x, out double y) { x = X; y = Y; }
}
