using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using RT.Util.Text;
using System.Security.Permissions;

namespace RT.Util
{
    /// <summary>
    /// A class which aids parsing command-line arguments.
    /// Requires the UnmanagedCode security permission due to the use
    /// of Environment.Exit function.
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

        /// <summary>All options that the user defined, including separators.</summary>
        private List<CmdLineOption> byDefineOrder = new List<CmdLineOption>();

        /// <summary>Only the options which had a non-null tiny name specified.</summary>
        private Dictionary<string, CmdLineOption> byTinyName = new Dictionary<string, CmdLineOption>();

        /// <summary>Only the options which had a non-null long name specified.</summary>
        private Dictionary<string, CmdLineOption> byLongName = new Dictionary<string, CmdLineOption>();

        /// <summary>
        /// This points to either byTinyName or byLongName. By default it points to
        /// byLongName, but the user of this class can call a method to change this to
        /// byTinyName.
        /// 
        /// The public methods used to access parse results will use this variable to
        /// look up option names. Hence this basically determines whether the user
        /// accesses options by their full names or their tiny names.
        /// </summary>
        private Dictionary<string, CmdLineOption> byPreferredName;

        /// <summary>
        /// Holds all unmatched arguments (i.e. arguments which were neither one of
        /// the defined option names nor one of the arguments).
        /// </summary>
        private List<string> unmatchedArgs = new List<string>();

        /// <summary>
        /// Keeps track of whether Parse() has ever been called.
        /// </summary>
        private bool Parsed = false;

        /// <summary>
        /// Keeps track of whether PrintProgramInfo() has ever been called.
        /// </summary>
        private static bool ProgramInfoPrinted = false;

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
        /// so this string can be long and may include line breaks and indented examples. Cool eh?</param>
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
        /// output between the option defined by the last call to <see>DefineOption</see>
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
        /// * The options won't be printed when printing help.
        /// * Specifying one of these options causes the program to print help
        ///   and terminate (at the time of the parsing).
        ///   
        /// The default help options are: -?, --help, --usage.
        /// 
        /// For more details see the <see>Parse</see> function.
        /// </summary>
        public void DefineDefaultHelpOptions()
        {
            DefineOption("?",  "help",  CmdOptionType.Switch, CmdOptionFlags.IsHelp, null);
            DefineOption(null, "usage", CmdOptionType.Switch, CmdOptionFlags.IsHelp, null);
        }

