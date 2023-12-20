using System.Diagnostics;
using System.Text.RegularExpressions;
using RT.Util;
using RT.Util.ExtensionMethods;
using RT.Util.Geometry;

namespace RT.KitchenSink;

/// <summary>Provides methods to parse the syntax used in SVG path data.</summary>
public static class DecodeSvgPath
{
    /// <summary>Encapsulates a piece of SVG path data.</summary>
    public class PathPiece
    {
        /// <summary>The type of piece (straight line, curve, etc.)</summary>
        public PathPieceType Type { get; private set; }
        /// <summary>The set of points associated with this piece.</summary>
        public PointD[] Points { get; private set; }

        /// <summary>Designates the end of a path or subpath.</summary>
        public static readonly PathPiece End = new() { Type = PathPieceType.End };

        /// <summary>
        ///     Constructor.</summary>
        /// <param name="type">
        ///     Type of path piece.</param>
        /// <param name="points">
        ///     Set of points.</param>
        public PathPiece(PathPieceType type, PointD[] points)
        {
            if (type == PathPieceType.End)
                throw new ArgumentException("type cannot be End. Use the static PathPiece.End value instead.", nameof(type));
            Type = type;
            Points = points ?? throw new ArgumentNullException(nameof(points));
        }

        private PathPiece() { }

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
                case PathPieceType.Move: ch = 'M'; break;
                case PathPieceType.Line: ch = 'L'; break;
                case PathPieceType.Curve: ch = 'C'; break;
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
                case PathPieceType.Move: ch = 'M'; break;
                case PathPieceType.Line: ch = 'L'; break;
                case PathPieceType.Curve: ch = 'C'; break;
                default: return "z";
            }
            return ch + " " + Points.Select(p => $"{p.X.ToString($"0.{new string('#', decimalPlaces)}")}{(useSpaces ? " " : ",")}{p.Y.ToString($"0.{new string('#', decimalPlaces)}")}").JoinString(" ");
        }

        /// <summary>
        ///     Returns a new <see cref="PathPiece"/> of the same <see cref="PathPieceType"/> in which all points have been
        ///     mapped through the <paramref name="selector"/>.</summary>
        /// <param name="selector">
        ///     A function to pass all points through.</param>
        /// <returns>
        ///     A new <see cref="PathPiece"/> of the same <see cref="PathPieceType"/>.</returns>
        public virtual PathPiece Select(Func<PointD, PointD> selector)
        {
            if (selector == null)
                throw new ArgumentNullException(nameof(selector));
            return Type == PathPieceType.End ? this : new PathPiece(Type, Points.Select(selector).ToArray());
        }
    }

    /// <summary>Encapsulates an elliptical arc in SVG path data.</summary>
    public sealed class PathPieceArc : PathPiece
    {
        /// <summary>X radius of the ellipse.</summary>
        public double RX { get; private set; }
        /// <summary>Y radius of the ellipse.</summary>
        public double RY { get; private set; }
        /// <summary>Rotation (in degrees, clockwise) of the ellipse.</summary>
        public double XAxisRotation { get; private set; }
        /// <summary>Determines if the arc should be greater than or less than 180 degrees.</summary>
        public bool LargeArcFlag { get; private set; }
        /// <summary>Determines if the arc should begin moving at positive angles or negative ones.</summary>
        public bool SweepFlag { get; private set; }
        /// <summary>
        ///     Returns the arc’s end-point.</summary>
        /// <remarks>
        ///     This is actually just <see cref="PathPiece.Points"/>[0].</remarks>
        public PointD EndPoint { get { return Points[0]; } }

        /// <summary>Constructor</summary>
        public PathPieceArc(double rx, double ry, double xAxisRotation, bool largeArcFlag, bool sweepFlag, PointD endPoint)
            : base(PathPieceType.Arc, new[] { endPoint })
        {
            RX = rx;
            RY = ry;
            XAxisRotation = xAxisRotation;
            LargeArcFlag = largeArcFlag;
            SweepFlag = sweepFlag;
        }

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

        /// <summary>
        ///     Returns a new <see cref="PathPiece"/> of the same <see cref="PathPieceType"/> in which all points have been
        ///     mapped through the <paramref name="selector"/>.</summary>
        /// <param name="selector">
        ///     A function to pass all points through.</param>
        /// <returns>
        ///     A new <see cref="PathPiece"/> of the same <see cref="PathPieceType"/>.</returns>
        public override PathPiece Select(Func<PointD, PointD> selector)
        {
            if (selector == null)
                throw new ArgumentNullException(nameof(selector));
            return new PathPieceArc(RX, RY, XAxisRotation, LargeArcFlag, SweepFlag, selector(EndPoint));
        }
    }

    /// <summary>Specifies a type of piece within an SVG path.</summary>
    public enum PathPieceType
    {
        /// <summary>
        ///     Moves to a new point without drawing a line. This is usually used only at the start of a subpath (i.e., at the
        ///     start of a path or after an <see cref="End"/>).</summary>
        Move,
        /// <summary>Draws a set of straight lines connecting each point (including the last point of the previous piece).</summary>
        Line,
        /// <summary>
        ///     Draws a set of Bézier curves. The length of the <see cref="PathPiece.Points"/> array is a multiple of three
        ///     (two control points and an end-point). The first start point is the last point of the previous piece.</summary>
        Curve,
        /// <summary>Draws an elliptical arc.</summary>
        Arc,
        /// <summary>Designates the end of a path or subpath.</summary>
        End
    }

    /// <summary>
    ///     Converts a sequence of <see cref="PathPiece"/> objects to a sequence of points using the specified <paramref
    ///     name="smoothness"/> to render Bézier curves.</summary>
    /// <param name="pieces">
    ///     The pieces that constitute the path.</param>
    /// <param name="smoothness">
    ///     A value indicating the maximum amount by which each Bézier curve is allowed to be approximated. The smaller this
    ///     value, the more points are generated for each Bézier curve.</param>
    /// <returns>
    ///     A sequence of points that represent the fully rendered path.</returns>
    public static IEnumerable<IEnumerable<PointD>> Do(IEnumerable<PathPiece> pieces, double smoothness)
    {
        if (pieces == null)
            throw new ArgumentNullException(nameof(pieces));
        if (smoothness <= 0)
            throw new ArgumentOutOfRangeException(nameof(smoothness), "The smoothness parameter cannot be zero or negative.");

        IEnumerable<IEnumerable<PointD>> iterator()
        {
            foreach (var group in pieces.Split(pp => pp.Type == PathPieceType.End).Where(gr => gr.Any()).Select(gr => gr.ToArray()))
            {
                yield return Enumerable.Range(0, group.Length).SelectMany(grIx =>
                {
                    var cur = group[grIx];
                    var prev = group[(grIx + group.Length - 1) % group.Length];

                    if (cur.Type == PathPieceType.Move || cur.Type == PathPieceType.Line)
                        return cur.Points;

                    var lastPoint = prev.Points[prev.Points.Length - 1];

                    if (cur.Type == PathPieceType.Curve && cur.Points.Length % 3 == 0)
                        return Enumerable.Range(0, cur.Points.Length / 3)
                            .SelectMany(ix => GeomUt.SmoothBézier(ix == 0 ? lastPoint : cur.Points[3 * ix - 1], cur.Points[3 * ix], cur.Points[3 * ix + 1], cur.Points[3 * ix + 2], smoothness).Skip(1))
                            .ToArray();

                    if (cur.Type == PathPieceType.Arc && cur is PathPieceArc arc && arc.Points.Length == 1)
                    {
                        var p1 = lastPoint.Rotated(arc.XAxisRotation * Math.PI / 180);
                        var p2 = arc.Points[0].Rotated(arc.XAxisRotation * Math.PI / 180);
                        var a = arc.RX;
                        var b = arc.RY;

                        var r1 = (p1.X - p2.X) / (2 * a);
                        var r2 = (p1.Y - p2.Y) / (2 * b);
                        var lambda = Math.Pow(r1, 2) + Math.Pow(r2, 2);
                        if (lambda > 1)
                        {
                            a *= Math.Sqrt(lambda);
                            b *= Math.Sqrt(lambda);
                            r1 = (p1.X - p2.X) / (2 * a);
                            r2 = (p1.Y - p2.Y) / (2 * b);
                            lambda = 1;
                        }
                        var a1 = Math.Atan2(-r1, r2);
                        var a2 = Math.Asin(Math.Sqrt(lambda));
                        var t1 = a1 + a2;
                        var t2 = a1 - a2;

                        var result = GeomUt.SmoothArc(new PointD(p1.X - a * Math.Cos(t1), p1.Y - b * Math.Sin(t1)), a, b, t1, arc.LargeArcFlag ? t2 + 2 * Math.PI : t2, smoothness);
                        if (arc.SweepFlag ^ arc.LargeArcFlag)
                            result = result.Select(p => p1 + p2 - p).Reverse();
                        return result.Skip(1).Select(p => p.Rotated(-arc.XAxisRotation * Math.PI / 180));
                    }

                    throw new NotImplementedException();
                });
            }
        }
        return iterator();
    }

    /// <summary>
    ///     Converts a string containing SVG path data to a sequence of points using the specified <paramref
    ///     name="smoothness"/> to render Bézier curves.</summary>
    /// <param name="svgPath">
    ///     The SVG path data.</param>
    /// <param name="smoothness">
    ///     A value indicating the maximum amount by which each curve (Bézier or arc) is allowed to be approximated. The
    ///     smaller this value, the more points are generated for each curve.</param>
    /// <returns>
    ///     A sequence of points that represent the fully rendered path.</returns>
    public static IEnumerable<IEnumerable<PointD>> Do(string svgPath, double smoothness)
    {
        return Do(DecodePieces(svgPath), smoothness);
    }

    /// <summary>
    ///     Converts a string containing SVG path data to a sequence of <see cref="PathPiece"/> objects.</summary>
    /// <param name="svgPath">
    ///     SVG path data to parse.</param>
    public static IEnumerable<PathPiece> DecodePieces(string svgPath)
    {
        // Parse all the commands and coordinates
        var numRegex = @"-?\d*(?:\.\d*)?\d(?:e-?\d+)?\s*,?\s*";
        var prevPoint = new PointD(0, 0);
        var prevStartPoint = (PointD?) null;
        var prevControlPoint = new PointD(0, 0);
        svgPath = svgPath.TrimStart();
        while (!string.IsNullOrWhiteSpace(svgPath))
        {
            Match m;
            if ((m = Regex.Match(svgPath, @"^[MLCQHVS]\s*({0})*".Fmt(numRegex), RegexOptions.IgnoreCase)).Success)
            {
                PathPieceType type;
                PointD[] points;
                bool prevControlPointDetermined = false;
                var numbers = m.Groups[1].Captures.Cast<Capture>().Select(c => double.Parse(c.Value.Trim().TrimEnd(',').Trim())).ToArray();

                switch (m.Value[0])
                {
                    case 'M':
                        type = PathPieceType.Move;
                        points = numbers.Split(2).Select(gr => new PointD(gr.First(), gr.Last())).ToArray();
                        prevPoint = points[points.Length - 1];
                        break;

                    case 'm':
                        type = PathPieceType.Move;
                        points = numbers.Split(2).Select(gr => new PointD(gr.First(), gr.Last())).ToArray();
                        for (int i = 0; i < points.Length; i++)
                            prevPoint = (points[i] += prevPoint);
                        break;

                    case 'L':
                        type = PathPieceType.Line;
                        points = numbers.Split(2).Select(gr => new PointD(gr.First(), gr.Last())).ToArray();
                        prevPoint = points[points.Length - 1];
                        break;

                    case 'l':
                        type = PathPieceType.Line;
                        points = numbers.Split(2).Select(gr => new PointD(gr.First(), gr.Last())).ToArray();
                        for (int i = 0; i < points.Length; i++)
                            prevPoint = (points[i] += prevPoint);
                        break;

                    case 'H':
                        type = PathPieceType.Line;
                        points = numbers.Select(x => new PointD(x, prevPoint.Y)).ToArray();
                        prevPoint = points.Last();
                        break;

                    case 'h':
                        type = PathPieceType.Line;
                        points = new PointD[numbers.Length];
                        for (int i = 0; i < numbers.Length; i++)
                            prevPoint = points[i] = new PointD(prevPoint.X + numbers[i], prevPoint.Y);
                        break;

                    case 'V':
                        type = PathPieceType.Line;
                        points = numbers.Select(y => new PointD(prevPoint.X, y)).ToArray();
                        prevPoint = points.Last();
                        break;

                    case 'v':
                        type = PathPieceType.Line;
                        points = new PointD[numbers.Length];
                        for (int i = 0; i < numbers.Length; i++)
                            prevPoint = points[i] = new PointD(prevPoint.X, prevPoint.Y + numbers[i]);
                        break;

                    case 'C':
                        type = PathPieceType.Curve;
                        points = numbers.Split(2).Select(x => new PointD(x.First(), x.Last())).ToArray();
                        Ut.Assert(points.Length % 3 == 0);
                        prevPoint = points.Last();
                        prevControlPoint = points.SkipLast(1).Last();
                        prevControlPointDetermined = true;
                        break;

                    case 'c':
                        type = PathPieceType.Curve;
                        points = numbers.Split(2).Select(x => new PointD(x.First(), x.Last())).ToArray();
                        Ut.Assert(points.Length % 3 == 0);
                        for (int i = 0; i < points.Length; i += 3)
                        {
                            points[i] += prevPoint;
                            points[i + 1] += prevPoint;
                            prevPoint = (points[i + 2] += prevPoint);
                        }
                        prevPoint = points.Last();
                        prevControlPoint = points.SkipLast(1).Last();
                        prevControlPointDetermined = true;
                        break;

                    case 'Q':
                    case 'q':
                        var relative = m.Value[0] == 'q';
                        type = PathPieceType.Curve;
                        var qPoints = numbers.Split(2).Select(x => new PointD(x.First(), x.Last())).ToArray();
                        Ut.Assert(qPoints.Length % 2 == 0);
                        points = new PointD[qPoints.Length / 2 * 3];
                        for (int i = 0, j = 0; i < qPoints.Length; i += 2, j += 3)
                        {
                            var ctrlPoint = qPoints[i];
                            var endPoint = qPoints[i + 1];
                            if (relative)
                            {
                                ctrlPoint += prevPoint;
                                endPoint += prevPoint;
                            }
                            points[j] = prevPoint + (ctrlPoint - prevPoint) * 2 / 3;
                            points[j + 1] = endPoint + (ctrlPoint - endPoint) * 2 / 3;
                            prevPoint = points[j + 2] = endPoint;
                        }
                        prevPoint = points.Last();
                        prevControlPoint = points.SkipLast(1).Last();
                        prevControlPointDetermined = true;
                        break;

                    case 'S':
                        type = PathPieceType.Curve;
                        var pointsList1 = new List<PointD>();
                        foreach (var pair in numbers.Split(2).Select(x => new PointD(x.First(), x.Last())).Split(2))
                        {
                            Ut.Assert(pair.Count() == 2);
                            pointsList1.Add(prevPoint + (prevPoint - prevControlPoint));
                            pointsList1.Add(prevControlPoint = pair.First());
                            pointsList1.Add(prevPoint = pair.Last());
                        }
                        points = pointsList1.ToArray();
                        prevControlPointDetermined = true;
                        break;

                    case 's':
                        type = PathPieceType.Curve;
                        var pointsList2 = new List<PointD>();
                        foreach (var pair in numbers.Split(2).Select(x => new PointD(x.First(), x.Last())).Split(2))
                        {
                            Ut.Assert(pair.Count() == 2);
                            pointsList2.Add(prevPoint + (prevPoint - prevControlPoint));
                            pointsList2.Add(prevControlPoint = (pair.First() + prevPoint));
                            pointsList2.Add(prevPoint = (pair.Last() + prevPoint));
                        }
                        points = pointsList2.ToArray();
                        prevControlPointDetermined = true;
                        break;

                    default:
                        throw new InvalidOperationException();
                }

                if (prevStartPoint == null)
                    prevStartPoint = points[0];
                if (!prevControlPointDetermined)
                    prevControlPoint = prevPoint;

                yield return new PathPiece(type, points);
            }
            else if ((m = Regex.Match(svgPath, @"^Z\s*", RegexOptions.IgnoreCase)).Success)
            {
                yield return PathPiece.End;
                prevPoint = prevStartPoint ?? new PointD(0, 0);
                prevStartPoint = null;
            }
            else if ((m = Regex.Match(svgPath, @"^A\s*(({0})({0})({0})([01])[\s,]*([01])[\s,]*({0})({0}))+".Fmt(numRegex), RegexOptions.IgnoreCase)).Success)
            {
                for (var cp = 0; cp < m.Groups[1].Captures.Count; cp++)
                {
                    double convert(int gr) => double.Parse(m.Groups[gr].Captures[cp].Value.Trim().TrimEnd(',').Trim());
                    var p = new PointD(convert(7), convert(8));
                    prevPoint = m.Value[0] == 'a' ? p + prevPoint : p;
                    yield return new PathPieceArc(convert(2), convert(3), convert(4), m.Groups[5].Captures[cp].Value != "0", m.Groups[6].Captures[cp].Value != "0", prevPoint);
                }
            }
            else
            {
                Debugger.Break();
                throw new NotImplementedException();
            }
            svgPath = svgPath.Substring(m.Length);
        }
    }
}
