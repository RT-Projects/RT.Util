namespace RT.Geometry;

/// <summary>
///     <para>
///         A utility class to find / test for intersections between geometric shapes.</para>
///     <para>
///         In this static class, function names always have the two basic shapes ordered using the following order:</para>
///     <list type="number">
///         <item>Line (straight line; infinite)</item>
///         <item>Ray (starts at a point, extends to infinity)</item>
///         <item>Segment (starts and ends on finite points)</item>
///         <item>Circle</item>
///         <item>Arc</item>
///         <item>Rectangle (axis-aligned, ordered coords of each edge are known)</item>
///         <item>BoundingBox (axis-aligned, ordered coords of each edge are known)</item></list>
///     <para>
///         Hence it's always LineWithCircle, never CircleWithLine.</para>
///     <para>
///         Many functions return one or multiple “lambda” values. These indicate how far along a line a point of intersection
///         is from its start point, relative to its end point:</para>
///     <list type="bullet">
///         <item>A negative value indicates a position “before” the start point; 0 is the start point; 0.5 is the midpoint; 1
///         is the endpoint; and a value greater than 1 indicates a position “beyond” the end point.</item>
///         <item>If no point of intersection exists, a lambda value of <c>double.NaN</c> is returned.</item>
///         <item>If only one point of intersection is found by a function that returns two lambdas, it is always the first
///         lambda that identifies the point of intersection while the second is set to <c>double.NaN</c>.</item></list>
///     <para>
///         In cases where multiple points of intersections are possible, such as between a line and a circle, those overloads
///         that return only one point always return the one that is closer to the line’s start point.</para></summary>
public static class Intersect
{
    /// <summary>
    ///     Finds the point of intersection between two lines.</summary>
    /// <remarks>
    ///     See <see cref="Intersect"/> for details.</remarks>
    public static void LineWithLine(ref EdgeD line1, ref EdgeD line2, out double line1Lambda, out double line2Lambda)
    {
        // line1 direction vector
        double l1dx = line1.End.X - line1.Start.X;
        double l1dy = line1.End.Y - line1.Start.Y;
        // line2 direction vector
        double l2dx = line2.End.X - line2.Start.X;
        double l2dy = line2.End.Y - line2.Start.Y;

        double denom = l1dx * l2dy - l1dy * l2dx;

        if (denom == 0)
        {
            line1Lambda = double.NaN;
            line2Lambda = double.NaN;
        }
        else
        {
            line1Lambda = (l2dx * (line1.Start.Y - line2.Start.Y) - l2dy * (line1.Start.X - line2.Start.X)) / denom;
            line2Lambda = (l1dx * (line1.Start.Y - line2.Start.Y) - l1dy * (line1.Start.X - line2.Start.X)) / denom;
        }
    }

    /// <summary>
    ///     Finds the point of intersection between two lines.</summary>
    /// <remarks>
    ///     See <see cref="Intersect"/> for details.</remarks>
    public static (PointD? point, double line1Lambda, double line2Lambda) LineWithLine(EdgeD line1, EdgeD line2)
    {
        LineWithLine(ref line1, ref line2, out var line1Lambda, out var line2Lambda);
        return (double.IsNaN(line1Lambda) ? null : line1.Start + line1Lambda * (line1.End - line1.Start), line1Lambda, line2Lambda);
    }

    /// <summary>
    ///     Finds the points of intersection between a line and a circle.</summary>
    /// <remarks>
    ///     See <see cref="Intersect"/> for details about the values returned.</remarks>
    public static void LineWithCircle(ref EdgeD line, ref CircleD circle, out double lambda1, out double lambda2)
    {
        // The following expressions come up a lot in the solution, so simplify using them.
        double dx = line.End.X - line.Start.X;
        double dy = line.End.Y - line.Start.Y;
        double ax = -line.Start.X + circle.Center.X;
        double ay = -line.Start.Y + circle.Center.Y;

        // Solve simultaneously for l:
        // Eq of a line:    x = sx + l * dx
        //                  y = sy + l * dy
        // Eq of a circle:  (x - cx)^2 + (y - cy)^2 = r^2
        //
        // Eventually we get a standard quadratic equation in l with the
        // following coefficients:
        double a = dx * dx + dy * dy;
        double b = -2 * (ax * dx + ay * dy);
        double c = ax * ax + ay * ay - circle.Radius * circle.Radius;

        // Now just solve the quadratic eqn...
        double D = b * b - 4 * a * c;
        if (D < 0)
        {
            lambda1 = lambda2 = double.NaN;
        }
        else
        {
            double sqrtD = Math.Sqrt(D);
            lambda1 = (-b - sqrtD) / (2 * a);
            lambda2 = (-b + sqrtD) / (2 * a);
        }
    }

