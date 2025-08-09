using RT.Internal;

namespace RT.Geometry;

/// <summary>Triangulation-related routines.</summary>
public static class Triangulate
{
    /// <summary>
    ///     Generates a Delaunay triangulation of the input points.</summary>
    /// <param name="vertices">
    ///     Input points to triangulate. Must not contain duplicates.</param>
    /// <returns>
    ///     A list of edges in the triangulation, as pairs of indices into <paramref name="vertices"/>. The indices within
    ///     each pair, as well as the pairs themselves, are ordered arbitrarily.</returns>
    public static IEnumerable<(int v1, int v2)> DelaunayEdges(IList<PointD> vertices)
    {
        if (vertices.Count <= 1)
            return [];
        return VoronoiDiagram.Triangulate(vertices);
    }

    /// <summary>
    ///     Generates a Delaunay triangulation of the input points. Ensures that <paramref name="requiredEdges"/> are all
    ///     present in the triangulation, by inserting new vertices as necessary.</summary>
    /// <param name="vertices">
    ///     Input points to triangulate. Must not contain duplicates. New vertices are added <b>in place</b> at the end of the
    ///     list.</param>
    /// <param name="requiredEdges">
    ///     Edges that must be present in the triangulation. This constraint is achieved by splitting required edges, which
    ///     updates this set <b>in place</b>. Expects a single pair per edge, in any order. Required edges must not intersect.</param>
    /// <param name="splitEdges">
    ///     Optional empty dictionary which gets populated as a lookup from each final split edge to the corresponding
    ///     original required edge.</param>
    /// <returns>
    ///     A list of edges in the triangulation, as pairs of indices into <paramref name="vertices"/>. The indices within
    ///     each pair, as well as the pairs themselves, are ordered arbitrarily.</returns>
    /// <remarks>
    ///     Does not guarantee that the set of new vertices is minimal. This is not a fast way to perform constrained
    ///     triangulation.</remarks>
    public static IEnumerable<(int v1, int v2)>
        DelaunayEdgesConstrained(List<PointD> vertices, HashSet<(int v1, int v2)> requiredEdges, Dictionary<(int v1, int v2), (int v1, int v2)> splitEdges = null)
    {
        if (requiredEdges.Any(e => e.v1 < 0 || e.v1 >= vertices.Count || e.v2 < 0 || e.v2 >= vertices.Count))
            throw new ArgumentException($"A required edge references a vertex outside of '{nameof(vertices)}'.", nameof(requiredEdges));
        if (requiredEdges.Any(e => e.v1 == e.v2))
            throw new ArgumentException("Invalid required edge links a vertex to itself.", nameof(requiredEdges));

        // This algorithm re-triangulates the whole input every time edges are split, which is slow. It's possible to re-triangulate only the affected triangles, but that's more complicated.
        while (true)
        {
            var edges = DelaunayEdges(vertices);
            var edgesSet = new HashSet<(int v1, int v2)>(edges);
            var toSplit = requiredEdges.Where(e => !edgesSet.Contains(e) && !edgesSet.Contains((e.v2, e.v1))).ToList();
            if (toSplit.Count == 0)
                return edges;
            foreach (var splitEdge in toSplit)
            {
                vertices.Add((vertices[splitEdge.v1] + vertices[splitEdge.v2]) / 2);
                GeomUt.Assert(requiredEdges.Remove(splitEdge));
                requiredEdges.Add((splitEdge.v1, vertices.Count - 1));
                requiredEdges.Add((vertices.Count - 1, splitEdge.v2));
                if (splitEdges != null)
                {
                    var originalEdge = splitEdge;
                    if (splitEdges.TryGetValue(splitEdge, out originalEdge))
                        splitEdges.Remove(splitEdge); // this was a split edge that got split again
                    splitEdges[(splitEdge.v1, vertices.Count - 1)] = originalEdge;
                    splitEdges[(vertices.Count - 1, splitEdge.v2)] = originalEdge;
                }
            }
        }
    }

