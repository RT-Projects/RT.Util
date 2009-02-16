using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RT.Util.ExtensionMethods
{
    /// <summary>
    /// Provides extension methods on built-in integer types such as <see cref="Int32"/> etc.
    /// </summary>
    public static class IntegerExtensions
    {
        /// <summary>
        /// Bitwise rotate left.
        /// </summary>
        public static ulong RotateLeft(this ulong input, int bitsToRotateBy)
        {
            return (input << bitsToRotateBy) | (input >> (64 - bitsToRotateBy));
        }
    }
}