    /// <summary>
    ///     Finds the points of intersection between a line and a circle.</summary>
    /// <remarks>
    ///     See <see cref="Intersect"/> for details about the values returned.</remarks>
    public static (PointD? point1, double lambda1, PointD? point2, double lambda2) LineWithCircle(EdgeD line, CircleD circle)
    {
        LineWithCircle(ref line, ref circle, out var lambda1, out var lambda2);
        return (double.IsNaN(lambda1) ? null : line.Start + lambda1 * (line.End - line.Start), lambda1, double.IsNaN(lambda2) ? null : line.Start + lambda2 * (line.End - line.Start), lambda2);
    }

    /// <summary>
    ///     Finds the point of intersection of a ray with a segment.</summary>
    /// <remarks>
    ///     See <see cref="Intersect"/> for details about the values returned.</remarks>
    public static void RayWithSegment(ref EdgeD ray, ref EdgeD segment, out double rayL, out double segmentL)
    {
        LineWithLine(ref ray, ref segment, out rayL, out segmentL);
        if (!double.IsNaN(rayL) && (rayL < 0 || segmentL < 0 || segmentL > 1))
            rayL = segmentL = double.NaN;
    }

    /// <summary>
    ///     Finds the point of intersection of a ray with a segment.</summary>
    /// <remarks>
    ///     See <see cref="Intersect"/> for details about the values returned.</remarks>
    public static (PointD? point, double rayL, double segmentL) RayWithSegment(EdgeD ray, EdgeD segment)
    {
        RayWithSegment(ref ray, ref segment, out double rayL, out double segmentL);
        return (double.IsNaN(rayL) || double.IsNaN(segmentL) ? null : ray.Start + (ray.End - ray.Start) * rayL, rayL, segmentL);
    }

    /// <summary>
    ///     Finds the points of intersection between a ray and a circle.</summary>
    /// <remarks>
    ///     See <see cref="Intersect"/> for details about the values returned.</remarks>
    public static void RayWithCircle(ref EdgeD ray, ref CircleD circle, out double lambda1, out double lambda2)
    {
        LineWithCircle(ref ray, ref circle, out lambda1, out lambda2);

        if (lambda1 < 0) lambda1 = double.NaN;
        if (lambda2 < 0) lambda2 = double.NaN;
        if (double.IsNaN(lambda1) && !double.IsNaN(lambda2))
            (lambda1, lambda2) = (lambda2, lambda1);
    }

    /// <summary>
    ///     Finds the points of intersection between a ray and a circle.</summary>
    /// <remarks>
    ///     See <see cref="Intersect"/> for details about the values returned.</remarks>
    public static (PointD? point1, double lambda1, PointD? point2, double lambda2) RayWithCircle(EdgeD ray, CircleD circle)
    {
        RayWithCircle(ref ray, ref circle, out double lambda1, out double lambda2);
        return (double.IsNaN(lambda1) ? null : ray.Start + (ray.End - ray.Start) * lambda1, lambda1,
            double.IsNaN(lambda2) ? null : ray.Start + (ray.End - ray.Start) * lambda2, lambda2);
    }

    /// <summary>
    ///     Finds the points of intersection between a ray and an arc.</summary>
    /// <remarks>
    ///     See <see cref="Intersect"/> for details about the values returned.</remarks>
    public static void RayWithArc(ref EdgeD ray, ref ArcD arc, out double lambda1, out double lambda2)
    {
        RayWithCircle(ref ray, ref arc.Circle, out lambda1, out lambda2);
        var sweepdir = Math.Sign(arc.AngleSweep);
        if (!double.IsNaN(lambda1))
        {
            var dir = (ray.Start + lambda1 * (ray.End - ray.Start) - arc.Circle.Center).Theta();
            if (!(GeomUt.AngleDifference(arc.AngleStart, dir) * sweepdir > 0 && GeomUt.AngleDifference(arc.AngleStart + arc.AngleSweep, dir) * sweepdir < 0))
                lambda1 = double.NaN;
        }
        if (!double.IsNaN(lambda2))
        {
            var dir = ((ray.Start + lambda2 * (ray.End - ray.Start)) - arc.Circle.Center).Theta();
            if (!(GeomUt.AngleDifference(arc.AngleStart, dir) * sweepdir > 0 && GeomUt.AngleDifference(arc.AngleStart + arc.AngleSweep, dir) * sweepdir < 0))
                lambda2 = double.NaN;
        }
        if (double.IsNaN(lambda1) && !double.IsNaN(lambda2))
        {
            lambda1 = lambda2;
            lambda2 = double.NaN;
        }
    }

