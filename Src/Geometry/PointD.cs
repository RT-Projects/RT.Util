using System;
using System.Collections.Generic;
using System.Text;

namespace RT.Util.Geometry
{
    /// <summary>Encapsulates a double-precision point.</summary>
    public struct PointD: IEquatable<PointD>
    {
        /// <summary>X-co-ordinate of the point.</summary>
        public double X;
        /// <summary>Y-co-ordinate of the point.</summary>
        public double Y;

        /// <summary>Initialises a double-precision point with the specified co-ordinates.</summary>
        public PointD(double X, double Y)
        {
            this.X = X;
            this.Y = Y;
        }

        /// <summary>Compares two <see cref="PointD"/> objects for equality.</summary>
        /// <param name="obj">Object to compare against.</param>
        /// <returns>True if considered equal.</returns>
        public override bool Equals(object obj)
        {
            if (obj is PointD)
                return ((PointD)obj).X == X && ((PointD)obj).Y == Y;
            return base.Equals(obj);
        }

        /// <summary>Compares two <see cref="PointD"/> objects for equality.</summary>
        /// <param name="other">Object to compare against.</param>
        /// <returns>True if considered equal.</returns>
        public bool Equals(PointD other)
        {
            return other.X == X && other.Y == Y;
        }

        /// <summary>Compares two <see cref="PointD"/> objects for equality.</summary>
        /// <param name="one">First object to compare.</param>
        /// <param name="other">Object to compare against.</param>
        /// <returns>True if considered equal.</returns>
        public static bool operator ==(PointD one, PointD other)
        {
            return one.Equals(other);
        }

        /// <summary>Compares two <see cref="PointD"/> objects for inequality.</summary>
        /// <param name="one">First object to compare.</param>
        /// <param name="other">Object to compare against.</param>
        /// <returns>True if considered different.</returns>
        public static bool operator !=(PointD one, PointD other)
        {
            return !one.Equals(other);
        }

        /// <summary>Returns a hash code for the current <see cref="PointD"/>.</summary>
        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        /// <summary>Converts the current <see cref="PointD"/> object to a <see cref="System.Drawing.PointF"/>.
        /// Note that doing so loses precision.</summary>
        /// <returns>Lower-precision <see cref="System.Drawing.PointF"/>.</returns>
        public System.Drawing.PointF ToPointF()
        {
            return new System.Drawing.PointF((float)X, (float)Y);
        }

        /// <summary>Provides a string representation of the current <see cref="PointD"/>.</summary>
        /// <returns>A string representation of the current <see cref="PointD"/>.</returns>
        public override string ToString()
        {
            return "X=" + X + ", Y=" + Y;
        }

        /// <summary>
        /// Returns a new PointD at the given offset of this one. Does NOT modify this PointD.
        /// </summary>
        /// <param name="ByX">Amount to move X co-ordinate by.</param>
        /// <param name="ByY">Amount to move Y co-ordinate by.</param>
        /// <returns>New PointD at the given offset of this one.</returns>
        public PointD Move(double ByX, double ByY)
        {
            return new PointD(this.X + ByX, this.Y + ByY);
        }
    }
}
