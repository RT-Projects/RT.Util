using System.Diagnostics;
using System.Text.RegularExpressions;
using RT.Internal;

namespace RT.Geometry;

/// <summary>Provides methods to parse the syntax used in SVG path data.</summary>
public static class SvgPath
{
    /// <summary>
    ///     Converts a sequence of <see cref="SvgPiece"/> objects to a sequence of points using the specified <paramref
    ///     name="smoothness"/> to render Bézier curves and circular arcs.</summary>
    /// <param name="pieces">
    ///     The pieces that constitute the path.</param>
    /// <param name="smoothness">
    ///     A value indicating the maximum amount by which each Bézier curve and circular arc is allowed to be approximated.
    ///     The smaller this value, the more points are generated for each curve/arc.</param>
    /// <returns>
    ///     A sequence of points that represent the fully rendered path.</returns>
    public static IEnumerable<IEnumerable<PointD>> Smooth(this IEnumerable<SvgPiece> pieces, double smoothness)
    {
        if (pieces == null)
            throw new ArgumentNullException(nameof(pieces));
        if (smoothness <= 0)
            throw new ArgumentOutOfRangeException(nameof(smoothness), "The smoothness parameter cannot be zero or negative.");

        IEnumerable<IEnumerable<PointD>> iterator()
        {
            foreach (var group in pieces.Split(pp => pp.Type == SvgPieceType.End).Where(gr => gr.Any()).Select(gr => gr.ToArray()))
            {
                yield return Enumerable.Range(0, group.Length).SelectMany(grIx =>
                {
                    var cur = group[grIx];
                    var prev = group[(grIx + group.Length - 1) % group.Length];

                    if (cur.Type == SvgPieceType.Move || cur.Type == SvgPieceType.Line)
                        return cur.Points;

                    var lastPoint = prev.Points[prev.Points.Length - 1];

                    if (cur.Type == SvgPieceType.Curve && cur.Points.Length % 3 == 0)
                        return Enumerable.Range(0, cur.Points.Length / 3)
                            .SelectMany(ix => GeomUt.SmoothBézier(ix == 0 ? lastPoint : cur.Points[3 * ix - 1], cur.Points[3 * ix], cur.Points[3 * ix + 1], cur.Points[3 * ix + 2], smoothness).Skip(1))
                            .ToArray();

                    if (cur.Type == SvgPieceType.Arc && cur is SvgArc arc && arc.Points.Length == 1)
                    {
                        // Rotate the start and end point in such a way that the ellipse becomes axis-aligned.
                        var p1raw = lastPoint.Rotate(-arc.XAxisRotation * Math.PI / 180);
                        var p2raw = arc.Points[0].Rotate(-arc.XAxisRotation * Math.PI / 180);

                        // Scale the y-coordinates in such a way that the desired ellipse becomes a circle.
                        var yFactor = arc.RX / arc.RY;
                        var p1 = new PointD(p1raw.X, p1raw.Y * yFactor);
                        var p2 = new PointD(p2raw.X, p2raw.Y * yFactor);
                        var d = (p1 - p2).Length / 2;

                        // If arc.SweepFlag is false, we need to draw the arc on the “other” side of the p1→p2 line.
                        // We will accomplish this by swapping the start and end points and later reversing the entire curve.
                        if (!arc.SweepFlag)
                            (p1, p2) = (p2, p1);

                        // Find the mid-point between the start and end point
                        var pm = (p1 + p2) / 2;

                        // Find the distance from that mid-point to the center of the circle
                        var m = Math.Sqrt(Math.Pow(arc.RX, 2) - Math.Pow(d, 2));

                        // Determine the center of the circle by dropping a perpendicular from the p1→p2 line and going a distance of m.
                        // The direction of the perpendicular depends on arc.LargeArcFlag.
                        var pc = pm + m * (arc.LargeArcFlag ? ((p2 - p1) / 2).RotateNeg90() : ((p2 - p1) / 2).Rotate90()).Normalized;

                        // Start and end angle
                        var startAngle = Math.Atan2(p1.Y - pc.Y, p1.X - pc.X);
                        var endAngle = Math.Atan2(p2.Y - pc.Y, p2.X - pc.X);
                        if (endAngle < startAngle)
                            endAngle += 2 * Math.PI;
                        var curve = GeomUt.SmoothCurve(startAngle, endAngle, t => new PointD(pc.X + arc.RX * Math.Cos(t), (pc.Y + arc.RX * Math.Sin(t)) / yFactor).Rotate(arc.XAxisRotation * Math.PI / 180), smoothness);
                        return arc.SweepFlag ? curve.Skip(1) : curve.Reversed().Skip(1);
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
    public static IEnumerable<IEnumerable<PointD>> Smooth(string svgPath, double smoothness)
    {
        return Smooth(Decode(svgPath), smoothness);
    }

    /// <summary>
    ///     Converts a string containing SVG path data to a sequence of <see cref="SvgPiece"/> objects.</summary>
    /// <param name="svgPath">
    ///     SVG path data to parse.</param>
    public static IEnumerable<SvgPiece> Decode(string svgPath)
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
            if ((m = Regex.Match(svgPath, $@"^[MLCQHVS]\s*({numRegex})*", RegexOptions.IgnoreCase)).Success)
            {
                SvgPieceType type;
                PointD[] points;
                bool prevControlPointDetermined = false;
                var numbers = m.Groups[1].Captures.Cast<Capture>().Select(c => double.Parse(c.Value.Trim().TrimEnd(',').Trim())).ToArray();

                switch (m.Value[0])
                {
                    case 'M':
                        type = SvgPieceType.Move;
                        points = numbers.Split(2).Select(gr => new PointD(gr.First(), gr.Last())).ToArray();
                        prevPoint = points[points.Length - 1];
                        break;

                    case 'm':
                        type = SvgPieceType.Move;
                        points = numbers.Split(2).Select(gr => new PointD(gr.First(), gr.Last())).ToArray();
                        for (int i = 0; i < points.Length; i++)
                            prevPoint = (points[i] += prevPoint);
                        break;

                    case 'L':
                        type = SvgPieceType.Line;
                        points = numbers.Split(2).Select(gr => new PointD(gr.First(), gr.Last())).ToArray();
                        prevPoint = points[points.Length - 1];
                        break;

                    case 'l':
                        type = SvgPieceType.Line;
                        points = numbers.Split(2).Select(gr => new PointD(gr.First(), gr.Last())).ToArray();
                        for (int i = 0; i < points.Length; i++)
                            prevPoint = (points[i] += prevPoint);
                        break;

                    case 'H':
                        type = SvgPieceType.Line;
                        points = numbers.Select(x => new PointD(x, prevPoint.Y)).ToArray();
                        prevPoint = points.Last();
                        break;

                    case 'h':
                        type = SvgPieceType.Line;
                        points = new PointD[numbers.Length];
                        for (int i = 0; i < numbers.Length; i++)
                            prevPoint = points[i] = new PointD(prevPoint.X + numbers[i], prevPoint.Y);
                        break;

                    case 'V':
                        type = SvgPieceType.Line;
                        points = numbers.Select(y => new PointD(prevPoint.X, y)).ToArray();
                        prevPoint = points.Last();
                        break;

                    case 'v':
                        type = SvgPieceType.Line;
                        points = new PointD[numbers.Length];
                        for (int i = 0; i < numbers.Length; i++)
                            prevPoint = points[i] = new PointD(prevPoint.X, prevPoint.Y + numbers[i]);
                        break;

                    case 'C':
                        type = SvgPieceType.Curve;
                        points = numbers.Split(2).Select(x => new PointD(x.First(), x.Last())).ToArray();
                        GeomUt.Assert(points.Length % 3 == 0);
                        prevPoint = points.Last();
                        prevControlPoint = points.SkipLast(1).Last();
                        prevControlPointDetermined = true;
                        break;

                    case 'c':
                        type = SvgPieceType.Curve;
                        points = numbers.Split(2).Select(x => new PointD(x.First(), x.Last())).ToArray();
                        GeomUt.Assert(points.Length % 3 == 0);
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
                        type = SvgPieceType.Curve;
                        var qPoints = numbers.Split(2).Select(x => new PointD(x.First(), x.Last())).ToArray();
                        GeomUt.Assert(qPoints.Length % 2 == 0);
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
                        type = SvgPieceType.Curve;
                        var pointsList1 = new List<PointD>();
                        foreach (var pair in numbers.Split(2).Select(x => new PointD(x.First(), x.Last())).Split(2))
                        {
                            GeomUt.Assert(pair.Count() == 2);
                            pointsList1.Add(prevPoint + (prevPoint - prevControlPoint));
                            pointsList1.Add(prevControlPoint = pair.First());
                            pointsList1.Add(prevPoint = pair.Last());
                        }
                        points = pointsList1.ToArray();
                        prevControlPointDetermined = true;
                        break;

                    case 's':
                        type = SvgPieceType.Curve;
                        var pointsList2 = new List<PointD>();
                        foreach (var pair in numbers.Split(2).Select(x => new PointD(x.First(), x.Last())).Split(2))
                        {
                            GeomUt.Assert(pair.Count() == 2);
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

                yield return new SvgPiece(type, points);
            }
            else if ((m = Regex.Match(svgPath, @"^Z\s*", RegexOptions.IgnoreCase)).Success)
            {
                yield return SvgPiece.End;
                prevPoint = prevStartPoint ?? new PointD(0, 0);
                prevStartPoint = null;
            }
            else if ((m = Regex.Match(svgPath, $@"^A\s*(({numRegex})({numRegex})({numRegex})([01])[\s,]*([01])[\s,]*({numRegex})({numRegex}))+", RegexOptions.IgnoreCase)).Success)
            {
                for (var cp = 0; cp < m.Groups[1].Captures.Count; cp++)
                {
                    double convert(int gr) => double.Parse(m.Groups[gr].Captures[cp].Value.Trim().TrimEnd(',').Trim());
                    var p = new PointD(convert(7), convert(8));
                    prevPoint = m.Value[0] == 'a' ? p + prevPoint : p;
                    yield return new SvgArc(convert(2), convert(3), convert(4), m.Groups[5].Captures[cp].Value != "0", m.Groups[6].Captures[cp].Value != "0", prevPoint);
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

    /// <summary>Reverses the direction of an SVG path.</summary>
    public static IEnumerable<SvgPiece> ReversePath(this IEnumerable<SvgPiece> svgPath)
    {
        var pathChunks = new List<(bool closed, List<SvgPiece> path)>();
        List<SvgPiece> curChunk = null;
        bool curClosed = false;
        foreach (var piece in svgPath)
        {
            if (piece.Type == SvgPieceType.End)
            {
                curClosed = true;
                continue;
            }
            else if (piece.Type == SvgPieceType.Move)
            {
                if (curChunk != null)
                    pathChunks.Add((curClosed, curChunk));
                curChunk = piece.Points.Length == 1 ? [piece] : [new SvgPiece(SvgPieceType.Move, piece.Points[0]), new(SvgPieceType.Line, piece.Points.Skip(1).ToArray())];
            }
            else
                curChunk.Add(piece);
        }
        if (curChunk == null)
            yield break;
        pathChunks.Add((curClosed, curChunk));

        foreach (var (closed, path) in pathChunks)
        {
            if (path[0].Type != SvgPieceType.Move || path[0].Points.Length != 1)
                throw new InvalidOperationException("Internal error: expected Move with length 1 at start of SVG path.");
            yield return new(SvgPieceType.Move, path.Last().Points.Last());
            for (var i = path.Count - 1; i >= 1; i--)
            {
                var nextPoint = path[i - 1].Points[path[i - 1].Points.Length - 1];
                switch (path[i])
                {
                    case { Type: SvgPieceType.Move }:
                    case { Type: SvgPieceType.End }:
                        throw new InvalidOperationException("Internal error: unexpected path type in SVG path.");
                    case { Type: SvgPieceType.Line }:
                    case { Type: SvgPieceType.Curve }:
                        yield return new(path[i].Type, [.. path[i].Points.Reversed().Skip(1), nextPoint]);
                        break;
                    case SvgArc arc:
                        yield return new SvgArc(arc.RX, arc.RY, arc.XAxisRotation, arc.LargeArcFlag, !arc.SweepFlag, nextPoint);
                        break;
                    default:
                        throw new InvalidOperationException($"Unsupported path type: {path[i].Type}.");
                }
            }
            if (closed)
                yield return SvgPiece.End;
        }
    }
}
