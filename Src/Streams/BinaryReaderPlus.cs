using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace RT.Util.Streams
{
    /// <summary>
    /// Extends BinaryReader with extra methods. Intended to be used
    /// with BinaryWriterPlus.
    /// </summary>
    public class BinaryReaderPlus : BinaryReader
    {
        /// <summary>
        /// Constructs a new reader.
        /// </summary>
        public BinaryReaderPlus(Stream output) : base(output) { }

        /// <summary>
        /// Constructs a new reader using the specified encoding for strings.
        /// </summary>
        public BinaryReaderPlus(Stream output, Encoding encoding) : base(output, encoding) { }

        /// <summary>
        /// Reads an int written by WriteInt32Optim or WriteUInt32Optim of the
        /// BinaryWriterPlus class. See those methods for details.
        /// </summary>
        public int ReadInt32Optim()
        {
            byte b = ReadByte();
            int shifts = 0;
            int res = 0;
            while (true)
            {
                bool havemore = (b & 128) != 0;
                b = (byte)(b & 127);
                res = res | (b << shifts);
                shifts += 7;
                if (!havemore)
                    break;
                b = ReadByte();
            }
            // Sign-extend
            if (shifts >= 32)
                return res;
            shifts = 32 - shifts;
            return (res << shifts) >> shifts;
        }

        /// <summary>
        /// Reads an int written by WriteUInt32Optim of the BinaryWriterPlus
        /// class. See that method for details.
        /// </summary>
        public int ReadUInt32Optim()
        {
            byte b = ReadByte();
            int shifts = 0;
            int res = 0;
            while (true)
            {
                bool havemore = (b & 128) != 0;
                b = (byte)(b & 127);
                res = res | (b << shifts);
                shifts += 7;
                if (!havemore)
                    break;
                b = ReadByte();
            }
            return res;
        }

        /// <summary>
        /// Reads an int written by WriteInt32Optim or WriteUInt32Optim of the
        /// BinaryWriterPlus class. See those methods for details.
        /// </summary>
        public long ReadInt64Optim()
        {
            byte b = ReadByte();
            int shifts = 0;
            long res = 0;
            while (true)
            {
                bool havemore = (b & 128) != 0;
                b = (byte)(b & 127);
                res = res | ((long)b << shifts);
                shifts += 7;
                if (!havemore)
                    break;
                b = ReadByte();
            }
            // Sign-extend
            if (shifts >= 64)
                return res;
            shifts = 64 - shifts;
            return (res << shifts) >> shifts;
        }

        /// <summary>
        /// Reads an int written by WriteUInt32Optim of the BinaryWriterPlus
        /// class. See that method for details.
        /// </summary>
        public ulong ReadUInt64Optim()
        {
            byte b = ReadByte();
            int shifts = 0;
            ulong res = 0;
            while (true)
            {
                bool havemore = (b & 128) != 0;
                b = (byte)(b & 127);
                res = res | ((ulong)b << shifts);
                shifts += 7;
                if (!havemore)
                    break;
                b = ReadByte();
            }
            return res;
        }
    }
}
