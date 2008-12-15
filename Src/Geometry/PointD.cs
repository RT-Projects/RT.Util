using System;
using System.Collections.Generic;
using System.Text;

namespace RT.Util.Geometry
{
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

        /// <summary>Compares two <see cref="PointD"/> objects for equality.</summary>
        /// <param name="obj">Object to compare against.</param>
        /// <returns>True if considered equal.</returns>
        public override bool Equals(object obj)
        {
            if (obj is PointD)
                return ((PointD) obj).X == X && ((PointD) obj).Y == Y;
            return base.Equals(obj);
        }

        /// <summary>Compares two <see cref="PointD"/> objects for equality.</summary>
        /// <param name="other">Object to compare against.</param>
        /// <returns>True if considered equal.</returns>
        public bool Equals(PointD other)
        {
            return other.X == X && other.Y == Y;
        }

        /// <summary>Compares two <see cref="PointD"/> objects for equality.</summary>
        /// <param name="one">First object to compare.</param>
        /// <param name="other">Object to compare against.</param>
        /// <returns>True if considered equal.</returns>
        public static bool operator ==(PointD one, PointD other)
        {
            return one.Equals(other);
        }

        /// <summary>Compares two <see cref="PointD"/> objects for inequality.</summary>
        /// <param name="one">First object to compare.</param>
        /// <param name="other">Object to compare against.</param>
        /// <returns>True if considered different.</returns>
        public static bool operator !=(PointD one, PointD other)
        {
            return !one.Equals(other);
        }

        /// <summary>
        /// Performs unary vector negation (i.e. the resulting point is of the same length but
        /// pointing in the opposite direction).
        /// </summary>
        public static PointD operator -(PointD vector)
        {
            return new PointD(-vector.X, -vector.Y);
        }

        /// <summary>Performs vector addition, returning the result.</summary>
        public static PointD operator +(PointD one, PointD other)
        {
            return new PointD(one.X + other.X, one.Y + other.Y);
        }

        /// <summary>Performs vector subtraction, returning the result.</summary>
        public static PointD operator -(PointD left, PointD right)
        {
            return new PointD(left.X - right.X, left.Y - right.Y);
        }

        /// <summary>Scales a vector by a scalar.</summary>
        public static PointD operator *(double scalar, PointD vector)
        {
            return new PointD(scalar * vector.X, scalar * vector.Y);
        }

        /// <summary>Scales a vector by a scalar.</summary>
        public static PointD operator *(PointD vector, double scalar)
        {
            return new PointD(scalar * vector.X, scalar * vector.Y);
        }

        /// <summary>Scales a vector by 1 / scalar (i.e. performs scalar division).</summary>
        public static PointD operator /(PointD vector, double scalar)
        {
            return new PointD(vector.X / scalar, vector.Y / scalar);
        }

        /// <summary>Returns a hash code for the current <see cref="PointD"/>.</summary>
        public override int GetHashCode()
        {
            return X.GetHashCode() ^ Y.GetHashCode();
        }

        /// <summary>Converts the current <see cref="PointD"/> object to a <see cref="System.Drawing.PointF"/>.
        /// Note that doing so loses precision.</summary>
        /// <returns>Lower-precision <see cref="System.Drawing.PointF"/>.</returns>
        public System.Drawing.PointF ToPointF()
        {
            return new System.Drawing.PointF((float) X, (float) Y);
        }

        /// <summary>Provides a string representation of the current <see cref="PointD"/>.</summary>
        /// <returns>A string representation of the current <see cref="PointD"/>.</returns>
        public override string ToString()
        {
            return "X=" + X + ", Y=" + Y;
        }

        /// <summary>Returns the length of the vector represented by this <see cref="PointD"/>.</summary>
        public double Abs()
        {
            return Math.Sqrt(X * X + Y * Y);
        }

        /// <summary>Returns the theta (angle) of the vector represented by this <see cref="PointD"/>.</summary>
        public double Theta()
        {
            return Math.Atan2(Y, X);
        }

        /// <summary>Returns the unit vector in the same direction as this one.</summary>
        public PointD Unit()
        {
            double len = Math.Sqrt(X * X + Y * Y);
            return new PointD(X / len, Y / len);
        }

        /// <summary>Returns the dot product of this vector with the specified one.</summary>
        public double Dot(PointD other)
        {
            return X * other.X + Y * other.Y;
        }

        /// <summary>Returns a vector normal to this one.</summary>
        public PointD Normal()
        {
            return new PointD(Y, -X);
        }

        /// <summary>
        /// Decomposes this vector into components relative to another vector.
        /// </summary>
        /// <param name="vector">Reference vector.</param>
        /// <param name="lenAlong">Length of this vector along the reference vector.</param>
        /// <param name="lenNormal">Length of this vector normal to the reference vector.</param>
        public void DecomposeAlong(PointD vector, out double lenAlong, out double lenNormal)
        {
            lenAlong = LengthProjectedOnto(vector);
            lenNormal = LengthProjectedOnto(vector.Normal());
        }

        /// <summary>
        /// Performs the inverse of <see cref="DecomposeAlong"/>, modifying the current vector in place.
        /// </summary>
        /// <param name="vector">Reference vector.</param>
        /// <param name="lenAlong">Length of this vector along the reference vector.</param>
        /// <param name="lenNormal">Length of this vector normal to the reference vector.</param>
        public void RecomposeAlong(PointD vector, double lenAlong, double lenNormal)
        {
            PointD unitVector = vector.Unit();
            PointD unitVectorNormal = unitVector.Normal();
            X = lenAlong * unitVector.X + lenNormal * unitVectorNormal.X;
            Y = lenAlong * unitVector.Y + lenNormal * unitVectorNormal.Y;
        }

        /// <summary>
        /// Returns the length of this vector's projection onto the specified vector.
        /// </summary>
        public double LengthProjectedOnto(PointD vector)
        {
            return Dot(vector) / vector.Abs();
        }

        /// <summary>
        /// Returns the length of this vector's projection onto a unit vector at the specified angle.
        /// </summary>
        public double LengthProjectedOnto(double angle)
        {
            // Simplifying the following, where vector is a unit vector with theta = angle
            // return Dot(vector) / vector.Abs();
            // return (X*vector.X + Y*vector.Y) / 1.0;
            return X * Math.Cos(angle) + Y * Math.Sin(angle);
        }

        /// <summary>
        /// Returns a vector representing the projection (i.e. length and direction) of
        /// this vector onto the specified vector.
        /// </summary>
        public PointD ProjectedOnto(PointD vector)
        {
            PointD unitVector = vector.Unit();
            return Dot(unitVector) * unitVector;
        }
    }
}
