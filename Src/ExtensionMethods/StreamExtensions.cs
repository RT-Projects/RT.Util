using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RT.Util.ExtensionMethods
{
    /// <summary>
    /// Provides extension methods on the <see cref="Stream"/> type.
    /// </summary>
    public static class StreamExtensions
    {
        /// <summary>Reads all bytes until the end of stream and returns them in a byte array.</summary>
        public static byte[] ReadAllBytes(this Stream stream)
        {
            if (stream.CanSeek)
            {
                return stream.Read((int) (stream.Length - stream.Position));
            }
            else
            {
                byte[] buffer = new byte[32768];
                using (MemoryStream ms = new MemoryStream())
                {
                    while (true)
                    {
                        int read = stream.Read(buffer, 0, buffer.Length);
                        if (read <= 0)
                            return ms.ToArray();
                        ms.Write(buffer, 0, read);
                    }
                }
            }
        }

        /// <summary>Reads all bytes until the end of stream and returns them in a byte array.</summary>
        public static async Task<byte[]> ReadAllBytesAsync(this Stream stream, CancellationToken? token = null)
        {
            if (stream.CanSeek)
                return await stream.ReadAsync((int)(stream.Length - stream.Position), token);

            byte[] buffer = new byte[32768];
            using (MemoryStream ms = new MemoryStream())
            {
                while (true)
                {
                    int read = await stream.ReadAsync(buffer, 0, buffer.Length, token ?? CancellationToken.None).ConfigureAwait(false);
                    if (read <= 0)
                        return ms.ToArray();
                    ms.Write(buffer, 0, read);
                }
            }
        }

        /// <summary>Reads all bytes until the end of stream and returns the number
        /// of bytes thus read without allocating too much memory.</summary>
        public static long ReadAllBytesGetLength(this Stream stream)
        {
            if (stream.CanSeek)
                return stream.Length - stream.Position;

            byte[] buffer = new byte[32768];
            long lengthSoFar = 0;
            while (true)
            {
                int read = stream.Read(buffer, 0, buffer.Length);
                if (read <= 0)
                    return lengthSoFar;
                lengthSoFar += read;
            }
        }

        /// <summary>Reads all bytes until the end of stream and returns the number
        /// of bytes thus read without allocating too much memory.</summary>
        public static async Task<long> ReadAllBytesGetLengthAsync(this Stream stream, CancellationToken? token = null)
        {
            if (stream.CanSeek)
                return stream.Length - stream.Position;

            byte[] buffer = new byte[32768];
            long lengthSoFar = 0;
            while (true)
            {
                int read = await stream.ReadAsync(buffer, 0, buffer.Length, token ?? CancellationToken.None).ConfigureAwait(false);
                if (read <= 0)
                    return lengthSoFar;
                lengthSoFar += read;
            }
        }

        /// <summary>Reads all bytes from the current Stream and converts them into text using the specified encoding.</summary>
        /// <param name="stream">Stream to read from.</param>
        /// <param name="encoding">Encoding to expect the text to be in. If <c>null</c> then the UTF-8 encoding is used.</param>
        /// <returns>The text read from the stream.</returns>
        public static string ReadAllText(this Stream stream, Encoding encoding = null)
        {
            using (var sr = new StreamReader(stream, encoding ?? Encoding.UTF8))
                return sr.ReadToEnd();
        }

        /// <summary>Reads all bytes from the current Stream and converts them into text using the specified encoding.</summary>
        /// <param name="stream">Stream to read from.</param>
        /// <param name="encoding">Encoding to expect the text to be in. If <c>null</c> then the UTF-8 encoding is used.</param>
        /// <returns>The text read from the stream.</returns>
        public static async Task<string> ReadAllTextAsync(this Stream stream, Encoding encoding = null, CancellationToken? token = null)
        {
            byte[] buffer = new byte[32768];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = await stream.ReadAsync(buffer, 0, buffer.Length, token ?? CancellationToken.None).ConfigureAwait(false)) > 0)
                    ms.Write(buffer, 0, read);
                return ms.ToArray().FromUtf8();
            }
        }

        /// <summary>
        /// Attempts to read the specified number of bytes from the stream. If there are fewer bytes left
        /// before the end of the stream, a shorter (possibly empty) array is returned.
        /// </summary>
        /// <param name="stream">Stream to read from.</param>
        /// <param name="length">Number of bytes to read from the stream.</param>
        public static byte[] Read(this Stream stream, int length)
        {
            byte[] buf = new byte[length];
            int read = stream.FillBuffer(buf, 0, length);
            if (read < length)
                Array.Resize(ref buf, read);
            return buf;
        }

        /// <summary>
        /// Attempts to read the specified number of bytes from the stream. If there are fewer bytes left
        /// before the end of the stream, a shorter (possibly empty) array is returned.
        /// </summary>
        /// <param name="stream">Stream to read from.</param>
        /// <param name="length">Number of bytes to read from the stream.</param>
        public static async Task<byte[]> ReadAsync(this Stream stream, int length, CancellationToken? token = null)
        {
            byte[] buf = new byte[length];
            int read = await stream.FillBufferAsync(buf, 0, length, token);
            if (read < length)
                Array.Resize(ref buf, read);
            return buf;
        }

        /// <summary>Writes the specified data to the current stream.</summary>
        public static void Write(this Stream stream, byte[] data)
        {
            stream.Write(data, 0, data.Length);
        }

        /// <summary>Writes the specified data to the current stream.</summary>
        public static Task WriteAsync(this Stream stream, byte[] data, CancellationToken? token = null)
        {
            return stream.WriteAsync(data, 0, data.Length, token ?? CancellationToken.None);
        }

        /// <summary>
        /// Attempts to fill the buffer with the specified number of bytes from the stream. If there are
        /// fewer bytes left in the stream than requested then all available bytes will be read into the buffer.
        /// </summary>
        /// <param name="stream">Stream to read from.</param>
        /// <param name="buffer">Buffer to write the bytes to.</param>
        /// <param name="offset">Offset at which to write the first byte read from the stream.</param>
        /// <param name="length">Number of bytes to read from the stream.</param>
        /// <returns>Number of bytes read from the stream into buffer. This may be less than requested, but only if the stream ended before the required number of bytes were read.</returns>
        public static int FillBuffer(this Stream stream, byte[] buffer, int offset, int length)
        {
            int totalRead = 0;
            while (length > 0)
            {
                var read = stream.Read(buffer, offset, length);
                if (read == 0)
                    return totalRead;
                offset += read;
                length -= read;
                totalRead += read;
            }
            return totalRead;
        }

        /// <summary>
        /// Attempts to fill the buffer with the specified number of bytes from the stream. If there are
        /// fewer bytes left in the stream than requested then all available bytes will be read into the buffer.
        /// </summary>
        /// <param name="stream">Stream to read from.</param>
        /// <param name="buffer">Buffer to write the bytes to.</param>
        /// <param name="offset">Offset at which to write the first byte read from the stream.</param>
        /// <param name="length">Number of bytes to read from the stream.</param>
        /// <returns>Number of bytes read from the stream into buffer. This may be less than requested, but only if the stream ended before the required number of bytes were read.</returns>
        public static async Task<int> FillBufferAsync(this Stream stream, byte[] buffer, int offset, int length, CancellationToken? token = null)
        {
            int totalRead = 0;
            while (length > 0)
            {
                var read = await stream.ReadAsync(buffer, offset, length, token ?? CancellationToken.None).ConfigureAwait(false);
                if (read == 0)
                    return totalRead;
                offset += read;
                length -= read;
                totalRead += read;
            }
            return totalRead;
        }

        /// <summary>Encodes the specified string as UTF-8 and writes it to the current stream.</summary>
        /// <param name="stream">Stream to write text to.</param>
        /// <param name="text">Text to write to the stream as UTF-8.</param>
        public static void WriteUtf8(this Stream stream, string text)
        {
            var data = text.ToUtf8();
            stream.Write(data, 0, data.Length);
        }

        #region Optim write

        /// <summary>Encodes a 32-bit signed integer in a variable number of bytes, using fewer bytes for values closer to zero.</summary>
        /// <remarks>
        /// <para>Writes an integer 7 bits at a time. This allows small integers to be
        /// stored in 1 byte, longer ones in 2, at the cost of storing the longest
        /// ones in 5 bytes.</para>
        /// 
        /// <para>Example for a positive int:</para>
        /// <code>00000000 00000000 01010101 01010101</code>
        /// <para>becomes three bytes:</para>
        /// <code>1,1010101 1,0101010 0,0000001</code>
        /// 
        /// <para>Example for a negative int:</para>
        /// <code>11111111 11111111 11010101 01010101</code>
        /// <para>becomes three bytes:</para>
        /// <code>1,1010101 1,0101010 0,1111111</code>
        /// 
        /// <para>Note how an extra byte is needed in this example. This is similar to
        /// requiring a sign bit, however this way the positive values are directly
        /// compatible with unsigned Optim values.</para>
        /// </remarks>
        public static void WriteInt32Optim(this Stream stream, int val)
        {
            while (val < -64 || val > 63)
            {
                stream.WriteByte((byte) (val | 128));
                val >>= 7;
            }
            stream.WriteByte((byte) (val & 127));
        }

        /// <summary>Encodes a 32-bit unsigned integer in a variable number of bytes, using fewer bytes for smaller values.</summary>
        /// <remarks>See <see cref="WriteInt32Optim"/> for the precise encoding.</remarks>
        public static void WriteUInt32Optim(this Stream stream, uint val)
        {
            while (val >= 128)
            {
                stream.WriteByte((byte) (val | 128));
                val >>= 7;
            }
            stream.WriteByte((byte) val);
        }

        /// <summary>Encodes a 64-bit signed integer in a variable number of bytes, using fewer bytes for values closer to zero.</summary>
        /// <remarks>See <see cref="WriteInt32Optim"/> for the precise encoding.</remarks>
        public static void WriteInt64Optim(this Stream stream, long val)
        {
            while (val < -64 || val > 63)
            {
                stream.WriteByte((byte) (val | 128));
                val >>= 7;
            }
            stream.WriteByte((byte) (val & 127));
        }

        /// <summary>Encodes a 64-bit unsigned integer in a variable number of bytes, using fewer bytes for smaller values.</summary>
        /// <remarks>See <see cref="WriteInt32Optim"/> for the precise encoding.</remarks>
        public static void WriteUInt64Optim(this Stream stream, ulong val)
        {
            while (val >= 128)
            {
                stream.WriteByte((byte) (val | 128));
                val >>= 7;
            }
            stream.WriteByte((byte) val);
        }

        /// <summary>Encodes a decimal in a variable number of bytes, using fewer bytes for frequently-occurring low-precision values.</summary>
        /// <remarks>
        /// <para>The first byte is a "header" byte. Its top bit indicates the sign of the value, while the remaining 7 bits encode the scale
        /// and the length, in bytes, of the mantissa component. Since the scale can be anything between 0..28 and the length can be up to 12,
        /// this number is simply an index into a lookup table which contains specific combinations of both values. These combinations were
        /// selected by analyzing the actual distribution of mantissa length + exponent pairs making a few assumptions about the likely inputs
        /// into arithmetic operations. The encoder makes sure to select a value representing the exact scale and the minimum representable
        /// mantissa length.</para>
        /// <para>The result is always at most 13 bytes long, which is the same as discarding the three unused bytes of the raw representation.</para>
        /// </remarks>
        public static void WriteDecimalOptim(this Stream stream, decimal val)
        {
            // .NET allows int[] to be cast to uint[] so just fool C# compiler into accepting this.
            uint[] bits = (uint[]) (object) decimal.GetBits(val);
            uint exponent = (bits[3] >> 16) & 31;
            bool negative = (bits[3] & 0x80000000U) != 0;

            var bytes = new byte[12];
            bytes[0] = (byte) (bits[0]);
            bytes[1] = (byte) (bits[0] >> 8);
            bytes[2] = (byte) (bits[0] >> 16);
            bytes[3] = (byte) (bits[0] >> 24);
            if (bits[1] != 0 || bits[2] != 0)
            {
                bytes[4] = (byte) (bits[1]);
                bytes[5] = (byte) (bits[1] >> 8);
                bytes[6] = (byte) (bits[1] >> 16);
                bytes[7] = (byte) (bits[1] >> 24);
            }
            if (bits[2] != 0)
            {
                bytes[8] = (byte) (bits[2]);
                bytes[9] = (byte) (bits[2] >> 8);
                bytes[10] = (byte) (bits[2] >> 16);
                bytes[11] = (byte) (bits[2] >> 24);
            }

            // Count the bytes we need to save. This is 12 minus the number of trailing (most significant) zero bytes.
            int dataBytes = 0;
            for (int i = 0; i < 12; i++)
                if (bytes[i] != 0)
                    dataBytes = i + 1;

            var lut = decimalOptimEncodeLUT[exponent];
            // Locate the next representable data bytes count which is at least as large as what we need.
            int dataBytesRepresentable = dataBytes;
            for (; dataBytesRepresentable <= 12; dataBytesRepresentable++)
                if (lut[dataBytesRepresentable] != 255)
                    break;

            byte header = lut[dataBytesRepresentable];
            if (negative)
                header |= 0x80;
            stream.WriteByte(header);
            stream.Write(bytes, 0, dataBytesRepresentable);
        }

        private static byte[][] _decimalOptimEncodeLUT;

        private static byte[][] decimalOptimEncodeLUT
        {
            get
            {
                if (_decimalOptimEncodeLUT == null)
                    _decimalOptimEncodeLUT = Ut.NewArray(
                        new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 },
                        new byte[] { 255, 13, 14, 15, 16, 17, 255, 255, 255, 255, 255, 255, 18 },
                        new byte[] { 255, 19, 20, 21, 22, 23, 255, 255, 255, 255, 255, 255, 24 },
                        new byte[] { 255, 25, 26, 27, 28, 29, 255, 255, 255, 255, 255, 255, 30 },
                        new byte[] { 255, 31, 32, 33, 34, 35, 36, 255, 255, 255, 255, 255, 37 },
                        new byte[] { 255, 38, 39, 40, 41, 42, 43, 255, 255, 255, 255, 255, 44 },
                        new byte[] { 255, 255, 45, 46, 47, 48, 49, 255, 255, 255, 255, 255, 50 },
                        new byte[] { 255, 255, 51, 52, 53, 54, 55, 255, 255, 255, 255, 255, 56 },
                        new byte[] { 255, 255, 255, 57, 58, 255, 255, 59, 255, 255, 255, 255, 60 },
                        new byte[] { 255, 255, 255, 61, 62, 255, 255, 255, 63, 255, 255, 255, 64 },
                        new byte[] { 255, 255, 255, 65, 255, 66, 255, 255, 67, 255, 255, 255, 68 },
                        new byte[] { 255, 255, 255, 69, 255, 255, 70, 255, 71, 255, 255, 255, 72 },
                        new byte[] { 255, 255, 255, 73, 255, 255, 74, 255, 75, 255, 255, 255, 76 },
                        new byte[] { 255, 255, 255, 77, 255, 255, 78, 255, 79, 255, 255, 255, 80 },
                        new byte[] { 255, 255, 255, 81, 255, 255, 82, 255, 83, 255, 255, 255, 84 },
                        new byte[] { 255, 255, 255, 85, 255, 255, 255, 86, 255, 87, 255, 255, 88 },
                        new byte[] { 255, 255, 255, 89, 255, 255, 255, 255, 255, 90, 255, 255, 91 },
                        new byte[] { 255, 255, 255, 255, 92, 255, 255, 255, 255, 93, 255, 255, 94 },
                        new byte[] { 255, 255, 255, 255, 255, 95, 255, 255, 255, 96, 255, 255, 97 },
                        new byte[] { 255, 255, 255, 255, 255, 255, 98, 255, 255, 255, 255, 99, 100 },
                        new byte[] { 255, 255, 255, 255, 255, 255, 101, 255, 255, 255, 255, 102, 103 },
                        new byte[] { 255, 255, 255, 255, 255, 255, 104, 255, 255, 255, 255, 105, 106 },
                        new byte[] { 255, 255, 255, 255, 255, 255, 255, 107, 255, 255, 255, 108, 109 },
                        new byte[] { 255, 255, 255, 255, 255, 255, 255, 255, 110, 255, 255, 111, 112 },
                        new byte[] { 255, 255, 255, 255, 255, 255, 255, 255, 113, 255, 255, 114, 115 },
                        new byte[] { 255, 255, 255, 255, 255, 255, 255, 255, 116, 255, 117, 255, 118 },
                        new byte[] { 255, 255, 255, 255, 255, 255, 255, 255, 119, 255, 120, 255, 121 },
                        new byte[] { 255, 255, 255, 255, 255, 255, 255, 255, 122, 255, 123, 255, 124 },
                        new byte[] { 255, 255, 255, 255, 255, 255, 255, 255, 125, 255, 126, 255, 127 }
                    );
                return _decimalOptimEncodeLUT;
            }
        }

        #endregion

        #region Optim read

        /// <summary>Decodes an integer encoded by <see cref="StreamExtensions.WriteInt32Optim"/> or <see cref="StreamExtensions.WriteUInt32Optim"/>.</summary>
        public static int ReadInt32Optim(this Stream stream)
        {
            byte b = 255;
            int shifts = 0;
            int res = 0;
            while (b > 127)
            {
                int read = stream.ReadByte();
                if (read < 0) throw new InvalidOperationException("Unexpected end of stream (#56384)");
                b = (byte) read;
                res = res | ((int) (b & 127) << shifts);
                shifts += 7;
            }
            // Sign-extend
            if (shifts >= 32) // can only be 28 or 35
                return res;
            shifts = 32 - shifts;
            return (res << shifts) >> shifts;
        }

        /// <summary>Decodes an integer encoded by <see cref="StreamExtensions.WriteUInt32Optim"/>.</summary>
        public static uint ReadUInt32Optim(this Stream stream)
        {
            byte b = 255;
            int shifts = 0;
            uint res = 0;
            while (b > 127)
            {
                int read = stream.ReadByte();
                if (read < 0) throw new InvalidOperationException("Unexpected end of stream (#25753)");
                b = (byte) read;
                res = res | ((uint) (b & 127) << shifts);
                shifts += 7;
            }
            return res;
        }

        /// <summary>Decodes an integer encoded by <see cref="StreamExtensions.WriteInt64Optim"/> or <see cref="StreamExtensions.WriteUInt64Optim"/>.</summary>
        public static long ReadInt64Optim(this Stream stream)
        {
            byte b = 255;
            int shifts = 0;
            long res = 0;
            while (b > 127)
            {
                int read = stream.ReadByte();
                if (read < 0) throw new InvalidOperationException("Unexpected end of stream (#16854)");
                b = (byte) read;
                res = res | ((long) (b & 127) << shifts);
                shifts += 7;
            }
            // Sign-extend
            if (shifts >= 64) // can only be 63 or 70
                return res;
            shifts = 64 - shifts;
            return (res << shifts) >> shifts;
        }

        /// <summary>Decodes an integer encoded by <see cref="StreamExtensions.WriteUInt64Optim"/>.</summary>
        public static ulong ReadUInt64Optim(this Stream stream)
        {
            byte b = 255;
            int shifts = 0;
            ulong res = 0;
            while (b > 127)
            {
                int read = stream.ReadByte();
                if (read < 0) throw new InvalidOperationException("Unexpected end of stream (#64783)");
                b = (byte) read;
                res = res | ((ulong) (b & 127) << shifts);
                shifts += 7;
            }
            return res;
        }

        /// <summary>Decodes a decimal encoded by <see cref="StreamExtensions.WriteDecimalOptim"/>.</summary>
        public static decimal ReadDecimalOptim(this Stream stream)
        {
            int header = stream.ReadByte();
            if (header < 0) throw new InvalidOperationException("Unexpected end of stream (#94313)");
            bool negative = (header & 0x80) != 0;
            short expolen = decimalOptimDecodeLUT[header & 127];
            int len = expolen >> 8;

            var bytes = new byte[12];
            int read = stream.FillBuffer(bytes, 0, (byte) len);
            if (read != len) throw new InvalidOperationException("Unexpected end of stream (#94314)");

            return new decimal(BitConverter.ToInt32(bytes, 0), BitConverter.ToInt32(bytes, 4), BitConverter.ToInt32(bytes, 8), negative, (byte) expolen);
        }

        private static short[] _decimalOptimDecodeLUT;

        private static short[] decimalOptimDecodeLUT
        {
            get
            {
                if (_decimalOptimDecodeLUT == null)
                    _decimalOptimDecodeLUT = new short[] {
                        0, 256, 512, 768, 1024, 1280, 1536, 1792, 2048, 2304, 2560, 2816, 3072, 257, 513, 769, 1025, 1281, 3073, 
                        258, 514, 770, 1026, 1282, 3074, 259, 515, 771, 1027, 1283, 3075, 
                        260, 516, 772, 1028, 1284, 1540, 3076, 261, 517, 773, 1029, 1285, 1541, 3077, 
                        518, 774, 1030, 1286, 1542, 3078, 519, 775, 1031, 1287, 1543, 3079, 776, 1032, 1800, 3080, 
                        777, 1033, 2057, 3081, 778, 1290, 2058, 3082, 779, 1547, 2059, 3083, 780, 1548, 2060, 3084, 
                        781, 1549, 2061, 3085, 782, 1550, 2062, 3086, 783, 1807, 2319, 3087, 784, 2320, 3088, 
                        1041, 2321, 3089, 1298, 2322, 3090, 1555, 2835, 3091, 1556, 2836, 3092, 1557, 2837, 3093, 1814, 2838, 3094, 
                        2071, 2839, 3095, 2072, 2840, 3096, 2073, 2585, 3097, 2074, 2586, 3098, 2075, 2587, 3099, 2076, 2588, 3100, 
                    };
                return _decimalOptimDecodeLUT;
            }
        }

        #endregion
    }
}
