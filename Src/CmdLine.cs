using System;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Permissions;
using System.Text;
using RT.Util.Dialogs;
using RT.Util.ExtensionMethods;
using RT.Util.Text;

namespace RT.Util
{
    /// <summary>
    /// A class which aids parsing command-line arguments.
    /// 
    /// <remarks>Requires the UnmanagedCode security permission due to the use
    /// of Environment.Exit function.</remarks>
    /// </summary>
    [SecurityPermission(SecurityAction.Demand, Flags=SecurityPermissionFlag.UnmanagedCode)]
    public class CmdLineParser
    {
        /// <summary>
        /// Describes a single option.
        /// </summary>
        private class CmdLineOption
        {
            public string TinyName;
            public string LongName;
            public string Description;

            public string NiceName;

            public CmdOptionType Type;
            public CmdOptionFlags Flags;

            public List<string> Value;
        }

        /// <summary>
        /// Describes a blank line used to separate options when printing help.
        /// </summary>
        private class CmdLineOptionSeparator : CmdLineOption
        {
        }

        /// <summary>All options that the user has defined, including separators.</summary>
        private List<CmdLineOption> byDefineOrder = new List<CmdLineOption>();

        /// <summary>Only the options which had a non-null tiny name specified.</summary>
        private Dictionary<string, CmdLineOption> byTinyName = new Dictionary<string, CmdLineOption>();

        /// <summary>Only the options which had a non-null long name specified.</summary>
        private Dictionary<string, CmdLineOption> byLongName = new Dictionary<string, CmdLineOption>();

        /// <summary>
        /// <para>This points to either byTinyName or byLongName. By default it points to
        /// byLongName, but the user of this class can call a method to change this to
        /// byTinyName.</para>
        /// 
        /// <para>The public methods used to access parse results use this variable to
        /// look up option names. Hence this basically determines whether the user
        /// accesses options by their full names or their tiny names.</para>
        /// </summary>
        private Dictionary<string, CmdLineOption> byPreferredName;

        /// <summary>
        /// Holds all positional arguments, that is, all arguments which do not look like
        /// named options and are not arguments to known named options.
        /// </summary>
        private List<string> unmatchedArgs;

        /// <summary>
        /// Holds all parse errors that occurred. Will be an empty (non-null) list when there
        /// are no errors.
        /// </summary>
        private List<string> _errors;

        /// <summary>
        /// Set to true when a help option is encountered during parsing. False otherwise.
        /// </summary>
        private bool _help;

        /// <summary>
        /// Holds a class providing an interface to the user, such as printing messages
        /// on the command line or displaying message boxes. Defaults to a console printer.
        /// </summary>
        private CmdLinePrinterBase printer = new CmdLineConsolePrinter();

        /// <summary>
        /// Keeps track of whether Parse() has ever been called.
        /// </summary>
        private bool Parsed = false;

        /// <summary>
        /// Keeps track of whether PrintProgramInfo() has been called.
        /// </summary>
        private bool ProgramInfoPrinted = false;

        /// <summary>
        /// Constructs a command line parser.
        /// </summary>
        public CmdLineParser()
        {
            ClearResults();
        }

        /// <summary>
        /// Constructs a command line parser which will use the specified printer class.
        /// </summary>
        public CmdLineParser(CmdLinePrinterBase printer)
            : this()
        {
            this.printer = printer;
        }

        /// <summary>
        /// Defines and describes a command-line argument.
        /// </summary>
        /// <param name="tinyName">Ideally a single character. Accessed with a single minus,
        /// e.g. an option named "c" here can be specified as "-c" on the command line.</param>
        /// <param name="longName">Ideally a fairly descriptive name. Accessed with a double minus,
        /// e.g. an option named "connection-string" can be specified as "--connection-string"
        /// on the command line.</param>
        /// <param name="type">Specifies whether an option is an on/off, a string value or a list of values.</param>
        /// <param name="flags">Selects the behaviour of this option.</param>
        /// <param name="description">A human-readable description of what the option does. This
        /// text is used when printing help. Note that the help text gets automatically wrapped,
        /// so this string can be long and may include line breaks and indented examples.</param>
        public void DefineOption(string tinyName, string longName, CmdOptionType type, CmdOptionFlags flags, string description)
        {
            if (tinyName == null && longName == null)
                throw new ArgumentException("Both the tiny and the long switch names are null. The user won't be able to specify it.");

            CmdLineOption opt = new CmdLineOption();
            opt.TinyName = tinyName;
            opt.LongName = longName;
            opt.Type = type;
            opt.Flags = flags;
            opt.Description = description;
            opt.NiceName = tinyName == null ? ("--"+longName) : longName == null ? ("-"+tinyName) : string.Format("-{0}/--{1}", tinyName, longName);

            byDefineOrder.Add(opt);
            if (tinyName != null)
                byTinyName.Add(tinyName, opt);
            if (longName != null)
                byLongName.Add(longName, opt);
        }

