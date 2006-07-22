/// Utils.cs  -  utility functions and classes

using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Runtime.InteropServices;

namespace RT.Util
{
    /// <summary>
    /// A pair of two values of specified types.
    /// </summary>
    public struct Pair<T1, T2>
    {
        public T1 E1;
        public T2 E2;

        public Pair(T1 Element1, T2 Element2)
        {
            E1 = Element1;
            E2 = Element2;
        }
    }

    public class EnumPairs<T>
    {
        private IList<T> A;

        private EnumPairs() {}

        public EnumPairs(IList<T> List)
        {
            A = List;
        }

        public IEnumerator<Pair<T, T>> GetEnumerator()
        {
            for (int i=0; i<A.Count-1; i++)
                for (int j=i+1; j<A.Count; j++)
                    yield return new Pair<T, T>(A[i], A[j]);
        }
    }

    public static class GDI
    {
        /// <summary>
        /// Caches previously used Pens of predefined width and the specified color.
        /// </summary>
        private static Dictionary<Color, Pen> PenCache = new Dictionary<Color, Pen>();

        /// <summary>
        /// Caches previously used Solid Brushes of the specified color.
        /// </summary>
        private static Dictionary<Color, Brush> BrushCache = new Dictionary<Color, Brush>();

        /// <summary>
        /// Returns a pen of the specified color. The pen will be retrieved from the cache
        /// in case it exists; otherwise it will be created and cached.
        /// </summary>
        /// <param name="clr">Color of the pen to be retrieved</param>
        public static Pen GetPen(Color clr)
        {
            if (PenCache.ContainsKey(clr))
                return PenCache[clr];

            Pen p = new Pen(clr, 3);
            PenCache[clr] = p;
            return p;
        }

        /// <summary>
        /// Returns a brush of the specified color. The brush will be retrieved from the cache
        /// in case it exists; otherwise it will be created and cached.
        /// </summary>
        /// <param name="clr">Color of the brush to be retrieved</param>
        public static Brush GetBrush(Color clr)
        {
            if (BrushCache.ContainsKey(clr))
                return BrushCache[clr];

            SolidBrush b = new SolidBrush(clr);
            BrushCache[clr] = b;
            return b;
        }
    }

    /// <summary>
    /// WinAPI function wrappers
    /// </summary>
    public static class WinAPI
    {
        static WinAPI()
        {
            QueryPerformanceFrequency(out PerformanceFreq);
        }

        [DllImport("Kernel32.dll")]
        public static extern bool QueryPerformanceCounter(
            out long lpPerformanceCount);

        [DllImport("Kernel32.dll")]
        public static extern bool QueryPerformanceFrequency(
            out long lpFrequency);

        public static readonly long PerformanceFreq;
    }

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
            return ContainsPoint(r.Left,  r.Top) || ContainsPoint(r.Left,  r.Bottom)
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