    /// <summary>
    ///     Finds the points of intersection between a ray and an arc.</summary>
    /// <remarks>
    ///     See <see cref="Intersect"/> for details about the values returned.</remarks>
    public static (PointD? point1, double lambda1, PointD? point2, double lambda2) RayWithArc(EdgeD ray, ArcD arc)
    {
        RayWithArc(ref ray, ref arc, out double lambda1, out double lambda2);
        return (double.IsNaN(lambda1) || double.IsNaN(lambda2) ? null : ray.Start + (ray.End - ray.Start) * lambda1, lambda1,
            double.IsNaN(lambda2) || double.IsNaN(lambda2) ? null : ray.Start + (ray.End - ray.Start) * lambda2, lambda2);
    }

    /// <summary>
    ///     Finds the points of intersection between a ray and a rectangle.</summary>
    /// <remarks>
    ///     See <see cref="Intersect"/> for details about the values returned.</remarks>
    public static void RayWithRectangle(ref EdgeD ray, ref RectangleD rect, out double lambda1, out double lambda2)
    {
        bool done1 = false;
        lambda1 = lambda2 = double.NaN;

        for (int i = 0; i < 4; i++)
        {
            var segment = i switch
            {
                0 => new EdgeD(rect.Left, rect.Top, rect.Right, rect.Top),
                1 => new EdgeD(rect.Right, rect.Top, rect.Right, rect.Bottom),
                2 => new EdgeD(rect.Right, rect.Bottom, rect.Left, rect.Bottom),
                3 => new EdgeD(rect.Left, rect.Bottom, rect.Left, rect.Top),
                _ => throw new InvalidOperationException("fsvxhfhj")   // unreachable
            };
            RayWithSegment(ref ray, ref segment, out var lambda, out _);

            if (!double.IsNaN(lambda))
            {
                if (!done1)
                {
                    lambda1 = lambda;
                    done1 = true;
                }
                else if (lambda != lambda1)
                {
                    if (lambda > lambda1)
                        lambda2 = lambda;
                    else
                    {
                        lambda2 = lambda1;
                        lambda1 = lambda;
                    }
                    return;
                }
            }
        }
    }

    /// <summary>
    ///     Finds the points of intersection between a ray and a rectangle.</summary>
    /// <remarks>
    ///     See <see cref="Intersect"/> for details about the values returned.</remarks>
    public static (PointD? point1, double lambda1, PointD? point2, double lambda2) RayWithRectangle(EdgeD ray, RectangleD rectangle)
    {
        RayWithRectangle(ref ray, ref rectangle, out double lambda1, out double lambda2);
        return (double.IsNaN(lambda1) ? null : ray.Start + (ray.End - ray.Start) * lambda1, lambda1, double.IsNaN(lambda2) ? null : ray.Start + (ray.End - ray.Start) * lambda2, lambda2);
    }