        /// <summary>
        /// When printing help, the options are printed in the order in which they were
        /// defined. Calling this function adds a single blank line separator in the help
        /// output between the option defined by the last call to <see cref="DefineOption"/>
        /// and the one defined by the next one. Multiple calls will result in multiple
        /// blank lines.
        /// </summary>
        public void DefineHelpSeparator()
        {
            byDefineOrder.Add(new CmdLineOptionSeparator());
        }

        /// <summary>
        /// Defines standard help options. These options are marked with the IsHelp
        /// option flag, which means that:
        ///
        /// <list>
        /// <item>The options won't be printed when printing help.</item>
        /// <item>Specifying one of these options causes the program to print help
        ///       and terminate (at the time of the parsing).</item>
        /// </list>
        ///
        /// <para>Default help options comprise: -?, --help, --usage.</para>
        ///
        /// <para>For more info see the <see cref="Parse"/> function</para>.
        /// </summary>
        public void DefineDefaultHelpOptions()
        {
            DefineOption("?",  "help",  CmdOptionType.Switch, CmdOptionFlags.IsHelp, null);
            DefineOption(null, "usage", CmdOptionType.Switch, CmdOptionFlags.IsHelp, null);
        }

        /// <summary>
        /// Clears all the results of the previous parse, effectively restoring the state of the
        /// parser to before the Parse method was called, with all options still defined.
        /// </summary>
        public void ClearResults()
        {
            Parsed = false;
            _errors = new List<string>();
            _help = false;
            unmatchedArgs = new List<string>();
            foreach (var option in byDefineOrder)
                option.Value = null;
        }

        /// <summary>
        /// <para>Parses the specified command line arguments according to the options
        /// defined (using the <see cref="DefineOption"/> etc functions).</para>
        ///
        /// <para>This method does not display the help message or errors when this is
        /// necessary. It just makes fields/methods such as <see cref="Errors"/>,
        /// <see cref="HadErrors"/>, <see cref="HadHelp"/> available for inspection and
        /// processing. The method <see cref="ProcessHelpAndErrors"/> can be used
        /// to print any information or messages as necessary and even terminate the
        /// program if the parse is unsuccessful.</para>
        /// </summary>
        public void Parse(string[] args)
        {
            if (Parsed)
                throw new RTException("Parse results must be cleared using ClearResults before calling Parse again.");

            // Reset the preferred access name to long name
            byPreferredName = byLongName;

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                CmdLineOption opt;

                if (arg.StartsWith("--") && byLongName.ContainsKey(arg.Substring(2)))
                    opt = byLongName[arg.Substring(2)];
                else if (arg.StartsWith("-") && byTinyName.ContainsKey(arg.Substring(1)))
                    opt = byTinyName[arg.Substring(1)];
                else
                {
                    if (arg.StartsWith("-"))
                        _errors.Add(string.Format("Option \"{0}\" doesn't match any of the allowed options.", arg));
                    else
                        unmatchedArgs.Add(arg);
                    continue;
                }

                // Is this a help option
                if ((opt.Flags & CmdOptionFlags.IsHelp) != 0)
                {
                    _help = true;
                    break;
                }

                // This option was matched
                switch (opt.Type)
                {
                case CmdOptionType.Switch:
                    opt.Value = new List<string>();
                    break;

                case CmdOptionType.Value:
                    if (i == args.Length - 1)
                        _errors.Add(string.Format("Option \"{0}\" requires a value to be specified.", opt.NiceName));
                    else if (opt.Value != null)
                        _errors.Add(string.Format("Option \"{0}\" cannot be specified more than once.", opt.NiceName));
                    else
                    {
                        opt.Value = new List<string>();
                        opt.Value.Add(args[i + 1]);
                        i++;
                    }
                    break;

                case CmdOptionType.List:
                    if (i == args.Length - 1)
                        _errors.Add(string.Format("Option \"{0}\" requires a value to be specified.", opt.NiceName));
                    else
                    {
                        if (opt.Value == null)
                            opt.Value = new List<string>();
                        opt.Value.Add(args[i + 1]);
                        i++;
                    }
                    break;
                }
            }

