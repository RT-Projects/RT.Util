using System;
using System.Collections.Generic;
using System.Text;

namespace RT.Util
{
    public interface Virtual2DArray<T>
    {
        int Width { get; }
        int Height { get; }
        T Get(int x, int y);
    }
}
