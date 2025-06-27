using System.Diagnostics;
using RT.Internal;

namespace RT.Geometry;

/// <summary>Contains general geometry-related utility functions.</summary>
public static class GeomUt
{
    /// <summary>"Unwinds" the specified angle so that it's in the range (-pi, pi]</summary>
    public static double NormalizedAngle(double a)
    {
        // Common cases
        if (a > -Math.PI && a <= Math.PI)
            return a;
        else if (a > 0 && a < 3 * Math.PI)
            return a - 2 * Math.PI;

        // General case - probably slow due to division
        double mod = Math.IEEERemainder(a, 2 * Math.PI);

        if (mod > Math.PI)
            mod -= 2 * Math.PI;

        if (mod == -Math.PI)
            return Math.PI;
        else
            return mod;
    }

    /// <summary>
    ///     Returns "angle" relative to "reference". I.e. if the angles are the same, returns 0; if angle is further
    ///     anticlockwise, returns a positive number.</summary>
    public static double AngleDifference(double reference, double angle)
    {
        return NormalizedAngle(angle - reference);
    }

    /// <summary>
    ///     Given a vector l, and two points pt1 and pt2, splits the space into two halves on the line defined by the vector
    ///     l. If both points lie in the same half, returns true. If they lie in different halves, or if at least one point
    ///     lies on the dividing line, returns false. If the vector l is of length 0, always returns false.</summary>
    public static bool ArePointsSameSideOfLine(
        double lX, double lY, double pt1X, double pt1Y, double pt2X, double pt2Y)
    {
        // Consider cross products of both point vectors with the line vector.
        double xp1 = lX * pt1Y - lY * pt1X;
        double xp2 = lX * pt2Y - lY * pt2X;
        // If both cross products are positive or both negative the points are on the
        // same side. If one or both of them is zero the point lies on the line and
        // therefore both points cannot be on the same side.
        return xp1 * xp2 > 0;
    }

    /// <summary>
    ///     Given a parametrized curve, generates a series of points along the curve such that no individual segment is more
    ///     than <paramref name="smoothness"/> away from the true curve.</summary>
    /// <param name="startT">
    ///     Value for <paramref name="fnc"/> at which the curve should start.</param>
    /// <param name="endT">
    ///     Value for <paramref name="fnc"/> at which the curve should end.</param>
    /// <param name="fnc">
    ///     Function that defines the curve by converting a parameter to a point.</param>
    /// <param name="smoothness">
    ///     Maximum amount by which the line segments are allowed to deviate from the curve.</param>
    public static IEnumerable<PointD> SmoothCurve(double startT, double endT, Func<double, PointD> fnc, double smoothness)
    {
        yield return fnc(startT);

        var stack = new Stack<(double from, double to)>();
        stack.Push((startT, endT));

        while (stack.Count > 0)
        {
            var (from, to) = stack.Pop();
            var p1 = fnc(from);
            var p2 = fnc(to);
            var midT = (from + to) / 2;
            var midCurve = fnc(midT);
            var dist = new EdgeD(p1, p2).Distance(midCurve);
            if (double.IsNaN(dist) || dist <= smoothness)
                yield return p2;
            else
            {
                stack.Push((midT, to));
                stack.Push((from, midT));
            }
        }
    }

    /// <summary>
    ///     Generates a series of points that approximate a cubic Bézier curve.</summary>
    /// <param name="start">
    ///     Start point of the curve.</param>
    /// <param name="c1">
    ///     First control point.</param>
    /// <param name="c2">
    ///     Second control point.</param>
    /// <param name="end">
    ///     End point of the curve.</param>
    /// <param name="smoothness">
    ///     Maximum amount by which the line segments are allowed to deviate from the curve.</param>
    public static IEnumerable<PointD> SmoothBézier(PointD start, PointD c1, PointD c2, PointD end, double smoothness) =>
        SmoothCurve(0, 1, t => Math.Pow(1 - t, 3) * start + 3 * (1 - t) * (1 - t) * t * c1 + 3 * (1 - t) * t * t * c2 + Math.Pow(t, 3) * end, smoothness);

    /// <summary>
    ///     Generates a series of points that approximate an elliptic arc curve.</summary>
    /// <param name="center">
    ///     Center of the ellipse.</param>
    /// <param name="a">
    ///     Horizontal radius of the ellipse.</param>
    /// <param name="b">
    ///     Vertical radius of the ellipse.</param>
    /// <param name="t1">
    ///     Parameter at which the ellipse starts. If it’s a circle, this is the angle from the x-axis, but for ellipses it is
    ///     stretched, so it’s not the real angle.</param>
    /// <param name="t2">
    ///     Parameter at which the ellipse ends. If it’s a circle, this is the angle from the x-axis, but for ellipses it is
    ///     stretched, so it’s not the real angle.</param>
    /// <param name="smoothness">
    ///     Maximum amount by which the line segments are allowed to deviate from the curve.</param>
    public static IEnumerable<PointD> SmoothArc(PointD center, double a, double b, double t1, double t2, double smoothness) =>
        SmoothCurve(t1, t2, t => new PointD(center.X + a * Math.Cos(t), center.Y + b * Math.Sin(t)), smoothness);

