using System;

namespace RT.Util.Geometry
{
    /// <summary>Represents a bounding box, in terms of the minimal and maximal X and Y coordinates.</summary>
    public struct BoundingBoxD
    {
        /// <summary>The smallest X coordinate.</summary>
        public double Xmin;
        /// <summary>The largest X coordinate.</summary>
        public double Xmax;
        /// <summary>The smallest Y coordinate.</summary>
        public double Ymin;
        /// <summary>The largest Y coordinate.</summary>
        public double Ymax;

        /// <summary>
        ///     Gets the difference between the larger and the smaller X limits of the box, i.e. the width of the bounding
        ///     box.</summary>
        public double Width { get { return Xmax - Xmin; } }
        /// <summary>
        ///     Gets the difference between the larger and the smaller Y limits of the box, i.e. the height of the bounding
        ///     box.</summary>
        public double Height { get { return Ymax - Ymin; } }

        /// <summary>Returns a new BoundingBox bounding a single point.</summary>
        public static BoundingBoxD FromPoint(double x, double y)
        {
            BoundingBoxD box = new BoundingBoxD();
            box.Xmin = box.Xmax = x;
            box.Ymin = box.Ymax = y;
            return box;
        }

        /// <summary>Returns a new BoundingBox bounding a single point.</summary>
        public static BoundingBoxD FromPoint(ref PointD pt)
        {
            BoundingBoxD box = new BoundingBoxD();
            box.Xmin = box.Xmax = pt.X;
            box.Ymin = box.Ymax = pt.Y;
            return box;
        }

        /// <summary>Returns a new BoundingBox bounding the two points specified.</summary>
        public static BoundingBoxD FromPoint(double x1, double y1, double x2, double y2)
        {
            BoundingBoxD box = new BoundingBoxD();
            if (x1 > x2) { box.Xmin = x2; box.Xmax = x1; }
            else         { box.Xmin = x1; box.Xmax = x2; }
            if (y1 > y2) { box.Ymin = y2; box.Ymax = y1; }
            else         { box.Ymin = y1; box.Ymax = y2; }
            return box;
        }

        /// <summary>Returns a new BoundingBox bounding the two points specified.</summary>
        public static BoundingBoxD FromPoint(ref PointD pt1, ref PointD pt2)
        {
            BoundingBoxD box = new BoundingBoxD();
            if (pt1.X > pt2.X) { box.Xmin = pt2.X; box.Xmax = pt1.X; }
            else               { box.Xmin = pt1.X; box.Xmax = pt2.X; }
            if (pt1.Y > pt2.Y) { box.Ymin = pt2.Y; box.Ymax = pt1.Y; }
            else               { box.Ymin = pt1.Y; box.Ymax = pt2.Y; }
            return box;
        }

        /// <summary>Returns a new BoundingBox bounding the two points specified.</summary>
        public static BoundingBoxD FromPoint(PointD pt1, PointD pt2)
        {
            BoundingBoxD box = new BoundingBoxD();
            if (pt1.X > pt2.X) { box.Xmin = pt2.X; box.Xmax = pt1.X; }
            else { box.Xmin = pt1.X; box.Xmax = pt2.X; }
            if (pt1.Y > pt2.Y) { box.Ymin = pt2.Y; box.Ymax = pt1.Y; }
            else { box.Ymin = pt1.Y; box.Ymax = pt2.Y; }
            return box;
        }

        /// <summary>Returns a new BoundingBox bounding the specified edge.</summary>
        public static BoundingBoxD FromEdge(ref EdgeD edge)
        {
            return FromPoint(ref edge.Start, ref edge.End);
        }

        /// <summary>Returns a new BoundingBox bounding the specified edge.</summary>
        public static BoundingBoxD FromEdge(EdgeD edge)
        {
            return FromPoint(ref edge.Start, ref edge.End);
        }

        /// <summary>Returns a new BoundingBox bounding the specified circle.</summary>
        public static BoundingBoxD FromCircle(ref PointD center, double radius)
        {
            BoundingBoxD box = new BoundingBoxD();
            box.Xmin = center.X - radius;
            box.Xmax = center.X + radius;
            box.Ymin = center.Y - radius;
            box.Ymax = center.Y + radius;
            return box;
        }

        /// <summary>An empty bounding box - which doesn't have any bounds yet.</summary>
        public static readonly BoundingBoxD Empty;

        static BoundingBoxD()
        {
            Empty = new BoundingBoxD();
            Empty.Xmin = Empty.Xmax = Empty.Ymin = Empty.Ymax = double.NaN;
        }

        /// <summary>Updates the bounding box by extending the bounds, if necessary, to include the specified point.</summary>
        public void AddPoint(PointD point)
        {
            if (double.IsNaN(Xmin))
                this = FromPoint(ref point);
            else
            {
                Xmin = Math.Min(Xmin, point.X);
                Xmax = Math.Max(Xmax, point.X);
                Ymin = Math.Min(Ymin, point.Y);
                Ymax = Math.Max(Ymax, point.Y);
            }
        }

        /// <summary>Updates the bounding box by extending the bounds, if necessary, to include the specified circle.</summary>
        public void AddCircle(ref PointD center, double radius)
        {
            if (double.IsNaN(Xmin))
                this = FromCircle(ref center, radius);
            else
            {
                Xmin = Math.Min(Xmin, center.X - radius);
                Xmax = Math.Max(Xmax, center.X + radius);
                Ymin = Math.Min(Ymin, center.Y - radius);
                Ymax = Math.Max(Ymax, center.Y + radius);
            }
        }

        /// <summary>Returns true if this bounding box intersects with the specified ray.</summary>
        public bool IntersectsWithRay(EdgeD ray)
        {
            return Intersect.RayWithBoundingBox(ref ray, ref this);
        }

        /// <summary>Returns true if this bounding box intersects with the specified bounding box.</summary>
        public bool IntersectsWithBoundingBox(BoundingBoxD box)
        {
            return Intersect.BoundingBoxWithBoundingBox(ref this, ref box);
        }

        /// <summary>Returns true iff this bounding box contains the specified point.</summary>
        public bool ContainsPoint(ref PointD point)
        {
            return point.X >= Xmin && point.X <= Xmax && point.Y >= Ymin && point.Y <= Ymax;
        }
    }
}
