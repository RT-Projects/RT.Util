namespace RT.Util.Geometry;

/// <summary>
///     Encapsulates a triangle defined by vertices represented as <see cref="PointD"/> values.</summary>
/// <remarks>
///     Instantiates a new triangle with the specified vertices.</remarks>
public class TriangleD(PointD v1, PointD v2, PointD v3)
{
    /// <summary>Vertices defining the triangle.</summary>
    public PointD V1 = v1, V2 = v2, V3 = v3;

    /// <summary>Returns a string representation of the triangle.</summary>
    public override string ToString() => $"Δ {V1} : {V2} : {V3}";

    /// <summary>Gets the edge connecting vertices <see cref="V1"/> and <see cref="V2"/>.</summary>
    public EdgeD Edge12 => new(V1, V2);
    /// <summary>Gets the edge connecting vertices <see cref="V2"/> and <see cref="V3"/>.</summary>
    public EdgeD Edge23 => new(V2, V3);
    /// <summary>Gets the edge connecting vertices <see cref="V3"/> and <see cref="V1"/>.</summary>
    public EdgeD Edge31 => new(V3, V1);

    /// <summary>Returns a value indicating whether one of the triangle vertices is equal to <paramref name="v"/>.</summary>
    public bool HasVertex(PointD v) => v == V1 || v == V2 || v == V3;

    /// <summary>
    ///     Returns a value indicating whether one of the triangle edges is equal to <paramref name="e"/>. Edge equality is
    ///     direction-insensitive.</summary>
    public bool HasEdge(EdgeD e) => e == Edge12 || e == Edge23 || e == Edge31;

    /// <summary>Enumerates the vertices <see cref="V1"/>, <see cref="V2"/> and <see cref="V3"/>.</summary>
    public IEnumerable<PointD> Vertices
    {
        get
        {
            yield return V1;
            yield return V2;
            yield return V3;
        }
    }

    /// <summary>Enumerates the edges <see cref="Edge12"/>, <see cref="Edge23"/> and <see cref="Edge31"/>.</summary>
    public IEnumerable<EdgeD> Edges
    {
        get
        {
            yield return Edge12;
            yield return Edge23;
            yield return Edge31;
        }
    }

    /// <summary>
    ///     Gets the centroid of the triangle. This point is guaranteed to lie inside the triangle, and is the fastest way to
    ///     obtain a point lying inside the triangle.</summary>
    public PointD Centroid => (V1 + V2 + V3) / 3;

    /// <summary>Gets the circumcenter of the triangle, i.e. the center of the triangle's circumcircle.</summary>
    public PointD Circumcenter
    {
        get
        {
            double ab = (V1.X * V1.X) + (V1.Y * V1.Y);
            double cd = (V2.X * V2.X) + (V2.Y * V2.Y);
            double ef = (V3.X * V3.X) + (V3.Y * V3.Y);

            return new PointD(
                x: (ab * (V3.Y - V2.Y) + cd * (V1.Y - V3.Y) + ef * (V2.Y - V1.Y)) / (V1.X * (V3.Y - V2.Y) + V2.X * (V1.Y - V3.Y) + V3.X * (V2.Y - V1.Y)) / 2,
                y: (ab * (V3.X - V2.X) + cd * (V1.X - V3.X) + ef * (V2.X - V1.X)) / (V1.Y * (V3.X - V2.X) + V2.Y * (V1.X - V3.X) + V3.Y * (V2.X - V1.X)) / 2);
        }
    }

    /// <summary>Returns a value indicating whether the circumcircle of this triangle contains <paramref name="v"/>.</summary>
    public bool CircumcircleContains(PointD v)
    {
        var circle = Circumcircle;
        return (v - circle.Center).Distance() <= circle.Radius;
    }

    /// <summary>Returns the circumcircle of this triangle.</summary>
    public CircleD Circumcircle
    {
        get
        {
            var pt = Circumcenter;
            return new CircleD(pt, (V1 - pt).Distance());
        }
    }
}
