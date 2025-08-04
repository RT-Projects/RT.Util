namespace RT.Geometry;

/// <summary>Specifies a type of piece within an SVG path.</summary>
public enum SvgPieceType
{
    /// <summary>
    ///     Moves to a new point without drawing a line. This is usually used only at the start of a subpath (i.e., at the
    ///     start of a path or after an <see cref="End"/>).</summary>
    Move,
    /// <summary>Draws a set of straight lines connecting each point (including the last point of the previous piece).</summary>
    Line,
    /// <summary>
    ///     Draws a set of Bézier curves. The length of the <see cref="SvgPiece.Points"/> array is a multiple of three (two
    ///     control points and an end-point). The first start point is the last point of the previous piece.</summary>
    Curve,
    /// <summary>Draws an elliptical arc.</summary>
    Arc,
    /// <summary>Designates the end of a path or subpath.</summary>
    End
}
