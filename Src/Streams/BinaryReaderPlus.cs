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
        public BinaryReaderPlus(Stream output) : base(output) { }
        public BinaryReaderPlus(Stream output, Encoding encoding) : base(output, encoding) { }

        /// <summary>
        /// Reads an int written by WriteOptim(int) or WriteOptim(uint) of the
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
            shifts = 32 - shifts;
            return (res << shifts) >> shifts;
        }

        /// <summary>
        /// Reads an int written by WriteOptim(uint) of the BinaryWriterPlus
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
    }
}
