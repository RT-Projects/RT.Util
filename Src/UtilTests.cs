using System;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Direct;
using NUnit.Framework;
using RT.Util.ExtensionMethods;

[assembly: Timeout(10000)]

namespace RT.Util
{
    static class UtilTests
    {
        static void Main(string[] args)
        {
            bool wait = !args.Contains("--no-wait");
            bool notimes = args.Contains("--no-times");

            string filter = null;
            var pos = args.IndexOf("--filter");
            if (pos != -1 && args.Length > pos + 1)
                filter = args[pos + 1];

            Console.OutputEncoding = Encoding.UTF8;
            NUnitDirect.RunTestsOnAssembly(Assembly.GetEntryAssembly(), notimes, filter);

            if (wait)
            {
                Console.WriteLine("Press Enter to exit.");
                Console.ReadLine();
            }
        }
    }
}