    /// <summary>
    ///     Merges adjacent (touching) polygons and removes the touching edges. Behaviour is undefined if any polygons
    ///     overlap. Only exactly matching edges are considered, and only if they have the opposite sense to each other.</summary>
    /// <param name="polygons">
    ///     Polygons to merge. This method removes entries from this list, and modifies polygon vertices.</param>
    /// <remarks>
    ///     Example use case: simplifying subsets of <see cref="VoronoiDiagram"/> polygons.</remarks>
    public static void MergeAdjacentPolygons(List<PolygonD> polygons)
    {
        bool anyChangesOuter;
        do
        {
            anyChangesOuter = false;
            var vertices = (from p in polygons from v in p.Vertices select (p, v)).ToLookup(x => x.v, x => x.p);
            var removedPolygons = new HashSet<PolygonD>();
            foreach (var polys in vertices.Where(grp => grp.Count() > 1))
            {
                foreach (var pair in polys.UniquePairs()) // might still contain duplicates if a polygon goes through the same vertex twice
                    if (pair.Item1 != pair.Item2 && !removedPolygons.Contains(pair.Item1) && !removedPolygons.Contains(pair.Item2))
                    {
                        // maybe merge these two
                        var p1 = pair.Item1;
                        var p2 = pair.Item2;
                        for (int is1 = 0; is1 < p1.Vertices.Count; is1++)
                            for (int is2 = 0; is2 < p2.Vertices.Count; is2++)
                            {
                                int ie1 = (is1 + 1) % p1.Vertices.Count;
                                int ie2 = (is2 + 1) % p2.Vertices.Count;
                                if (p1.Vertices[is1] == p2.Vertices[ie2] && p1.Vertices[ie1] == p2.Vertices[is2])
                                {
                                    anyChangesOuter = true;
                                    // Concatenate the two polygons while removing the shared edge by dropping duplicate vertices from the second polygon
                                    var newVertices = Enumerable.Range(ie1, p1.Vertices.Count).Select(i1 => p1.Vertices[i1 % p1.Vertices.Count])
                                        .Concat(Enumerable.Range(ie2 + 1, p2.Vertices.Count - 2).Select(i2 => p2.Vertices[i2 % p2.Vertices.Count]))
                                        .ToList();
                                    // Simplify any redundant edges in this new polygon (this happens if the two polygons shared more than one edge)
                                    bool anyChanges;
                                    do
                                    {
                                        anyChanges = false;
                                        for (int i = 0; i < newVertices.Count; i++)
                                        {
                                            int ib = (i - 1 + newVertices.Count) % newVertices.Count;
                                            int ia = (i + 1) % newVertices.Count;
                                            if (newVertices[ib] == newVertices[ia])
                                            {
                                                // Remove the vertex at i and the duplicate vertex at ib
                                                newVertices = Enumerable.Range(ia, newVertices.Count - 2).Select(ii => newVertices[ii % newVertices.Count]).ToList();
                                                anyChanges = true;
                                            }
                                        }
                                    } while (anyChanges);
                                    // Update polygon 1 vertices
                                    p1.Vertices.Clear();
                                    p1.Vertices.AddRange(newVertices);
                                    // Polygon 2 gets removed entirely
                                    if (removedPolygons.Contains(p1))
                                        throw new Exception();
                                    removedPolygons.Add(p2);
                                    if (removedPolygons.Contains(p1))
                                        throw new Exception();
                                    goto next;
                                }
                            }
                        next:;
                    }
            }
            polygons.RemoveAll(p => removedPolygons.Contains(p));
        } while (anyChangesOuter);
    }

    /// <summary>
    ///     Determines whether a cubic Bézier curve intersects itself.</summary>
    /// <param name="start">
    ///     Start point of the Bézier curve.</param>
    /// <param name="c1">
    ///     First control point of the Bézier curve.</param>
    /// <param name="c2">
    ///     Second control point of the Bézier curve.</param>
    /// <param name="end">
    ///     End point of the Bézier curve.</param>
    public static bool IsBézierSelfIntersecting(PointD start, PointD c1, PointD c2, PointD end)
    {
        // Move, rotate and scale the curve such that the first point is (0,0) and the first control point is (1,0)
        var c2t = (c2 - start).Rotated((c1 - start).Theta()) / c1.Distance(start);
        var q = (end - start).Rotated((c1 - start).Theta()) / c1.Distance(start);

        // If the part under the square root is negative, there will not be a solution
        var det = 9 * c2t.Y * c2t.Y - 6 * c2t.Y * q.Y - 3 * q.Y * q.Y + 12 * c2t.Y * c2t.X * q.Y - 12 * c2t.Y * c2t.Y * q.X;
        if (det < 0)
            return false;

        var denom = -3 * c2t.Y + c2t.Y * q.X + 2 * q.Y - c2t.X * q.Y;

        // Try both the positive and negative square root
        for (var sgn = -1; sgn <= 1; sgn += 2)
        {
            var ct = -c2t.Y * 3 / 2 + q.Y / 2 + sgn * Math.Sqrt(det) / 2;
            var t1 = (-3 * c2t.Y + ct - q.Y) / denom;
            var t2 = ct / denom;
            if (t1 >= 0 && t1 <= 1 && t2 >= 0 && t2 <= 1)
                return true;
        }
        return false;
    }

    /// <summary>
    ///     Checks the specified condition and causes the debugger to break if it is false. Throws an <see cref="Exception"/>
    ///     afterwards.</summary>
    [DebuggerHidden]
    internal static void Assert(bool assertion, string message = null)
    {
        if (!assertion)
        {
            if (Debugger.IsAttached)
                Debugger.Break();
            throw new Exception(message ?? "Assertion failure");
        }
    }
}
