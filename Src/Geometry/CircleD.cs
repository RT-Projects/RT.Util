using System;
using System.Collections.Generic;
using System.Text;

namespace RT.Util.Geometry
{
    /// <summary>Encapsulates a double-precision circle.</summary>
    public struct CircleD
    {
        /// <summary>Center of the circle.</summary>
        public PointD Center;
        /// <summary>Radius of the circle.</summary>
        public double Radius;

        /// <summary>Initialises a new <see cref="CircleD"/> with the specified center co-ordinates and radius.</summary>
        /// <param name="X">Center X co-ordinate.</param>
        /// <param name="Y">Center Y co-ordinate.</param>
        /// <param name="rad">Radius.</param>
        public CircleD(double X, double Y, double rad)
        {
            Center = new PointD(X, Y);
            Radius = rad;
        }

        /// <summary>Provides a string representation of the current <see cref="CircleD"/>.</summary>
        /// <returns>A string representation of the current <see cref="CircleD"/>.</returns>
        public override string ToString()
        {
            return Center.ToString() + " / " + Radius;
        }

        /// <summary>
        /// Given this circle and another circle, tries to find a third and fourth circle with
        /// a given target radius such that the new circles are both tangent to the first two.
        /// </summary>
        /// <param name="Other">The other circle.</param>
        /// <param name="TargetRadius">Target radius for output circles.</param>
        /// <returns>The two output circles if they exist. If the input circles are further
        /// apart than twice the target radius, the desires circles do not exist and null is returned.</returns>
        public Tuple<CircleD, CircleD>? FindTangentCircles(CircleD Other, double TargetRadius)
        {
            double A = ((Center.X - Other.Center.X) * (Center.X - Other.Center.X)) /
                       ((Other.Center.Y - Center.Y) * (Other.Center.Y - Center.Y)) + 1;
            double T = Center.Y - (Radius * Radius - Other.Radius * Other.Radius + Other.Center.X * Other.Center.X
                                   - Center.X * Center.X + Other.Center.Y * Other.Center.Y - Center.Y * Center.Y
                                   + 2 * TargetRadius * (Radius - Other.Radius)) / (Other.Center.Y - Center.Y) / 2;
            double B = -2 * Center.X - 2 * T * (Center.X - Other.Center.X) / (Other.Center.Y - Center.Y);
            double C = Center.X * Center.X - (Radius + TargetRadius) * (Radius + TargetRadius) + T * T;

            double Q = B * B - 4 * A * C;
            // At this point, Q < 0 means the circles are too far apart, so no solution
            if (Q < 0) return null;

            double S = Math.Sqrt(Q);
            double xa = (-B + S) / (2 * A);
            double xb = (-B - S) / (2 * A);

            // These Sqrts should succeed, i.e. their parameter should never be < 0
            double ya = Math.Sqrt(-Other.Center.X * Other.Center.X - xa * xa + TargetRadius * TargetRadius
                                  + 2 * Other.Center.X * xa + Other.Radius * Other.Radius + 2 * Other.Radius * TargetRadius);
            double yb = Math.Sqrt(-Center.X * Center.X - xb * xb + TargetRadius * TargetRadius
                                  + 2 * Center.X * xb + Radius * Radius + 2 * Radius * TargetRadius);

            if (Math.Sign(Center.X - Other.Center.X) != Math.Sign(Center.Y - Other.Center.Y))
            {
                ya += Other.Center.Y;
                yb = Center.Y - yb;
            }
            else
            {
                ya = Other.Center.Y - ya;
                yb += Center.Y;
            }

            return new Tuple<CircleD, CircleD>(
                new CircleD(xa, ya, TargetRadius),
                new CircleD(xb, yb, TargetRadius)
            );
        }
    }
}
