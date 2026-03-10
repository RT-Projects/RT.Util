namespace RT.Geometry;

/// <summary>Encapsulates an elliptical arc in SVG path data.</summary>
public sealed class SvgArc(double rx, double ry, double xAxisRotation, bool largeArcFlag, bool sweepFlag, PointD endPoint) : SvgPiece(SvgPieceType.Arc, new[] { endPoint })
{
    /// <summary>X radius of the ellipse.</summary>
    public double RX { get; private set; } = rx;
    /// <summary>Y radius of the ellipse.</summary>
    public double RY { get; private set; } = ry;
    /// <summary>Rotation (in degrees, clockwise) of the ellipse.</summary>
    public double XAxisRotation { get; private set; } = xAxisRotation;
    /// <summary>Determines if the arc should be greater than or less than 180 degrees.</summary>
    public bool LargeArcFlag { get; private set; } = largeArcFlag;
    /// <summary>Determines if the arc should begin moving at positive angles or negative ones.</summary>
    public bool SweepFlag { get; private set; } = sweepFlag;
    /// <summary>
    ///     Returns the arc’s end-point.</summary>
    /// <remarks>
    ///     This is actually just <see cref="SvgPiece.Points"/>[0].</remarks>
    public PointD EndPoint => Points[0];

    /// <summary>
    ///     Recreates the path in SVG path data syntax.</summary>
    /// <param name="useSpaces">
    ///     If <c>true</c>, the x- and y-coordinates are separated by spaces; otherwise, commas (the default).</param>
    public override string ToString(bool useSpaces) =>
        $"A {RX}{(useSpaces ? " " : ",")}{RY} {XAxisRotation} {(LargeArcFlag ? "1" : "0")} {(SweepFlag ? "1" : "0")} {Points[0].X}{(useSpaces ? " " : ",")}{Points[0].Y}";

    /// <summary>
    ///     Recreates the path in SVG path data syntax.</summary>
    /// <param name="decimalPlaces">
    ///     Specifies the number of decimal places to use for the floating-point numbers.</param>
    /// <param name="useSpaces">
    ///     If <c>true</c>, the x- and y-coordinates are separated by spaces; otherwise, commas (the default).</param>
    public override string ToString(int decimalPlaces, bool useSpaces = false) =>
        $"A {RX.ToString($"0.{new string('#', decimalPlaces)}")}{(useSpaces ? " " : ",")}{RY.ToString($"0.{new string('#', decimalPlaces)}")} {XAxisRotation.ToString($"0.{new string('#', decimalPlaces)}")} {(LargeArcFlag ? "1" : "0")} {(SweepFlag ? "1" : "0")} {Points[0].X.ToString($"0.{new string('#', decimalPlaces)}")}{(useSpaces ? " " : ",")}{Points[0].Y.ToString($"0.{new string('#', decimalPlaces)}")}";

    /// <inheritdoc/>
    public override SvgPiece Select(Func<PointD, PointD> selector) => selector == null
        ? throw new ArgumentNullException(nameof(selector))
        : throw new InvalidOperationException($"Select cannot be used on an SvgArc because the radii and flags could be affected by a coordinate transform, not just the start and end points. To apply a coordinate transform, use Smooth() to render the arc as points first and apply the transform to the result.");
}
