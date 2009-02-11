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
        /// <param name="op">How many bits to rotate by.</param>
        public static ulong RotateLeft(this ulong input, int op)
        {
            return (input << op) | (input >> (64 - op));
        }
    }
}
