using System;
using System.Collections.Generic;
using System.Text;

namespace RT.Util.Geometry
{
    /// <summary>
    /// This class encapsulates double-precision polygons.
    /// </summary>
    public class PolygonD
    {
        private List<PointD> FVertices;

        public List<PointD> Vertices { get { return FVertices; } }

        public PolygonD(List<PointD> Vertices)
        {
            FVertices = Vertices;
        }

        public bool ContainsPoint(PointD Point)
        {
            int NumIntersect = 0;
            for (int i = 0; i < FVertices.Count; i++)
            {
                PointD p = FVertices[i];
                EdgeD Edge1 = new EdgeD(Point, new PointD(0, 0));
                EdgeD Edge2 = new EdgeD(p, FVertices[(i + 1) % FVertices.Count]);
                if (Edge1.IntersectsWith(Edge2))
                    NumIntersect++;
            }
            return NumIntersect % 2 == 1;
        }

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
    }
}
