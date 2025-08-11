using System.Drawing;
using System.Runtime.CompilerServices;
using RT.Internal;

namespace RT.Geometry;

/// <summary>This class encapsulates double-precision polygons.</summary>
public sealed class PolygonD
{
    private List<PointD> _vertices;

    /// <summary>Returns a list of vertices of the polygon.</summary>
    public List<PointD> Vertices => _vertices;

    /// <summary>
    ///     Enumerates the edges of this polygon in vertex order. The enumerable is "live" and reflects any changes to <see
    ///     cref="Vertices"/> immediately.</summary>
    public IEnumerable<EdgeD> Edges => Vertices.ConsecutivePairs(closed: true).Select(pair => new EdgeD(pair.Item1, pair.Item2));

    /// <summary>
    ///     Initializes a polygon from a given list of vertices.</summary>
    /// <param name="vertices">
    ///     Vertices (corner points) to initialize polygon from.</param>
    public PolygonD(IEnumerable<PointD> vertices)
    {
        _vertices = new List<PointD>(vertices);
    }

    /// <summary>
    ///     Initializes a polygon from a given array of vertices.</summary>
    /// <param name="vertices">
    ///     Vertices (corner points) to initialize polygon from.</param>
    public PolygonD(params PointD[] vertices)
    {
        _vertices = new List<PointD>(vertices);
    }

    /// <summary>
    ///     Determines whether the current <see cref="PolygonD"/> contains the specified <see cref="Point"/>.</summary>
    /// <param name="point">
    ///     Point to check.</param>
    /// <returns>
    ///     True if the specified point lies inside the current <see cref="PolygonD"/>.</returns>
    public bool ContainsPoint(Point point) => ContainsPoint(new PointD(point.X, point.Y));

