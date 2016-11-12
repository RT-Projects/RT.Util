using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace RT.Util.Geometry
{
    /// <summary>This class encapsulates double-precision polygons.</summary>
    public sealed class PolygonD
    {
        private List<PointD> _vertices;

        /// <summary>Returns a list of vertices of the polygon.</summary>
        public List<PointD> Vertices { get { return _vertices; } }

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
        ///     Determines whether the current <see cref="PolygonD"/> contains the specified point.</summary>
        /// <param name="point">
        ///     Point to check.</param>
        /// <returns>
        ///     True if the specified point lies inside the current <see cref="PolygonD"/>.</returns>
        public bool ContainsPoint(Point point)
        {
            return ContainsPoint(new PointD(point.X, point.Y));
        }

        /// <summary>
        ///     Determines whether the current <see cref="PolygonD"/> contains the specified point. If the point lies exactly
        ///     on one of the polygon edges, it is considered to be contained in the polygon.</summary>
        /// <param name="point">
        ///     Point to check.</param>
        /// <returns>
        ///     True if the specified point lies inside the current <see cref="PolygonD"/>.</returns>
        public bool ContainsPoint(PointD point)
        {
            foreach (var edge in ToEdges())
                if (edge.ContainsPoint(point))
                    return true;
            bool c = false;
            PointD p = _vertices[_vertices.Count - 1];
            foreach (PointD q in _vertices)
            {
                if ((((q.Y <= point.Y) && (point.Y < p.Y)) ||
                     ((p.Y <= point.Y) && (point.Y < q.Y))) &&
                    (point.X < (p.X - q.X) * (point.Y - q.Y) / (p.Y - q.Y) + q.X))
                    c = !c;
                p = q;
            }
            return c;
        }

        /// <summary>
        ///     Determines the area of the current <see cref="PolygonD"/>.</summary>
        /// <returns>
        ///     The area of the current <see cref="PolygonD"/> in square units.</returns>
        public double Area()
        {
            double area = 0;
            PointD p = _vertices[_vertices.Count - 1];
            foreach (PointD q in _vertices)
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
        public PointF[] ToPointFArray()
        {
            return _vertices.Select(x => x.ToPointF()).ToArray();
        }

        /// <summary>Calculates the centroid of this polygon.</summary>
        public PointD Centroid()
        {
            // from http://stackoverflow.com/a/2792459/33080
            PointD centroid = new PointD(0, 0);
            double signedArea = 0.0;
            double x0 = 0.0; // Current vertex X
            double y0 = 0.0; // Current vertex Y
            double x1 = 0.0; // Next vertex X
            double y1 = 0.0; // Next vertex Y
            double a = 0.0;  // Partial signed area

            // For all vertices except last
            int i = 0;
            for (i = 0; i < _vertices.Count - 1; ++i)
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
        ///     Determines whether this polygon is convex or concave. Throws if all vertices lie on a straight line, or if
        ///     there are 2 or fewer vertices.</summary>
        public bool IsConvex()
        {
            if (_vertices.Count <= 2)
                throw new InvalidOperationException();
            bool? crossPositive = null;
            for (int i = 0; i < _vertices.Count; i++)
            {
                var pt0 = i == 0 ? _vertices[_vertices.Count - 1] : _vertices[i - 1];
                var pt1 = _vertices[i];
                var pt2 = i == _vertices.Count - 1 ? _vertices[0] : _vertices[i + 1];
                double crossZ = (pt1 - pt0).CrossZ(pt2 - pt1);
                if (crossZ != 0)
                {
                    if (crossPositive == null)
                        crossPositive = crossZ > 0;
                    else if (crossPositive != crossZ > 0)
                        return false;
                }
            }
            if (crossPositive == null)
                throw new InvalidOperationException("All polygon points lie on a straight line.");
            return true;
        }

        /// <summary>Returns the bounding box of this polygon.</summary>
        public BoundingBoxD BoundingBox()
        {
            return new BoundingBoxD
            {
                Xmin = _vertices.Min(v => v.X),
                Xmax = _vertices.Max(v => v.X),
                Ymin = _vertices.Min(v => v.Y),
                Ymax = _vertices.Max(v => v.Y)
            };
        }

        /// <summary>Returns an array containing all the edges of this polygon.</summary>
        public EdgeD[] ToEdges()
        {
            var edges = new EdgeD[_vertices.Count];
            int i;
            for (i = 0; i < _vertices.Count - 1; i++)
                edges[i] = new EdgeD(_vertices[i], _vertices[i + 1]);
            edges[i] = new EdgeD(_vertices[i], _vertices[0]);
            return edges;
        }
    }
}
