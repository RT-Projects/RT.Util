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

        /// <summary>
        /// Returns a new PointD at the given offset of this one. Does NOT modify this PointD.
        /// </summary>
        /// <param name="ByX">Amount to move X co-ordinate by.</param>
        /// <param name="ByY">Amount to move Y co-ordinate by.</param>
        /// <returns>New PointD at the given offset of this one.</returns>
        public PointD Move(double ByX, double ByY)
        {
            return new PointD(this.X + ByX, this.Y + ByY);
        }
    }
}
