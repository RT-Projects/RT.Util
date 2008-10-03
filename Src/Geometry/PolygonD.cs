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
        private List<PointD> FVertices;

        /// <summary>Returns a list of vertices of the polygon.</summary>
        public List<PointD> Vertices { get { return FVertices; } }

        /// <summary>Initialises a polygon from a given list of vertices.</summary>
        /// <param name="Vertices">Vertices (corner points) to initialise polygon from.</param>
        public PolygonD(IEnumerable<PointD> Vertices)
        {
            FVertices = new List<PointD>(Vertices);
        }

        /// <summary>
        /// Determines whether the current <see cref="PolygonD"/> contains the specified point.
        /// </summary>
        /// <param name="Point">Point to check.</param>
        /// <returns>True if the specified point lies inside the current <see cref="PolygonD"/>.</returns>
        public bool ContainsPoint(Point Point)
        {
            return ContainsPoint(new PointD(Point.X, Point.Y));
        }

        /// <summary>
        /// Determines whether the current <see cref="PolygonD"/> contains the specified point.
        /// </summary>
        /// <param name="Point">Point to check.</param>
        /// <returns>True if the specified point lies inside the current <see cref="PolygonD"/>.</returns>
        public bool ContainsPoint(PointD Point)
        {
            bool c = false;
            PointD p = FVertices[FVertices.Count - 1];
            foreach (PointD q in FVertices)
            {
                if ((((q.Y <= Point.Y) && (Point.Y < p.Y)) ||
                     ((p.Y <= Point.Y) && (Point.Y < q.Y))) &&
                    (Point.X < (p.X - q.X) * (Point.Y - q.Y) / (p.Y - q.Y) + q.X))
                    c = !c;
                p = q;
            }
            return c;
        }

        /// <summary>Determines the area of the current <see cref="PolygonD"/>.</summary>
        /// <returns>The area of the current <see cref="PolygonD"/> in square units.</returns>
        public double Area()
        {
            double Area = 0;
            PointD p = FVertices[FVertices.Count - 1];
            foreach (PointD q in FVertices)
            {
                Area += q.Y * p.X - q.X * p.Y;
                p = q;
            }
            return Area / 2;
        }

        /// <summary>
        /// Converts the current <see cref="PolygonD"/> to an array of <see cref="PointF"/> structures.
        /// Note that this conversion loses precision.
        /// </summary>
        /// <returns>Array of converted vertices with lower precision.</returns>
        public PointF[] ToPointFArray()
        {
            return FVertices.Select(x => x.ToPointF()).ToArray();
        }
    }
}
