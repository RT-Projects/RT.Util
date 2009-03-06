using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RT.Util.ExtensionMethods;
using System.Reflection;
using NUnit.Framework;

namespace RT.Util
{
    static class UtilTests
    {
        static void Main(string[] args)
        {
            foreach (var ty in Assembly.GetExecutingAssembly().GetExportedTypes().Where(t => t.GetCustomAttributes(typeof(TestFixtureAttribute), true).Any()))
            {
                Console.WriteLine("Testing type: " + ty);
                var sts = Activator.CreateInstance(ty);

                foreach (var meth in ty.GetMethods().Where(m => m.GetCustomAttributes(typeof(TestFixtureSetUpAttribute), false).Any()))
                {
                    Console.WriteLine("-- Running setup: " + meth.Name);
                    meth.Invoke(sts, new object[] { });
                }

                foreach (var meth in ty.GetMethods().Where(m => m.GetCustomAttributes(typeof(TestAttribute), false).Any()))
                {
                    Console.WriteLine("-- Running test: " + meth.Name);
                    meth.Invoke(sts, new object[] { });
                }

                foreach (var meth in ty.GetMethods().Where(m => m.GetCustomAttributes(typeof(TestFixtureTearDownAttribute), false).Any()))
                {
                    Console.WriteLine("-- Running teardown: " + meth.Name);
                    meth.Invoke(sts, new object[] { });
                }
            }

            Console.WriteLine("");
            Console.WriteLine("Tests passed; press Enter to exit.");
            Console.ReadLine();
        }
    }
}
