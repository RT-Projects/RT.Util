namespace RT.Geometry;

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
    ///     Gets the difference between the larger and the smaller X limits of the box, i.e. the width of the bounding box.</summary>
    public readonly double Width => Xmax - Xmin;
    /// <summary>
    ///     Gets the difference between the larger and the smaller Y limits of the box, i.e. the height of the bounding box.</summary>
    public readonly double Height => Ymax - Ymin;

    /// <summary>Returns a new BoundingBox bounding a single point.</summary>
    public static BoundingBoxD FromPoint(double x, double y)
    {
        BoundingBoxD box = new();
        box.Xmin = box.Xmax = x;
        box.Ymin = box.Ymax = y;
        return box;
    }

    /// <summary>Returns a new BoundingBox bounding a single point.</summary>
    public static BoundingBoxD FromPoint(ref PointD pt)
    {
        BoundingBoxD box = new();
        box.Xmin = box.Xmax = pt.X;
        box.Ymin = box.Ymax = pt.Y;
        return box;
    }

    /// <summary>Returns a new BoundingBox bounding the two points specified.</summary>
    public static BoundingBoxD FromPoint(double x1, double y1, double x2, double y2)
    {
        BoundingBoxD box = new();
        if (x1 > x2) { box.Xmin = x2; box.Xmax = x1; }
        else { box.Xmin = x1; box.Xmax = x2; }
        if (y1 > y2) { box.Ymin = y2; box.Ymax = y1; }
        else { box.Ymin = y1; box.Ymax = y2; }
        return box;
    }

    /// <summary>Returns a new BoundingBox bounding the two points specified.</summary>
    public static BoundingBoxD FromPoint(ref PointD pt1, ref PointD pt2)
    {
        BoundingBoxD box = new();
        if (pt1.X > pt2.X) { box.Xmin = pt2.X; box.Xmax = pt1.X; }
        else { box.Xmin = pt1.X; box.Xmax = pt2.X; }
        if (pt1.Y > pt2.Y) { box.Ymin = pt2.Y; box.Ymax = pt1.Y; }
        else { box.Ymin = pt1.Y; box.Ymax = pt2.Y; }
        return box;
    }

    /// <summary>Returns a new BoundingBox bounding the two points specified.</summary>
    public static BoundingBoxD FromPoint(PointD pt1, PointD pt2)
    {
        BoundingBoxD box = new();
        if (pt1.X > pt2.X) { box.Xmin = pt2.X; box.Xmax = pt1.X; }
        else { box.Xmin = pt1.X; box.Xmax = pt2.X; }
        if (pt1.Y > pt2.Y) { box.Ymin = pt2.Y; box.Ymax = pt1.Y; }
        else { box.Ymin = pt1.Y; box.Ymax = pt2.Y; }
        return box;
    }

    /// <summary>Returns a new BoundingBox bounding all the points specified.</summary>
    public static BoundingBoxD FromPoint(IEnumerable<PointD> sites)
    {
        var result = new BoundingBoxD();
        result.AddPoint(sites);
        return result;
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
        BoundingBoxD box = new()
        {
            Xmin = center.X - radius,
            Xmax = center.X + radius,
            Ymin = center.Y - radius,
            Ymax = center.Y + radius
        };
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
    public void AddPoint(double x, double y)
    {
        if (double.IsNaN(Xmin))
            this = FromPoint(x, y);
        else
        {
            Xmin = Math.Min(Xmin, x);
            Xmax = Math.Max(Xmax, x);
            Ymin = Math.Min(Ymin, y);
            Ymax = Math.Max(Ymax, y);
        }
    }

    /// <summary>Updates the bounding box by extending the bounds, if necessary, to include the specified point.</summary>
    public void AddPoint(PointD point)
    {
        AddPoint(point.X, point.Y);
    }

    /// <summary>Updates the bounding box by extending the bounds, if necessary, to include the specified points.</summary>
    public void AddPoint(IEnumerable<PointD> points)
    {
        foreach (var pt in points)
            AddPoint(pt);
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

    /// <summary>Updates the bounding box by extending the bounds, if necessary, to include the specified bounding box.</summary>
    public void AddBoundingBox(BoundingBoxD box)
    {
        AddPoint(box.Xmin, box.Ymin);
        AddPoint(box.Xmax, box.Ymin);
        AddPoint(box.Xmax, box.Ymax);
        AddPoint(box.Xmin, box.Ymax);
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
    public readonly bool ContainsPoint(ref PointD point) => point.X >= Xmin && point.X <= Xmax && point.Y >= Ymin && point.Y <= Ymax;

    /// <summary>Returns an array of the four edges of this bounding box.</summary>
    public readonly EdgeD[] ToEdges() => [YminEdge(), XmaxEdge(), YmaxEdge(), XminEdge()];

    /// <summary>Returns the horizontal edge of this bounding box with the smallest Y coordinate.</summary>
    public readonly EdgeD YminEdge() => new(Xmin, Ymin, Xmax, Ymin);
    /// <summary>Returns the vertical edge of this bounding box with the largest X coordinate.</summary>
    public readonly EdgeD XmaxEdge() => new(Xmax, Ymin, Xmax, Ymax);
    /// <summary>Returns the horizontal edge of this bounding box with the largest Y coordinate.</summary>
    public readonly EdgeD YmaxEdge() => new(Xmax, Ymax, Xmin, Ymax);
    /// <summary>Returns the vertical edge of this bounding box with the smallest X coordinate.</summary>
    public readonly EdgeD XminEdge() => new(Xmin, Ymax, Xmin, Ymin);

    /// <summary>Returns an array of the four vertices of this bounding box.</summary>
    public readonly PointD[] ToVertices() => [new PointD(Xmin, Ymin), new PointD(Xmax, Ymin), new PointD(Xmax, Ymax), new PointD(Xmin, Ymax)];

    /// <summary>Converts this bounding box to a polygon.</summary>
    public readonly PolygonD ToPolygonD()
    {
        return new PolygonD(ToVertices());
    }

    /// <summary>Returns the area of this bounding box.</summary>
    public readonly double Area()
    {
        return Width * Height;
    }
}
