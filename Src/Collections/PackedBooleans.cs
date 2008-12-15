using System;
using System.Collections.Generic;
using System.Text;

namespace RT.Util.Collections
{
    /// <summary>
    /// Uses an underlying byte[] array to store booleans as bits.
    /// </summary>
    public class PackedBooleans
    {
        private byte[] _booleans;

        /// <summary>Initialises a PackedBooleans array containing the specified number of bits.</summary>
        /// <param name="length">Number of bits to store.</param>
        public PackedBooleans(int length) { _booleans = new byte[(length+7)/8]; }

        /// <summary>Returns the bit at index Index.</summary>
        /// <param name="index">The index of the bit to return.</param>
        /// <returns>The bit at index Index.</returns>
        public bool Get(int index) { return (_booleans[index/8] & (1 << (index % 8))) != 0; }

        /// <summary>Sets the bit at index Index.</summary>
        /// <param name="index">The index of the bit to set.</param>
        /// <param name="value">The value to set the specified bit to.</param>
        public void Set(int index, bool value)
        {
            if (value)
                _booleans[index/8] |= (byte) (1 << (index % 8));
            else
                _booleans[index/8] &= (byte) (~(1 << (index % 8)));
        }
    }
}