    /// <summary>
    ///     Checks for intersections between a ray and a bounding box. Returns true if there is at least one intersection.</summary>
    public static bool RayWithBoundingBox(ref EdgeD ray, ref BoundingBoxD box)
    {
        double dx = ray.End.X - ray.Start.X;
        double dy = ray.End.Y - ray.Start.Y;
        double k, c;  // temporaries

        // Check intersection with horizontal bounds
        if (dy != 0)
        {
            // Upper line
            k = (box.Ymax - ray.Start.Y) / dy;
            if (k >= 0)
            {
                c = ray.Start.X + k * dx;
                if (c >= box.Xmin && c <= box.Xmax)
                    return true;
            }
            // Lower line
            k = (box.Ymin - ray.Start.Y) / dy;
            if (k >= 0)
            {
                c = ray.Start.X + k * dx;
                if (c >= box.Xmin && c <= box.Xmax)
                    return true;
            }
        }
        // Check intersection with vertical bounds
        if (dx != 0)
        {
            // Rightmost line
            k = (box.Xmax - ray.Start.X) / dx;
            if (k >= 0)
            {
                c = ray.Start.Y + k * dy;
                if (c >= box.Ymin && c <= box.Ymax)
                    return true;
            }
            // Leftmost line
            k = (box.Xmin - ray.Start.X) / dx;
            if (k >= 0)
            {
                c = ray.Start.Y + k * dy;
                if (c >= box.Ymin && c <= box.Ymax)
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    ///     If the two specified line segments touch anywhere, returns true. Otherwise returns false. See Remarks.</summary>
    /// <remarks>
    ///     Support for zero-length segments is partial - if one of the segments is of length 0 the result is correct, but if
    ///     both are the result is always true.</remarks>
    public static bool SegmentWithSegment(
        double f1x, double f1y, double t1x, double t1y,
        double f2x, double f2y, double t2x, double t2y)
    {
        // This is what's intended:
        //bool a = GeomUt.ArePointsSameSideOfLine(t1x - f1x, t1y - f1y,
        //    f2x - f1x, f2y - f1y, t2x - f1x, t2y - f1y);
        //bool b = GeomUt.ArePointsSameSideOfLine(t2x - f2x, t2y - f2y,
        //    f1x - f2x, f1y - f2y, t1x - f2x, t1y - f2y);
        //return !(a || b);

        // This is the same but expanded and rearranged slightly for speed
        double tf1x = t1x - f1x;
        double tf1y = t1y - f1y;
        double tf2x = t2x - f2x;
        double tf2y = t2y - f2y;
        double f21x = f2x - f1x;
        double f21y = f2y - f1y;
        return
            ((tf1x * f21y - tf1y * f21x) * (tf1x * (t2y - f1y) - tf1y * (t2x - f1x)) <= 0) &&
            ((tf2y * f21x - tf2x * f21y) * (tf2x * (t1y - f2y) - tf2y * (t1x - f2x)) <= 0);
    }

    /// <summary>
    ///     Checks for intersections between the two bounding boxes specified by the coordinates. Returns true if there is at
    ///     least one intersection. Coordinates ending with "1" belong to the first box, "2" to the second one. Coordinates
    ///     starting with "f" MUST be less than or equal to ones starting with "t".</summary>
    public static bool BoundingBoxWithBoundingBox(
        double fx1, double fy1, double tx1, double ty1,
        double fx2, double fy2, double tx2, double ty2)
    {
        return !((fx2 > tx1 && tx2 > tx1) || (fx2 < fx1 && tx2 < fx1)
              || (fy2 > ty1 && ty2 > ty1) || (fy2 < fy1 && ty2 < fy1));
    }

    /// <summary>
    ///     Checks for intersections between the two bounding boxes specified by the coordinates. Returns true if there is at
    ///     least one intersection.</summary>
    public static bool BoundingBoxWithBoundingBox(ref BoundingBoxD box1, ref BoundingBoxD box2)
    {
        return !((box2.Xmin > box1.Xmax && box2.Xmax > box1.Xmax) || (box2.Xmin < box1.Xmin && box2.Xmax < box1.Xmin)
              || (box2.Ymin > box1.Ymax && box2.Ymax > box1.Ymax) || (box2.Ymin < box1.Ymin && box2.Ymax < box1.Ymin));
    }

    /// <summary>Returns a polygon formed by intersecting an arbitrary polygon with a convex polygon.</summary>
    public static PolygonD PolygonWithConvexPolygon(PolygonD mainPoly, PolygonD clipPoly)
    {
        if (mainPoly.Vertices.Count <= 2 || clipPoly.Vertices.Count <= 2)
            throw new InvalidOperationException("One of the polygons has 2 vertices or fewer.");
        var resultVertices = mainPoly.Vertices.ToList();
        foreach (var clipEdge in clipPoly.ToEdges())
        {
            var newVertices = new List<PointD>();
            var vertexStart = resultVertices[resultVertices.Count - 1];
            foreach (var vertexEnd in resultVertices)
            {
                if (clipEdge.CrossZ(vertexStart) > 0)
                {
                    if (clipEdge.CrossZ(vertexEnd) > 0)
                        newVertices.Add(vertexEnd);
                    else
                        newVertices.Add(LineWithLine(clipEdge, new EdgeD(vertexStart, vertexEnd)).point.Value);
                }
                else
                {
                    if (clipEdge.CrossZ(vertexEnd) > 0)
                    {
                        newVertices.Add(LineWithLine(clipEdge, new EdgeD(vertexStart, vertexEnd)).point.Value);
                        newVertices.Add(vertexEnd);
                    }
                }
                vertexStart = vertexEnd;
            }
            resultVertices = newVertices;
        }
        return new PolygonD(resultVertices);
    }
}
