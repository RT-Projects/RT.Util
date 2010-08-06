using System;
using System.Reflection;
using System.Text;
using NUnit.Direct;

namespace RT.Util
{
    static class UtilTests
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            NUnitDirect.RunTestsOnAssembly(Assembly.GetEntryAssembly());

            if (args.Length != 1 || args[0] != "--no-wait")
            {
                Console.WriteLine("Press Enter to exit.");
                Console.ReadLine();
            }
        }
    }
}
