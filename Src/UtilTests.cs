using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RT.Util.ExtensionMethods;
using System.Reflection;
using NUnit.Framework;
using System.IO;
using System.Text.RegularExpressions;

namespace RT.Util
{
    static class UtilTests
    {
        static void Main(string[] args)
        {
#pragma warning disable 0162
            if (false)
            {
                Testing.GenerateTestingCode(@"..\..\main\common\Util\UtilTests.cs", "Run Tests", Assembly.GetExecutingAssembly().GetExportedTypes(),
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
#pragma warning restore 0162
        }
    }
}
