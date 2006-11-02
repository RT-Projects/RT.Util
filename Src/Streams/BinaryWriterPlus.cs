using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace RT.Util.Streams
{
    /// <summary>
    /// Extends BinaryWriter with extra methods. Intended to be used
    /// with BinaryReaderPlus.
    /// </summary>
    public class BinaryWriterPlus : BinaryWriter
    {
        public BinaryWriterPlus(Stream output) : base(output) { }
        public BinaryWriterPlus(Stream output, Encoding encoding) : base(output, encoding) { }

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
        public virtual void WriteInt32Optim(int val)
        {
            byte b = 0;
            while (true)
            {
                b = (byte)(val & 127);
                val >>= 7;
                // terminate if val is all zeroes and top bit is zero (end of positive),
                // or all ones (-1) and top bit is one (end of negative).
                if (((val == 0)&&((b & 64) == 0)) || ((val == -1)&&((b & 64) != 0)))
                    break;
                b |= 128;
                Write(b);
            }
            Write(b);
        }

        /// <summary>
        /// See WriteInt32Optim of this function for more info. Note that values
        /// written by this function cannot be safely read as signed ints, but the
        /// other way is fine.
        /// </summary>
        public virtual void WriteUInt32Optim(uint val)
        {
            byte b = 0;
            while (true)
            {
                b = (byte)(val & 127);
                val >>= 7;
                // terminate if there are no more bits
                if (val == 0)
                    break;
                b |= 128;
                Write(b);
            }
            Write(b);
        }
    }
}
