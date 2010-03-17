using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Permissions;
using System.Text;
using System.Text.RegularExpressions;
using RT.Util.Consoles;
using RT.Util.Dialogs;
using RT.Util.ExtensionMethods;
using RT.Util.Text;

namespace RT.Util
{
    /// <summary>(obsolete) A class which aids parsing command-line arguments.</summary>
    /// <remarks>Requires the UnmanagedCode security permission due to the use of Environment.Exit().</remarks>
    [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode)]
    [Obsolete("Use RT.Util.CommandLine.CommandLineParser.ParseCommandLine<T> instead.")]
    public class CmdLineParser
    {
        /// <summary>
        /// Describes a single option.
        /// </summary>
        private class option
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
        private class separator : option
        {
        }

        /// <summary>All options that the user has defined, including separators.</summary>
        private List<option> _byDefineOrder = new List<option>();

        /// <summary>Only the options which had a non-null tiny name specified.</summary>
        private Dictionary<string, option> _byTinyName = new Dictionary<string, option>();

        /// <summary>Only the options which had a non-null long name specified.</summary>
        private Dictionary<string, option> _byLongName = new Dictionary<string, option>();

        /// <summary>Contains the union of <see cref="_byTinyName"/> and <see cref="_byLongName"/>.</summary>
        private Dictionary<string, option> _byEitherName = new Dictionary<string, option>();

        /// <summary>
        /// Holds all positional arguments, that is, all arguments which do not look like
        /// named options and are not arguments to known named options.
        /// </summary>
        private List<string> _unmatchedArgs;

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
        private CmdLinePrinterBase _printer = new CmdLineConsolePrinter();

        /// <summary>
        /// Keeps track of whether Parse() has ever been called.
        /// </summary>
        private bool _parsed = false;

        /// <summary>
        /// Keeps track of whether PrintProgramInfo() has been called.
        /// </summary>
        private bool _programInfoPrinted = false;

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
            _printer = printer;
        }

        /// <summary>
        /// Defines and describes a command-line argument.
        /// </summary>
        /// <param name="tinyName">Ideally a single character. Accessed with a single minus,
        /// e.g. an option named "c" here can be specified as "-c" on the command line. Null to omit.</param>
        /// <param name="longName">Ideally a fairly descriptive name. Accessed with a double minus,
        /// e.g. an option named "connection-string" can be specified as "--connection-string"
        /// on the command line. Null to omit.</param>
        /// <param name="type">Specifies whether an option is an on/off, a string value or a list of values.</param>
        /// <param name="flags">Selects the behaviour of this option.</param>
        /// <param name="description">A human-readable description of what the option does. This
        /// text is used when printing help. Note that the help text gets automatically wrapped,
        /// so this string can be long and may include line breaks and indented examples.</param>
        public void DefineOption(string tinyName, string longName, CmdOptionType type, CmdOptionFlags flags, string description)
        {
            if (tinyName == null && longName == null)
                throw new ArgumentException("Both the tiny and the long switch names are null. The user won't be able to specify it.");

            option opt = new option();
            opt.TinyName = tinyName;
            opt.LongName = longName;
            opt.Type = type;
            opt.Flags = flags;
            opt.Description = description;
            opt.NiceName = tinyName == null ? ("--" + longName) : longName == null ? ("-" + tinyName) : string.Format("-{0}/--{1}", tinyName, longName);

            _byDefineOrder.Add(opt);
            if (tinyName != null)
            {
                _byTinyName.Add(tinyName, opt);
                _byEitherName.Add(tinyName, opt);
            }
            if (longName != null)
            {
                _byLongName.Add(longName, opt);
                _byEitherName.Add(longName, opt);
            }
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
            _byDefineOrder.Add(new separator());
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
            DefineOption("?", "help", CmdOptionType.Switch, CmdOptionFlags.IsHelp, null);
            DefineOption(null, "usage", CmdOptionType.Switch, CmdOptionFlags.IsHelp, null);
        }

        /// <summary>
        /// Clears all the results of the previous parse, effectively restoring the state of the
        /// parser to before the Parse method was called, with all options still defined.
        /// </summary>
        public void ClearResults()
        {
            _parsed = false;
            _errors = new List<string>();
            _help = false;
            _unmatchedArgs = new List<string>();
            foreach (var option in _byDefineOrder)
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
            if (_parsed)
                throw new RTException("Parse results must be cleared using ClearResults before calling Parse again.");

            List<option> ignoreReq = new List<option>();

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                option opt;

                if (arg.StartsWith("--") && _byLongName.ContainsKey(arg.Substring(2)))
                    opt = _byLongName[arg.Substring(2)];
                else if (arg.StartsWith("-") && _byTinyName.ContainsKey(arg.Substring(1)))
                    opt = _byTinyName[arg.Substring(1)];
                else
                {
                    if (arg.StartsWith("-"))
                        _errors.Add(string.Format("Option \"{0}\" doesn't match any of the allowed options.", arg));
                    else
                        _unmatchedArgs.Add(arg);
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
                        {
                            _errors.Add(string.Format("Option \"{0}\" requires a value to be specified.", opt.NiceName));
                            ignoreReq.Add(opt);
                        }
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
                        {
                            _errors.Add(string.Format("Option \"{0}\" requires a value to be specified.", opt.NiceName));
                            ignoreReq.Add(opt);
                        }
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
            foreach (option opt in _byDefineOrder)
            {
                if (!ignoreReq.Contains(opt) && (opt.Flags & CmdOptionFlags.Required) != 0 && opt.Value == null)
                    _errors.Add(string.Format("Option \"{0}\" is a required option and must not be omitted.", opt.NiceName));
            }

            _parsed = true;
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
            var arguments = OptPositional.Count == 0 ? "none" : ("\"" + OptPositional.JoinString("\", \"") + "\"");
            if (count == 0 && OptPositional.Count > 0)
                Error("No positional arguments are expected. Received arguments: {0}".Fmt(arguments));
            else if (OptPositional.Count != count)
                Error("Exactly {0} positional argument(s) expected. Received arguments: {1}".Fmt(count, arguments));
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
            _printer.Commit(success);
        }

        /// <summary>
        /// Prints all the error messages. Does nothing if there are none.
        /// </summary>
        public void PrintErrors()
        {
            if (_errors.Count > 0)
            {
                _printer.PrintLine("Errors:");
                _printer.PrintLine("");
                foreach (var err in _errors)
                {
                    foreach (var line in ("    " + err).WordWrap(_printer.MaxWidth - 4))
                        _printer.PrintLine(line);
                    _printer.PrintLine("");
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
            if (_programInfoPrinted)
                return;

            Assembly assembly = Assembly.GetEntryAssembly();

            // Title
            try { _printer.PrintLine(assembly.GetCustomAttributes<AssemblyTitleAttribute>().First().Title); }
            catch { }

            // Version
            try { _printer.PrintLine("Version: " + assembly.GetName().Version.ToString()); }
            catch { }

            // Copyright
            try { _printer.PrintLine(assembly.GetCustomAttributes<AssemblyCopyrightAttribute>().First().Copyright); }
            catch { }

            _printer.PrintLine("");
            _programInfoPrinted = true;
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

            _printer.PrintLine("Usage:");
            _printer.PrintLine("");

            //
            // Construct lists of tokens for the one line summary
            //
            List<string> requiredSwitches = new List<string>();
            List<string> optionalSwitches = new List<string>();

            foreach (option option in _byDefineOrder)
            {
                if ((option.Flags & CmdOptionFlags.IsHelp) != 0 || option is separator)
                    continue;

                List<string> switches = (option.Flags & CmdOptionFlags.Required) != 0
                    ? requiredSwitches
                    : optionalSwitches;

                // The switch itself
                string switchName = option.TinyName == null ? "--" + option.LongName : "-" + option.TinyName;

                // Argument(s)
                string argName = option.LongName ?? "value";

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
            _printer.Print("    ");
            var entryAssembly = Assembly.GetEntryAssembly();
            _printer.Print(entryAssembly == null ? "<programname>" : entryAssembly.ManifestModule.Name);
            foreach (string token in requiredSwitches)
                _printer.Print(" " + token);
            foreach (string token in optionalSwitches)
                _printer.Print(" [" + token + "]");
            _printer.PrintLine("");

            //
            // Print a table of options and their descriptions
            //
            bool anyPrintableOptions = false;
            var width = ConsoleUtil.WrapToWidth();
            var leftIndent = 4;
            TextTable table = new TextTable { MaxWidth = _printer.MaxWidth - leftIndent - 4, ColumnSpacing = 3, RowSpacing = 1 };
            int row = 0;
            for (int i = 0; i < _byDefineOrder.Count; i++)
            {
                option option = _byDefineOrder[i];

                // Skip help options
                if ((option.Flags & CmdOptionFlags.IsHelp) != 0)
                    continue;

                if (option is separator)
                {
                    /* nothing - this will leave an empty row in the table */
                }
                else
                {
                    anyPrintableOptions = true;

                    if (option.TinyName != null)
                        table.SetCell(0, row, "-" + option.TinyName, true);

                    if (option.LongName != null)
                        table.SetCell(1, row, "--" + option.LongName, true);

                    if (option.Description != null)
                        table.SetCell(2, row, option.Description);
                }

                row++;
            }

            _printer.PrintLine("");
            if (anyPrintableOptions)
            {
                _printer.PrintLine("Available options:");
                _printer.PrintLine("");
                _printer.PrintLine(Regex.Replace(table.ToString(), "^", new string(' ', leftIndent), RegexOptions.Multiline));
            }
        }

        #endregion

        #region Option retrieval

        /// <summary>
        /// Gets a list of all options which did not look like named options (did not start
        /// with a minus) and were not arguments to known named options.
        /// </summary>
        public List<string> OptPositional
        {
            get
            {
                if (!_parsed)
                    throw new InvalidOperationException("The Parse() method must be called before this method can be used.");

                return _unmatchedArgs;
            }
        }

        /// <summary>
        /// Returns true if the specified Switch-type option was set.
        /// </summary>
        public bool OptSwitch(string name)
        {
            if (!_parsed)
                throw new InvalidOperationException("The Parse() method must be called before this method can be used.");

            return _byEitherName.ContainsKey(name) && _byEitherName[name].Value != null;
        }

        /// <summary>
        /// Returns the value specified for a Value-type option, or "null" if
        /// the option was not specified.
        ///
        /// Identical to OptValue(name, null);
        /// </summary>
        public string OptValue(string name)
        {
            return OptValue(name, null);
        }

        /// <summary>
        /// Returns the value specified for a Value-type option, or the
        /// default value if it wasn't specified.
        /// </summary>
        public string OptValue(string name, string defaultIfUnspecified)
        {
            if (!_parsed)
                throw new InvalidOperationException("The Parse() method must be called before this method can be used.");

            return !_byEitherName.ContainsKey(name) || _byEitherName[name].Value == null ? defaultIfUnspecified : _byEitherName[name].Value[0];
        }

        /// <summary>
        /// Returns a list of values specified for the given List-type option,
        /// in the order in which they were specified. If there are none,
        /// returns an empty list; never returns null.
        /// </summary>
        public List<string> OptList(string name)
        {
            if (!_parsed)
                throw new InvalidOperationException("The Parse() method must be called before this method can be used.");

            if (!_byEitherName.ContainsKey(name) || _byEitherName[name].Value == null)
                return new List<string>();
            else
                return _byEitherName[name].Value;
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
                if (!_parsed)
                    throw new InvalidOperationException("The Parse() method must be called before this method can be used.");
                if (!_byEitherName.ContainsKey(name))
                    return null;

                switch (_byEitherName[name].Type)
                {
                    case CmdOptionType.Switch:
                        return _byEitherName[name].Value == null ? null : "true";
                    case CmdOptionType.Value:
                        return _byEitherName[name].Value == null ? null : _byEitherName[name].Value[0];
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
                if (!_parsed)
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
                if (!_parsed)
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
                if (!_parsed)
                    throw new InvalidOperationException("The Parse() method must be called before this method can be used.");

                return _help;
            }
        }

        #endregion
    }

    /// <summary>(obsolete) Enumerates the possible command-line option types.</summary>
    public enum CmdOptionType
    {
        /// <summary>This option can either be set or not set, and does not allow a value to be specified.</summary>
        Switch,
        /// <summary>This option takes a value which must be specified immediately following the option.</summary>
        Value,
        /// <summary>Like Value but can be specified multiple times, allowing to specify a list of values.</summary>
        List
    }

    /// <summary>(obsolete) Lists flags that determine the behaviour of an option.</summary>
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

    /// <summary>(obsolete) Abstract base class for a command line parser output printer. Provides an interface
    /// through which <see cref="CmdLineParser"/> provides information to the user.</summary>
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
    /// (obsolete) Prints <see cref="CmdLineParser"/> messages to the console.
    /// </summary>
    public class CmdLineConsolePrinter : CmdLinePrinterBase
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
            get { return ConsoleUtil.WrapToWidth(); }
        }
#pragma warning restore 1591    // Missing XML comment for publicly visible type or member
    }

    /// <summary>(obsolete) Prints <see cref="CmdLineParser"/> messages using message boxes.</summary>
    public class CmdLineMessageboxPrinter : CmdLinePrinterBase
    {
#pragma warning disable 1591    // Missing XML comment for publicly visible type or member
        private StringBuilder _buffer = new StringBuilder();
        private int _maxWidth = 80;

        public CmdLineMessageboxPrinter() { }

        public CmdLineMessageboxPrinter(int maxWidth)
        {
            _maxWidth = maxWidth;
        }

        public override void Print(string text)
        {
            _buffer.Append(text);
        }

        public override void PrintLine(string text)
        {
            _buffer.Append(text);
            _buffer.Append("\r\n");
        }

        public override void Commit(bool success)
        {
            if (_buffer.Length == 0)
                return;

            new DlgMessage
            {
                Type = success ? DlgType.Info : DlgType.Warning,
                Message = _buffer.ToString(),
                Font = new System.Drawing.Font("Consolas", 9)
            }.Show();

            _buffer.Remove(0, _buffer.Length);
        }

        public override int MaxWidth
        {
            get { return _maxWidth; }
        }
#pragma warning restore 1591    // Missing XML comment for publicly visible type or member
    }

}
