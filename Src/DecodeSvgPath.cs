using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using RT.Util;
using RT.Util.ExtensionMethods;
using RT.Util.Geometry;

namespace RT.KitchenSink
{
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
            public static readonly PathPiece End = new PathPiece { Type = PathPieceType.End };

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
                if (points == null)
                    throw new ArgumentNullException(nameof(points));
                Type = type;
                Points = points;
            }

            private PathPiece() { }

            /// <summary>Recreates the path in SVG path data syntax.</summary>
            public override string ToString()
            {
                char ch;
                switch (Type)
                {
                    case PathPieceType.Move: ch = 'M'; break;
                    case PathPieceType.Line: ch = 'L'; break;
                    case PathPieceType.Curve: ch = 'C'; break;
                    default: return "z";
                }
                return ch + " " + Points.Select(p => $"{p.X},{p.Y}").JoinString(" ");
            }

            /// <summary>
            ///     Returns a new <see cref="PathPiece"/> of the same <see cref="PathPieceType"/> in which all points have
            ///     been mapped through the <paramref name="selector"/>.</summary>
            /// <param name="selector">
            ///     A function to pass all points through.</param>
            /// <returns>
            ///     A new <see cref="PathPiece"/> of the same <see cref="PathPieceType"/>.</returns>
            public virtual PathPiece Select(Func<PointD, PointD> selector)
            {
                if (selector == null)
                    throw new ArgumentNullException(nameof(selector));
                if (Type == PathPieceType.End)
                    return this;
                return new PathPiece(Type, Points.Select(selector).ToArray());
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

            /// <summary>Recreates the path in SVG path data syntax.</summary>
            public override string ToString() => $"A {RX},{RY} {XAxisRotation} {(LargeArcFlag ? "1" : "0")} {(SweepFlag ? "1" : "0")} {Points[0].X},{Points[0].Y}";

            /// <summary>
            ///     Returns a new <see cref="PathPiece"/> of the same <see cref="PathPieceType"/> in which all points have
            ///     been mapped through the <paramref name="selector"/>.</summary>
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
            ///     Moves to a new point without drawing a line. This is usually used only at the start of a subpath (i.e., at
            ///     the start of a path or after an <see cref="End"/>).</summary>
            Move,
            /// <summary>Draws a set of straight lines connecting each point (including the last point of the previous piece).</summary>
            Line,
            /// <summary>
            ///     Draws a set of Bézier curves. The length of the <see cref="PathPiece.Points"/> array is a multiple of
            ///     three (two control points and an end-point). The first start point is the last point of the previous
            ///     piece.</summary>
            Curve,
            /// <summary>Draws an elliptical arc.</summary>
            Arc,
            /// <summary>Designates the end of a path or subpath.</summary>
            End
        }

        /// <summary>
        ///     Converts a sequence of <see cref="PathPiece"/> objects to a sequence of points using the specified <paramref
        ///     name="bézierSmoothness"/> to render Bézier curves.</summary>
        /// <param name="pieces">
        ///     The pieces that constitute the path.</param>
        /// <param name="bézierSmoothness">
        ///     A value indicating the maximum amount by which each Bézier curve is allowed to be approximated. The smaller
        ///     this value, the more points are generated for each Bézier curve.</param>
        /// <returns>
        ///     A sequence of points that represent the fully rendered path.</returns>
        public static IEnumerable<IEnumerable<PointD>> Do(IEnumerable<PathPiece> pieces, double bézierSmoothness)
        {
            if (pieces == null)
                throw new ArgumentNullException(nameof(pieces));
            if (bézierSmoothness <= 0)
                throw new ArgumentOutOfRangeException(nameof(bézierSmoothness), "The bézierSmoothness parameter cannot be zero or negative.");

            foreach (var group in pieces.Split(pp => pp.Type == PathPieceType.End).Where(gr => gr.Any()).Select(gr => gr.ToArray()))
            {
                yield return Enumerable.Range(0, group.Length).SelectMany(grIx =>
                {
                    var next = group[grIx];
                    var prev = group[(grIx + group.Length - 1) % group.Length];

                    if (next.Type == PathPieceType.Move || next.Type == PathPieceType.Line)
                        return next.Points;

                    if (next.Type == PathPieceType.Curve)
                        return Enumerable.Range(0, next.Points.Length / 3)
                            .SelectMany(ix => smoothBézier(ix == 0 ? prev.Points[prev.Points.Length - 1] : next.Points[3 * ix - 1], next.Points[3 * ix], next.Points[3 * ix + 1], next.Points[3 * ix + 2], bézierSmoothness).Skip(1))
                            .ToArray();

                    throw new InvalidOperationException();
                });
            }
        }

        private static PointD bé(PointD start, PointD c1, PointD c2, PointD end, double t) => Math.Pow((1 - t), 3) * start + 3 * (1 - t) * (1 - t) * t * c1 + 3 * (1 - t) * t * t * c2 + Math.Pow(t, 3) * end;

        private static IEnumerable<PointD> smoothBézier(PointD start, PointD c1, PointD c2, PointD end, double smoothness)
        {
            yield return start;

            var stack = new Stack<Tuple<double, double>>();
            stack.Push(Tuple.Create(0d, 1d));

            while (stack.Count > 0)
            {
                var elem = stack.Pop();
                var p1 = bé(start, c1, c2, end, elem.Item1);
                var p2 = bé(start, c1, c2, end, elem.Item2);
                var midT = (elem.Item1 + elem.Item2) / 2;
                var midCurve = bé(start, c1, c2, end, midT);
                var dist = new EdgeD(p1, p2).Distance(midCurve);
                if (double.IsNaN(dist) || dist <= smoothness)
                    yield return p2;
                else
                {
                    stack.Push(Tuple.Create(midT, elem.Item2));
                    stack.Push(Tuple.Create(elem.Item1, midT));
                }
            }
        }

        /// <summary>
        ///     Converts a string containing SVG path data to a sequence of points using the specified <paramref
        ///     name="bézierSmoothness"/> to render Bézier curves.</summary>
        /// <param name="svgPath">
        ///     The SVG path data.</param>
        /// <param name="bézierSmoothness">
        ///     A value indicating the maximum amount by which each Bézier curve is allowed to be approximated. The smaller
        ///     this value, the more points are generated for each Bézier curve.</param>
        /// <returns>
        ///     A sequence of points that represent the fully rendered path.</returns>
        public static IEnumerable<IEnumerable<PointD>> Do(string svgPath, double bézierSmoothness)
        {
            return Do(DecodePieces(svgPath), bézierSmoothness);
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
                if ((m = Regex.Match(svgPath, @"^[MLCHVS]\s*({0})*".Fmt(numRegex), RegexOptions.IgnoreCase)).Success)
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
                else if ((m = Regex.Match(svgPath, @"^A\s*({0})*".Fmt(numRegex), RegexOptions.IgnoreCase)).Success)
                {
                    var numbers = m.Groups[1].Captures.Cast<Capture>().Select(c => double.Parse(c.Value.Trim().TrimEnd(',').Trim())).ToArray();
                    if (numbers.Length % 7 != 0)
                        Debugger.Break();
                    for (int i = 0; i < numbers.Length; i += 7)
                    {
                        var p = new PointD(numbers[i + 5], numbers[i + 6]);
                        prevPoint = m.Value[0] == 'a' ? p + prevPoint : p;
                        yield return new PathPieceArc(numbers[i + 0], numbers[i + 1], numbers[i + 2], numbers[i + 3] != 0, numbers[i + 4] != 0, prevPoint);
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
}
