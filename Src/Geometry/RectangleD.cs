using System;
using System.Drawing;

namespace RT.Util.Geometry
{
    /// <summary>A double-precision rectangle struct, representing an axis-aligned rectangle.</summary>
    public struct RectangleD : IEquatable<RectangleD>
    {
        /// <summary>Represents an instance of the <see cref="RectangleD"/> class with its members uninitialized.</summary>
        public static readonly RectangleD Empty = new RectangleD();

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
        /// <summary>Gets the X coordinate of the minimal-Y boundary.</summary>
        public double Top { get { return Y; } }
        /// <summary>Gets the X coordinate of the maximal-X boundary.</summary>
        public double Right { get { return X + Width; } }
        /// <summary>Gets the Y coordinate of the maximal-Y boundary.</summary>
        public double Bottom { get { return Y + Height; } }
        /// <summary>Returns true if this rectangle has zero extent.</summary>
        public bool IsEmpty { get { return Width == 0 && Height == 0; } }

        /// <summary>
        ///     Checks if the perimeter of this rectangle intersects with that of <paramref name="rect"/>.</summary>
        /// <param name="rect">
        ///     Other rectangle to check against.</param>
        /// <returns>
        ///     Returns true if the perimeter of this rectangle intersects with that of <paramref name="rect"/>.</returns>
        public bool PerimeterIntersectsWith(RectangleD rect)
        {
            return Contains(rect.Left, rect.Top) || Contains(rect.Left, rect.Bottom)
                || Contains(rect.Right, rect.Top) || Contains(rect.Right, rect.Bottom)
                || rect.Contains(Left, Top) || rect.Contains(Left, Bottom)
                || rect.Contains(Right, Top) || rect.Contains(Right, Bottom)
                || (rect.Left >= Left && rect.Right <= Right && rect.Top <= Top && rect.Bottom >= Bottom)
                || (rect.Left <= Left && rect.Right >= Right && rect.Top >= Top && rect.Bottom <= Bottom);
        }

        /// <summary>
        ///     Determines if this rectangle intersects with <paramref name="rect"/>.</summary>
        /// <param name="rect">
        ///     The rectangle to test.</param>
        /// <returns>
        ///     Returns true if there is any intersection, otherwise false.</returns>
        public bool IntersectsWith(RectangleD rect)
        {
            return (rect.X < X + Width) && (rect.X + rect.Width > X) && (rect.Y < Y + Height) && (rect.Y + rect.Height > Y);
        }

        /// <summary>Returns true if the specified point is contained within the rectangle (or lies exactly on a boundary).</summary>
        public bool Contains(double x, double y)
        {
            return (x >= Left) && (x <= Right) && (y >= Top) && (y <= Bottom);
        }

        /// <summary>Returns true if the two rectangles have identical coordinates and sizes.</summary>
        public bool Equals(RectangleD other)
        {
            return X == other.X && Y == other.Y && Width == other.Width && Height == other.Height;
        }

        /// <summary>Compares two rectangles for equality using <see cref="Equals(RectangleD)"/>.</summary>
        public static bool operator ==(RectangleD one, RectangleD other)
        {
            return one.Equals(other);
        }

        /// <summary>Compares two rectangles for inequality using <see cref="Equals(RectangleD)"/>.</summary>
        public static bool operator !=(RectangleD one, RectangleD other)
        {
            return !one.Equals(other);
        }

        /// <summary>Returns a hash code for the rectangle.</summary>
        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        /// <summary>Converts the rectangle to a string representation.</summary>
        public override string ToString()
        {
            return "(" + Left + ", " + Top + "); W=" + Width + "; H=" + Height;
        }

        /// <summary>Compares a rectangle to any other object.</summary>
        public override bool Equals(object obj)
        {
            if (obj is RectangleD)
                return Equals((RectangleD) obj);
            return base.Equals(obj);
        }

        /// <summary>
        ///     Creates the smallest possible third rectangle that can contain both of two rectangles that form a union.</summary>
        /// <param name="a">
        ///     A rectangle to union.</param>
        /// <param name="b">
        ///     A rectangle to union.</param>
        /// <returns>
        ///     A third <see cref="RectangleD"/> structure that contains both of the two rectangles that form the union.</returns>
        public static RectangleD Union(RectangleD a, RectangleD b)
        {
            double left = Math.Min(a.X, b.X);
            double right = Math.Max(a.X + a.Width, b.X + b.Width);
            double top = Math.Min(a.Y, b.Y);
            double bottom = Math.Max(a.Y + a.Height, b.Y + b.Height);
            return new RectangleD(left, top, right - left, bottom - top);
        }

        /// <summary>
        ///     Converts the current <see cref="RectangleD"/> to a <c>System.Drawing.Rectangle</c> by rounding the
        ///     double-precision values to the nearest integer values.</summary>
        /// <returns>
        ///     A <c>System.Drawing.Rectangle</c>.</returns>
        public Rectangle Round()
        {
            return new Rectangle((int) Math.Round(X), (int) Math.Round(Y), (int) Math.Round(Width), (int) Math.Round(Height));
        }

        /// <summary>
        ///     Returns the smallest <c>System.Drawing.Rectangle</c> that entirely contains the current <see
        ///     cref="RectangleD"/>.</summary>
        /// <returns>
        ///     A <c>System.Drawing.Rectangle</c>.</returns>
        public Rectangle RoundOutward()
        {
            int x = (int) Math.Floor(X);
            int y = (int) Math.Floor(Y);
            return new Rectangle(x, y, (int) Math.Ceiling(X + Width) - x, (int) Math.Ceiling(Y + Height) - y);
        }

        /// <summary>
        ///     Converts the specified <c>System.Drawing.Rectangle</c> structure to a <see cref="RectangleD"/> structure.</summary>
        /// <param name="self">
        ///     The <c>System.Drawing.Rectangle</c> structure to convert.</param>
        /// <returns>
        ///     The <see cref="RectangleD"/> structure that is converted from the specified <c>System.Drawing.Rectangle</c>
        ///     structure.</returns>
        public static implicit operator RectangleD(Rectangle self) { return new RectangleD(self.X, self.Y, self.Width, self.Height); }
    }
}
