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
    }
}
