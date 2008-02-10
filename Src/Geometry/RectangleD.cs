using System;
using System.Collections.Generic;
using System.Text;

namespace RT.Util.Geometry
{
    /// <summary>
    /// A double-precision rectangle class which supports intersect tests.
    /// </summary>
    public struct RectangleD:IEquatable<RectangleD>
    {
        public double X;
        public double Y;
        public double Width;
        public double Height;

        public RectangleD(double X, double Y, double Width, double Height)
        {
            this.X = X;
            this.Y = Y;
            this.Width = Width;
            this.Height = Height;
        }

        public double Left { get { return X; } }
        public double Top { get { return Y; } }
        public double Right { get { return X + Width; } }
        public double Bottom { get { return Y + Height; } }

        /// <summary>
        /// Checks if the perimeter of this rectangle intersects with that of another specified rectangle.
        /// </summary>
        /// <param name="r">Other rectangle to check against.</param>
        /// <returns>Returns true if the perimeter of this rectangle intersects with that of r.</returns>
        public bool IntersectsWith(RectangleD r)
        {
            return ContainsPoint(r.Left, r.Top) || ContainsPoint(r.Left, r.Bottom)
                || ContainsPoint(r.Right, r.Top) || ContainsPoint(r.Right, r.Bottom)
                || (r.Left >= Left && r.Right <= Right && r.Top <= Top && r.Bottom >= Bottom)
                || (r.Left <= Left && r.Right >= Right && r.Top >= Top && r.Bottom <= Bottom);
        }

        public bool ContainsPoint(double X, double Y)
        {
            return (X >= Left) && (X <= Right) && (Y >= Top) && (Y <= Bottom);
        }

        public bool Equals(RectangleD other)
        {
            return X == other.X && Y == other.Y && Width == other.Width && Height == other.Height;
        }

        public static bool operator ==(RectangleD one, RectangleD other)
        {
            return one.Equals(other);
        }
        public static bool operator !=(RectangleD one, RectangleD other)
        {
            return !one.Equals(other);
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        public override string ToString()
        {
            return "(" + Left + ", " + Top + "); W=" + Width + "; H=" + Height;
        }

        public override bool Equals(object obj)
        {
            if (obj is RectangleD)
                return Equals((RectangleD)obj);
            return base.Equals(obj);
        }
    }
}
