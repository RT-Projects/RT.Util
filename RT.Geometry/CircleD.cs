using System.Collections.Generic;
using RT.Internal;

namespace RT.Geometry;

/// <summary>Encapsulates a double-precision circle.</summary>
public struct CircleD
{
    /// <summary>Center of the circle.</summary>
    public PointD Center;
    /// <summary>Radius of the circle.</summary>
    public double Radius;

    /// <summary>
    ///     Initialises a new <see cref="CircleD"/> with the specified center co-ordinates and radius.</summary>
    /// <param name="x">
    ///     Center X co-ordinate.</param>
    /// <param name="y">
    ///     Center Y co-ordinate.</param>
    /// <param name="rad">
    ///     Radius.</param>
    public CircleD(double x, double y, double rad)
    {
        Center = new PointD(x, y);
        Radius = rad;
    }

    /// <summary>
    ///     Initialises a new <see cref="CircleD"/> with the specified center co-ordinates and radius.</summary>
    /// <param name="center">
    ///     Center co-ordinates.</param>
    /// <param name="rad">
    ///     Radius.</param>
    public CircleD(PointD center, double rad)
    {
        Center = center;
        Radius = rad;
    }

    /// <summary>
    ///     Provides a string representation of the current <see cref="CircleD"/>.</summary>
    /// <returns>
    ///     A string representation of the current <see cref="CircleD"/>.</returns>
    public override string ToString()
    {
        return Center.ToString() + " / " + Radius;
    }

    /// <summary>
    ///     Given this circle and another circle, tries to find a third and fourth circle with a given target radius such that
    ///     the new circles are both tangent to the first two.</summary>
    /// <param name="other">
    ///     The other circle.</param>
    /// <param name="targetRadius">
    ///     Target radius for output circles.</param>
    /// <returns>
    ///     The two output circles if they exist. If the input circles are further apart than twice the target radius, the
    ///     desires circles do not exist and null is returned.</returns>
    public (CircleD, CircleD)? FindTangentCircles(CircleD other, double targetRadius)
    {
        double a = ((Center.X - other.Center.X) * (Center.X - other.Center.X)) /
                   ((other.Center.Y - Center.Y) * (other.Center.Y - Center.Y)) + 1;
        double t = Center.Y - (Radius * Radius - other.Radius * other.Radius + other.Center.X * other.Center.X
                               - Center.X * Center.X + other.Center.Y * other.Center.Y - Center.Y * Center.Y
                               + 2 * targetRadius * (Radius - other.Radius)) / (other.Center.Y - Center.Y) / 2;
        double b = -2 * Center.X - 2 * t * (Center.X - other.Center.X) / (other.Center.Y - Center.Y);
        double c = Center.X * Center.X - (Radius + targetRadius) * (Radius + targetRadius) + t * t;

        double q = b * b - 4 * a * c;
        // At this point, Q < 0 means the circles are too far apart, so no solution
        if (q < 0)
            return null;

        double s = Math.Sqrt(q);
        double xa = (-b + s) / (2 * a);
        double xb = (-b - s) / (2 * a);

        // These Sqrts should succeed, i.e. their parameter should never be < 0
        double ya = Math.Sqrt(-other.Center.X * other.Center.X - xa * xa + targetRadius * targetRadius
                              + 2 * other.Center.X * xa + other.Radius * other.Radius + 2 * other.Radius * targetRadius);
        double yb = Math.Sqrt(-Center.X * Center.X - xb * xb + targetRadius * targetRadius
                              + 2 * Center.X * xb + Radius * Radius + 2 * Radius * targetRadius);

        if (Math.Sign(Center.X - other.Center.X) != Math.Sign(Center.Y - other.Center.Y))
        {
            ya += other.Center.Y;
            yb = Center.Y - yb;
        }
        else
        {
            ya = other.Center.Y - ya;
            yb += Center.Y;
        }

        return (new CircleD(xa, ya, targetRadius), new CircleD(xb, yb, targetRadius));
    }

    /// <summary>
    ///     Determines whether this circle contains the specified <paramref name="point"/>.</summary>
    /// <param name="point">
    ///     Point to check.</param>
    /// <returns>
    ///     <c>true</c> if the point is contained in this circle, <c>false</c> otherwise.</returns>
    public readonly bool Contains(PointD point) => Center.Distance(point) <= Radius;

    private static Random _rnd = new();

