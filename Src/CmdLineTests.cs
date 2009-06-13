using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.Text.RegularExpressions;

namespace RT.Util
{
    public class CmdLineTestPrinter : CmdLinePrinterBase
    {
        public string Output = "";
        public override void Print(string text) { Output += text; }
        public override void PrintLine(string text) { Output += text + "\n"; }
        public override void Commit(bool success) { }
        public override int MaxWidth { get { return 80; } }
    }

    [TestFixture]
    public class CmdLineTests
    {
        [Test]
        public void TestCmdLineParser()
        {
            CmdLineTestPrinter cltp = new CmdLineTestPrinter();
            CmdLineParser clp = new CmdLineParser(cltp);

            try { clp.DefineOption(null, null, CmdOptionType.Value, CmdOptionFlags.Optional, ""); Assert.Fail(); }
            catch (ArgumentException) { }

            clp.DefineDefaultHelpOptions();

            // These should all fail because Parse() wasn't called
            try { var dummy = clp.Errors; Assert.Fail(); }
            catch (InvalidOperationException) { }
            try { var dummy = clp.OptPositional; Assert.Fail(); }
            catch (InvalidOperationException) { }
            try { var dummy = clp.OptValue("x"); Assert.Fail(); }
            catch (InvalidOperationException) { }
            try { var dummy = clp.OptSwitch("x"); Assert.Fail(); }
            catch (InvalidOperationException) { }
            try { var dummy = clp.OptList("x"); Assert.Fail(); }
            catch (InvalidOperationException) { }
            try { var dummy = clp.HadErrors; Assert.Fail(); }
            catch (InvalidOperationException) { }
            try { var dummy = clp.HadHelp; Assert.Fail(); }
            catch (InvalidOperationException) { }

            // Should fail: option "-a" undefined
            clp.Parse(new[] { "-a", "arg" });
            Assert.AreEqual(1, clp.Errors.Count);
            Assert.AreEqual("Option \"-a\" doesn't match any of the allowed options.", clp.Errors[0]);

            // Subsequent Parse() without ClearResults() should generate RTException
            try { clp.Parse(new string[] { }); Assert.Fail(); }
            catch (RTException) { }
            clp.ClearResults();

            clp.DefineOption(null, "switch", CmdOptionType.Switch, CmdOptionFlags.Optional, "If specified, switches sides.");
            clp.DefineOption("q", "requiredswitch", CmdOptionType.Switch, CmdOptionFlags.Required, "Specify this, whether you want to or not.");
            // Should fail: "-v" undefined and required option "-q" missing
            clp.Parse(new[] { "-v", "arg" });
            Assert.AreEqual(2, clp.Errors.Count);
            Assert.IsTrue(clp.Errors.Contains("Option \"-v\" doesn't match any of the allowed options."));
            Assert.IsTrue(clp.Errors.Contains("Option \"-q/--requiredswitch\" is a required option and must not be omitted."));
            clp.ClearResults();

            clp.DefineOption("v", "value", CmdOptionType.Value, CmdOptionFlags.Required, "Defines the value of life.");
            // Should succeed: v_arg corresponds to -v, but s_arg is a positional argument
            clp.Parse(new[] { "-q", "-v", "v_arg", "--switch", "s_arg" });
            Assert.AreEqual(0, clp.Errors.Count);
            Assert.AreEqual(0, clp.OptList("list").Count);
            Assert.AreEqual("v_arg", clp.OptValue("value"));
            Assert.IsTrue(clp.OptSwitch("switch"));
            Assert.AreEqual(1, clp.OptPositional.Count);
            Assert.AreEqual("s_arg", clp.OptPositional[0]);
            clp.ClearResults();

            clp.DefineHelpSeparator();
            clp.DefineOption("l", "list", CmdOptionType.List, CmdOptionFlags.Optional, "Stores the specified items as a shopping list.");
            // Should succeed: --switch is optional
            clp.Parse(new[] { "-q", "-l", "one", "two", "--list", "three", "-v", "value" });
            Assert.AreEqual(0, clp.Errors.Count);
            Assert.AreEqual(2, clp.OptList("list").Count);
            Assert.AreEqual("one", clp.OptList("list")[0]);
            Assert.AreEqual("three", clp.OptList("list")[1]);
            Assert.AreEqual("value", clp.OptValue("value"));
            Assert.AreEqual(1, clp.OptPositional.Count);
            Assert.AreEqual("two", clp.OptPositional[0]);
            Assert.AreEqual("value", clp["value"]);
            Assert.IsNull(clp["notexist"]);
            Assert.AreEqual("true", clp["q"]);
            try { var dummy = clp["list"]; Assert.Fail(); }
            catch (InvalidOperationException) { }
            clp.ClearResults();

            // Should fail
            clp.Parse(new[] { "-q", "-v", "value1", "-v", "value2", "-l" });
            Assert.AreEqual(2, clp.Errors.Count);
            Assert.IsTrue(clp.Errors.Contains("Option \"-v/--value\" cannot be specified more than once."));
            Assert.IsTrue(clp.Errors.Contains("Option \"-l/--list\" requires a value to be specified."));
            clp.ClearResults();

            // Should fail
            clp.Parse(new[] { "-q", "-v" });
            Assert.AreEqual(1, clp.Errors.Count);
            Assert.IsTrue(clp.Errors.Contains("Option \"-v/--value\" requires a value to be specified."));
            Assert.IsTrue(clp.HadErrors);
            Assert.IsFalse(clp.HadHelp);

            clp.PrintErrors();
            Assert.AreEqual("Errors:\n\n    Option \"-v/--value\" requires a value to be specified.\n\n", cltp.Output);
            cltp.Output = "";
            clp.PrintHelp();
            clp.PrintCommit(true);
            // Do this regex match without Singleline mode so that .* only matches within a line.
            Assert.IsTrue(Regex.IsMatch(cltp.Output, @"^(.*\nVersion: .*\nCopyright ©.*\n)?\nUsage:\n\n    (.*\.exe|<programname>) -q -v <value> \[--switch\] \[\[-l <list 1> \[\.\.\. -l <list N>\]\]\]\n\nAvailable options:\n\n         --switch           If specified, switches sides\.\r\n    -q   --requiredswitch   Specify this, whether you want to or not\.\r\n    -v   --value            Defines the value of life\.\r\n    -l   --list             Stores the specified items as a shopping list\.\r\n\n$"), "m: " + cltp.Output);

            var oldOutput = cltp.Output;
            clp.PrintProgramInfo();
            Assert.IsTrue(oldOutput == cltp.Output);

            clp.ClearResults();
            clp.DefineOption("x", "extrahelp", CmdOptionType.Switch, CmdOptionFlags.IsHelp, "Same as -h or -?.");
            clp.Parse(new[] { "-x" });
            Assert.IsTrue(clp.HadHelp);
        }
    }
}
