using System;
using System.Collections.Generic;
using System.Text;

namespace RT.Util.Geometry
{
    /// <summary>
    /// A double-precision class encapsulating a straight line segment connecting two points.
    /// </summary>
    public struct EdgeD : IEquatable<EdgeD>
    {
        /// <summary>Start point of the line segment.</summary>
        public PointD Start;
        /// <summary>End point of the line segment.</summary>
        public PointD End;

        /// <summary>Initialises a line segment using the specified start and end point.</summary>
        public EdgeD(PointD Start, PointD End)
        {
            this.Start = Start;
            this.End = End;
        }

        /// <summary>Initialises a line segment using the start point (X1, Y1) and the end point (X2, Y2).</summary>
        public EdgeD(double X1, double Y1, double X2, double Y2)
        {
            this.Start = new PointD(X1, Y1);
            this.End = new PointD(X2, Y2);
        }

        /// <summary>Returns the difference in the X-co-ordinates of the start and end point of this <see cref="EdgeD"/>.</summary>
        public double Width { get { return Math.Abs(Start.X - End.X); } }
        /// <summary>Returns the difference in the Y-co-ordinates of the start and end point of this <see cref="EdgeD"/>.</summary>
        public double Height { get { return Math.Abs(Start.Y - End.Y); } }

        /// <summary>Determines whether two edges intersect.</summary>
        /// <param name="r"><see cref="EdgeD"/> to compare against.</param>
        /// <returns>True if both edges intersect with each other.</returns>
        public bool IntersectsWith(EdgeD r)
        {
            double mx = End.X - Start.X;
            double my = End.Y - Start.Y;
            double rmx = r.End.X - r.Start.X;
            double rmy = r.End.Y - r.Start.Y;
            double dx = r.Start.X - Start.X;
            double dy = Start.Y - r.Start.Y;

            double d = (mx * rmy - my * rmx);
            double n = (mx * dy + my * dx) / d;
            double q = (rmx * dy + rmy * dx) / d;

            return (n >= 0 && n < 1 && q >= 0 && q < 1);
        }

        /// <summary>Compares two <see cref="EdgeD"/> objects for equality.</summary>
        /// <param name="other">Object to compare this one against.</param>
        /// <returns>True if considered equal.</returns>
        public bool Equals(EdgeD other)
        {
            return (Start == other.Start && End == other.End) || (Start == other.End && End == other.Start);
        }

        /// <summary>Compares two <see cref="EdgeD"/> objects for equality.</summary>
        /// <param name="one">First object to compare.</param>
        /// <param name="other">Object to compare against.</param>
        /// <returns>True if considered equal.</returns>
        public static bool operator ==(EdgeD one, EdgeD other)
        {
            return one.Equals(other);
        }

        /// <summary>Compares two <see cref="EdgeD"/> objects for inequality.</summary>
        /// <param name="one">First object to compare.</param>
        /// <param name="other">Object to compare against.</param>
        /// <returns>True if considered different.</returns>
        public static bool operator !=(EdgeD one, EdgeD other)
        {
            return !one.Equals(other);
        }

        /// <summary>Returns a hash code for the current <see cref="EdgeD"/>.</summary>
        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        /// <summary>Provides a string representation of the current <see cref="EdgeD"/>.</summary>
        /// <returns>A string representation of the current <see cref="EdgeD"/>.</returns>
        public override string ToString()
        {
            return Start + " ==> " + End;
        }

        /// <summary>Compares two <see cref="EdgeD"/> objects for equality.</summary>
        /// <param name="obj">Object to compare against.</param>
        /// <returns>True if considered equal.</returns>
        public override bool Equals(object obj)
        {
            if (obj is EdgeD)
                return Equals((EdgeD)obj);
            return base.Equals(obj);
        }
    }
}
