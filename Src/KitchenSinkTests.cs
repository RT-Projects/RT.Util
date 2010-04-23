using System;
using System.Reflection;
using NUnit.Direct;
using NUnit.Framework;
using RT.Util;

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
