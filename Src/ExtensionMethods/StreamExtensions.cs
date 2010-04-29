using System;
using System.IO;
using System.Text;

namespace RT.Util.ExtensionMethods
{
    /// <summary>
    /// Provides extension methods on the <see cref="Stream"/> type.
    /// </summary>
    public static class StreamExtensions
    {
        /// <summary>
        /// Reads all bytes until the end of stream and returns them in a byte array.
        /// </summary>
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

        /// <summary>
        /// Reads all bytes from the current Stream and converts them into text using the specified encoding.
        /// </summary>
        /// <param name="stream">Stream to read from.</param>
        /// <param name="encoding">Encoding to expect the text to be in.</param>
        /// <returns>The text read from the stream.</returns>
        public static string ReadAllText(this Stream stream, Encoding encoding)
        {
            var txt = encoding.GetString(stream.ReadAllBytes());
            return txt;
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
                Array.Resize(ref buf, length);
            return buf;
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

        #endregion
    }
}
