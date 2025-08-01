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
    public static IEnumerable<(int vertexA, int vertexB)> DelaunayEdges(PointD[] vertices)
    {
        if (vertices.Length <= 1)
            return [];
        return VoronoiDiagram.Triangulate(vertices);
    }

    /// <summary>
    ///     Generates a Delaunay triangulation of the input points. Ensures that the specified edges are all present in the
    ///     triangulation, by inserting new vertices as necessary.</summary>
    /// <param name="vertices">
    ///     Input points to triangulate. Must not contain duplicates.</param>
    /// <param name="requiredEdges">
    ///     Required edges must not intersect.</param>
    /// <returns>
    ///     <para>
    ///         <c>Edges</c>: A list of edges in the triangulation, as pairs of indices into <paramref name="vertices"/>. The
    ///         indices within each pair, as well as the pairs themselves, are ordered arbitrarily.</para>
    ///     <para>
    ///         <c>NewVertices</c>: a replacement for <paramref name="vertices"/>, made of the original list of vertices in
    ///         the original order, plus newly added vertices to satisfy <paramref name="requiredEdges"/>.</para></returns>
    /// <remarks>
    ///     Does not guarantee that the set of new vertices is minimal. Does not track which edges were split to produce the
    ///     new vertices (easy to fix if needed).</remarks>
    public static (IEnumerable<(int vertexA, int vertexB)> edges, List<PointD> newVertices)
        DelaunayEdgesConstrained(PointD[] vertices, IEnumerable<(int vertexA, int vertexB)> requiredEdges)
    {
        if (requiredEdges.Any(e => e.vertexA < 0 || e.vertexA >= vertices.Length || e.vertexB < 0 || e.vertexB >= vertices.Length))
            throw new ArgumentException($"A required edge references a vertex outside of '{nameof(vertices)}'.", nameof(requiredEdges));
        if (requiredEdges.Any(e => e.vertexA == e.vertexB))
            throw new ArgumentException("Invalid required edge links a vertex to itself.", nameof(requiredEdges));

        // This algorithm re-triangulates the whole input every time edges are split, which is slow. It's possible to re-triangulate only the affected triangles, but that's more complicated.
        var allVertices = vertices.ToList();
        var requiredEdgesSet = new HashSet<(int vertexA, int vertexB)>(requiredEdges);
        while (true)
        {
            var edges = DelaunayEdges(allVertices.ToArray());
            var adjacent = edges.SelectMany(e => new[] { e, (e.vertexB, e.vertexA) }).ToLookup(e => e.Item1, e => e.Item2);
            var splitEdges = requiredEdgesSet.Where(e => !adjacent[e.vertexA].Contains(e.vertexB)).ToList();
            if (splitEdges.Count == 0)
                return (edges, allVertices);
            // Another suboptimal part: adjacency lookup takes as much as 5% of the total time, but we discard it on return. It will be needed again to produce triangles from edges.
            foreach (var se in splitEdges)
            {
                allVertices.Add((allVertices[se.vertexA] + allVertices[se.vertexB]) / 2);
                requiredEdgesSet.Remove(se);
                requiredEdgesSet.Add((se.vertexA, allVertices.Count - 1));
                requiredEdgesSet.Add((allVertices.Count - 1, se.vertexB));
            }
        }
    }

    /// <summary>
    ///     Generates a Delaunay triangulation of the input points.</summary>
    /// <param name="vertices">
    ///     Input points to triangulate. Must not contain duplicates.</param>
    /// <param name="reverseOrder">
    ///     When true, triangle vertices are ordered by increasing angle, otherwise by decreasing angle.</param>
    /// <returns>
    ///     A list of triangles.</returns>
    public static IEnumerable<TriangleD> DelaunayTriangles(PointD[] vertices, bool reverseOrder = false)
    {
        if (vertices.Length < 3)
            return [];
        var edges = DelaunayEdges(vertices);
        return TrianglesFromEdges(vertices, edges, reverseOrder);
    }

    /// <summary>
    ///     Finds triangles in a valid triangulation defined by a list of edges.</summary>
    /// <param name="edges">
    ///     A list of edges between numbered nodes. Edge direction does not matter; there must be at most one edge between any
    ///     pair of nodes.</param>
    /// <returns>
    ///     A list of triangles, each represented by its three nodes, in arbitrary order.</returns>
    /// <remarks>
    ///     Currently only supports "full" triangulations, where every triangle is reachable from every other triangle via
    ///     shared edges. Can be adapted to be less strict, by continuing from an unprocessed edge until all vertices have
    ///     been visited.</remarks>
    public static IEnumerable<(int v1, int v2, int v3)> TriangleVerticesFromEdges(IEnumerable<(int vertexA, int vertexB)> edges)
    {
        var adjacent = new Dictionary<int, HashSet<int>>();
        foreach (var e in edges.SelectMany(e => new[] { e, (e.vertexB, e.vertexA) }))
        {
            if (!adjacent.TryGetValue(e.Item1, out var set))
                adjacent.Add(e.Item1, set = new HashSet<int>());
            set.Add(e.Item2);
        }
        if (adjacent.Count == 0)
            return [];

        var todo = new Queue<int>(adjacent.Keys);
        var finished = new HashSet<int>(); // there can be no more triangles containing these vertices
        var triangles = new List<(int v1, int v2, int v3)>();
        while (finished.Count < adjacent.Count)
        {
            var cv = todo.Dequeue();
            finished.Add(cv);
            // Consider all adjacent vertices that aren't finished
            foreach (var nv1 in adjacent[cv])
            {
                if (finished.Contains(nv1))
                    continue;
                // Is it connected to any other unfinished vertices adjacent to the current vertex?
                foreach (var nv2 in adjacent[nv1] /*always exists*/)
                {
                    if (!adjacent[cv].Contains(nv2) || finished.Contains(nv2))
                        continue;
                    // nv1 and nv2 are not finished, adjacent to cv, and to each other
                    // Add triangle but only if nv1 < nv2, as we'll discover each such traingle twice in this loop
                    if (nv1 < nv2)
                        triangles.Add((cv, nv1, nv2));
                }
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
    ///     When true, triangle vertices are ordered by increasing angle, otherwise by decreasing angle.</param>
    /// <returns>
    ///     A list of triangle vertices.</returns>
    /// <remarks>
    ///     Behaviour is undefined if the input graph contains any faces that are not triangles. Only supports "full"
    ///     triangulations, where every triangle is reachable from every other triangle via shared edges (easy to fix if
    ///     needed).</remarks>
    public static IEnumerable<(int v1, int v2, int v3)> TriangleVerticesFromEdges(PointD[] vertices, IEnumerable<(int vertexA, int vertexB)> edges, bool reverseOrder = false)
    {
        var wantSign = reverseOrder ? -1 : 1;
        return TriangleVerticesFromEdges(edges).Select(t =>
            wantSign == Math.Sign((vertices[t.v2] - vertices[t.v1]).CrossZ(vertices[t.v3] - vertices[t.v1])) ? t : (t.v1, t.v3, t.v2));
    }

    /// <summary>
    ///     Generates triangles from a valid triangulation defined by a list of edges.</summary>
    /// <param name="vertices">
    ///     Vertices of the graph.</param>
    /// <param name="edges">
    ///     Edges between <paramref name="vertices"/>, by index. Edge direction does not matter; there must be at most one
    ///     edge between any pair of vertices.</param>
    /// <param name="reverseOrder">
    ///     When true, triangle vertices are ordered by increasing angle, otherwise by decreasing angle.</param>
    /// <returns>
    ///     A list of triangles.</returns>
    /// <remarks>
    ///     Behaviour is undefined if the input graph contains any faces that are not triangles. Only supports "full"
    ///     triangulations, where every triangle is reachable from every other triangle via shared edges (easy to fix if
    ///     needed).</remarks>
    public static IEnumerable<TriangleD> TrianglesFromEdges(PointD[] vertices, IEnumerable<(int vertexA, int vertexB)> edges, bool reverseOrder = false)
    {
        return TriangleVerticesFromEdges(vertices, edges, reverseOrder)
            .Select(t => new TriangleD(vertices[t.v1], vertices[t.v2], vertices[t.v3]));
    }
}
