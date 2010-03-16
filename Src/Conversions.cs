using System;
using System.Diagnostics;
using System.Text;

namespace RT.KitchenSink
{
#pragma warning disable 1591    // Missing XML comment for publicly visible type or member
    
    public static class Conversions
    {
        public const string Base32RChars = "abcdefghijkmnpqrstuvwxyz23456789";
        public static int[] Base32RInverse; // inverse base-32-roman lookup table

        static Conversions()
        {
            // Initialise the base-32 inverse lookup table
            Base32RInverse = new int[256];
            for (int i = 0; i < Base32RInverse.Length; i++)
                Base32RInverse[i] = -1;
            for (int i = 0; i < Base32RChars.Length; i++)
                Base32RInverse[(int) Base32RChars[i]] = i;
        }

        public static string Base32REncode(this byte[] bytes)
        {
            StringBuilder result = new StringBuilder();
            int i = 0;
            int bytesLeft = bytes.Length;
            while (bytesLeft > 0)
            {
                // 00000 00011 11111 12222 22223 33333 33444 44444
                byte b0 = bytes[i];
                byte b1 = bytesLeft >= 2 ? bytes[i + 1] : (byte) 0;
                byte b2 = bytesLeft >= 3 ? bytes[i + 2] : (byte) 0;
                byte b3 = bytesLeft >= 4 ? bytes[i + 3] : (byte) 0;
                byte b4 = bytesLeft >= 5 ? bytes[i + 4] : (byte) 0;
                result.Append(Base32RChars[b0 >> 3]); // 0
                result.Append(Base32RChars[(b0 & 7) << 2 | b1 >> 6]); // 1
                if (--bytesLeft == 0) break;
                result.Append(Base32RChars[(b1 & 62) >> 1]); // 2
                result.Append(Base32RChars[(b1 & 1) << 4 | b2 >> 4]); // 3
                if (--bytesLeft == 0) break;
                result.Append(Base32RChars[(b2 & 15) << 1 | b3 >> 7]); // 4
                if (--bytesLeft == 0) break;
                result.Append(Base32RChars[(b3 & 124) >> 2]); // 5
                result.Append(Base32RChars[(b3 & 3) << 3 | b4 >> 5]); // 6
                if (--bytesLeft == 0) break;
                result.Append(Base32RChars[b4 & 31]); // 7
                --bytesLeft;
                i += 5;
            }

            return result.ToString();
        }

        public static byte[] Base32RDecode(this string input)
        {
            // See how many bytes are encoded at the end of the string
            int padding = input.Length % 8;
            if (padding == 1 || padding == 3 || padding == 6)
                throw new ArgumentException("The input string to Base32RDecode is not a valid base-32 encoded string");

            padding = (padding + 1) / 2; // convert the valid values as follows: 0=>0, 2=>1, 4=>2, 5=>3, 7=>4
            byte[] result = new byte[(input.Length / 8) * 5 + padding];

            int ri = 0, ii = 0; // result index & input index
            int charsLeft = input.Length;
            while (charsLeft > 0)
            {
                // 00000111 11222223 33334444 45555566 66677777

                uint c0 = checked((uint) Base32RInverse[input[ii++]]);
                uint c1 = checked((uint) Base32RInverse[input[ii++]]);
                result[ri++] = (byte) (c0 << 3 | c1 >> 2);
                if ((charsLeft -= 2) <= 0) break;

                uint c2 = checked((uint) Base32RInverse[input[ii++]]);
                uint c3 = checked((uint) Base32RInverse[input[ii++]]);
                result[ri++] = (byte) ((c1 & 3) << 6 | c2 << 1 | c3 >> 4);
                if ((charsLeft -= 2) <= 0) break;

                uint c4 = checked((uint) Base32RInverse[input[ii++]]);
                result[ri++] = (byte) ((c3 & 15) << 4 | (c4 >> 1));
                if ((charsLeft -= 1) <= 0) break;

                uint c5 = checked((uint) Base32RInverse[input[ii++]]);
                uint c6 = checked((uint) Base32RInverse[input[ii++]]);
                result[ri++] = (byte) ((c4 & 1) << 7 | c5 << 2 | c6 >> 3);
                if ((charsLeft -= 2) <= 0) break;

                uint c7 = checked((uint) Base32RInverse[input[ii++]]);
                result[ri++] = (byte) ((c6 & 7) << 5 | c7);
                charsLeft -= 1;
            }

            return result;
        }
    }

    static class Tests
    {
        private static void assertBase32Array(byte[] arr)
        {
            string b64u = arr.Base32REncode();
            byte[] dec = b64u.Base32RDecode();
            Debug.Assert(arr.Length == dec.Length);
            for (int i = 0; i < arr.Length; i++)
                Debug.Assert(arr[i] == dec[i]);
        }

        public static void TestBase32()
        {
            assertBase32Array(new byte[] { });

            for (int i = 0; i < 256; i++)
                assertBase32Array(new byte[] { (byte) i });

            for (byte i = 5; i < 200; i += 61) // 5, 66, 127, 188
                for (int j = 0; j < 256; j++)
                    assertBase32Array(new byte[] { i, (byte) j });

            for (byte i = 5; i < 200; i += 61)
                for (int j = 0; j < 256; j++)
                    assertBase32Array(new byte[] { i, i, (byte) j });

            for (byte i = 5; i < 200; i += 61)
                for (int j = 0; j < 256; j++)
                    assertBase32Array(new byte[] { i, i, i, (byte) j });

            for (byte i = 5; i < 200; i += 61)
                for (int j = 0; j < 256; j++)
                    assertBase32Array(new byte[] { i, i, i, i, (byte) j });

            for (byte i = 5; i < 200; i += 61)
                for (int j = 0; j < 256; j++)
                    assertBase32Array(new byte[] { i, i, (byte) j, i, i, (byte) j });

            for (byte i = 5; i < 200; i += 61)
                for (int j = 0; j < 256; j++)
                    assertBase32Array(new byte[] { i, i, i, (byte) j, i, i, (byte) j });

            for (byte i = 5; i < 200; i += 61)
                for (int j = 0; j < 256; j++)
                    assertBase32Array(new byte[] { i, (byte) j, i, i, i, i, i, (byte) j });
        }
    }

}
