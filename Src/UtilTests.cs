using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RT.Util.ExtensionMethods;

namespace RT.Util
{
    static class UtilTests
    {
        static void Main()
        {
            // Note: this is not supposed to list every single test, since keeping the list
            // up-to-date would be a chore. Instead this is here so that individual tests
            // can be executed and debugged using Visual Studio rather than NUnit.
            new IEnumerableExtensionsTests().TestEquals();

            Console.WriteLine("Done testing.");
        }
    }
}