    /// <summary>
    ///     Generates a Delaunay triangulation of the specified polygons, supporting arbitrary nesting of islands and holes,
    ///     but not supporting self-intersections. Additional vertices may be inserted only on the polygon edge, to maintain
    ///     the Delaunay property. Unless a single convex polygon is passed in, the result includes edges outside the area
    ///     defined by the polygons.</summary>
    /// <param name="polygons">
    ///     Polygons to triangulate.</param>
    /// <returns>
    ///     <para>
    ///         <c>vertices</c>: all vertices referenced by the edge pairs, starting with original polygon vertices in order,
    ///         with additional vertices resulting from polygon edge splits appended at the end.</para>
    ///     <para>
    ///         <c>edges</c>: all edges in the triangulation, represented as pairs of indices into <c>vertices</c>; one entry
    ///         per pair of vertices, ordered arbitrarily.</para>
    ///     <para>
    ///         <c>polygonEdges</c>: all edges that comprise the original polygon borders. This is similar to a concatenation
    ///         of all edges of <paramref name="polygons"/>, differing only in that some edges become split to maintain the
    ///         Delaunay property.</para></returns>
    public static (List<PointD> vertices, IEnumerable<(int v1, int v2)> edges, HashSet<(int v1, int v2)> polygonEdges)
        DelaunayEdgesConstrained(IEnumerable<PolygonD> polygons)
    {
        var vertices = new List<PointD>();
        var polygonEdges = new HashSet<(int v1, int v2)>();
        foreach (var polygon in polygons)
        {
            for (int i = 0; i < polygon.Vertices.Count; i++)
                polygonEdges.Add((vertices.Count + i, vertices.Count + (i + 1) % polygon.Vertices.Count));
            vertices.AddRange(polygon.Vertices);
        }
        var edges = DelaunayEdgesConstrained(vertices, polygonEdges);
        return (vertices, edges, polygonEdges);
    }

    /// <summary>
    ///     Generates a Delaunay triangulation of the input points.</summary>
    /// <param name="vertices">
    ///     Input points to triangulate. Must not contain duplicates.</param>
    /// <param name="reverseOrder">
    ///     When false, triangle vertices are ordered by increasing angle, otherwise by decreasing angle.</param>
    /// <returns>
    ///     A list of triangles.</returns>
    public static IEnumerable<TriangleD> DelaunayTriangles(IList<PointD> vertices, bool reverseOrder = false)
    {
        if (vertices.Count < 3)
            return [];
        var edges = DelaunayEdges(vertices);
        return TrianglesFromEdges(vertices, edges, reverseOrder);
    }

    /// <summary>
    ///     Generates a Delaunay triangulation of the specified polygons. The triangles cover the inside area defined by the
    ///     polygons, supporting arbitrary nesting of islands and holes, but not supporting self-intersections. Additional
    ///     vertices may be inserted only on the polygon edge, to maintain the Delaunay property.</summary>
    /// <param name="polygons">
    ///     Polygons to triangulate.</param>
    /// <param name="reverseOrder">
    ///     When false, triangle vertices are ordered by increasing angle, otherwise by decreasing angle.</param>
    /// <returns>
    ///     A list of triangles.</returns>
    public static List<TriangleD> DelaunayTriangles(IEnumerable<PolygonD> polygons, bool reverseOrder = false)
    {
        var (vertices, edges, polygonEdges) = DelaunayEdgesConstrained(polygons);
        return TrianglesFromEdges(vertices, edges, reverseOrder)
            .Where(t => polygons.Count(p => p.ContainsPoint(t.Centroid)) % 2 == 1).ToList();
    }

