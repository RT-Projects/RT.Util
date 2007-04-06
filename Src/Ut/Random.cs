/// Random.cs  -  random number generation functions

using System;

namespace RT.Util
{
    /// <summary>
    /// This class offers some generic static functions which are hard to categorize
    /// under any more specific classes.
    /// </summary>
    public static partial class Ut
    {
        /// <summary>
        /// An application-wide random number generator - use this generator if all you
        /// need is a random number. Create a new generator only if you really need to.
        /// </summary>
        public static Random Rnd = new Random();

        /// <summary>
        /// Returns a random double between 0.0 and 1.0. It is unclear whether 0 or 1
        /// is ever returned by this function.
        /// </summary>
        public static double RndDouble()
        {
            return Rnd.NextDouble();
        }

        /// <summary>
        /// Returns a random double between Min and Max. It is unclear whether
        /// Min or Max can ever be returned.
        /// </summary>
        public static double RndDouble(double Min, double Max)
        {
            return Rnd.NextDouble() * (Max - Min) + Min;
        }

        /// <summary>
        /// Returns a random non-negative integer.
        /// </summary>
        public static int RndInt()
        {
            return Rnd.Next();
        }

        /// <summary>
        /// Returns a random integer between Min (inclusive) and Max (exclusive).
        /// </summary>
        public static int RndInt(int Min, int Max)
        {
            return Rnd.Next(Min, Max);
        }
    }
}
