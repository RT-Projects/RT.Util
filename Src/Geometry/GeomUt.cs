using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RT.Util.Geometry
{
    /// <summary>
    /// Contains general geometry-related utility functions.
    /// </summary>
    public static class GeomUt
    {
        /// <summary>
        /// "Unwinds" the specified angle so that it's in the range (-pi, pi]
        /// </summary>
        public static double NormalizedAngle(double a)
        {
            // Common cases
            if (a > -Math.PI && a <= Math.PI)
                return a;
            else if (a > 0 && a < 3*Math.PI)
                return a - 2*Math.PI;

            // General case - probably slow due to division
            double mod = Math.IEEERemainder(a, 2*Math.PI);

            if (mod > Math.PI)
                mod -= 2*Math.PI;

            if (mod == -Math.PI)
                return Math.PI;
            else
                return mod;
        }

        /// <summary>
        /// Returns "angle" relative to "reference". I.e. if the angles are
        /// the same, returns 0; if angle is further anticlockwise, returns
        /// a positive number.
        /// </summary>
        public static double AngleDifference(double reference, double angle)
        {
            return NormalizedAngle(angle - reference);
        }

        /// <summary>
        /// Given a vector l, and two points pt1 and pt2, splits the space into two
        /// halves on the line defined by the vector l. If both points lie in the same
        /// half, returns true. If they lie in different halves, or if at least one point
        /// lies on the dividing line, returns false.
        /// <remarks>
        /// If the vector l is of length 0, always returns false.
        /// </remarks>
        /// </summary>
        public static bool ArePointsSameSideOfLine(
            double lX, double lY, double pt1X, double pt1Y, double pt2X, double pt2Y)
        {
            // Consider cross products of both point vectors with the line vector.
            double xp1 = lX * pt1Y - lY * pt1X;
            double xp2 = lX * pt2Y - lY * pt2X;
            // If both cross products are positive or both negative the points are on the
            // same side. If one or both of them is zero the point lies on the line and
            // therefore both points cannot be on the same side.
            return xp1 * xp2 > 0;
        }
    }
}
