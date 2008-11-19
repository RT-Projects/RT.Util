using System;
using System.Collections.Generic;
using System.Text;

namespace RT.Util.Collections
{
    /// <summary>Encapsulates a two-dimensional array of values of a specified type which are retrieved using a Get() method.</summary>
    public interface Virtual2DArray<T>
    {
        /// <summary>Width of the array.</summary>
        int Width { get; }
        /// <summary>Height of the array.</summary>
        int Height { get; }
        /// <summary>Method to retrieve a value from the array.</summary>
        /// <param name="x">Index along the x-axis.</param>
        /// <param name="y">Index along the y-axis.</param>
        /// <returns>The value at the position (x, y) in the array.</returns>
        T Get(int x, int y);
    }
}
