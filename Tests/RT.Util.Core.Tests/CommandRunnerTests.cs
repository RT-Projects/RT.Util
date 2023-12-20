using System.Runtime.InteropServices;
using NUnit.Framework;
using RT.Util.ExtensionMethods;

namespace RT.Util;

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
        testArgs(@"%PATH%");
        testArgs("\"%PATH%\"");
        testArgs("^%PATH^%");
        testArgs("""
            --folder="C:\Program Files\"
            """);
    }

    private static void testArgs(params string[] args)
    {
        testArgs(false, args);
    }

    private static void testArgs(bool checkTrivial, params string[] args)
    {
        if (checkTrivial)
            Assert.AreEqual(args.JoinString(" "), CommandRunner.ArgsToCommandLine(args));
        var path1 = @"C:\Temp\PrintArgs\PrintArgs.exe";
        if (File.Exists(path1))
            testArgsReal(new[] { path1 }.Concat(args).ToArray());
        else
            testArgsEmulated(args);
        var path2 = @"C:\Temp\PrintArgs\With Space\PrintArgs.exe";
        if (File.Exists(path2))
            testArgsReal(new[] { path2 }.Concat(args).ToArray());
    }

    private static void testArgsReal(string[] args)
    {
        // This requires "printargs", which is an external binary that uses CommandLineToArgvW(GetCommandLine()) to parse and print the arguments it has received.
        // This is a true test of what really happens. Without printargs, this test has to emulate a fair chunk of what happens behind the scenes.
        var cmd = new CommandRunner();
        cmd.SetCommand(args);
        cmd.CaptureEntireStdout = true;
        cmd.StartAndWait();
        var lines = cmd.EntireStdout.FromUtf8().Split("\n").Select(l => l.Trim()).Where(l => l != "").ToArray();
        Assert.AreEqual(args.Length, lines.Length - 2);
        Assert.AreEqual($"Count: [{args.Length}]", lines[0]);
        Assert.AreEqual("END", lines.Last());
        for (int i = 0; i < args.Length; i++)
            Assert.AreEqual($"{i}: [{args[i]}]", lines[i + 1]);
    }

    private static void testArgsEmulated(string[] args)
    {
        // Partially emulate what happens to the command and check that the received arguments are identical to the arguments passed in.
        // This obviously doesn't test everything, in particular it completely skips the processing done by cmd.exe
        var args1 = "C:\\dummypath.exe".Concat(args).ToArray();
        var str1 = CommandRunner.ArgsToCommandLine(args1);
        var argsSplit1 = NativeSplit(str1);
        Assert.IsTrue(args1.SequenceEqual(argsSplit1));
        var args2 = "C:\\Program Files\\dummypath.exe".Concat(args).ToArray();
        var str2 = CommandRunner.ArgsToCommandLine(args2);
        var argsSplit2 = NativeSplit(str2);
        Assert.IsTrue(args2.SequenceEqual(argsSplit2));
    }

    private static void testArgsEcho(string[] args)
    {
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
            result = CommandLineToArgvW(commandline, out count);
            if (result == IntPtr.Zero)
                throw new Exception("420s7");

            var args = new string[count];
            for (int i = 0; i < count; i++)
                args[i] = Marshal.PtrToStringUni(Marshal.ReadIntPtr(result, i * IntPtr.Size));
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
