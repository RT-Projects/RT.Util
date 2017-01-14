using System;

namespace RT.Util.Geometry
{
    /// <summary>A double-precision class encapsulating a straight line segment connecting two points.</summary>
    public struct EdgeD : IEquatable<EdgeD>
    {
        /// <summary>Start point of the line segment.</summary>
        public PointD Start;
        /// <summary>End point of the line segment.</summary>
        public PointD End;

        /// <summary>Initialises a line segment using the specified start and end point.</summary>
        public EdgeD(PointD start, PointD end)
        {
            Start = start;
            End = end;
        }

        /// <summary>Initialises a line segment using the start point (X1, Y1) and the end point (X2, Y2).</summary>
        public EdgeD(double x1, double y1, double x2, double y2)
        {
            Start = new PointD(x1, y1);
            End = new PointD(x2, y2);
        }

        /// <summary>
        ///     Initialises a line segment starting at the specified point. The ending point is 1 unit away at the specified
        ///     angle.</summary>
        public EdgeD(PointD start, double angle)
        {
            Start = start;
            End.X = Start.X + Math.Cos(angle);
            End.Y = Start.Y + Math.Sin(angle);
        }

        /// <summary>Returns the difference in the X-co-ordinates of the start and end point of this <see cref="EdgeD"/>.</summary>
        public double Width { get { return Math.Abs(Start.X - End.X); } }
        /// <summary>Returns the difference in the Y-co-ordinates of the start and end point of this <see cref="EdgeD"/>.</summary>
        public double Height { get { return Math.Abs(Start.Y - End.Y); } }

        /// <summary>
        ///     Determines whether two edges intersect.</summary>
        /// <param name="r">
        ///     <see cref="EdgeD"/> to compare against.</param>
        /// <param name="excludeVertexTouching">
        ///     If <c>true</c>, edges that touch at the vertex are not considered to be intersecting.</param>
        /// <returns>
        ///     True if both edges intersect with each other.</returns>
        public bool IntersectsWith(EdgeD r, bool excludeVertexTouching = false)
        {
            if (excludeVertexTouching && (r.Start == Start || r.End == Start || r.Start == End || r.End == End))
                return false;

            double mx = End.X - Start.X;
            double my = End.Y - Start.Y;
            double rmx = r.End.X - r.Start.X;
            double rmy = r.End.Y - r.Start.Y;
            double dx = r.Start.X - Start.X;
            double dy = Start.Y - r.Start.Y;

            double d = (mx * rmy - my * rmx);
            double n = (mx * dy + my * dx) / d;
            double q = (rmx * dy + rmy * dx) / d;

            return (n >= 0 && n <= 1 && q >= 0 && q <= 1);
        }

        /// <summary>
        ///     Compares two <see cref="EdgeD"/> objects for equality.</summary>
        /// <param name="other">
        ///     Object to compare this one against.</param>
        /// <returns>
        ///     True if considered equal.</returns>
        public bool Equals(EdgeD other)
        {
            return (Start == other.Start && End == other.End) || (Start == other.End && End == other.Start);
        }

        /// <summary>
        ///     Compares two <see cref="EdgeD"/> objects for equality.</summary>
        /// <param name="one">
        ///     First object to compare.</param>
        /// <param name="other">
        ///     Object to compare against.</param>
        /// <returns>
        ///     True if considered equal.</returns>
        public static bool operator ==(EdgeD one, EdgeD other)
        {
            return one.Equals(other);
        }

        /// <summary>
        ///     Compares two <see cref="EdgeD"/> objects for inequality.</summary>
        /// <param name="one">
        ///     First object to compare.</param>
        /// <param name="other">
        ///     Object to compare against.</param>
        /// <returns>
        ///     True if considered different.</returns>
        public static bool operator !=(EdgeD one, EdgeD other)
        {
            return !one.Equals(other);
        }

