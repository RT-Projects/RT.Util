using System.Drawing;

namespace RT.Geometry;

/// <summary>
///     A double-precision rectangle struct, representing an axis-aligned rectangle.</summary>
/// <remarks>
///     Constructs a new rectangle.</remarks>
public struct RectangleD(double x, double y, double width, double height) : IEquatable<RectangleD>
{
    /// <summary>Represents an instance of the <see cref="RectangleD"/> class with its members uninitialized.</summary>
    public static readonly RectangleD Empty = default;

    /// <summary>X coordinate of the minimal-X boundary</summary>
    public double X = x;
    /// <summary>Y coordinate of the minimal-Y boundary</summary>
    public double Y = y;
    /// <summary>The width of the rectangle.</summary>
    public double Width = width;
    /// <summary>The height of the rectangle.</summary>
    public double Height = height;

    /// <summary>Gets the X coordinate of the minimal-X boundary.</summary>
    public readonly double Left => X;
    /// <summary>Gets the X coordinate of the minimal-Y boundary.</summary>
    public readonly double Top => Y;
    /// <summary>Gets the X coordinate of the maximal-X boundary.</summary>
    public readonly double Right => X + Width;
    /// <summary>Gets the Y coordinate of the maximal-Y boundary.</summary>
    public readonly double Bottom => Y + Height;
    /// <summary>Returns true if this rectangle has zero extent.</summary>
    public readonly bool IsEmpty => Width == 0 && Height == 0;

    /// <summary>Returns the top-left point of this rectangle.</summary>
    public readonly PointD TopLeft => new(Left, Top);
    /// <summary>Returns the top-right point of this rectangle.</summary>
    public readonly PointD TopRight => new(Right, Top);
    /// <summary>Returns the bottom-left point of this rectangle.</summary>
    public readonly PointD BottomLeft => new(Left, Bottom);
    /// <summary>Returns the bottom-right point of this rectangle.</summary>
    public readonly PointD BottomRight => new(Right, Bottom);

    /// <summary>
    ///     Checks if the perimeter of this rectangle intersects with that of <paramref name="rect"/>.</summary>
    /// <param name="rect">
    ///     Other rectangle to check against.</param>
    /// <returns>
    ///     Returns true if the perimeter of this rectangle intersects with that of <paramref name="rect"/>.</returns>
    public bool PerimeterIntersectsWith(RectangleD rect) => Contains(rect.Left, rect.Top) || Contains(rect.Left, rect.Bottom)
            || Contains(rect.Right, rect.Top) || Contains(rect.Right, rect.Bottom)
            || rect.Contains(Left, Top) || rect.Contains(Left, Bottom)
            || rect.Contains(Right, Top) || rect.Contains(Right, Bottom)
            || (rect.Left >= Left && rect.Right <= Right && rect.Top <= Top && rect.Bottom >= Bottom)
            || (rect.Left <= Left && rect.Right >= Right && rect.Top >= Top && rect.Bottom <= Bottom);

    /// <summary>
    ///     Determines if this rectangle intersects with <paramref name="rect"/>.</summary>
    /// <param name="rect">
    ///     The rectangle to test.</param>
    /// <returns>
    ///     Returns true if there is any intersection, otherwise false.</returns>
    public readonly bool IntersectsWith(RectangleD rect) => (rect.X < X + Width) && (rect.X + rect.Width > X) && (rect.Y < Y + Height) && (rect.Y + rect.Height > Y);

    /// <summary>Returns true if the specified point is contained within the rectangle (or lies exactly on a boundary).</summary>
    public readonly bool Contains(double x, double y) => (x >= Left) && (x <= Right) && (y >= Top) && (y <= Bottom);

    /// <summary>Returns true if the two rectangles have identical coordinates and sizes.</summary>
    public readonly bool Equals(RectangleD other) => X == other.X && Y == other.Y && Width == other.Width && Height == other.Height;

    /// <summary>Compares two rectangles for equality using <see cref="Equals(RectangleD)"/>.</summary>
    public static bool operator ==(RectangleD one, RectangleD other) => one.Equals(other);

    /// <summary>Compares two rectangles for inequality using <see cref="Equals(RectangleD)"/>.</summary>
    public static bool operator !=(RectangleD one, RectangleD other) => !one.Equals(other);

    /// <summary>Returns a hash code for the rectangle.</summary>
    public override int GetHashCode() => ToString().GetHashCode();

    /// <summary>Scales the rectangle’s position and size by the specified factor.</summary>
    public static RectangleD operator *(RectangleD rect, double scale) => new RectangleD(rect.X * scale, rect.Y * scale, rect.Width * scale, rect.Height * scale);
    /// <summary>Scales the rectangle’s position and size by the specified factor.</summary>
    public static RectangleD operator *(double scale, RectangleD rect) => new RectangleD(rect.X * scale, rect.Y * scale, rect.Width * scale, rect.Height * scale);
    /// <summary>Moves the rectangle’s position by the specified offset.</summary>
    public static RectangleD operator +(RectangleD rect, PointD offset) => new RectangleD(rect.X + offset.X, rect.Y + offset.Y, rect.Width, rect.Height);
    /// <summary>Moves the rectangle’s position by the specified offset.</summary>
    public static RectangleD operator +(PointD offset, RectangleD rect) => new RectangleD(rect.X + offset.X, rect.Y + offset.Y, rect.Width, rect.Height);
    /// <summary>Moves the rectangle’s position by the negation of the specified offset.</summary>
    public static RectangleD operator -(RectangleD rect, PointD offset) => new RectangleD(rect.X - offset.X, rect.Y - offset.Y, rect.Width, rect.Height);

    /// <summary>Converts the rectangle to a string representation.</summary>
    public override readonly string ToString() => "(" + Left + ", " + Top + "); W=" + Width + "; H=" + Height;

    /// <summary>Compares a rectangle to any other object.</summary>
    public override readonly bool Equals(object obj) => obj is RectangleD rect && Equals(rect);

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
    public readonly Rectangle Round() => new Rectangle((int) Math.Round(X), (int) Math.Round(Y), (int) Math.Round(Width), (int) Math.Round(Height));

    /// <summary>
    ///     Returns the smallest <c>System.Drawing.Rectangle</c> that entirely contains the current <see cref="RectangleD"/>.</summary>
    /// <returns>
    ///     A <c>System.Drawing.Rectangle</c>.</returns>
    public readonly Rectangle RoundOutward()
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
    public static implicit operator RectangleD(Rectangle self) => new RectangleD(self.X, self.Y, self.Width, self.Height);

    /// <summary>
    ///     Returns a new <see cref="RectangleD"/> in which the <see cref="Width"/> and/or <see cref="Height"/> are never
    ///     negative, by flipping the rectangle as necessary.</summary>
    /// <returns>
    ///     A normalized <see cref="RectangleD"/>.</returns>
    public readonly RectangleD Normalize() => new(
            Width < 0 ? X + Width : X,
            Height < 0 ? Y + Height : Y,
            Width < 0 ? -Width : Width,
            Height < 0 ? -Height : Height);

    /// <summary>Converts this rectangle to a <see cref="RectangleF"/>.</summary>
    public readonly RectangleF ToRectangleF() => new((float) X, (float) Y, (float) Width, (float) Height);
}