    /// <summary>
    ///     Determines whether the current <see cref="PolygonD"/> contains the specified point. If the point lies exactly on
    ///     one of the polygon edges, it is considered to be contained in the polygon.</summary>
    /// <param name="point">
    ///     Point to check.</param>
    /// <returns>
    ///     True if the specified point lies inside the current <see cref="PolygonD"/>.</returns>
    public bool ContainsPoint(PointD point)
    {
        // from https://github.com/AngusJohnson/Clipper2/blob/ee705be33ad9560861e859c50328a38e136b1d57/CSharp/Clipper2Lib/Clipper.Core.cs#L722
        int len = _vertices.Count, start = 0;
        if (len < 3) return false;

        while (start < len && _vertices[start].Y == point.Y) start++;
        if (start == len) return false;

        double d;
        bool isAbove = _vertices[start].Y < point.Y, startingAbove = isAbove;
        int val = 0, i = start + 1, end = len;
        while (true)
        {
            if (i == end)
            {
                if (end == 0 || start == 0) break;
                end = start;
                i = 0;
            }

            if (isAbove)
            {
                while (i < end && _vertices[i].Y < point.Y) i++;
            }
            else
            {
                while (i < end && _vertices[i].Y > point.Y) i++;
            }

            if (i == end) continue;

            PointD curr = _vertices[i], prev;
            prev = i > 0 ? _vertices[i - 1] : _vertices[len - 1];

            if (curr.Y == point.Y)
            {
                if (curr.X == point.X || (curr.Y == prev.Y &&
                  ((point.X < prev.X) != (point.X < curr.X))))
                    return true; // on edge
                i++;
                if (i == start) break;
                continue;
            }

            if (point.X < curr.X && point.X < prev.X)
            {
                // we're only interested in edges crossing on the left
            }
            else if (point.X > prev.X && point.X > curr.X)
            {
                val = 1 - val; // toggle val
            }
            else
            {
                d = crossProduct3(prev, curr, point);
                if (d == 0) return true; // on edge
                if ((d < 0) == isAbove) val = 1 - val;
            }
            isAbove = !isAbove;
            i++;
        }

        if (isAbove == startingAbove) return val != 0;
        if (i == len) i = 0;
        d = i == 0 ? crossProduct3(_vertices[len - 1], _vertices[0], point) : crossProduct3(_vertices[i - 1], _vertices[i], point);
        if (d == 0) return true; // on edge
        if ((d < 0) == isAbove) val = 1 - val;

        return val != 0;
        #region Licence
        /*
        Boost Software License - Version 1.0 - August 17th, 2003

        Permission is hereby granted, free of charge, to any person or organization
        obtaining a copy of the software and accompanying documentation covered by
        this license (the "Software") to use, reproduce, display, distribute,
        execute, and transmit the Software, and to prepare derivative works of the
        Software, and to permit third-parties to whom the Software is furnished to
        do so, all subject to the following:

        The copyright notices in the Software and this entire statement, including
        the above license grant, this restriction and the following disclaimer,
        must be included in all copies of the Software, in whole or in part, and
        all derivative works of the Software, unless such copies or derivative
        works are solely in the form of machine-executable object code generated by
        a source language processor.

        THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
        IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
        FITNESS FOR A PARTICULAR PURPOSE, TITLE AND NON-INFRINGEMENT. IN NO EVENT
        SHALL THE COPYRIGHT HOLDERS OR ANYONE DISTRIBUTING THE SOFTWARE BE LIABLE
        FOR ANY DAMAGES OR OTHER LIABILITY, WHETHER IN CONTRACT, TORT OR OTHERWISE,
        ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
        DEALINGS IN THE SOFTWARE.
        */
        #endregion
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double crossProduct3(PointD pt1, PointD pt2, PointD pt) =>
#if NET8_0_OR_GREATER
        double.FusedMultiplyAdd(pt2.X - pt1.X, pt.Y - pt2.Y, (pt1.Y - pt2.Y /*negated*/) * (pt.X - pt2.X));
#else
        (pt2.X - pt1.X) * (pt.Y - pt2.Y) - (pt2.Y - pt1.Y) * (pt.X - pt2.X);
#endif


    /// <summary>
    ///     Determines the area of the current <see cref="PolygonD"/>.</summary>
    /// <returns>
    ///     The area of the current <see cref="PolygonD"/> in square units.</returns>
    public double Area()
    {
        double area = 0;
        var p = _vertices[_vertices.Count - 1];
        foreach (var q in _vertices)
        {
            area += q.Y * p.X - q.X * p.Y;
            p = q;
        }
        return area / 2;
    }

    /// <summary>
    ///     Converts the current <see cref="PolygonD"/> to an array of <see cref="PointF"/> structures. Note that this
    ///     conversion loses precision.</summary>
    /// <returns>
    ///     Array of converted vertices with lower precision.</returns>
    public PointF[] ToPointFArray() => _vertices.Select(x => x.ToPointF()).ToArray();

    /// <summary>Calculates the centroid of this polygon.</summary>
    public PointD Centroid()
    {
        // from http://stackoverflow.com/a/2792459/33080
        PointD centroid = new(0, 0);
        var signedArea = 0.0;
        double x0; // Current vertex X
        double y0; // Current vertex Y
        double x1; // Next vertex X
        double y1; // Next vertex Y
        double a;  // Partial signed area

        // For all vertices except last
        var i = 0;
        for (; i < _vertices.Count - 1; ++i)
        {
            x0 = _vertices[i].X;
            y0 = _vertices[i].Y;
            x1 = _vertices[i + 1].X;
            y1 = _vertices[i + 1].Y;
            a = x0 * y1 - x1 * y0;
            signedArea += a;
            centroid.X += (x0 + x1) * a;
            centroid.Y += (y0 + y1) * a;
        }

        // Do last vertex
        x0 = _vertices[i].X;
        y0 = _vertices[i].Y;
        x1 = _vertices[0].X;
        y1 = _vertices[0].Y;
        a = x0 * y1 - x1 * y0;
        signedArea += a;
        centroid.X += (x0 + x1) * a;
        centroid.Y += (y0 + y1) * a;

        signedArea *= 0.5;
        centroid.X /= (6.0 * signedArea);
        centroid.Y /= (6.0 * signedArea);

        return centroid;
    }

    /// <summary>
    ///     Determines whether this polygon is convex or concave. Throws if all vertices lie on a straight line, or if there
    ///     are 2 or fewer vertices.</summary>
    public bool IsConvex()
    {
        if (_vertices.Count <= 2)
            throw new InvalidOperationException();
        bool? crossPositive = null;
        for (var i = 0; i < _vertices.Count; i++)
        {
            var pt0 = i == 0 ? _vertices[_vertices.Count - 1] : _vertices[i - 1];
            var pt1 = _vertices[i];
            var pt2 = i == _vertices.Count - 1 ? _vertices[0] : _vertices[i + 1];
            var crossZ = (pt1 - pt0).CrossZ(pt2 - pt1);
            if (crossZ != 0)
            {
                if (crossPositive == null)
                    crossPositive = crossZ > 0;
                else if (crossPositive != crossZ > 0)
                    return false;
            }
        }
        return crossPositive != null ? true : throw new InvalidOperationException("All polygon points lie on a straight line.");
    }

    /// <summary>Returns the bounding box of this polygon.</summary>
    public BoundingBoxD BoundingBox() => new()
    {
        Xmin = _vertices.Min(v => v.X),
        Xmax = _vertices.Max(v => v.X),
        Ymin = _vertices.Min(v => v.Y),
        Ymax = _vertices.Max(v => v.Y)
    };

    /// <summary>Returns an array containing all the edges of this polygon.</summary>
    public IEnumerable<EdgeD> ToEdges()
    {
        int i;
        for (i = 0; i < _vertices.Count - 1; i++)
            yield return new EdgeD(_vertices[i], _vertices[i + 1]);
        yield return new EdgeD(_vertices[i], _vertices[0]);
    }

    /// <summary>
    ///     Determines whether the specified points constitute one of the edges of the polygon in either direction.</summary>
    /// <param name="start">
    ///     Start point of an edge.</param>
    /// <param name="end">
    ///     End point of an edge.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="start"/> is one of the polygon’s vertices and <paramref name="end"/> is either the
    ///     one immediately following it or the one immediately preceding it; <c>false</c> otherwise.</returns>
    public bool ContainsEdge(PointD start, PointD end)
    {
        for (var i = 0; i < _vertices.Count; i++)
            if ((_vertices[i] == start && _vertices[(i + 1) % _vertices.Count] == end) || (_vertices[i] == end && _vertices[(i + 1) % _vertices.Count] == start))
                return true;
        return false;
    }

    /// <summary>
    ///     Determines whether the specified edge constitutes one of the edges of the polygon exactly.</summary>
    /// <param name="edge">
    ///     Edge to check for.</param>
    /// <returns>
    ///     <c>true</c> if the edge’s start point is one of the polygon’s vertices and the edge’s end point is either the one
    ///     immediately following it or the one immediately preceding it; <c>false</c> otherwise.</returns>
    public bool ContainsEdge(EdgeD edge) => ContainsEdge(edge.Start, edge.End);
}
