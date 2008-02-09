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
            if (Start.X == End.X)
            {
                if (!((r.Start.X > Start.X) ^ (r.End.X > End.X)) && (r.Start.X != Start.X || r.End.X != End.X))
                    return false;
                double YIntersect = r.Start.Y + (r.End.Y - r.Start.Y) / (r.End.X - r.Start.X) * (Start.X - r.Start.X);
                return (YIntersect > Start.Y && YIntersect < End.Y) || (YIntersect < Start.Y && YIntersect > End.Y);
            }

            double m = (r.End.Y - r.Start.Y) / (r.End.X - r.Start.X);
            double c = r.Start.Y - m * r.Start.X;
            double y1 = m * Start.X + c;
            double y2 = m * End.X + c;
            return ((Start.Y > y1) ^ (End.Y > y2)) || (Start.Y == y1 && End.Y == y2);
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
