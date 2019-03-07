using System;
using System.Security.Cryptography;

namespace RT.Util
{
    /// <summary>This class offers static functions which generate random numbers in a thread-safe manner.</summary>
    public static class Rnd
    {
        private static Random _rnd = new Random();

        /// <summary>Resets the random number generator using the specified <paramref name="seed"/>.</summary>
        public static void Reset(int seed)
        {
            _rnd = new Random(seed);
        }

        /// <summary>Returns a random double between 0.0 and 1.0.</summary>
        public static double NextDouble()
        {
            lock (_rnd)
                return _rnd.NextDouble();
        }

        /// <summary>
        ///     Returns a random double between <paramref name="min"/> and <paramref name="max"/>. It is unclear whether
        ///     <paramref name="min"/> or <paramref name="max"/> can ever be returned.</summary>
        public static double NextDouble(double min, double max)
        {
            lock (_rnd)
                return _rnd.NextDouble() * (max - min) + min;
        }

        /// <summary>Returns a random non-negative integer.</summary>
        public static int Next()
        {
            lock (_rnd)
                return _rnd.Next();
        }

        /// <summary>Returns a non-negative random number less than <paramref name="max"/>.</summary>
        public static int Next(int max)
        {
            lock (_rnd)
                return _rnd.Next(max);
        }

        /// <summary>
        ///     Returns a random integer between <paramref name="min"/> (inclusive) and <paramref name="max"/> (exclusive).</summary>
        public static int Next(int min, int max)
        {
            lock (_rnd)
                return _rnd.Next(min, max);
        }

        /// <summary>Returns a random boolean.</summary>
        public static bool NextBoolean()
        {
            lock (_rnd)
                return _rnd.Next(0, 2) != 0;
        }

        /// <summary>Fills the specified buffer with random bytes.</summary>
        public static void NextBytes(byte[] buffer)
        {
            lock (_rnd)
                _rnd.NextBytes(buffer);
        }

        /// <summary>Returns a new array with the specified number of elements, filled with random bytes.</summary>
        public static byte[] NextBytes(int count)
        {
            var buffer = new byte[count];
            lock (_rnd)
                _rnd.NextBytes(buffer);
            return buffer;
        }

        /// <summary>Returns a random non-negative 64-bit integer.</summary>
        public static long NextLong()
        {
            lock (_rnd)
                return BitConverter.ToInt64(NextBytes(8), 0) & 0x7fffffffffffffff;
        }

        /// <summary>
        ///     Generates a random string of the specified length, taking characters from the specified arsenal of characters.</summary>
        /// <param name="length">
        ///     Length of the string to generate.</param>
        /// <param name="takeCharactersFrom">
        ///     Arsenal to take random characters from. (Default is upper- and lower-case letters and digits.)</param>
        /// <param name="rnd">
        ///     If not <c>null</c>, uses the specified random number generator.</param>
        public static string GenerateString(int length, string takeCharactersFrom = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789", Random rnd = null)
        {
            if (takeCharactersFrom == null)
                throw new ArgumentNullException(nameof(takeCharactersFrom));
            return new string(Ut.NewArray(length, i => takeCharactersFrom[rnd == null ? Rnd.Next(takeCharactersFrom.Length) : rnd.Next(takeCharactersFrom.Length)]));
        }
    }

    /// <summary>
    ///     This class offers static functions which generate cryptographically-strong random numbers in a thread-safe manner.</summary>
    public static class RndCrypto
    {
        /// <summary>This class is documented to be completely thread-safe, so no locking is required.</summary>
        private static RNGCryptoServiceProvider _rnd = new RNGCryptoServiceProvider();

        /// <summary>Fills the specified buffer with cryptographically-strong random bytes.</summary>
        public static void NextBytes(byte[] buffer)
        {
            _rnd.GetBytes(buffer); // "this method is thread safe"
        }

        /// <summary>
        ///     Returns a new array with the specified number of elements, filled with cryptographically-strong random bytes.</summary>
        public static byte[] NextBytes(int count)
        {
            var bytes = new byte[count];
            _rnd.GetBytes(bytes); // "this method is thread safe"
            return bytes;
        }

        /// <summary>Returns a random non-negative integer.</summary>
        public static int Next()
        {
            return BitConverter.ToInt32(NextBytes(4), 0) & 0x7fffffff;
        }

        /// <summary>Returns a non-negative random number less than <paramref name="max"/>.</summary>
        public static int Next(int max)
        {
            return Next() % max;
        }

        /// <summary>
        ///     Returns a random integer between <paramref name="min"/> (inclusive) and <paramref name="max"/> (exclusive).</summary>
        public static int Next(int min, int max)
        {
            return min + Next(max - min);
        }

        /// <summary>Returns a random non-negative 64-bit integer.</summary>
        public static long NextLong()
        {
            return BitConverter.ToInt64(NextBytes(8), 0) & 0x7fffffffffffffff;
        }

        /// <summary>Returns a random double between 0.0 and 1.0.</summary>
        public static double NextDouble()
        {
            return (double) Next() / (double) int.MaxValue;
        }

        /// <summary>
        ///     Returns a random double between <paramref name="min"/> and <paramref name="max"/>. It is unclear whether
        ///     <paramref name="min"/> or <paramref name="max"/> can ever be returned.</summary>
        public static double NextDouble(double min, double max)
        {
            return NextDouble() * (max - min) + min; // it might be possible to do better than this by using Next() directly?
        }

        /// <summary>Returns a random boolean.</summary>
        public static bool NextBoolean()
        {
            return Next(0, 2) != 0;
        }

        /// <summary>
        ///     Generates a random string of the specified length, taking characters from the specified arsenal of characters.</summary>
        /// <param name="length">
        ///     Length of the string to generate.</param>
        /// <param name="takeCharactersFrom">
        ///     Arsenal to take random characters from. (Default is upper- and lower-case letters and digits.)</param>
        public static string GenerateString(int length, string takeCharactersFrom = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789")
        {
            if (takeCharactersFrom == null)
                throw new ArgumentNullException(nameof(takeCharactersFrom));
            var chars = new char[length];
            var bytes = NextBytes(length * 4);
            for (int i = 0; i < length; i++)
            {
                bytes[i * 4 + 3] = 0;
                chars[i] = takeCharactersFrom[BitConverter.ToInt32(bytes, i * 4) % takeCharactersFrom.Length];
            }
            return new string(chars);
        }
    }
}
