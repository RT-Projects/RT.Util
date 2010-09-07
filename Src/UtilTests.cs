using System;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Direct;
using NUnit.Framework;

[assembly: Timeout(10000)]

namespace RT.Util
{
    static class UtilTests
    {
        static void Main(string[] args)
        {
            bool wait = !args.Contains("--no-wait");
            bool notimes = args.Contains("--no-times");

            Console.OutputEncoding = Encoding.UTF8;
            NUnitDirect.RunTestsOnAssembly(Assembly.GetEntryAssembly(), notimes);

            if (wait)
            {
                Console.WriteLine("Press Enter to exit.");
                Console.ReadLine();
            }
        }
    }
}
