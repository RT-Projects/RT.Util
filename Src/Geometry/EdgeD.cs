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
        public PointD Start;
        public PointD End;

        public EdgeD(PointD Start, PointD End)
        {
            this.Start = Start;
            this.End = End;
        }
        public EdgeD(double X1, double Y1, double X2, double Y2)
        {
            this.Start = new PointD(X1, Y1);
            this.End = new PointD(X2, Y2);
        }

        public double Width { get { return Math.Abs(Start.X - End.X); } }
        public double Height { get { return Math.Abs(Start.Y - End.Y); } }

        public bool IntersectsWith(EdgeD r)
        {
            if (Start.X == End.X && r.Start.X == r.End.X)
            {
                if (Start.X != r.Start.X)
                    return false;
                return (Math.Min(Start.Y, End.Y) < Math.Max(r.Start.Y, r.End.Y)) &&
                       (Math.Max(Start.Y, End.Y) > Math.Min(r.Start.Y, r.End.Y));
            }

            if (Start.X == End.X)
            {
                double xx = Start.X;
                if (!(((r.Start.X > xx) ^ (r.End.X > xx)) || r.Start.X == xx || r.End.X == xx))
                    return false;

                double YIntersect = r.Start.Y + (r.End.Y - r.Start.Y) / (r.End.X - r.Start.X) * (Start.X - r.Start.X);
                return YIntersect > Math.Min(Start.Y, End.Y) && YIntersect < Math.Max(Start.Y, End.Y);
            }

            if (r.Start.X == r.End.X)
                return r.IntersectsWith(this);

            // Find the point of intersection
            double m = (End.Y - Start.Y) / (End.X - Start.X);
            double c = Start.Y - m * Start.X;
            double rm = (r.End.Y - r.Start.Y) / (r.End.X - r.Start.X);
            double rc = r.Start.Y - rm * r.Start.X;
            double x = (rc - c) / (m - rm);
            double y = m * x + c;
            return
                (x >= Math.Min(Start.X, End.X) && x <= Math.Max(Start.X, End.X)) &&
                (x >= Math.Min(r.Start.X, r.End.X) && x <= Math.Max(r.Start.X, r.End.X)) &&
                (y >= Math.Min(Start.Y, End.Y) && y <= Math.Max(Start.Y, End.Y)) &&
                (y >= Math.Min(r.Start.Y, r.End.Y) && y <= Math.Max(r.Start.Y, r.End.Y));
        }

        public bool Equals(EdgeD other)
        {
            return (Start == other.Start && End == other.End) || (Start == other.End && End == other.Start);
        }

        public static bool operator ==(EdgeD one, EdgeD other)
        {
            return one.Equals(other);
        }
        public static bool operator !=(EdgeD one, EdgeD other)
        {
            return !one.Equals(other);
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        public override string ToString()
        {
            return Start + " ==> " + End;
        }

        public override bool Equals(object obj)
        {
            if (obj is EdgeD)
                return Equals((EdgeD)obj);
            return base.Equals(obj);
        }
    }
}
