using System;
using RT.Util.Collections;

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
        /// <param name="x">Center X co-ordinate.</param>
        /// <param name="y">Center Y co-ordinate.</param>
        /// <param name="rad">Radius.</param>
        public CircleD(double x, double y, double rad)
        {
            Center = new PointD(x, y);
            Radius = rad;
        }

        /// <summary>Initialises a new <see cref="CircleD"/> with the specified center co-ordinates and radius.</summary>
        /// <param name="center">Center co-ordinates.</param>
        /// <param name="rad">Radius.</param>
        public CircleD(PointD center, double rad)
        {
            Center = center;
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
        /// <param name="other">The other circle.</param>
        /// <param name="targetRadius">Target radius for output circles.</param>
        /// <returns>The two output circles if they exist. If the input circles are further
        /// apart than twice the target radius, the desires circles do not exist and null is returned.</returns>
        public RT.Util.ObsoleteTuple.Tuple<CircleD, CircleD>? FindTangentCircles(CircleD other, double targetRadius)
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
            if (q < 0) return null;

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

            return new RT.Util.ObsoleteTuple.Tuple<CircleD, CircleD>(new CircleD(xa, ya, targetRadius), new CircleD(xb, yb, targetRadius));
        }
    }
}
