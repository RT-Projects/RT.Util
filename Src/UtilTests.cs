using System;
using System.Reflection;
using NUnit.Direct;

namespace RT.Util
{
    static class UtilTests
    {
        static void Main(string[] args)
        {
            NUnitDirect.RunTestsOnAssembly(Assembly.GetEntryAssembly());
            Console.WriteLine("Press Enter to exit.");
            Console.ReadLine();
        }
    }
}
