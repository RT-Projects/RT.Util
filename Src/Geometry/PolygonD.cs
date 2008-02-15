using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

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

        public bool ContainsPoint(Point Point)
        {
            return ContainsPoint(new PointD(Point.X, Point.Y));
        }

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

        public PointF[] ToPointFArray()
        {
            PointF[] PointFArray = new PointF[FVertices.Count];
            for (int i = 0; i < FVertices.Count; i++)
                PointFArray[i] = FVertices[i].ToPointF();
            return PointFArray;
        }
    }
}