            // Verify that all required options have been specified.
            foreach (CmdLineOption opt in byDefineOrder)
            {
                if ((opt.Flags & CmdOptionFlags.Required) != 0 && opt.Value == null)
                    _errors.Add(string.Format("Option \"{0}\" is a required option and must not be omitted.", opt.NiceName));
            }

            Parsed = true;
        }

        /// <summary>
        /// Intended to follow up a call to <see cref="Parse"/>, will print help and/or errors
        /// and terminate the program, if necessary. Does nothing on a normal successful parse.
        /// </summary>
        public void ProcessHelpAndErrors()
        {
            if (_help)
            {
                PrintHelp();
                PrintCommit(true);
                ExitProgram(true);
            }
            else if (_errors.Count > 0)
            {
                PrintHelp();
                PrintErrors();
                PrintCommit(false);
                ExitProgram(false);
            }
        }

        /// <summary>
        /// Prints usage help, then prints the specified error, then terminates the program.
        /// </summary>
        public void Error(string text)
        {
            _errors.Add(text);
            PrintHelp();
            PrintErrors();
            PrintCommit(false);
            ExitProgram(false);
        }

        /// <summary>
        /// Reports an error using <see cref="Error"/> if the number of positional arguments is
        /// not equal to "count". Lists all positional arguments in the error message.
        /// </summary>
        public void ErrorIfPositionalArgsCountNot(int count)
        {
            if (count == 0 && OptPositional.Count > 0)
                Error("No positional arguments are expected. Offending arguments: \"{0}\"".Fmt(OptPositional.Join("\", \"")));
            else if (OptPositional.Count != count)
                Error("Exactly {0} positional argument(s) expected. Got {1}: \"{2}\"".Fmt(count, OptPositional.Count, OptPositional.Join("\", \"")));
        }

        /// <summary>
        /// Terminates the program. The exit code will be set to 0 or 1 according to
        /// the "success" parameter.
        /// </summary>
        public void ExitProgram(bool success)
        {
            Environment.Exit(success ? 0 : 1);
        }

        #region Printing commands

        /// <summary>
        /// "Commits" the messages sent to the user. This is usually called when no further
        /// messages are expected. For example, when using a <see cref="CmdLineMessageboxPrinter"/>
        /// this causes the message box to be displayed.
        /// </summary>
        /// <param name="success">Indicates whether the message is a "success" or a "failure"-style message.</param>
        public void PrintCommit(bool success)
        {
            printer.Commit(success);
        }

        /// <summary>
        /// Prints all the error messages. Does nothing if there are none.
        /// </summary>
        public void PrintErrors()
        {
            if (_errors.Count > 0)
            {
                printer.PrintLine("Errors:");
                printer.PrintLine("");
                foreach (var err in _errors)
                {
                    foreach (var line in ("    " + err).WordWrap(printer.MaxWidth - 5))
                        printer.PrintLine(line);
                    printer.PrintLine("");
                }
            }
        }

