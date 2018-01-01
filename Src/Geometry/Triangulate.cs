using System;
using System.Collections.Generic;
using System.Linq;
using RT.Util.ExtensionMethods;

namespace RT.Util.Geometry
{
    /// <summary>Triangulation-related routines.</summary>
    public static class Triangulate
    {
        /// <summary>
        ///     Constructs a Delaunay triangulation of the <paramref name="vertices"/>.</summary>
        /// <remarks>
        ///     Adapted mainly from https://github.com/Bl4ckb0ne/delaunay-triangulation</remarks>
        public static IEnumerable<TriangleD> Delaunay(IEnumerable<PointD> vertices)
        {
            // Find a "super-triangle" which contains all vertices
            var minX = vertices.Min(v => v.X);
            var minY = vertices.Min(v => v.Y);
            var maxX = vertices.Max(v => v.X);
            var maxY = vertices.Max(v => v.Y);

            var deltaMax = Math.Max(maxX - minX, maxY - minY);
            var midx = (minX + maxX) / 2;
            var midy = (minY + maxY) / 2;

            var super = new TriangleD(
                new PointD(midx - 20 * deltaMax, midy - deltaMax),
                new PointD(midx, midy + 20 * deltaMax),
                new PointD(midx + 20 * deltaMax, midy - deltaMax));

            // Triangulation starts off with just the super-triangle
            var triangles = new List<TriangleD> { super };

            var polygon = new List<EdgeD>();
            foreach (var v in vertices)
            {
                triangles.RemoveAll(t =>
                {
                    if (!t.CircumcircleContains(v))
                        return false;
                    polygon.Add(new EdgeD(t.V1, t.V2));
                    polygon.Add(new EdgeD(t.V2, t.V3));
                    polygon.Add(new EdgeD(t.V3, t.V1));
                    return true;
                });

                // Remove all edges from polygon which appear more than once
                polygon = polygon.Where((e1, i1) => !polygon.Where((e2, i2) => i1 != i2 && e1 == e2).Any()).ToList();

                // Add a triangle between this vertex and each edge of the polygon
                triangles.AddRange(polygon.Select(e => new TriangleD(e.Start, e.End, v)));

                polygon.Clear();
            }

            // Remove all triangles adjacent to the original super-triangle
            triangles.RemoveAll(t => t.HasVertex(super.V1) || t.HasVertex(super.V2) || t.HasVertex(super.V3));

            return triangles;
        }

        /// <summary>
        ///     Constructs a Delaunay-like triangulation constrained to contain all edges listed in <paramref
        ///     name="requiredEdges"/>. The implementation is naive and very slow for larger meshes, because instead of
        ///     incrementally re-triangulating only the affected triangles upon an edge split, it performs a full
        ///     triangulation every time.</summary>
        public static IEnumerable<TriangleD> DelaunayConstrained(IEnumerable<PointD> vertices, IEnumerable<EdgeD> requiredEdges)
        {
            if (requiredEdges.Any(e => !vertices.Contains(e.Start) || !vertices.Contains(e.End)))
                throw new ArgumentException("The end points of every required edge must be in the list of vertices to triangulate");
            if (requiredEdges.Any(e => e.Start == e.End))
                throw new ArgumentException("Required edges must have a non-zero length");

            var curVertices = vertices.ToHashSet();
            var requiredEdgeSet = requiredEdges.ToHashSet(); // to avoid missing edges due to floating point errors, we update the list every time we split a required edge
            while (true)
            {
                var triangulation = Triangulate.Delaunay(curVertices);
                var splitEdge = requiredEdgeSet.Where(re => !triangulation.Any(t => re == t.Edge12 || re == t.Edge23 || re == t.Edge31)).Cast<EdgeD?>().FirstOrDefault();
                if (splitEdge == null)
                    return triangulation;
                var newVertex = (splitEdge.Value.Start + splitEdge.Value.End) / 2;
                requiredEdgeSet.Remove(splitEdge.Value);
                requiredEdgeSet.Add(new EdgeD(splitEdge.Value.Start, newVertex));
                requiredEdgeSet.Add(new EdgeD(newVertex, splitEdge.Value.End));
                curVertices.Add(newVertex);
            }
        }
    }
}
