using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Linq;

namespace RT.Util.Geometry
{
    /// <summary>
    /// This class encapsulates double-precision polygons.
    /// </summary>
    public class PolygonD
    {
        private List<PointD> _vertices;

        /// <summary>Returns a list of vertices of the polygon.</summary>
        public List<PointD> Vertices { get { return _vertices; } }

        /// <summary>Initialises a polygon from a given list of vertices.</summary>
        /// <param name="vertices">Vertices (corner points) to initialise polygon from.</param>
        public PolygonD(IEnumerable<PointD> vertices)
        {
            _vertices = new List<PointD>(vertices);
        }

        /// <summary>
        /// Determines whether the current <see cref="PolygonD"/> contains the specified point.
        /// </summary>
        /// <param name="point">Point to check.</param>
        /// <returns>True if the specified point lies inside the current <see cref="PolygonD"/>.</returns>
        public bool ContainsPoint(Point point)
        {
            return ContainsPoint(new PointD(point.X, point.Y));
        }

        /// <summary>
        /// Determines whether the current <see cref="PolygonD"/> contains the specified point.
        /// </summary>
        /// <param name="point">Point to check.</param>
        /// <returns>True if the specified point lies inside the current <see cref="PolygonD"/>.</returns>
        public bool ContainsPoint(PointD point)
        {
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

        /// <summary>Determines the area of the current <see cref="PolygonD"/>.</summary>
        /// <returns>The area of the current <see cref="PolygonD"/> in square units.</returns>
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
        /// Converts the current <see cref="PolygonD"/> to an array of <see cref="PointF"/> structures.
        /// Note that this conversion loses precision.
        /// </summary>
        /// <returns>Array of converted vertices with lower precision.</returns>
        public PointF[] ToPointFArray()
        {
            return _vertices.Select(x => x.ToPointF()).ToArray();
        }
    }
}
