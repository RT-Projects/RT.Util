using System;

namespace RT.Util.Geometry
{
    /// <summary>
    /// A utility class to find / test for intersections between geometric shapes.
    /// 
    /// <para>
    /// In this static class, function names always have the two basic shapes
    /// ordered using the following order:
    /// </para>
    /// 
    /// <list type="number">
    ///   <item>Line (infinite)</item>
    ///   <item>Ray (starts at a point, extends to infinity)</item>
    ///   <item>Segment (starts and ends on finite points)</item>
    ///   <item>Circle</item>
    ///   <item>BoundingBox (axis-aligned, ordered coords of each edge are known)</item>
    /// </list>
    ///
    /// <para>
    /// Hence it's always LineWithCircle, never CircleWithLine.
    /// </para>
    /// </summary>
    public static class Intersect
    {
        #region LineWithLine

        /// <summary>
        /// Finds the point of intersection of two lines. The result is in terms of
        /// lambda along each of the lines. Point of intersection is defined as
        /// "line.Start + lambda * line", for each line. If the lines don't intersect,
        /// the lambdas are set to NaN.
        /// </summary>
        public static void LineWithLine(ref EdgeD line1, ref EdgeD line2,
                                        out double line1lambda, out double line2lambda)
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
                line1lambda = double.NaN;
                line2lambda = double.NaN;
            }
            else
            {
                line1lambda = (l2dx*(line1.Start.Y - line2.Start.Y) - l2dy*(line1.Start.X - line2.Start.X)) / denom;
                line2lambda = (l1dx*(line1.Start.Y - line2.Start.Y) - l1dy*(line1.Start.X - line2.Start.X)) / denom;
            }
        }

        /// <summary>
        /// <para>Finds the point of intersection between two lines, specified by a
        /// point on a line and the gradient. Works correctly if the gradient
        /// is infinite (vertical line), for both positive and negative inf.</para>
        /// 
        /// <para>If a single intersection exists, returns the coordinates in ix, iy.
        /// If the lines coincide, returns Inf,Inf. If lines are parallel but
        /// don't coincide returns NaN,NaN.</para>
        /// 
        /// <para>WARNING: untested.</para>
        /// </summary>
        public static void LineWithLine(out double ix, out double iy,
            double x1, double y1, double dydx1, double x2, double y2, double dydx2)
        {
            bool gradeq = dydx1 == dydx2;
            bool inf1 = double.IsInfinity(dydx1);
            bool inf2 = double.IsInfinity(dydx2);

            if (!(gradeq || inf1 || inf2))
            {
                // Most common case - no infinities, lines not parallel.
                // Could do extra checks for max accuracy but not convinced that's
                // going to be worth the performance penalty.
                ix = (y2 - y1 + dydx1 * x1 - dydx2 * x2) / (dydx1 - dydx2);
                iy = y1 + dydx1 * (ix - x1);
            }
            else if (gradeq || (inf1 && inf2))
            {
                // Lines are parallel
                bool intersect = false;
                if (double.IsInfinity(dydx1))
                    // Vertical lines
                    intersect = x1 == x2;
                else
                    // Not vertical lines
                    intersect = (y1 == y2 + dydx2 * (x1 - x2));

                // If they intersect, return Inf,Inf. If not, return NaN,NaN
                if (intersect)
                    ix = iy = double.PositiveInfinity;
                else
                    ix = iy = double.NaN;
            }
            else if (inf1)
            {
                // Line 1 is vertical but the other one isn't
                ix = x1;
                iy = y2 + dydx2 * (x1 - x2);
            }
            else /*if (inf2)*/
            {
                // Line 2 is vertical but the other one isn't
                ix = x2;
                iy = y1 + dydx1 * (x2 - x1);
            }
        }

        /// <summary>
        /// <para>Finds the point of intersection between two lines, specified by
        /// two points each.</para>
        /// 
        /// <para>If a single intersection exists, returns the coordinates in ix, iy.
        /// If the lines coincide, returns Inf,Inf. If lines are parallel but
        /// don't coincide returns NaN,NaN.</para>
        /// 
        /// <para>WARNING: untested.</para>
        /// </summary>
        public static void LineWithLine(out double ix, out double iy,
            double fx1, double fy1, double tx1, double ty1,
            double fx2, double fy2, double tx2, double ty2)
        {
            double num = (tx2 - fx2) * (fy1 - fy2) - (ty2 - fy2) * (fx1 - fx2);
            double den = (ty2 - fy2) * (tx1 - fx1) - (tx2 - fx2) * (ty1 - fy1);

            if (den != 0)
            {
                // Lines are not parallel
                double u = num / den;
                ix = fx1 + u * (tx1 - fx1);
                iy = fy1 + u * (ty2 - fy1);
            }
            else if (num != 0)
            {
                // Parallel but not coincident
                ix = iy = double.NaN;
            }
            else
            {
                // Parallel and coincident
                ix = iy = double.PositiveInfinity;
            }
        }

        #endregion

        #region LineWithCircle

        /// <summary>
        /// Finds the points of intersection between a line and a circle. The results
        /// are two lambdas along the line, one for each point, or NaN if there is no
        /// intersection.
        /// </summary>
        public static void LineWithCircle(ref EdgeD line, ref CircleD circle,
                                          out double lambda1, out double lambda2)
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
            double a = dx*dx + dy*dy;
            double b = -2 * (ax*dx + ay*dy);
            double c = ax*ax + ay*ay - circle.Radius*circle.Radius;

            // Now just solve the quadratic eqn...
            double D = b*b - 4*a*c;
            if (D < 0)
            {
                lambda1 = lambda2 = double.NaN;
            }
            else
            {
                double sqrtD = Math.Sqrt(D);
                lambda1 = (-b + sqrtD) / (2*a);
                lambda2 = (-b - sqrtD) / (2*a);
            }
        }

        /// <summary>
        /// Finds the points of intersection between a line and a circle. The results
        /// are two lambdas along the line, one for each point, or NaN if there is no
        /// intersection.
        /// </summary>
        public static void LineWithCircle(EdgeD line, CircleD circle,
                                          out double lambda1, out double lambda2)
        {
            LineWithCircle(ref line, ref circle, out lambda1, out lambda2);
        }

        #endregion

        #region RayWithSegment

        /// <summary>
        /// Calculates the intersection of a ray with a segment. Returns the result as the
        /// lambdas of the intersection point along the ray and the segment. If there is no
        /// intersection returns double.NaN in both lambdas.
        /// </summary>
        public static void RayWithSegment(ref EdgeD ray, ref EdgeD segment, out double rayL, out double segmentL)
        {
            Intersect.LineWithLine(ref ray, ref segment, out rayL, out segmentL);

            if (!double.IsNaN(rayL) && ((rayL < 0) || (segmentL < 0) || (segmentL > 1)))
                rayL = segmentL = double.NaN;
        }

        #endregion

        #region RayWithCircle

        /// <summary>
        /// Finds the points of intersection between a ray and a circle. The
        /// resulting lambdas along the ray are sorted in ascending order, so
        /// the "first" intersection is always in lambda1 (if any). Lambda may
        /// be NaN if there is no intersection (or no "second" intersection).
        /// </summary>
        public static void RayWithCircle(ref EdgeD ray, ref CircleD circle,
                                         out double lambda1, out double lambda2)
        {
            LineWithCircle(ref ray, ref circle, out lambda1, out lambda2);

            // Sort the two values in ascending order, with NaN last,
            // while resetting negative values to NaNs
            if (lambda1 < 0) lambda1 = double.NaN;
            if (lambda2 < 0) lambda2 = double.NaN;
            if (lambda1 > lambda2 || double.IsNaN(lambda1))
            {
                double temp = lambda1;
                lambda1 = lambda2;
                lambda2 = temp;
            }
        }

        #endregion

        #region RayWithArc

        /// <summary>
        /// Finds the points of intersection between a ray and an arc. The
        /// resulting lambdas along the ray are sorted in ascending order, so
        /// the "first" intersection is always in lambda1 (if any). Lambda may
        /// be NaN if there is no intersection (or no "second" intersection).
        /// </summary>
        public static void RayWithArc(ref EdgeD ray, ref ArcD arc,
                                         out double lambda1, out double lambda2)
        {
            RayWithCircle(ref ray, ref arc.Circle, out lambda1, out lambda2);
            var sweepdir = Math.Sign(arc.AngleSweep);
            if (!double.IsNaN(lambda1))
            {
                var dir = ((ray.Start + lambda1 * (ray.End - ray.Start)) - arc.Circle.Center).Theta();
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

        #endregion

        #region RayWithRectangle

        /// <summary>
        /// Finds intersections between a ray and a rectangle. Returns the lambdas of intersections, if any,
        /// or NaN otherwise. Guarantees that lambda1 &lt; lambda2, and if only one of them is NaN then
        /// it's lambda2. Lambda is such that ray.Start + lambda * (ray.End - ray.Start) gives the point of intersection.
        /// </summary>
        public static void RayWithRectangle(ref EdgeD ray, ref RectangleD rect, out double lambda1, out double lambda2)
        {
            double lambda, dummy;
            bool done1 = false;
            lambda1 = lambda2 = double.NaN;

            for (int i = 0; i < 4; i++)
            {
                EdgeD segment;
                switch (i)
                {
                    case 0: segment = new EdgeD(rect.Left, rect.Top, rect.Right, rect.Top); break;
                    case 1: segment = new EdgeD(rect.Right, rect.Top, rect.Right, rect.Bottom); break;
                    case 2: segment = new EdgeD(rect.Right, rect.Bottom, rect.Left, rect.Bottom); break;
                    case 3: segment = new EdgeD(rect.Left, rect.Bottom, rect.Left, rect.Top); break;
                    default: throw new InternalErrorException("fsvxhfhj"); // shut up compiler about uninitialized "segment" variable
                }

                Intersect.RayWithSegment(ref ray, ref segment, out lambda, out dummy);

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

        #endregion

        #region RayWithBoundingBox

        /// <summary>
        /// Checks for intersections between a ray and a bounding box. Returns true if
        /// there is at least one intersection.
        /// </summary>
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

        #endregion

        #region SegmentWithSegment

        /// <summary>
        /// If the two specified line segments touch anywhere, returns true. Otherwise
        /// returns false.
        /// <remarks>
        /// Support for zero-length segments is partial - if one of the segments is of
        /// length 0 the result is correct, but if both are the result is always true.
        /// </remarks>
        /// </summary>
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

        #endregion

        #region BoundingBoxWithBoundingBox

        /// <summary>
        /// Checks for intersections between the two bounding boxes specified by the coordinates.
        /// Returns true if there is at least one intersection. Coordinates ending with "1" belong
        /// to the first box, "2" to the second one. Coordinates starting with "f" MUST be less than
        /// or equal to ones starting with "t".
        /// </summary>
        public static bool BoundingBoxWithBoundingBox(
            double fx1, double fy1, double tx1, double ty1,
            double fx2, double fy2, double tx2, double ty2)
        {
            return !((fx2 > tx1 && tx2 > tx1) || (fx2 < fx1 && tx2 < fx1)
                  || (fy2 > ty1 && ty2 > ty1) || (fy2 < fy1 && ty2 < fy1));
        }

        /// <summary>
        /// Checks for intersections between the two bounding boxes specified by the coordinates.
        /// Returns true if there is at least one intersection.
        /// </summary>
        public static bool BoundingBoxWithBoundingBox(ref BoundingBoxD box1, ref BoundingBoxD box2)
        {
            return !((box2.Xmin > box1.Xmax && box2.Xmax > box1.Xmax) || (box2.Xmin < box1.Xmin && box2.Xmax < box1.Xmin)
                  || (box2.Ymin > box1.Ymax && box2.Ymax > box1.Ymax) || (box2.Ymin < box1.Ymin && box2.Ymax < box1.Ymin));
        }

        #endregion
    }
}
