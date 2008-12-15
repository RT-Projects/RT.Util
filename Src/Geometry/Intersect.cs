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
    }
}
