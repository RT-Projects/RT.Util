using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace RT.Util.Streams
{
    /// <summary>
    /// Calculates RSync checksums over bytes.
    /// </summary>
    public class RSyncChecksumCalculator
    {
        private uint windowSize;
        private byte[] window;
        private bool windowFull;
        private int windowHead;
        private int windowTail; // not used until window is full

        private uint a, b;

        /// <summary>
        /// Initialises the checksum calculator. Window Size determines the number of bytes
        /// which are hashed (see rsync algorithm details if this is unclear).
        /// </summary>
        public RSyncChecksumCalculator(int windowSize)
        {
            this.windowSize = (uint) windowSize;

            window = new byte[windowSize];
            windowFull = false;
            windowHead = -1;

            a = b = 0;
        }

        /// <summary>
        /// Passes all bytes in the array through the rsync hash algorithm.
        /// </summary>
        public void ProcessBytes(byte[] buffer)
        {
            ProcessBytes(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Passes the specified bytes in the array through the rsync hash algorithm.
        /// </summary>
        public void ProcessBytes(byte[] buffer, int offset, int count)
        {
            int endoffset = offset + count;
            if (endoffset > buffer.Length || offset < 0)
                throw new ArgumentOutOfRangeException("The arguments to ProcessBytes() point outside the array.");

            if (!windowFull)
            {
                for (; offset < endoffset; offset++)
                {
                    windowHead++;
                    window[windowHead] = buffer[offset];

                    a += buffer[offset];
                    b += buffer[offset] * (windowSize - (uint) windowHead);

                    if (windowHead == windowSize - 1)
                    {
                        offset++;
                        windowTail = 0;
                        windowFull = true;
                        break;
                    }
                }
            }

            for (; offset < endoffset; offset++)
            {
                a += (uint) (buffer[offset] - window[windowTail]);
                b += a - window[windowTail] * windowSize;

                if (windowHead < windowSize - 1) windowHead++; else windowHead = 0;
                if (windowTail < windowSize - 1) windowTail++; else windowTail = 0;
                window[windowHead] = buffer[offset];
            }

        }

        /// <summary>
        /// Returns the rsync checksum calculated so far.
        /// </summary>
        public uint CurrentChecksum
        {
            get
            {
                return (b << 16) | (a & 0xFFFF);
            }
        }

        /// <summary>
        /// Returns the rsync checksum calculated so far.
        /// </summary>
        public byte[] CurrentChecksumBytes
        {
            get
            {
                uint cs = CurrentChecksum;
                return new byte[] { (byte) cs, (byte) (cs >> 8), (byte) (cs >> 16), (byte) (cs >> 24) };
            }
        }
    }

    /// <summary>
    /// Timwi's version of the RSync checksum calculator. Based on the generic queue class.
    /// May or may not be noticeably slower than the much longer version above.
    /// </summary>
    public class RSyncChecksumCalculatorTimwi
    {
#pragma warning disable 1591    // Missing XML comment for publicly visible type or member
        private uint windowSize;
        private Queue<byte> window;

        private uint a, b;

        public RSyncChecksumCalculatorTimwi(int windowSize)
        {
            this.windowSize = (uint)windowSize;

            window = new Queue<byte>(windowSize + 1);

            a = b = 0;
        }

        public void ProcessBytes(byte[] buffer)
        {
            ProcessBytes(buffer, 0, buffer.Length);
        }

        public void ProcessBytes(byte[] buffer, int offset, int count)
        {
            int endoffset = offset + count;
            if (endoffset > buffer.Length || offset < 0)
                throw new ArgumentOutOfRangeException("The arguments to ProcessBytes() point outside the array.");

            for (uint i = (uint)offset; i < endoffset; i++)
            {
                window.Enqueue(buffer[i]);
                a += buffer[i];
                if (window.Count > windowSize)
                {
                    byte by = window.Dequeue();
                    a -= by;
                    b += a - windowSize*by;
                }
                else
                    b += (windowSize - i) * buffer[i];
            }
        }

        public uint CurrentChecksum
        {
            get
            {
                return (b << 16) | (a & 0xFFFF);
            }
        }
#pragma warning restore 1591    // Missing XML comment for publicly visible type or member
    }

    /// <summary>
    /// Calculates rsync checksum of all values that are read/written via this stream.
    /// </summary>
    public class RSyncChecksumStream : Stream
    {
        private Stream stream = null;
        private RSyncChecksumCalculator calc;

        /// <summary>
        /// This is the underlying stream. All reads/writes and most other operations
        /// on this class are performed on this underlying stream.
        /// </summary>
        public virtual Stream BaseStream { get { return stream; } }

        private RSyncChecksumStream() { }

        /// <summary>
        /// Initialises an rsync calculation stream using the specified stream as the
        /// underlying stream and the specified rsync window size (number of bytes)
        /// </summary>
        public RSyncChecksumStream(Stream stream, int window)
        {
            this.stream = stream;
            calc = new RSyncChecksumCalculator(window);
        }

#pragma warning disable 1591    // Missing XML comment for publicly visible type or member
        public override bool CanRead { get { return stream.CanRead; } }
        public override bool CanSeek { get { return stream.CanSeek; } }
        public override bool CanWrite { get { return stream.CanWrite; } }
        public override void Flush() { stream.Flush(); }
        public override long Length { get { return stream.Length; } }
        
        public override long Position
        {
            get { return stream.Position; }
            set { stream.Position = value; }
        }

        /// <summary>
        /// Seeking is ignored (but propagated to the underlying stream).
        /// All the bytes seeked over will be ignored.
        /// </summary>
        public override long Seek(long offset, SeekOrigin origin)
        {
            return stream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            stream.SetLength(value);
        }
#pragma warning restore 1591    // Missing XML comment for publicly visible type or member

        /// <summary>
        /// Reads data from the underlying stream. Updates the RSync with the bytes read.
        /// </summary>
        public override int Read(byte[] buffer, int offset, int count)
        {
            int numread = stream.Read(buffer, offset, count);
            calc.ProcessBytes(buffer, offset, numread);

            return numread;
        }

        /// <summary>
        /// Writes data to the underlying stream. Updates the RSync with the bytes written.
        /// </summary>
        public override void Write(byte[] buffer, int offset, int count)
        {
            stream.Write(buffer, offset, count);
            calc.ProcessBytes(buffer, offset, count);
        }

        /// <summary>
        /// Returns the rsync checksum calculated so far for all the bytes read/written.
        /// </summary>
        public uint CurrentChecksum
        {
            get
            {
                return calc.CurrentChecksum;
            }
        }

        /// <summary>
        /// Returns the rsync checksum calculated so far for all the bytes read/written.
        /// </summary>
        public byte[] CurrentChecksumBytes
        {
            get
            {
                return calc.CurrentChecksumBytes;
            }
        }

    }

}
