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
        private byte[] FBooleans;

        /// <summary>Initialises a PackedBooleans array containing the specified number of bits.</summary>
        /// <param name="Length">Number of bits to store.</param>
        public PackedBooleans(int Length) { FBooleans = new byte[(Length+7)/8]; }

        /// <summary>Returns the bit at index Index.</summary>
        /// <param name="Index">The index of the bit to return.</param>
        /// <returns>The bit at index Index.</returns>
        public bool Get(int Index) { return (FBooleans[Index/8] & (1 << (Index % 8))) != 0; }

        /// <summary>Sets the bit at index Index.</summary>
        /// <param name="Index">The index of the bit to set.</param>
        /// <param name="Value">The value to set the specified bit to.</param>
        public void Set(int Index, bool Value)
        {
            if (Value)
                FBooleans[Index/8] |= (byte) (1 << (Index % 8));
            else
                FBooleans[Index/8] &= (byte) (~(1 << (Index % 8)));
        }
    }
}
