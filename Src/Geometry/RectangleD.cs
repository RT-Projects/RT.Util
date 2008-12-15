using System;

namespace RT.Util.Geometry
{
    /// <summary>
    /// A double-precision rectangle struct, representing an axis-aligned rectangle.
    /// </summary>
    public struct RectangleD:IEquatable<RectangleD>
    {
        /// <summary>X coordinate of the minimal-X boundary</summary>
        public double X;
        /// <summary>Y coordinate of the minimal-Y boundary</summary>
        public double Y;
        /// <summary>The width of the rectangle.</summary>
        public double Width;
        /// <summary>The height of the rectangle.</summary>
        public double Height;

        /// <summary>Constructs a new rectangle.</summary>
        public RectangleD(double x, double y, double width, double height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        /// <summary>Gets the X coordinate of the minimal-X boundary.</summary>
        public double Left { get { return X; } }
        /// <summary>Gets the X coordinate of the maximal-X boundary.</summary>
        public double Top { get { return Y; } }
        /// <summary>Gets the X coordinate of the maximal-X boundary.</summary>
        public double Right { get { return X + Width; } }
        /// <summary>Gets the Y coordinate of the maximal-Y boundary.</summary>
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

        /// <summary>
        /// Returns true if the specified point is contained within the rectangle
        /// (or lies exactly on a boundary).
        /// </summary>
        public bool ContainsPoint(double x, double y)
        {
            return (x >= Left) && (x <= Right) && (y >= Top) && (y <= Bottom);
        }

        /// <summary>
        /// Returns true if the two rectangles have identical coordinates and sizes.
        /// </summary>
        public bool Equals(RectangleD other)
        {
            return X == other.X && Y == other.Y && Width == other.Width && Height == other.Height;
        }

        /// <summary>
        /// Compares two rectangles for equality using <see cref="Equals(RectangleD)"/>.
        /// </summary>
        public static bool operator ==(RectangleD one, RectangleD other)
        {
            return one.Equals(other);
        }

        /// <summary>
        /// Compares two rectangles for inequality using <see cref="Equals(RectangleD)"/>.
        /// </summary>
        public static bool operator!=(RectangleD one, RectangleD other)
        {
            return !one.Equals(other);
        }

        /// <summary>
        /// Returns a hash code for the rectangle.
        /// </summary>
        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        /// <summary>
        /// Converts the rectangle to a string representation.
        /// </summary>
        public override string ToString()
        {
            return "(" + Left + ", " + Top + "); W=" + Width + "; H=" + Height;
        }

        /// <summary>
        /// Compares a rectangle to any other object.
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj is RectangleD)
                return Equals((RectangleD)obj);
            return base.Equals(obj);
        }
    }
}
