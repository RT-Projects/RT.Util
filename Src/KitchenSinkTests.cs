using System;
using System.Reflection;
using NUnit.Framework;
using RT.Util;

namespace RT.KitchenSink.Tests
{
    static class KitchenSinkTests
    {
        static void Main(string[] args)
        {
            if (false)
            {
                Testing.GenerateTestingCode(@"..\..\main\common\KitchenSink\KitchenSinkTests.cs", "Run Tests", Assembly.GetExecutingAssembly().GetExportedTypes(),
                    typeof(TestFixtureAttribute), typeof(TestFixtureSetUpAttribute), typeof(TestAttribute), typeof(TestFixtureTearDownAttribute));
            }
            else
            {
                #region Run Tests
                #endregion

                Console.WriteLine("");
                Console.WriteLine("Tests passed; press Enter to exit.");
                Console.ReadLine();
            }
        }
    }
}
