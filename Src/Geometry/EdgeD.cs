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
