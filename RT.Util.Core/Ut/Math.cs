using System;
using System.Collections.Generic;
using System.Text;

namespace RT.Util
{
#if EXPORT_UTIL
    public
#endif
    static partial class Ut
    {
        /// <summary>Calculates the greatest common divisor of <paramref name="a"/> and <paramref name="b"/>.</summary>
        public static int Gcd(int a, int b)
        {
            while (a != 0 && b != 0)
            {
                if (a > b)
                    a %= b;
                else
                    b %= a;
            }
            return a | b;
        }
        /// <summary>Calculates the greatest common divisor of <paramref name="a"/> and <paramref name="b"/>.</summary>
        public static uint Gcd(uint a, uint b)
        {
            while (a != 0 && b != 0)
            {
                if (a > b)
                    a %= b;
                else
                    b %= a;
            }
            return a | b;
        }
        /// <summary>Calculates the greatest common divisor of <paramref name="a"/> and <paramref name="b"/>.</summary>
        public static long Gcd(long a, long b)
        {
            while (a != 0 && b != 0)
            {
                if (a > b)
                    a %= b;
                else
                    b %= a;
            }
            return a | b;
        }
        /// <summary>Calculates the greatest common divisor of <paramref name="a"/> and <paramref name="b"/>.</summary>
        public static ulong Gcd(ulong a, ulong b)
        {
            while (a != 0 && b != 0)
            {
                if (a > b)
                    a %= b;
                else
                    b %= a;
            }
            return a | b;
        }
    }
}