    /// <summary>
    ///     Returns the smallest circle that encloses all the given points. If 1 point is given, a circle of radius 0 is
    ///     returned.</summary>
    /// <param name="points">
    ///     The set of points to circumscribe.</param>
    /// <returns>
    ///     The circumscribed circle.</returns>
    /// <remarks>
    ///     <list type="bullet">
    ///         <item><description>
    ///             Runs in expected O(n) time, randomized.</description></item></list></remarks>
    /// <exception cref="InvalidOperationException">
    ///     The input collection contained zero points.</exception>
    public static CircleD GetCircumscribedCircle(IList<PointD> points)
    {
        // Clone list to preserve the caller's data
        var shuffled = new List<PointD>(points);

        // Fisher-Yates shuffle
        for (int j = shuffled.Count; j >= 1; j--)
        {
            int item = _rnd.Next(0, j);
            if (item < j - 1)
                (shuffled[j - 1], shuffled[item]) = (shuffled[item], shuffled[j - 1]);
        }

        // Progressively add points to circle or recompute circle
        // Initially: No boundary points known
        CircleD? circ = null;
        for (int i = 0; i < shuffled.Count; i++)
        {
            PointD p = shuffled[i];
            if (circ == null || !circ.Value.Contains(p))
                circ = MakeCircleOnePoint(shuffled.GetRange(0, i + 1), p);
        }
        if (circ == null)
            throw new InvalidOperationException("The input collection did not contain any points.");
        return circ.Value;

        // One boundary point known
        CircleD MakeCircleOnePoint(List<PointD> pts, PointD p)
        {
            CircleD c = new CircleD(p, 0);
            for (int i = 0; i < pts.Count; i++)
            {
                PointD q = pts[i];
                if (!c.Contains(q))
                {
                    if (c.Radius == 0)
                        c = MakeDiameter(p, q);
                    else
                        c = MakeCircleTwoPoints(pts.GetRange(0, i + 1), p, q);
                }
            }
            return c;
        }

        // Two boundary pts known
        CircleD MakeCircleTwoPoints(List<PointD> pts, PointD p, PointD q)
        {
            CircleD crc = MakeDiameter(p, q);
            CircleD left = new CircleD(new PointD(0, 0), -1);
            CircleD right = new CircleD(new PointD(0, 0), -1);

            // For each point not in the two-point circle
            PointD pq = q - p;
            foreach (PointD r in pts)
            {
                if (crc.Contains(r))
                    continue;

                // Form a circumcircle and classify it on left or right side
                double cross = Cross(pq, r - p);
                CircleD c = MakeCircumcircle(p, q, r);
                if (c.Radius < 0)
                    continue;
                else if (cross > 0 && (left.Radius < 0 || Cross(pq, c.Center - p) > Cross(pq, left.Center - p)))
                    left = c;
                else if (cross < 0 && (right.Radius < 0 || Cross(pq, c.Center - p) < Cross(pq, right.Center - p)))
                    right = c;
            }

            // Select which circle to return
            if (left.Radius < 0 && right.Radius < 0)
                return crc;
            else if (left.Radius < 0)
                return right;
            else if (right.Radius < 0)
                return left;
            else
                return left.Radius <= right.Radius ? left : right;
        }

        CircleD MakeDiameter(PointD a, PointD b)
        {
            PointD c = new PointD((a.X + b.X) / 2, (a.Y + b.Y) / 2);
            return new CircleD(c, Math.Max(c.Distance(a), c.Distance(b)));
        }

        CircleD MakeCircumcircle(PointD a, PointD b, PointD c)
        {
            // Mathematical algorithm from Wikipedia: Circumscribed circle
            double ox = (Math.Min(Math.Min(a.X, b.X), c.X) + Math.Max(Math.Min(a.X, b.X), c.X)) / 2;
            double oy = (Math.Min(Math.Min(a.Y, b.Y), c.Y) + Math.Max(Math.Min(a.Y, b.Y), c.Y)) / 2;
            double ax = a.X - ox, ay = a.Y - oy;
            double bx = b.X - ox, by = b.Y - oy;
            double cx = c.X - ox, cy = c.Y - oy;
            double d = (ax * (by - cy) + bx * (cy - ay) + cx * (ay - by)) * 2;
            if (d == 0)
                return new CircleD(new PointD(0, 0), -1);
            double x = ((ax * ax + ay * ay) * (by - cy) + (bx * bx + by * by) * (cy - ay) + (cx * cx + cy * cy) * (ay - by)) / d;
            double y = ((ax * ax + ay * ay) * (cx - bx) + (bx * bx + by * by) * (ax - cx) + (cx * cx + cy * cy) * (bx - ax)) / d;
            PointD p = new PointD(ox + x, oy + y);
            double r = Math.Max(Math.Max(p.Distance(a), p.Distance(b)), p.Distance(c));
            return new CircleD(p, r);
        }

        // Signed area / determinant thing
        double Cross(PointD p, PointD q) => p.X * q.Y - p.Y * q.X;
    }

    /// <summary>Returns the rectangle that fully encloses this circle.</summary>
    public RectangleD ToRectangle() => new(Center.X - Radius, Center.Y - Radius, Radius * 2, Radius * 2);

    /// <summary>
    ///     Returns the circle that has all three given points in its perimeter.</summary>
    /// <exception cref="InvalidOperationException">
    ///     The three given points are collinear.</exception>
    public static CircleD FromThreePoints(PointD a, PointD b, PointD c)
    {
        var x12 = a.X - b.X;
        var x13 = a.X - c.X;
        var y12 = a.Y - b.Y;
        var y13 = a.Y - c.Y;

        var sx13 = a.X * a.X - c.X * c.X;
        var sy13 = a.Y * a.Y - c.Y * c.Y;
        var sx21 = b.X * b.X - a.X * a.X;
        var sy21 = b.Y * b.Y - a.Y * a.Y;

        var centerX = (sx13 * y12 + sy13 * y12 + sx21 * y13 + sy21 * y13) / (2 * ((b.X - a.X) * y13 - (c.X - a.X) * y12));
        var centerY = (sx13 * x12 + sy13 * x12 + sx21 * x13 + sy21 * x13) / (2 * ((b.Y - a.Y) * x13 - (c.Y - a.Y) * x12));
        if (double.IsNaN(centerX) || double.IsInfinity(centerX) || double.IsNaN(centerY) || double.IsInfinity(centerY))
            throw new InvalidOperationException("Cannot deduce circle from three points that are collinear.");
        var radius = Math.Sqrt((centerX - a.X) * (centerX - a.X) + (centerY - a.Y) * (centerY - a.Y));

        return new CircleD(centerX, centerY, radius);
    }
}