        /// <summary>
        /// Prints a short summary of program information. The information is deduced
        /// from the attributes of the Entry Assembly via reflection. The following
        /// text will be printed:
        ///
        /// * [Assembly Title]
        /// * Version: [Assembly Version]
        /// * [Assembly Copyright]
        /// * newline
        ///
        /// Any attributes that cannot be retrieved (e.g. they were not specified)
        /// will be skipped silently.
        ///
        /// Calling this function multiple times won't have any effect - only the
        /// first call causes the info to be printed.
        /// </summary>
        public void PrintProgramInfo()
        {
            if (ProgramInfoPrinted)
                return;

            Assembly assembly = Assembly.GetEntryAssembly();

            // Title
            try { printer.PrintLine((assembly.GetCustomAttributes(typeof(AssemblyTitleAttribute), false)[0] as AssemblyTitleAttribute).Title); }
            catch { }

            // Version
            try { printer.PrintLine("Version: " + assembly.GetName().Version.ToString()); }
            catch (Exception E) { printer.PrintLine(E.Message); }

            // Copyright
            try { printer.PrintLine((assembly.GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false)[0] as AssemblyCopyrightAttribute).Copyright); }
            catch { }

            printer.PrintLine("");
            ProgramInfoPrinted = true;
        }

        /// <summary>
        /// Prints automatically generated usage information, consisting of program information,
        /// brief usage summary and detailed description of every option.
        /// </summary>
        /// <remarks>
        /// Prints automatically generated usage information. This consists of:
        ///
        /// 1. Program Info (unless already printed, see <see cref="PrintProgramInfo"/>.
        /// 2. Brief usage summary
        /// 3. Detailed description of every option.
        ///
        /// Brief usage summary shows all options on one line following the exe name,
        /// either by tiny name or, if not available, by long name. Option flags and
        /// type affect the formatting. E.g.:
        ///
        /// <code>testproj.exe -r &lt;required-option&gt; [--optional-switch]</code>
        ///
        /// Detailed summary is a table with one row per option. It shows the tiny
        /// and long option name as well as a description. Long, multi-line descriptions
        /// with indented paragraphs will be word-wrapped properly according to the
        /// console window width.
        /// </remarks>
        public void PrintHelp()
        {
            PrintProgramInfo();

            printer.PrintLine("Usage:");
            printer.PrintLine("");

            //
            // Construct lists of tokens for the one line summary
            //
            List<string> requiredSwitches = new List<string>();
            List<string> optionalSwitches = new List<string>();

            foreach (CmdLineOption option in byDefineOrder)
            {
                if ((option.Flags & CmdOptionFlags.IsHelp) != 0 || option is CmdLineOptionSeparator)
                    continue;

                List<string> switches = (option.Flags & CmdOptionFlags.Required) != 0
                    ? requiredSwitches
                    : optionalSwitches;

                // The switch itself
                string switchName = "-" + option.TinyName;
                if (switchName == null)
                    switchName = "--" + option.LongName;
                if (switchName == null)
                    continue; // the DefineOption function is supposed to ensure that this never happens

                // Argument(s)
                string argName = option.LongName;
                if (argName == null)
                    argName = "value";

                // Build the string
                switch (option.Type)
                {
                case CmdOptionType.Switch:
                    switches.Add(switchName);
                    break;
                case CmdOptionType.Value:
                    switches.Add(string.Format("{0} <{1}>", switchName, argName));
                    break;
                case CmdOptionType.List:
                    switches.Add(string.Format("[{0} <{1} 1> [... {0} <{1} N>]]", switchName, argName));
                    break;
                }
            }

            //
            // Print the one-line summary
            //
            printer.Print("    ");
            printer.Print(Assembly.GetEntryAssembly().ManifestModule.Name);
            foreach (string token in requiredSwitches)
                printer.Print(" " + token);
            foreach (string token in optionalSwitches)
                printer.Print(" [" + token + "]");
            printer.PrintLine("");

            //
            // Print a table of options and their descriptions
            //
            TextTable table = new TextTable();
            int row = 0;
            for (int i = 0; i < byDefineOrder.Count; i++)
            {
                CmdLineOption option = byDefineOrder[i];

                // Skip help options
                if ((option.Flags & CmdOptionFlags.IsHelp) != 0)
                    continue;

                if (option is CmdLineOptionSeparator)
                {
                    /* nothing - this will leave an empty row in the table */
                }
                else
                {
                    if (option.TinyName != null)
                        table[row, 0] = "-" + option.TinyName;

                    if (option.LongName != null)
                        table[row, 1] = "--" + option.LongName;

                    if (option.Description != null)
                        table[row, 2] = option.Description;
                }

                row++;
            }

            table.SetAutoSize(2, true);

            printer.PrintLine("");
            printer.PrintLine("Available options:");
            printer.PrintLine("");

            printer.PrintLine(table.GetText(4, printer.MaxWidth - 5, 3, false));
        }

        #endregion

        #region Option retrieval

        /// <summary>
        /// Call this function to change the behaviour of <see cref="OptValue(string)"/>
        /// and <see cref="OptList"/> functions so that they look up option values
        /// by the tiny name rather than by long name (which is the default).
        /// </summary>
        public void GetOptionsByTinyName()
        {
            byPreferredName = byTinyName;
        }

        /// <summary>
        /// Call this function to change the behaviour of <see cref="OptValue(string)"/>
        /// and <see cref="OptList"/> functions so that they look up option values
        /// by the long name. Since this is the default after calling <see cref="Parse"/>
        /// anyway it's not necessary to do this.
        /// </summary>
        public void GetOptionsByLongName()
        {
            byPreferredName = byLongName;
        }

        /// <summary>
        /// Gets a list of all options which did not look like named options (did not start
        /// with a minus) and were not arguments to known named options.
        /// </summary>
        public List<string> OptPositional
        {
            get
            {
                if (!Parsed)
                    throw new InvalidOperationException("The Parse() method must be called before this method can be used.");

                return unmatchedArgs;
            }
        }

        /// <summary>
        /// Returns true if the specified Switch-type option was set.
        /// </summary>
        public bool OptSwitch(string name)
        {
            if (!Parsed)
                throw new InvalidOperationException("The Parse() method must be called before this method can be used.");

            return byPreferredName[name].Value != null;
        }

        /// <summary>
        /// Returns the value specified for a Value-type option, or "null" if
        /// the option was not specified.
        ///
        /// Identical to OptValue(name, null);
        /// </summary>
        public string OptValue(string name)
        {
            if (!Parsed)
                throw new InvalidOperationException("The Parse() method must be called before this method can be used.");

            return OptValue(name, null);
        }

        /// <summary>
        /// Returns the value specified for a Value-type option, or the
        /// default value if it wasn't specified.
        /// </summary>
        public string OptValue(string name, string defaultIfUnspecified)
        {
            if (!Parsed)
                throw new InvalidOperationException("The Parse() method must be called before this method can be used.");

            return byPreferredName[name].Value == null ? defaultIfUnspecified : byPreferredName[name].Value[0];
        }

        /// <summary>
        /// Returns a list of values specified for the given List-type option,
        /// in the order in which they were specified. If there are none,
        /// returns an empty list; never returns null.
        /// </summary>
        public List<string> OptList(string name)
        {
            if (!Parsed)
                throw new InvalidOperationException("The Parse() method must be called before this method can be used.");

            if (byPreferredName[name].Value == null)
                return new List<string>();
            else
                return byPreferredName[name].Value;
        }

        /// <summary>
        /// Gets the value of the specified option.
        ///
        /// If the option is a value:
        ///     returns the value or null for optional unspecified values.
        /// If the option is a switch:
        ///     returns the string "true" if specified or null if unspecified.
        /// If the option is a list:
        ///     throws an <see cref="InvalidOperationException"/>.
        /// </summary>
        public string this[string name]
        {
            get
            {
                if (!Parsed)
                    throw new InvalidOperationException("The Parse() method must be called before this method can be used.");

                switch (byPreferredName[name].Type)
                {
                    case CmdOptionType.Switch:
                        return byPreferredName[name].Value == null ? null : "true";
                    case CmdOptionType.Value:
                        return byPreferredName[name].Value == null ? null : byPreferredName[name].Value[0];
                    case CmdOptionType.List:
                        throw new InvalidOperationException("Cannot access a List-type command line option using the indexer.");
                    default:
                        throw new Exception("Internal error");
                }
            }
        }

        #endregion

        #region Other parse results

        /// <summary>
        /// Gets the list of parse errors found. Can only be called after 
        /// </summary>
        public List<string> Errors
        {
            get
            {
                if (!Parsed)
                    throw new InvalidOperationException("The Parse() method must be called before this method can be used.");

                return _errors;
            }
        }

        /// <summary>
        /// Returns true if the parse resulted in errors. Errors can be retrieved using <see cref="Errors"/>.
        /// The user can be informed of these errors automatically by calling <see cref="ProcessHelpAndErrors"/>.
        /// </summary>
        public bool HadErrors
        {
            get
            {
                if (!Parsed)
                    throw new InvalidOperationException("The Parse() method must be called before this method can be used.");

                return _errors.Count > 0;
            }
        }

        /// <summary>
        /// Returns true if a help option was specified. It can be processed manually by calling <see cref="PrintHelp"/>,
        /// or automatically using <see cref="ProcessHelpAndErrors"/>.
        /// </summary>
        public bool HadHelp
        {
            get
            {
                if (!Parsed)
                    throw new InvalidOperationException("The Parse() method must be called before this method can be used.");

                return _help;
            }
        }

        #endregion
    }

    /// <summary>Enumerates the possible command line option types.</summary>
    public enum CmdOptionType
    {
        /// <summary>This option can either be set or not set, and does not allow a value to be specified.</summary>
        Switch,
        /// <summary>This option takes a value which must be specified immediately following the option.</summary>
        Value,
        /// <summary>Like Value but can be specified multiple times, allowing to specify a list of values.</summary>
        List
    }

    /// <summary>Lists flags that determine the behaviour of an option.</summary>
    [Flags]
    public enum CmdOptionFlags
    {
        /// <summary>This option must be specified on the command line, otherwise parse will fail.</summary>
        Required = 1,
        /// <summary>This option may or may not be specified.</summary>
        Optional = 0,
        /// <summary>If specified, this option causes help to be displayed. It is NOT listed on the help screen.</summary>
        IsHelp = 2,
    }

    /// <summary>
    /// Abstract base class for a command line parser output printer. Provides an interface
    /// through which <see cref="CmdLineParser"/> provides information to the user.
    /// </summary>
    public abstract class CmdLinePrinterBase
    {
        /// <summary>Prints the specified text</summary>
        public abstract void Print(string text);
        /// <summary>Prints the specified text, followed by an end of line</summary>
        public abstract void PrintLine(string text);
        /// <summary>"Commits" the text printed so far, for printers that can only show information in chunks.</summary>
        public abstract void Commit(bool success);
        /// <summary>Returns the maximum width that the messages printed to this printer can have.
        /// Any longer messages will be automatically wrapped by the parser before printing them.</summary>
        public abstract int MaxWidth { get; }
    }

    /// <summary>
    /// Prints <see cref="CmdLineParser"/> messages to the console.
    /// </summary>
    public class CmdLineConsolePrinter: CmdLinePrinterBase
    {
#pragma warning disable 1591    // Missing XML comment for publicly visible type or member
        public override void Print(string text)
        {
            Console.Write(text);
        }

        public override void PrintLine(string text)
        {
            Console.WriteLine(text);
        }

        public override void Commit(bool success)
        {
        }

        public override int MaxWidth
        {
            get { return Console.WindowWidth; }
        }
#pragma warning restore 1591    // Missing XML comment for publicly visible type or member
    }

    /// <summary>
    /// Prints <see cref="CmdLineParser"/> messages using message boxes.
    /// </summary>
    public class CmdLineMessageboxPrinter: CmdLinePrinterBase
    {
#pragma warning disable 1591    // Missing XML comment for publicly visible type or member
        private StringBuilder buffer = new StringBuilder();
        private int maxWidth = 80;

        public CmdLineMessageboxPrinter() { }

        public CmdLineMessageboxPrinter(int maxWidth)
        {
            this.maxWidth = maxWidth;
        }

        public override void Print(string text)
        {
            buffer.Append(text);
        }

        public override void PrintLine(string text)
        {
            buffer.Append(text);
            buffer.Append("\r\n");
        }

        public override void Commit(bool success)
        {
            if (buffer.Length == 0)
                return;

            new DlgMessage
            {
                Type = success ? DlgType.Info : DlgType.Warning,
                Message = buffer.ToString(),
                Font = new System.Drawing.Font("Consolas", 9)
            }.Show();

            buffer.Remove(0, buffer.Length);
        }

        public override int MaxWidth
        {
            get { return maxWidth; }
        }
#pragma warning restore 1591    // Missing XML comment for publicly visible type or member
    }

}