    /// <summary>
    ///     Finds triangles in a valid triangulation defined by a list of edges.</summary>
    /// <param name="vertices">
    ///     Vertices of the graph.</param>
    /// <param name="edges">
    ///     Edges between <paramref name="vertices"/>, by index. Edge direction does not matter; there must be at most one
    ///     edge between any pair of vertices.</param>
    /// <param name="reverseOrder">
    ///     When false, triangle vertices are ordered by increasing angle, otherwise by decreasing angle.</param>
    /// <returns>
    ///     A list of triangles, each represented by its three nodes, in arbitrary order.</returns>
    /// <returns>
    ///     A list of triangle vertices.</returns>
    public static List<(int v1, int v2, int v3)> TriangleVerticesFromEdges(IList<PointD> vertices, IEnumerable<(int v1, int v2)> edges, bool reverseOrder = false)
    {
        var adjacent = new Dictionary<int, List<int>>(); // List is faster than HashSet for typical data
        foreach (var e in edges.SelectMany(e => new[] { e, (e.v2, e.v1) }))
        {
            if (!adjacent.TryGetValue(e.Item1, out var set))
                adjacent.Add(e.Item1, set = new List<int>());
            set.Add(e.Item2);
        }
        if (adjacent.Count == 0)
            return [];

        var finished = new HashSet<int>(); // there can be no more triangles containing these vertices
        var triangles = new List<(int v1, int v2, int v3)>();
        foreach (var adj in adjacent)
        {
            var cv = adj.Key;
            var adjacent_cv = adj.Value;
            finished.Add(cv);
            // Sort adjacent vertices by angle (including finished, since we want to check for being consecutive)
            adjacent_cv = adjacent_cv.OrderBy(v =>
            {
                var dx = vertices[v].X - vertices[cv].X;
                var dy = vertices[v].Y - vertices[cv].Y;
                var p = dy / (Math.Abs(dx) + Math.Abs(dy));
                if (dx < 0) p = 2 - p;
                return p; // pseudo-angle: https://stackoverflow.com/a/16542043/33080
            }).ToList();
            // Consider all adjacent vertices that aren't finished
            for (int nv1i = 0; nv1i < adjacent_cv.Count; nv1i++)
            {
                var nv1 = adjacent_cv[nv1i];
                if (finished.Contains(nv1))
                    continue;
                // To form a valid triangle with cv and nv1, the third vertex must be consecutive by angle
                // We hit each triangle twice, in both vertex orders; we only consider the order specified by reverseOrder
                var nv2i = (!reverseOrder ? (nv1i + 1) : (nv1i - 1 + adjacent_cv.Count)) % adjacent_cv.Count;
                var nv2 = adjacent_cv[nv2i];
                // nv2 is definitely adjacent to cv; it must also be adjacent to nv1, and not finished
                if (!adjacent[nv1].Contains(nv2) || finished.Contains(nv2))
                    continue;
                // it's possible that nv1 and nv2 are consecutive but exceeding 180deg (for example, a triangle on the "edge" of the triangulation)
                if (Math.Sign((vertices[nv1] - vertices[cv]).CrossZ(vertices[nv2] - vertices[cv])) != (!reverseOrder ? 1 : -1))
                    continue; // this test is rare enough not to impact performance - unlike any potential changes to adjacent_cv to record a break in being consecutive
                triangles.Add((cv, nv1, nv2));
            }
        }
        return triangles;
    }

    /// <summary>
    ///     Generates triangles from a valid triangulation defined by a list of edges.</summary>
    /// <param name="vertices">
    ///     Vertices of the graph.</param>
    /// <param name="edges">
    ///     Edges between <paramref name="vertices"/>, by index. Edge direction does not matter; there must be at most one
    ///     edge between any pair of vertices.</param>
    /// <param name="reverseOrder">
    ///     When false, triangle vertices are ordered by increasing angle, otherwise by decreasing angle.</param>
    /// <returns>
    ///     A list of triangles.</returns>
    public static IEnumerable<TriangleD> TrianglesFromEdges(IList<PointD> vertices, IEnumerable<(int v1, int v2)> edges, bool reverseOrder = false)
    {
        return TriangleVerticesFromEdges(vertices, edges, reverseOrder)
            .Select(t => new TriangleD(vertices[t.v1], vertices[t.v2], vertices[t.v3]));
    }
}