        /// <summary>Returns a hash code for the current <see cref="EdgeD"/>.</summary>
        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        /// <summary>
        ///     Provides a string representation of the current <see cref="EdgeD"/>.</summary>
        /// <returns>
        ///     A string representation of the current <see cref="EdgeD"/>.</returns>
        public override string ToString()
        {
            return Start + " ⇒ " + End;
        }

        /// <summary>
        ///     Compares two <see cref="EdgeD"/> objects for equality.</summary>
        /// <param name="obj">
        ///     Object to compare against.</param>
        /// <returns>
        ///     True if considered equal.</returns>
        public override bool Equals(object obj)
        {
            if (obj is EdgeD)
                return Equals((EdgeD) obj);
            return base.Equals(obj);
        }

        /// <summary>Returns a point on this edge that is as near as possible to the specified point.</summary>
        public PointD PointOnEdgeNearestTo(PointD point)
        {
            double lambda = LambdaOfPointDroppedPerpendicularly(point);

            if (lambda <= 0)
                return Start;
            else if (lambda >= 1)
                return End;
            else
                return Start + lambda * (End - Start);
        }

        /// <summary>
        ///     Calculates the projection of the specified point onto the line defined by this edge. Returns the Lambda of
        ///     this point P, defined by P = Start + Lambda * (End - Start). Hence the lambda is 0 if the projection falls
        ///     exactly onto the Start point, and 1 if it falls on the End point.</summary>
        public double LambdaOfPointDroppedPerpendicularly(PointD point)
        {
            // Drop the point onto the line defined by:  L = Start + lambda * (End - Start)
            // Perpendicular line goes through "point" and the direction is (End - Start).Normal()
            PointD dir = End - Start;
            // for reference below: PointD Ndir = dir.Normal();

            // Now solve for lambda:
            // L = N
            // L = Start + lambda * dir;
            // N = point + m * Ndir;
            // Start + lambda * dir = point + m * Ndir;
            //
            // start.x + l * dir.x = point.x + m * Ndir.x;
            // start.y + l * dir.y = point.y + m * Ndir.y;
            // Now substitute Ndir.X = dir.Y, and Ndir.Y = -dir.X
            // [...]

            return (dir.X * (point.X - Start.X) + dir.Y * (point.Y - Start.Y)) / (dir.X * dir.X + dir.Y * dir.Y);
        }

        /// <summary>
        ///     Calculates the distance between the specified point and the extended straight line identified by this edge.</summary>
        public double Distance(PointD point)
        {
            var lambda = LambdaOfPointDroppedPerpendicularly(point);
            var pointOnLine = Start + lambda * (End - Start);
            return Math.Sqrt(Math.Pow(point.X - pointOnLine.X, 2) + Math.Pow(point.Y - pointOnLine.Y, 2));
        }

        /// <summary>
        ///     Returns true if the specified point lies exactly on this edge. Accurate results are not guaranteed on edges
        ///     which are not axis-aligned.</summary>
        public bool ContainsPoint(PointD point)
        {
            if (Start == point || End == point)
                return true;
            if (Start.X == End.X)
                return (point.X == Start.X) && (point.Y >= Math.Min(Start.Y, End.Y)) && (point.Y <= Math.Max(Start.Y, End.Y));
            return (point.Y == Start.Y + point.X * (End.Y - Start.Y) / (End.X - Start.X)) && (point.X >= Math.Min(Start.X, End.X)) && (point.X <= Math.Max(Start.X, End.X));
        }

        /// <summary>
        ///     Returns the Z component of the cross product of this edge, treated as a vector from <see cref="Start"/> to
        ///     <see cref="End"/>, with a vector from <see cref="Start"/> to the specified point.</summary>
        public double CrossZ(PointD point)
        {
            return (End - Start).CrossZ(point - Start);
        }

        /// <summary>
        ///     Returns the Z component of the cross product of this edge, treated as a vector, with the specified edge, also
        ///     treated as a vector.</summary>
        public double CrossZ(EdgeD edge)
        {
            return (End - Start).CrossZ(edge.End - edge.Start);
        }
    }
}
