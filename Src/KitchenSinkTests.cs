using System;
using System.Reflection;
using NUnit.Direct;
using NUnit.Framework;
using RT.Util;

[assembly: Timeout(20000)]

namespace RT.KitchenSink.Tests
{
    static class KitchenSinkTests
    {
        static void Main(string[] args)
        {
            NUnitDirect.RunTestsOnAssembly(Assembly.GetEntryAssembly());
            Console.WriteLine("Press Enter to exit.");
            Console.ReadLine();
        }
    }
}