        /// <summary>
        /// Parses the specified command line arguments according to the options
        /// defined (using the Define* functions).
        /// 
        /// The parse options define what happens in case there is an error. This
        /// function can print help, list errors and terminate the program in such
        /// case, but it doesn't have to. See individual <see>CmdParse</see> values
        /// for more info.
        /// 
        /// After a successful call to this function the results can be accessed
        /// via the Opt* methods/properties.
        /// 
        /// Calling this method multiple times erases the results produced by the
        /// previous call and generates new results.
        /// </summary>
        public List<string> Parse(string[] args, CmdParse parseOptions)
        {
            List<string> errors = new List<string>();

            // Reset the preferred access name to long name
            byPreferredName = byLongName;

            // Reset all the previously parsed options, if any
            unmatchedArgs.Clear();
            foreach (CmdLineOption opt in byDefineOrder)
                opt.Value = null;

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                CmdLineOption opt = null;

                if (arg.StartsWith("--") && byLongName.ContainsKey(arg.Substring(2)))
                    opt = byLongName[arg.Substring(2)];
                else if (arg.StartsWith("-") && byTinyName.ContainsKey(arg.Substring(1)))
                    opt = byTinyName[arg.Substring(1)];

                // Is this option unmatched?
                if (opt == null)
                {
                    unmatchedArgs.Add(arg);
                    if ((parseOptions & CmdParse.FailIfAnyUnmatched) != 0)
                        errors.Add(string.Format("Option \"{0}\" doesn't match any of the allowed options.", arg));
                    continue;
                }
                
                // Is this a help option
                if ((opt.Flags & CmdOptionFlags.IsHelp) != 0)
                {
                    PrintHelp();
                    Environment.Exit(0);
                }

                // This option was matched
                switch (opt.Type)
                {
                case CmdOptionType.Switch:
                    opt.Value = new List<string>();
                    break;

                case CmdOptionType.Value:
                    if (i == args.Length - 1)
                        errors.Add(string.Format("Option \"{0}\" requires a value to be specified.", opt.NiceName));
                    else if (opt.Value != null)
                        errors.Add(string.Format("Option \"{0}\" cannot be specified more than once.", opt.NiceName));
                    else
                    {
                        opt.Value = new List<string>();
                        opt.Value.Add(args[i + 1]);
                        i++;
                    }
                    break;

                case CmdOptionType.List:
                    if (i == args.Length - 1)
                        errors.Add(string.Format("Option \"{0}\" requires a value to be specified.", opt.NiceName));
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
                    errors.Add(string.Format("Option \"{0}\" is a required option and cannot be omitted.", opt.NiceName));
            }

            // All arguments are parsed now. Display errors, if any.
            if (errors.Count == 0)
            {
                Parsed = true;
                return null;
            }
            else
            {
                if ((parseOptions & CmdParse.IfFailShowHelp) != 0)
                    PrintHelp();

                if ((parseOptions & CmdParse.IfFailKillApp) != 0)
                {
                    // Print errors first
                    foreach (string err in errors)
                        System.Console.WriteLine(err);

                    // Then kill the app
                    Environment.Exit(1);
                }

                // Otherwise return the list of errors
                return errors;
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
        public static void PrintProgramInfo()
        {
            if (ProgramInfoPrinted)
                return;

            Assembly assembly = Assembly.GetEntryAssembly();

            // Title
            try { Console.WriteLine((assembly.GetCustomAttributes(typeof(AssemblyTitleAttribute), false)[0] as AssemblyTitleAttribute).Title); }
            catch { }

            // Version
            try { Console.WriteLine("Version: " + assembly.GetName().Version.ToString()); }
            catch (Exception E) { Console.WriteLine(E.Message); }

            // Copyright
            try { Console.WriteLine((assembly.GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false)[0] as AssemblyCopyrightAttribute).Copyright); }
            catch { }

            Console.WriteLine();
            ProgramInfoPrinted = true;
        }

        /// <summary>
        /// Prints automatically generated usage information. This consists of:
        /// 
        /// 1. Program Info (unless already printed, see <see>PrintProgramInfo</see>.
        /// 2. Brief usage summary
        /// 3. Detailed description of every option.
        /// 
        /// Brief usage summary shows all options on one line following the exe name,
        /// either by tiny name or, if not available, by long name. Option flags and
        /// type affect the formatting. E.g.:
        /// 
        ///     testproj.exe -r <required-option> [--optional-switch]
        ///     
        /// Detailed summary is a table with one row per option. It shows the tiny
        /// and long option name as well as a description. The description is printed
        /// using <see>TextWordWrapped</see>, which means long, multi-line and even
        /// indented text will be printed properly. The console window width will
        /// be used for word wrapping.
        /// </summary>
        public void PrintHelp()
        {
            PrintProgramInfo();

            Console.WriteLine("Usage:");
            Console.WriteLine();

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
            // Print the one line summary
            //
            Console.Write("    ");
            Console.Write(Assembly.GetEntryAssembly().ManifestModule.Name);
            foreach (string token in requiredSwitches)
                Console.Write(" " + token);
            foreach (string token in optionalSwitches)
                Console.Write(" [" + token + "]");
            Console.WriteLine();

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

            Console.WriteLine();
            Console.WriteLine("Available options:");
            Console.WriteLine();

            Console.WriteLine(table.GetText(4, Console.WindowWidth - 5, 3, false));
        }

        /// <summary>
        /// Call this function to change the behaviour of <see>OptBool</see>,
        /// <see>OptValue</see> and <see>OptList</see> functions so that they
        /// look up option values by the tiny name rather than by long name
        /// (which is the default).
        /// </summary>
        public void GetOptionsByTinyName()
        {
            byPreferredName = byTinyName;
        }

        /// <summary>
        /// Call this function to change the behaviour of <see>OptBool</see>,
        /// <see>OptValue</see> and <see>OptList</see> functions so that they
        /// look up option values by the long name. Since this is the default
        /// after calling <see>Parse</see> anyway it's not necessary to do this.
        /// </summary>
        public void GetOptionsByLongName()
        {
            byPreferredName = byLongName;
        }

        /// <summary>
        /// Gets a list of all options which didn't match any of the defined
        /// options and were not arguments to the defined options.
        /// </summary>
        public List<string> OptUnmatched
        {
            get
            {
                if (!Parsed)
                    throw new InvalidOperationException("This method can only be called after parsing the arguments.");

                return unmatchedArgs;
            }
        }

        /// <summary>
        /// Returns true if the specified Switch-type option was set.
        /// </summary>
        public bool OptSwitch(string name)
        {
            if (!Parsed)
                throw new InvalidOperationException("This method can only be called after parsing the arguments.");

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
                throw new InvalidOperationException("This method can only be called after parsing the arguments.");

            return OptValue(name, null);
        }

        /// <summary>
        /// Returns the value specified for a Value-type option, or the
        /// default value if it wasn't specified.
        /// </summary>
        public string OptValue(string name, string defaultIfUnspecified)
        {
            if (!Parsed)
                throw new InvalidOperationException("This method can only be called after parsing the arguments.");

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
                throw new InvalidOperationException("This method can only be called after parsing the arguments.");

            if (byPreferredName[name].Value == null)
                return new List<string>();
            else
                return byPreferredName[name].Value;
        }
    }

    public enum CmdOptionType
    {
        /// <summary>This option can either be set or not set, and does not allow a value to be specified.</summary>
        Switch,
        /// <summary>This option takes a value which must be specified immediately following the option.</summary>
        Value,
        /// <summary>Like Value but can be specified multiple times, allowing to specify a list of values.</summary>
        List
    }

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

    [Flags]
    public enum CmdParse
    {
        /// <summary>Upon a parse failure print help to the console automatically.</summary>
        IfFailShowHelp = 1,
        /// <summary>Upon a parse failure the application will be terminated and a list of
        /// errors will be printed to the console.</summary>
        IfFailKillApp = 2,

        /// <summary>The same as IfFailShowHelp + IfFailKillApp.</summary>
        IfFailShowHelpAndKillApp = 3,

        /// <summary>Parse will fail if any unmatched options are found.</summary>
        FailIfAnyUnmatched = 4,
    }

}
