using RT.Internal;

namespace RT.Geometry;

/// <summary>Encapsulates a piece of SVG path data.</summary>
public class SvgPiece
{
    /// <summary>The type of piece (straight line, curve, etc.)</summary>
    public SvgPieceType Type { get; private set; }
    /// <summary>The set of points associated with this piece.</summary>
    public PointD[] Points { get; private set; }

    /// <summary>Designates the end of a path or subpath.</summary>
    public static readonly SvgPiece End = new() { Type = SvgPieceType.End };

    /// <summary>
    ///     Constructor.</summary>
    /// <param name="type">
    ///     Type of path piece.</param>
    /// <param name="points">
    ///     Set of points.</param>
    public SvgPiece(SvgPieceType type, params PointD[] points)
    {
        if (type == SvgPieceType.End)
            throw new ArgumentException("type cannot be End. Use the static PathPiece.End value instead.", nameof(type));
        Type = type;
        Points = points ?? throw new ArgumentNullException(nameof(points));
    }

    private SvgPiece() { }

    /// <summary>Recreates the path in SVG path data syntax.</summary>
    public sealed override string ToString() => ToString(useSpaces: false);

    /// <summary>
    ///     Recreates the path in SVG path data syntax.</summary>
    /// <param name="useSpaces">
    ///     If <c>true</c>, the x- and y-coordinates are separated by spaces; otherwise, commas (the default).</param>
    public virtual string ToString(bool useSpaces)
    {
        char ch;
        switch (Type)
        {
            case SvgPieceType.Move: ch = 'M'; break;
            case SvgPieceType.Line: ch = 'L'; break;
            case SvgPieceType.Curve: ch = 'C'; break;
            default: return "z";
        }
        return ch + " " + Points.Select(p => $"{p.X}{(useSpaces ? " " : ",")}{p.Y}").JoinString(" ");
    }

    /// <summary>
    ///     Recreates the path in SVG path data syntax.</summary>
    /// <param name="decimalPlaces">
    ///     Specifies the number of decimal places to use for the floating-point numbers.</param>
    /// <param name="useSpaces">
    ///     If <c>true</c>, the x- and y-coordinates are separated by spaces; otherwise, commas (the default).</param>
    public virtual string ToString(int decimalPlaces, bool useSpaces = false)
    {
        char ch;
        switch (Type)
        {
            case SvgPieceType.Move: ch = 'M'; break;
            case SvgPieceType.Line: ch = 'L'; break;
            case SvgPieceType.Curve: ch = 'C'; break;
            default: return "z";
        }
        return ch + " " + Points.Select(p => $"{p.X.ToString($"0.{new string('#', decimalPlaces)}")}{(useSpaces ? " " : ",")}{p.Y.ToString($"0.{new string('#', decimalPlaces)}")}").JoinString(" ");
    }

    /// <summary>
    ///     Returns a new <see cref="SvgPiece"/> of the same <see cref="SvgPieceType"/> in which all points have been mapped
    ///     through the <paramref name="selector"/>.</summary>
    /// <param name="selector">
    ///     A function to pass all points through.</param>
    /// <returns>
    ///     A new <see cref="SvgPiece"/> of the same <see cref="SvgPieceType"/>.</returns>
    public virtual SvgPiece Select(Func<PointD, PointD> selector)
    {
        if (selector == null)
            throw new ArgumentNullException(nameof(selector));
        return Type == SvgPieceType.End ? this : new SvgPiece(Type, Points.Select(selector).ToArray());
    }
}
