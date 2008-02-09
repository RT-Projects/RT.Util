using System;
using System.Collections.Generic;
using System.Text;

namespace RT.Util
{
    /// <summary>
    /// A double-precision rectangle class which supports intersect tests.
    /// </summary>
    public struct RectangleD
    {
        public double X;
        public double Y;
        public double Width;
        public double Height;

        public RectangleD(double X, double Y, double Width, double Height)
        {
            this.X = X;
            this.Y = Y;
            this.Width = Width;
            this.Height = Height;
        }

        public double Left { get { return X; } }
        public double Top { get { return Y; } }
        public double Right { get { return X + Width; } }
        public double Bottom { get { return Y + Height; } }

        public bool IntersectsWith(RectangleD r)
        {
            return ContainsPoint(r.Left, r.Top) || ContainsPoint(r.Left, r.Bottom)
                || ContainsPoint(r.Right, r.Top) || ContainsPoint(r.Right, r.Bottom)
                || (r.Left >= Left && r.Right <= Right && r.Top <= Top && r.Bottom >= Bottom)
                || (r.Left <= Left && r.Right >= Right && r.Top >= Top && r.Bottom <= Bottom);
        }

        public bool ContainsPoint(double X, double Y)
        {
            return (X >= Left) && (X <= Right) && (Y >= Top) && (Y <= Bottom);
        }
    }
}
