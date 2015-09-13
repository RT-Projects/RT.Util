using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using NUnit.Framework;
using RT.Util.ExtensionMethods;

namespace RT.Util
{
    [TestFixture]
    public sealed class CommandRunnerTests
    {
        [Test]
        public static void Test()
        {
            testArgs();
            testArgs(true, "abc");
            testArgs(true, "abc", "def");
            testArgs("abc def");
            testArgs("");
            testArgs("abc", "", "def");
            testArgs("abc", "   ", "def");
            testArgs("abc ", "   ", "  def");
            testArgs(@"abc""foo""def");
            testArgs(@"abcfoo""bardef");
            testArgs(@"abcdef""");
            testArgs(@"""abcdef");
            testArgs(@"abcdef\");
            testArgs(@"abcdef\\");
            testArgs(@"abcdef\\\");
            testArgs(@"abc\""");
            testArgs(@"abc\""""");
            testArgs(@"abc\""""""");
            testArgs(@"test >nul");
            testArgs(@"test stuff", ">nul");
            testArgs(@"test stuff \""&whoami");
        }

        private static void testArgs(params string[] args)
        {
            testArgs(false, args);
        }

        private static void testArgs(bool checkTrivial, params string[] args)
        {
#if false
            // This requires "printargs", which is an external binary that uses CommandLineToArgvW(GetCommandLine()) to parse and print the arguments it has received.
            // This is a true test of what really happens. Without printargs, this test has to emulate a fair chunk of what happens behind the scenes.
            var cmd = new CommandRunner();
            cmd.SetCommand(new[] { "printargs" }.Concat(args));
            if (checkTrivial)
                Assert.AreEqual("printargs " + args.JoinString(" "), cmd.Command);
            cmd.CaptureEntireStdout = true;
            cmd.StartAndWait();
            var lines = cmd.EntireStdout.FromUtf8().Split("\n").Select(l => l.Trim()).Where(l => l != "").ToArray();
            Assert.AreEqual(args.Length, lines.Length - 2);
            var countS = Regex.Match(lines[0], @"^Count: \[(\d+)\]$");
            Assert.IsTrue(countS.Success);
            Assert.IsTrue(lines.Last() == "END");
            var count = int.Parse(countS.Groups[1].Value);
            Assert.AreEqual(count, args.Length);
            for (int i = 0; i < args.Length; i++)
            {
                var matchedArgS = Regex.Match(lines[i + 1], @"^{0}: \[(.*?)\]$".Fmt(i + 1));
                Assert.IsTrue(matchedArgS.Success);
                Assert.AreEqual(args[i], matchedArgS.Groups[1].Value);
            }
#else
            // Partially emulate what happens to the command and check that the received arguments are identical to the arguments passed in.
            // This obviously doesn't test everything, in particular it completely skips the processing done by cmd.exe
            var str = CommandRunner.ArgsToCommandLine(args);
            if (checkTrivial)
                Assert.AreEqual(args.JoinString(" "), str);
            var argsSplit = NativeSplit(str);
            Assert.IsTrue(args.SequenceEqual(argsSplit));
#endif

            // "echo" is a special case: it does not parse its arguments using CommandLineToArgvW; it's just a cmd.exe command that outputs the arguments "as-is".
            // Test that we do in fact get them back as-is.
            var naivelyJoinedArgs = args.JoinString(" ");
            var echo = CommandRunner.RunRaw(@"echo " + naivelyJoinedArgs).GoGetOutputText();
            if (naivelyJoinedArgs.Trim() != "") // else it says "echo is on"
                Assert.AreEqual(naivelyJoinedArgs + "\r\n", echo);
        }

        private static string[] NativeSplit(string commandline)
        {
            int count;
            IntPtr result = IntPtr.Zero;
            try
            {
                result = CommandLineToArgvW(@"C:\dummypath.exe " + commandline, out count); // because this function acts inconsistently when parsing an empty string
                if (result == IntPtr.Zero)
                    throw new Exception("420s7");

                var args = new string[count - 1];
                for (int i = 1; i < count; i++)
                    args[i - 1] = Marshal.PtrToStringUni(Marshal.ReadIntPtr(result, i * IntPtr.Size));
                return args;
            }
            finally
            {
                Marshal.FreeHGlobal(result);
            }
        }

        [DllImport("shell32.dll", SetLastError = true)]
        static extern IntPtr CommandLineToArgvW([MarshalAs(UnmanagedType.LPWStr)] string lpCmdLine, out int pNumArgs);
    }
}
