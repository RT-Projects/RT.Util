using System;
using System.Collections.Generic;
using System.Text;

namespace RT.Util.Geometry
{
    public struct PointD: IEquatable<PointD>
    {
        public double X;
        public double Y;

        public PointD(double X, double Y)
        {
            this.X = X;
            this.Y = Y;
        }

        public override bool Equals(object obj)
        {
            if (obj is PointD)
                return ((PointD)obj).X == X && ((PointD)obj).Y == Y;
            return base.Equals(obj);
        }

        public bool Equals(PointD other)
        {
            return other.X == X && other.Y == Y;
        }

        public static bool operator ==(PointD one, PointD other)
        {
            return one.Equals(other);
        }

        public static bool operator !=(PointD one, PointD other)
        {
            return !one.Equals(other);
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        public System.Drawing.PointF ToPointF()
        {
            return new System.Drawing.PointF((float)X, (float)Y);
        }

        public override string ToString()
        {
            return "X=" + X + ", Y=" + Y;
        }
    }
}
