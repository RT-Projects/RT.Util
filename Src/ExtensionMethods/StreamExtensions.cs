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
                var len = (int) (stream.Length - stream.Position);
                byte[] buffer = new byte[len];
                stream.Read(buffer, 0, len);
                return buffer;
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
        /// before the end of the stream, a shorter array is returned. If the end of the stream is already
        /// reached, returns null.
        /// </summary>
        /// <param name="stream">Stream to read from.</param>
        /// <param name="length">Number of bytes to read from the stream.</param>
        public static byte[] Read(this Stream stream, int length)
        {
            byte[] buf = new byte[length];
            var bytesRead = stream.Read(buf, 0, length);
            if (bytesRead == length)
                return buf;
            if (bytesRead == 0)
                return null;
            byte[] result = new byte[bytesRead];
            Buffer.BlockCopy(buf, 0, result, 0, bytesRead);
            return result;
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

        /// <summary>
        /// Writes an integer 7 bits at a time. This allows really short ints to be
        /// stored in 1 byte, longer ones in 2, at the cost of storing the longest
        /// ones in 5 bytes.
        /// 
        /// Example for a positive int:
        /// 00000000 00000000 01010101 01010101
        /// becomes three bytes: 1,1010101 1,0101010 0,0000001
        /// 
        /// Example for a negative int:
        /// 11111111 11111111 11010101 01010101
        /// becomes three bytes: 1,1010101 1,0101010 0,1111111
        /// 
        /// Note how an extra byte is needed in this example. This is similar to
        /// requiring a sign bit, however this way the positive values are directly
        /// compatible with unsigned Optim values.
        /// </summary>
        public static void WriteInt32Optim(this Stream stream, int val)
        {
            while (val <= -127 || val >= 127)
            {
                stream.WriteByte((byte) (val | 128));
                val >>= 7;
            }
            stream.WriteByte((byte) (val & 127));
        }

        /// <summary>
        /// See WriteInt32Optim of this function for more info. Note that values
        /// written by this function cannot be safely read as signed int32s, but the
        /// other way is fine.
        /// </summary>
        public static void WriteUInt32Optim(this Stream stream, uint val)
        {
            while (val >= 128)
            {
                stream.WriteByte((byte) (val | 128));
                val >>= 7;
            }
            stream.WriteByte((byte) val);
        }

        /// <summary>
        /// See WriteInt32Optim for more info.
        /// </summary>
        public static void WriteInt64Optim(this Stream stream, long val)
        {
            while (val <= -127 || val >= 127)
            {
                stream.WriteByte((byte) (val | 128));
                val >>= 7;
            }
            stream.WriteByte((byte) (val & 127));
        }

        /// <summary>
        /// See WriteInt32Optim for more info. Note that values
        /// written by this function cannot be safely read as signed int64s, but the
        /// other way is fine.
        /// </summary>
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

        /// <summary>
        /// Reads an int written by <see cref="StreamExtensions.WriteInt32Optim"/> or <see cref="StreamExtensions.WriteUInt32Optim"/>.
        /// </summary>
        public static int ReadInt32Optim(this Stream stream)
        {
            byte b = (byte) stream.ReadByte();
            int shifts = 0;
            int res = 0;
            while (true)
            {
                bool havemore = (b & 128) != 0;
                b = (byte) (b & 127);
                res = res | (b << shifts);
                shifts += 7;
                if (!havemore)
                    break;
                b = (byte) stream.ReadByte();
            }
            // Sign-extend
            if (shifts >= 32)
                return res;
            shifts = 32 - shifts;
            return (res << shifts) >> shifts;
        }

        /// <summary>
        /// Reads an int written by <see cref="StreamExtensions.WriteUInt32Optim"/>.
        /// </summary>
        public static uint ReadUInt32Optim(this Stream stream)
        {
            byte b = (byte) stream.ReadByte();
            int shifts = 0;
            uint res = 0;
            while (true)
            {
                bool havemore = (b & 128) != 0;
                b = (byte) (b & 127);
                res = res | ((uint) b << shifts);
                shifts += 7;
                if (!havemore)
                    break;
                b = (byte) stream.ReadByte();
            }
            return res;
        }

        /// <summary>
        /// Reads an int written by <see cref="StreamExtensions.WriteInt64Optim"/> or <see cref="StreamExtensions.WriteUInt64Optim"/>.
        /// </summary>
        public static long ReadInt64Optim(this Stream stream)
        {
            byte b = (byte) stream.ReadByte();
            int shifts = 0;
            long res = 0;
            while (true)
            {
                bool havemore = (b & 128) != 0;
                b = (byte) (b & 127);
                res = res | ((long) b << shifts);
                shifts += 7;
                if (!havemore)
                    break;
                b = (byte) stream.ReadByte();
            }
            // Sign-extend
            if (shifts >= 64)
                return res;
            shifts = 64 - shifts;
            return (res << shifts) >> shifts;
        }

        /// <summary>
        /// Reads an int written by <see cref="StreamExtensions.WriteUInt64Optim"/>.
        /// </summary>
        public static ulong ReadUInt64Optim(this Stream stream)
        {
            byte b = (byte) stream.ReadByte();
            int shifts = 0;
            ulong res = 0;
            while (true)
            {
                bool havemore = (b & 128) != 0;
                b = (byte) (b & 127);
                res = res | ((ulong) b << shifts);
                shifts += 7;
                if (!havemore)
                    break;
                b = (byte) stream.ReadByte();
            }
            return res;
        }

        #endregion
    }
}
