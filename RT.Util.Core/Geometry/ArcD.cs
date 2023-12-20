namespace RT.Util.Geometry;

/// <summary>Encapsulates a double-precision circular arc.</summary>
public struct ArcD
{
    /// <summary>The circle on which the arc lies.</summary>
    public CircleD Circle;
    /// <summary>The angle at which the arc starts.</summary>
    public double AngleStart;
    /// <summary>The angle which the arc sweeps.</summary>
    public double AngleSweep;
}
