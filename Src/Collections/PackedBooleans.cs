using System;
using System.Collections.Generic;
using System.Text;

namespace RT.Util.Collections
{
    /// <summary>
    /// Uses an underlying int[] array to store booleans as bits.
    /// </summary>
    public class PackedBooleans
    {
        private uint[] _booleans;
        private int _length;

        /// <summary>Initialises a PackedBooleans array containing the specified number of bits.</summary>
        /// <param name="length">Number of bits to store.</param>
        public PackedBooleans(int length)
        {
            _booleans = new uint[(length+31)/32];
            _length = length;
        }

        /// <summary>Returns the bit at index Index.</summary>
        /// <param name="index">The index of the bit to return.</param>
        /// <returns>The bit at index Index.</returns>
        public bool Get(int index) { return this[index]; }

        /// <summary>Sets the bit at index Index.</summary>
        /// <param name="index">The index of the bit to set.</param>
        /// <param name="value">The value to set the specified bit to.</param>
        public void Set(int index, bool value) { this[index] = value; }

        /// <summary>
        /// Gets or sets the bit at the specified index.
        /// </summary>
        public bool this[int index]
        {
            get
            {
                if (index >= _length || index < 0)
                    throw new ArgumentOutOfRangeException("index", "Attempting to access bit outside the array");
                return (_booleans[index/32] & (1 << (index % 32))) != 0;
            }
            set
            {
                if (index >= _length || index < 0)
                    throw new ArgumentOutOfRangeException("index", "Attempting to access bit outside the array");
                if (value)
                    _booleans[index/32] |= (1u << (index % 32));
                else
                    _booleans[index/32] &= ~(1u << (index % 32));
            }
        }

        /// <summary>
        /// Gets the number of bits stored in this instance.
        /// </summary>
        public int Length { get { return _length; } }

        /// <summary>
        /// Returns true if the data stored in this PackedBooleans is the same
        /// as that stored in another instance.
        /// </summary>
        public override bool Equals(object obj)
        {
            if (!(obj is PackedBooleans))
                return base.Equals(obj);

            PackedBooleans other = (PackedBooleans)obj;
            if (_length != other._length)
                return false;

            for (int i = 0; i < _booleans.Length; i++)
                if (_booleans[i] != other._booleans[i])
                    return false;

            return true;
        }

        /// <summary>
        /// Gets a hash code based on the data in this instance.
        /// </summary>
        public override int GetHashCode()
        {
            uint hash1 = 0xFFFFFFFF;
            uint hash2 = 0xFFFFFFFF;
            for (uint i = 0; i < _booleans.Length; i++)
            {
                hash1 = unchecked(hash1 + _booleans[i]);
                hash2 = unchecked(hash2 + hash1);
            }
            return unchecked((int)(((hash1 & 0xFFFF) + (hash1 >> 16)) | ((hash2 << 16) + (hash2 & 0xFFFF0000))));
        }
    }
}
