using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using RT.Util.Consoles;
using RT.Util.ExtensionMethods;
using RT.Util.Lingo;
using RT.Util.Text;

namespace RT.Util.CommandLine
{
    /// <summary>Implements a command-line parser that can turn the commands and options specified by the user on the command line into a strongly-typed instance of a specific class. See remarks for more details.</summary>
    /// <typeparam name="T">The class containing the fields and attributes which define the command-line syntax.</typeparam>
    /// <remarks><para>The following conditions must be met by the class wishing to receive the options and parameters:</para>
    /// <list type="bullet">
    /// <item><description>It must be a reference type (a class) and it must have a parameterless constructor.</description></item>
    /// <item><description>Each field in the class must be of type string, string[], bool, an enum type, or a class with the <see cref="CommandGroupAttribute"/>.</description></item>
    /// <item><description>A field of an enum type can be positional (marked with the <see cref="IsPositionalAttribute"/>) or not. If it is neither positional nor mandatory (see below), it must have a <see cref="DefaultValueAttribute"/>.</description></item>
    /// <item><description>Every value of such an enum must have an <see cref="OptionAttribute"/> if the field is optional, or a <see cref="CommandNameAttribute"/> if it is positional.</description></item>
    /// <item><description>A field of type bool must have an <see cref="OptionAttribute"/> and cannot be positional.</description></item>
    /// <item><description>A field of type string or string[] can be positional or optional. If it is optional, it must have an <see cref="OptionAttribute"/>. If it is of type string[] and positional, it must be the last field in the class.</description></item>
    /// <item><description>A field of any other type must be the last one, must be marked positional, and must be an abstract class with a <see cref="CommandGroupAttribute"/>. This class must have at least two derived classes with a <see cref="CommandNameAttribute"/>.</description></item>
    /// <item><description>Wherever an <see cref="OptionAttribute"/> or <see cref="CommandNameAttribute"/> is required, several such attributes are allowed to specify several alternative names for the same option or command (e.g. short and long names).</description></item>
    /// <item><description>Any field that is not positional can be made mandatory by using the <see cref="IsMandatoryAttribute"/>.</description></item>
    /// <item><description><para>Every field must have documentation or be explicitly marked with <see cref="UndocumentedAttribute"/>. For enum-typed fields, the enum values must have documentation or <see cref="UndocumentedAttribute"/> instead.</para>
    ///                                     <para>Documentation is provided in one of the following ways:</para>
    ///    <list type="bullet">
    ///        <item><description>Monolingual, translation-agnostic (unlocalisable) applications use the <see cref="DocumentationLiteralAttribute"/> to specify documentation directly.</description></item>
    ///        <item><description>Translatable applications must declare methods with the following signature: <c>static string FieldNameDoc(Translation)</c>.
    ///                                            The first parameter must be of the same type as the object passed in for <see cref="ApplicationTr"/>.
    ///                                            The name of the method is the name of the field or enum value followed by "Doc".
    ///                                            The return value is the translated string.</description></item>
    ///    </list>
    /// </description></item>
    /// </list>
    /// </remarks>
    public sealed class CommandLineParser<T>
    {
        /// <summary>
        /// Gets or sets the application's translation object which contains the localised strings that document the command-line options and commands.
        /// This object is passed in to the FieldNameDoc() methods described in the documentation for <see cref="CommandLineParser&lt;T&gt;"/>.
        /// </summary>
        public TranslationBase ApplicationTr { get; set; }

        /// <summary>Parses the specified command-line arguments into an instance of the specified type. See the remarks section of the documentation for <see cref="CommandLineParser&lt;T&gt;"/> for features and limitations.</summary>
        /// <param name="args">The command-line arguments to be parsed.</param>
        /// <returns>An instance of the class <typeparamref name="T"/> containing the options and parameters specified by the user on the command line.</returns>
        public T Parse(string[] args)
        {
            return (T) parseCommandLine(args, typeof(T), 0);
        }

        /// <summary>
        /// Throws a <see cref="CommandLineValidationException"/> exception, passing it the top-level command line argument type.
        /// </summary>
        /// <param name="errorMessage">The error message to be displayed to the user, describing the validation error.</param>
        public void ValidationError(string errorMessage)
        {
            throw new CommandLineValidationException(errorMessage, getHelpGenerator(typeof(T)));
        }

        private sealed class positionalParameterInfo
        {
            public Action ProcessParameter;
            public Action ProcessEndOfParameters;
        }

        private object parseCommandLine(string[] args, Type type, int i)
        {
            var ret = type.GetConstructor(Type.EmptyTypes).Invoke(null);
            var options = new Dictionary<string, Action>();
            var positionals = new List<positionalParameterInfo>();
            var missingMandatories = new List<FieldInfo>();
            FieldInfo swallowingField = null;

            foreach (var fieldForeachVariable in type.GetFields())
            {
                var field = fieldForeachVariable; // This is necessary for the lambda expressions to work
                var positional = field.IsDefined<IsPositionalAttribute>();
                var defaultAttr = field.GetCustomAttributes<DefaultValueAttribute>().FirstOrDefault();
                var defaultValue = defaultAttr == null ? null : defaultAttr.DefaultValue;

                if (field.IsDefined<IsMandatoryAttribute>())
                    missingMandatories.Add(field);

                if (field.FieldType.IsEnum && positional)
                {
                    positionals.Add(new positionalParameterInfo
                    {
                        ProcessParameter = () =>
                        {
                            positionals.RemoveAt(0);
                            foreach (var e in field.FieldType.GetFields(BindingFlags.Static | BindingFlags.Public))
                            {
                                foreach (var cmd in e.GetCustomAttributes<CommandNameAttribute>().Where(cmdAttr => cmdAttr.Name.Equals(args[i])))
                                {
                                    field.SetValue(ret, e.GetValue(null));
                                    i++;
                                    return;
                                }
                            }
                            throw new UnrecognizedCommandOrOptionException(args[i], getHelpGenerator(type));
                        },
                        ProcessEndOfParameters = () => { throw new MissingParameterException(field.Name, getHelpGenerator(type)); }
                    });
                }
                else if (field.FieldType.IsEnum)   // not positional
                {
                    foreach (var e in field.FieldType.GetFields(BindingFlags.Static | BindingFlags.Public).Where(fld => !fld.GetValue(null).Equals(defaultValue)))
                    {
                        foreach (var a in e.GetCustomAttributes<OptionAttribute>())
                        {
                            options[a.Name] = () =>
                            {
                                field.SetValue(ret, e.GetValue(null));
                                i++;
                                missingMandatories.Remove(field);
                                foreach (var e2 in field.FieldType.GetFields(BindingFlags.Static | BindingFlags.Public).Where(fld => !fld.GetValue(null).Equals(defaultValue)))
                                    foreach (var a2 in e2.GetCustomAttributes<OptionAttribute>())
                                        options[a2.Name] = () => { throw new IncompatibleCommandOrOptionException(a.Name, a2.Name, getHelpGenerator(type)); };
                                options[a.Name] = () => { i++; };
                            };
                        }
                    }
                }
                else if (field.FieldType == typeof(bool))
                {
                    foreach (var a in field.GetCustomAttributes<OptionAttribute>())
                        options[a.Name] = () => { field.SetValue(ret, true); i++; missingMandatories.Remove(field); };
                }
                else if (field.FieldType == typeof(string) || ExactConvert.IsTrueIntegerType(field.FieldType) || ExactConvert.IsTrueIntegerNullableType(field.FieldType))
                {
                    if (positional)
                    {
                        positionals.Add(new positionalParameterInfo
                        {
                            ProcessParameter = () =>
                            {
                                // The following code is also duplicated below
                                if (ExactConvert.IsTrueIntegerType(field.FieldType))
                                {
                                    object res;
                                    if (!ExactConvert.Try(field.FieldType, args[i], out res))
                                        throw new InvalidIntegerParameterException(field.Name, getHelpGenerator(type));
                                    field.SetValue(ret, res);
                                }
                                else if (ExactConvert.IsTrueIntegerNullableType(field.FieldType))
                                {
                                    object res;
                                    if (!ExactConvert.Try(field.FieldType.GetGenericArguments()[0], args[i], out res))
                                        throw new InvalidIntegerParameterException(field.Name, getHelpGenerator(type));
                                    field.SetValue(ret, res);
                                }
                                else
                                    field.SetValue(ret, args[i]);

                                positionals.RemoveAt(0);
                                i++;
                            },
                            ProcessEndOfParameters = () => { throw new MissingParameterException(field.Name, getHelpGenerator(type)); }
                        });
                    }
                    else
                    {
                        foreach (var eForeach in field.GetCustomAttributes<OptionAttribute>())
                        {
                            var e = eForeach;
                            options[e.Name] = () =>
                            {
                                i++;
                                if (i >= args.Length)
                                    throw new IncompleteOptionException(e.Name, getHelpGenerator(type));
                                // The following code is also duplicated above
                                if (ExactConvert.IsTrueIntegerType(field.FieldType))
                                {
                                    object res;
                                    if (!ExactConvert.Try(field.FieldType, args[i], out res))
                                        throw new InvalidIntegerParameterException(field.Name, getHelpGenerator(type));
                                    field.SetValue(ret, res);
                                }
                                else if (ExactConvert.IsTrueIntegerNullableType(field.FieldType))
                                {
                                    object res;
                                    if (!ExactConvert.Try(field.FieldType.GetGenericArguments()[0], args[i], out res))
                                        throw new InvalidIntegerParameterException(field.Name, getHelpGenerator(type));
                                    field.SetValue(ret, res);
                                }
                                else
                                    field.SetValue(ret, args[i]);

                                i++;
                                missingMandatories.Remove(field);
                            };
                        }
                    }
                }
                else if (field.FieldType == typeof(string[]))
                {
                    if (positional)
                    {
                        positionals.Add(new positionalParameterInfo
                        {
                            ProcessParameter = () =>
                            {
                                var prev = (string[]) field.GetValue(ret);
                                if (prev == null || prev.Length == 0)
                                    field.SetValue(ret, new string[] { args[i] });
                                else
                                    field.SetValue(ret, prev.Concat(args[i]).ToArray());
                                i++;
                            },
                            ProcessEndOfParameters = () =>
                            {
                                if (field.GetValue(ret) == null)
                                    field.SetValue(ret, new string[] { });
                            }
                        });
                    }
                    else
                    {
                        field.SetValue(ret, new string[] { });
                        foreach (var eForeach in field.GetCustomAttributes<OptionAttribute>())
                        {
                            var e = eForeach;
                            options[e.Name] = () =>
                            {
                                i++;
                                if (i >= args.Length)
                                    throw new IncompleteOptionException(e.Name, getHelpGenerator(type));
                                var prev = (string[]) field.GetValue(ret);
                                if (prev == null || prev.Length == 0)
                                    field.SetValue(ret, new string[] { args[i] });
                                else
                                    field.SetValue(ret, prev.Concat(args[i]).ToArray());
                                i++;
                                missingMandatories.Remove(field);
                            };
                        }
                    }
                }
                else if (field.FieldType.IsClass && field.FieldType.IsDefined<CommandGroupAttribute>())
                {
                    swallowingField = field;
                    positionals.Add(new positionalParameterInfo
                    {
                        ProcessParameter = () =>
                        {
                            positionals.RemoveAt(0);
                            foreach (var subclass in field.FieldType.Assembly.GetTypes().Where(t => t.IsSubclassOf(field.FieldType)))
                                foreach (var cmdName in subclass.GetCustomAttributes<CommandNameAttribute>().Where(c => c.Name.Equals(args[i])))
                                {
                                    field.SetValue(ret, parseCommandLine(args, subclass, i + 1));
                                    i = args.Length;
                                    return;
                                }
                            throw new UnrecognizedCommandOrOptionException(args[i], getHelpGenerator(type));
                        },
                        ProcessEndOfParameters = () => { throw new MissingParameterException(field.Name, getHelpGenerator(type)); }
                    });
                }
                else    // This only happens if the post-build check didn't run
                    throw new UnrecognizedTypeException(type.FullName, field.Name, getHelpGenerator(type));
            }

            bool suppressOptions = false;

            while (i < args.Length)
            {
                if (args[i] == "--")
                {
                    suppressOptions = true;
                    i++;
                }
                else if (!suppressOptions && args[i][0] == '-')
                {
                    if (options.ContainsKey(args[i]))
                        options[args[i]]();
                    else
                        throw new UnrecognizedCommandOrOptionException(args[i], getHelpGenerator(type));
                }
                else
                {
                    if (positionals.Count == 0)
                        throw new UnexpectedParameterException(args.Subarray(i), getHelpGenerator(type));
                    positionals[0].ProcessParameter();
                }
            }

            if (positionals.Count > 0)
                positionals[0].ProcessEndOfParameters();

            if (missingMandatories.Count > 0)
                throw new MissingOptionException(missingMandatories[0], swallowingField, getHelpGenerator(type));

            Type[] typeParam;
            string error = null;
            if (type.TryGetInterfaceGenericParameters(typeof(ICommandLineValidatable<>), out typeParam))
            {
                var tp = typeof(ICommandLineValidatable<>).MakeGenericType(typeParam[0]);
                if (typeParam[0] != ApplicationTr.GetType())
                    throw new CommandLineValidationException(@"The type {0} implements {1}, but ApplicationTr is of type {2}. If ApplicationTr is right, the interface implemented should be {3}.".Fmt(
                        type.FullName,
                        tp.FullName,
                        ApplicationTr.GetType().FullName,
                        typeof(ICommandLineValidatable<>).MakeGenericType(ApplicationTr.GetType()).FullName
                    ), getHelpGenerator(type));

                var meth = tp.GetMethod("Validate");
                if (meth == null || !meth.GetParameters().Select(p => p.ParameterType).SequenceEqual(new Type[] { typeParam[0] }))
                    throw new CommandLineValidationException(@"Couldn't find the Validate method in the {0} type.".Fmt(tp.FullName), getHelpGenerator(type));

                error = (string) meth.Invoke(ret, new object[] { ApplicationTr });
            }
            else if (typeof(ICommandLineValidatable).IsAssignableFrom(type))
                error = ((ICommandLineValidatable) ret).Validate();

            if (error != null)
                throw new CommandLineValidationException(error, getHelpGenerator(type));

            return ret;
        }

        private Func<Translation, int, ConsoleColoredString> getHelpGenerator(Type type)
        {
            return (tr, wrapWidth) =>
            {
                if (tr == null)
                    tr = new Translation();

                int leftMargin = 3;

                var help = new List<ConsoleColoredString>();
                string commandName = type.GetCustomAttributes<CommandNameAttribute>().Select(c => c.Name).OrderByDescending(c => c.Length).FirstOrDefault();
                commandName = commandName == null ? Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location) : "... " + commandName;

                help.Add(new ConsoleColoredString(tr.Usage + " ", ConsoleColor.Green));
                help.Add(commandName);

                FieldInfo[] optional, required;
                getOptionalRequiredFieldsForHelp(type, out optional, out required);

                //
                // ##  Go through all the OPTIONAL parameters, listing them in the "Usage" line
                // ##  (we'll construct the table explaining them later, after the required options)
                //
                foreach (var f in optional)
                {
                    IEnumerable<OptionAttribute> attrsRaw;
                    if (f.FieldType.IsEnum)
                    {
                        var defAttr = f.GetCustomAttributes<DefaultValueAttribute>().FirstOrDefault();
                        if (defAttr == null)
                            continue;
                        attrsRaw = f.FieldType.GetFields(BindingFlags.Public | BindingFlags.Static).Where(fi => !fi.GetValue(null).Equals(defAttr.DefaultValue) && !fi.IsDefined<UndocumentedAttribute>()).SelectMany(fi => fi.GetCustomAttributes<OptionAttribute>());
                    }
                    else
                        attrsRaw = f.GetCustomAttributes<OptionAttribute>();

                    help.Add(new ConsoleColoredString(" [", ConsoleColor.DarkGray));
                    var attrs = attrsRaw.Any(a => !a.Name.StartsWith("--")) ? attrsRaw.Where(a => !a.Name.StartsWith("--")) : attrsRaw;
                    var c = new ConsoleColoredString(attrs.First().Name, ConsoleColor.Cyan);
                    foreach (var attr in attrs.Skip(1))
                    {
                        c = c + new ConsoleColoredString("|", ConsoleColor.DarkGray);
                        c = c + new ConsoleColoredString(attr.Name, ConsoleColor.Cyan);
                    }
                    if ((f.FieldType == typeof(string) || f.FieldType == typeof(string[]) ||
                        (ExactConvert.IsTrueIntegerType(f.FieldType) && !f.FieldType.IsEnum) ||
                        (ExactConvert.IsTrueIntegerNullableType(f.FieldType) && !f.FieldType.GetGenericArguments()[0].IsEnum)))
                        c = c + new ConsoleColoredString(" <" + f.Name + ">", ConsoleColor.Cyan);
                    help.Add(c);
                    if (f.FieldType.IsArray)
                    {
                        help.Add(new ConsoleColoredString(" [", ConsoleColor.DarkGray));
                        help.Add(c);
                        help.Add(new ConsoleColoredString(" [...]]", ConsoleColor.DarkGray));
                    }
                    help.Add(new ConsoleColoredString("]", ConsoleColor.DarkGray));
                }

                var anyCommandsWithSuboptions = false;
                var requiredParamsTable = new TextTable { MaxWidth = wrapWidth - leftMargin, ColumnSpacing = 3, RowSpacing = 1, LeftMargin = leftMargin };
                int row = 0;

                //
                // ##  Go through all the REQUIRED parameters, listing them in the "Usage" line as well as constructing the table explaining them
                //
                foreach (var f in required.OrderBy(f => f.IsDefined<IsPositionalAttribute>()))
                {
                    if (!f.IsDefined<IsPositionalAttribute>())
                    {
                        var opts = f.GetCustomAttributes<OptionAttribute>().Select(opt => opt.Name).ToArray();
                        if (opts.Length > 1)
                        {
                            help.Add(new ConsoleColoredString(" {", ConsoleColor.DarkGray));
                            help.Add(opts[0]);
                            foreach (var opt in opts.Skip(1))
                            {
                                help.Add(new ConsoleColoredString("|", ConsoleColor.DarkGray));
                                help.Add(opt);
                            }
                            help.Add(new ConsoleColoredString("}", ConsoleColor.DarkGray));
                        }
                        else
                            help.Add(" " + opts[0]);
                    }

                    help.Add(" ");
                    var cmdName = new ConsoleColoredString("<" + f.Name + ">", ConsoleColor.Cyan);
                    help.Add(cmdName);

                    if (f.FieldType.IsEnum)
                    {
                        var positional = f.IsDefined<IsPositionalAttribute>();
                        requiredParamsTable.SetCell(0, row, cmdName, noWrap: true);
                        foreach (var el in f.FieldType.GetFields(BindingFlags.Static | BindingFlags.Public))
                        {
                            var str = positional
                                ? el.GetCustomAttributes<CommandNameAttribute>().Select(o => o.Name).OrderBy(c => c.Length).JoinString("\n")
                                : el.GetCustomAttributes<OptionAttribute>().Select(o => o.Name).OrderBy(c => c.Length).JoinString("\n");
                            requiredParamsTable.SetCell(1, row, new ConsoleColoredString(str, ConsoleColor.White), noWrap: true);
                            requiredParamsTable.SetCell(2, row, getDocumentation(el, type), colSpan: 2);
                            row++;
                        }
                    }
                    else if (f.FieldType.IsDefined<CommandGroupAttribute>())
                    {
                        help.Add(" ...");
                        int origRow = row;
                        foreach (var ty in f.FieldType.Assembly.GetTypes().Where(t => t.IsSubclassOf(f.FieldType) && t.IsDefined<CommandNameAttribute>() && !t.IsAbstract && !t.IsDefined<UndocumentedAttribute>())
                            .OrderBy(t => t.GetCustomAttributes<CommandNameAttribute>().MinElement(c => c.Name.Length).Name))
                        {
                            FieldInfo[] subOptional, subRequired;
                            getOptionalRequiredFieldsForHelp(ty, out subOptional, out subRequired);
                            var cell1 = ConsoleColoredString.Empty;
                            var cell2 = ConsoleColoredString.Empty;
                            var suboptions = subOptional.Any() || subRequired.Any();
                            anyCommandsWithSuboptions |= suboptions;
                            var asterisk = suboptions ? new ConsoleColoredString("*", ConsoleColor.DarkYellow) + ConsoleColoredString.NewLine : ConsoleColoredString.NewLine;
                            foreach (var cn in ty.GetCustomAttributes<CommandNameAttribute>().OrderBy(c => c.Name).Select(c => new ConsoleColoredString(c.Name, ConsoleColor.White)))
                                if (cn.Length > 2) cell2 += cn + asterisk; else cell1 += cn + asterisk;

                            requiredParamsTable.SetCell(1, row, cell1.Length == 0 ? cell1 : cell1.Substring(0, cell1.Length - ConsoleColoredString.NewLine.Length), noWrap: true);
                            requiredParamsTable.SetCell(2, row, cell2.Length == 0 ? cell2 : cell2.Substring(0, cell2.Length - ConsoleColoredString.NewLine.Length), noWrap: true);
                            requiredParamsTable.SetCell(3, row, getDocumentation(ty, ty));
                            row++;
                        }
                        requiredParamsTable.SetCell(0, origRow, new ConsoleColoredString("<" + f.Name + ">", ConsoleColor.Cyan), rowSpan: row - origRow, noWrap: true);
                    }
                    else
                    {
                        requiredParamsTable.SetCell(0, row, new ConsoleColoredString("<" + f.Name + ">", ConsoleColor.Cyan), noWrap: true);
                        requiredParamsTable.SetCell(1, row, getDocumentation(f, type), colSpan: 3);
                        row++;
                    }
                }

                var optionalParamsTable = new TextTable { MaxWidth = wrapWidth - leftMargin, ColumnSpacing = 3, RowSpacing = 1, LeftMargin = leftMargin };
                row = 0;

                //
                // ##  Go through all the OPTIONAL parameters, constructing the table explaining them
                // ##  (we've already listed them in the Usage line because they have to come before the required parameters there)
                //
                foreach (var f in optional)
                {
                    // Add to the table with documentation
                    if (f.FieldType.IsEnum)
                    {
                        foreach (var el in f.FieldType.GetFields(BindingFlags.Static | BindingFlags.Public).Where(e => !e.GetValue(null).Equals(f.GetCustomAttributes<DefaultValueAttribute>().First().DefaultValue) && !e.IsDefined<UndocumentedAttribute>()))
                        {
                            optionalParamsTable.SetCell(0, row, new ConsoleColoredString(el.GetCustomAttributes<OptionAttribute>().Where(o => !o.Name.StartsWith("--")).Select(o => o.Name).OrderBy(cmd => cmd.Length).JoinString(", "), ConsoleColor.White), noWrap: true);
                            optionalParamsTable.SetCell(1, row, new ConsoleColoredString(el.GetCustomAttributes<OptionAttribute>().Where(o => o.Name.StartsWith("--")).Select(o => o.Name).OrderBy(cmd => cmd.Length).JoinString(", "), ConsoleColor.White), noWrap: true);
                            optionalParamsTable.SetCell(2, row, getDocumentation(el, type));
                            row++;
                        }
                    }
                    else
                    {
                        optionalParamsTable.SetCell(0, row, new ConsoleColoredString(f.GetCustomAttributes<OptionAttribute>().Where(o => !o.Name.StartsWith("--")).Select(o => o.Name).OrderBy(cmd => cmd.Length).JoinString(", "), ConsoleColor.White), noWrap: true);
                        optionalParamsTable.SetCell(1, row, new ConsoleColoredString(f.GetCustomAttributes<OptionAttribute>().Where(o => o.Name.StartsWith("--")).Select(o => o.Name).OrderBy(cmd => cmd.Length).JoinString(", "), ConsoleColor.White), noWrap: true);
                        optionalParamsTable.SetCell(2, row, getDocumentation(f, type));
                        row++;
                    }
                }

                var helpString = new List<ConsoleColoredString>();

                // Word-wrap the usage line
                foreach (var line in new ConsoleColoredString(help.ToArray()).WordWrap(wrapWidth, tr.Usage.Translation.Length + 1))
                {
                    helpString.Add(line);
                    helpString.Add(ConsoleColoredString.NewLine);
                }

                // Word-wrap the documentation for the command (if any)
                var doc = getDocumentation(type, type);
                if (doc != null)
                {
                    helpString.Add(ConsoleColoredString.NewLine);
                    foreach (var line in ConsoleColoredString.FromEggsNodeWordWrap(doc, wrapWidth))
                    {
                        helpString.Add(line);
                        helpString.Add(ConsoleColoredString.NewLine);
                    }
                }

                // Table of required parameters
                if (required.Any())
                {
                    helpString.Add(ConsoleColoredString.NewLine);
                    helpString.Add(new ConsoleColoredString(tr.ParametersHeader, ConsoleColor.White));
                    helpString.Add(ConsoleColoredString.NewLine);
                    helpString.Add(ConsoleColoredString.NewLine);
                    requiredParamsTable.RemoveEmptyColumns();
                    helpString.Add(requiredParamsTable.ToColoredString());
                }

                // Table of optional parameters
                if (optional.Any())
                {
                    helpString.Add(ConsoleColoredString.NewLine);
                    helpString.Add(new ConsoleColoredString(tr.OptionsHeader, ConsoleColor.White));
                    helpString.Add(ConsoleColoredString.NewLine);
                    helpString.Add(ConsoleColoredString.NewLine);
                    optionalParamsTable.RemoveEmptyColumns();
                    helpString.Add(optionalParamsTable.ToColoredString());
                }

                // "This command accepts further arguments on the command line."
                if (anyCommandsWithSuboptions)
                {
                    helpString.Add(ConsoleColoredString.NewLine);
                    foreach (var line in (new ConsoleColoredString("* ", ConsoleColor.DarkYellow) + tr.AdditionalOptions.Translation).WordWrap(wrapWidth, 2))
                    {
                        helpString.Add(line);
                        helpString.Add(ConsoleColoredString.NewLine);
                    }
                }

                return new ConsoleColoredString(helpString.ToArray());
            };
        }

        private void getOptionalRequiredFieldsForHelp(Type type, out FieldInfo[] optional, out FieldInfo[] required)
        {
            optional = type.GetAllFields().Where(f => !f.IsDefined<IsPositionalAttribute>() && !f.IsDefined<IsMandatoryAttribute>() && !f.IsDefined<UndocumentedAttribute>()).ToArray();
            required = type.GetAllFields().Where(f => f.IsDefined<IsPositionalAttribute>() || f.IsDefined<IsMandatoryAttribute>()).ToArray();
        }

        private EggsNode getDocumentation(MemberInfo member, Type inType)
        {
            if (member.IsDefined<DocumentationLiteralAttribute>())
                return member.GetCustomAttributes<DocumentationLiteralAttribute>().Select(d => EggsML.Parse(d.Text)).First();
            if (ApplicationTr == null)
                return null;

            if (!(member is Type) && inType.IsSubclassOf(member.DeclaringType))
                inType = member.DeclaringType;
            var meth = inType.GetMethod(member.Name + "Doc", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { ApplicationTr.GetType() }, null);
            if (meth == null || meth.ReturnType != typeof(string))
                return null;
            var str = (string) meth.Invoke(null, new object[] { ApplicationTr });
            return str == null ? null : EggsML.Parse(str);
        }

        #region Post-build step check

#if DEBUG
        /// <summary>Performs safety checks to ensure that the structure of your command-line syntax defining class is valid according to the criteria laid out in the documentation of <see cref="CommandLineParser&lt;T&gt;"/>.
        /// Run this method as a post-build step to ensure reliability of execution. For an example of use, see <see cref="Ut.RunPostBuildChecks"/>. This method is available only in DEBUG mode.</summary>
        /// <param name="rep">Object to report post-build errors to.</param>
        /// <param name="applicationTrType">The type of the translation object, derived from <see cref="TranslationBase"/>, which would be assigned to <see cref="ApplicationTr"/> at normal run-time.</param>
        /// <remarks>If this method runs successfully, it guarantees that <see cref="UnrecognizedTypeException"/> does not occur at run-time.</remarks>
        public static void PostBuildStep(IPostBuildReporter rep, Type applicationTrType)
        {
            postBuildStep(rep, typeof(T), applicationTrType, false);
        }

        private static void postBuildStep(IPostBuildReporter rep, Type commandLineType, Type applicationTrType, bool checkClassDoc)
        {
            if (!commandLineType.IsClass)
                rep.Error(@"{0} is not a class.".Fmt(commandLineType.FullName), (commandLineType.IsEnum ? "enum " : commandLineType.IsInterface ? "interface " : typeof(Delegate).IsAssignableFrom(commandLineType) ? "delegate " : "struct ") + commandLineType.Name);

            if (commandLineType.GetConstructor(Type.EmptyTypes) == null)
                rep.Error(@"{0} does not have a default constructor.".Fmt(commandLineType.FullName), "class " + commandLineType.Name);

            if (applicationTrType != null)
            {
                Type[] typeParam;
                if (commandLineType.TryGetInterfaceGenericParameters(typeof(ICommandLineValidatable<>), out typeParam) && typeParam[0] != applicationTrType)
                    rep.Error(@"The type {0} implements {1}, but the ApplicationTrType is {2}. If ApplicationTr is right, the interface implemented should be {3}.".Fmt(
                        commandLineType.FullName,
                        typeof(ICommandLineValidatable<>).MakeGenericType(typeParam[0]).FullName,
                        applicationTrType.FullName,
                        typeof(ICommandLineValidatable<>).MakeGenericType(applicationTrType).FullName
                    ), "class " + commandLineType.Name);
            }

            var optionTaken = new Dictionary<string, MemberInfo>();
            var sensibleDocMethods = new List<MethodInfo>();
            FieldInfo lastField = null;

            if (checkClassDoc)
                checkDocumentation(rep, commandLineType, commandLineType, applicationTrType, sensibleDocMethods);

            foreach (var field in commandLineType.GetFields())
            {
                if (lastField != null)
                    rep.Error(@"The type of {0}.{1} necessitates that it is the last one in the class.".Fmt(lastField.DeclaringType.FullName, lastField.Name), "class " + commandLineType.Name, field.Name);
                var positional = field.IsDefined<IsPositionalAttribute>();

                if ((positional || field.IsDefined<IsMandatoryAttribute>()) && field.IsDefined<UndocumentedAttribute>())
                    rep.Error(@"{0}.{1}: Fields cannot simultaneously be mandatory/positional and also undocumented.".Fmt(field.DeclaringType.FullName, field.Name), "class " + commandLineType.Name, field.Name);

                // (1) if it's a field of type enum:
                if (field.FieldType.IsEnum)
                {
                    var defaultAttr = field.GetCustomAttributes<DefaultValueAttribute>().FirstOrDefault();
                    var commandsTaken = new Dictionary<string, FieldInfo>();

                    // check that it is either positional OR has a DefaultAttribute, but not both
                    if (positional && defaultAttr != null)
                        rep.Error(@"{0}.{1}: Fields of an enum type cannot both be positional and have a default value.".Fmt(commandLineType.FullName, field.Name), "class " + commandLineType.Name, field.Name);
                    if (!positional && defaultAttr == null)
                        rep.Error(@"{0}.{1}: Fields of an enum type must be either positional or have a default value.".Fmt(commandLineType.FullName, field.Name), "class " + commandLineType.Name, field.Name);

                    // check that it doesn't have an [Option] or [CommandName] attribute, because the enum values are supposed to have that instead
                    if (field.GetCustomAttributes<OptionAttribute>().Any() || field.GetCustomAttributes<CommandNameAttribute>().Any())
                        rep.Error(@"{0}.{1}: Fields of an enum type cannot have [Option] or [CommandName] attributes; these attributes should go on the enum values in the enum type instead.".Fmt(commandLineType.FullName, field.Name), "class " + commandLineType.Name, field.Name);

                    foreach (var enumField in field.FieldType.GetFields(BindingFlags.Static | BindingFlags.Public))
                    {
                        if (!positional && enumField.GetValue(null).Equals(defaultAttr.DefaultValue))
                            continue;

                        // check that the enum values all have documentation
                        checkDocumentation(rep, enumField, commandLineType, applicationTrType, sensibleDocMethods);

                        if (positional)
                        {
                            // check that the enum values all have at least one CommandName, and they do not clash
                            var cmdNames = enumField.GetCustomAttributes<CommandNameAttribute>();
                            if (!cmdNames.Any())
                                rep.Error(@"{0}.{1} (used by {2}.{3}): Enum value does not have a [CommandName] attribute.".Fmt(field.FieldType.FullName, enumField.Name, commandLineType.FullName, field.Name), "enum " + field.FieldType.Name, enumField.Name);
                            checkCommandNamesUnique(rep, cmdNames, commandsTaken, commandLineType, field, enumField);
                        }
                        else
                        {
                            // check that the non-default enum values' Options are present and do not clash
                            var options = enumField.GetCustomAttributes<OptionAttribute>();
                            if (!options.Any())
                                rep.Error(@"{0}.{1} (used by {2}.{3}): Enum value must have at least one [Option] attribute.".Fmt(field.FieldType.FullName, enumField.Name, commandLineType.FullName, field.Name), "enum " + field.FieldType.Name, enumField.Name);
                            checkOptionsUnique(rep, options, optionTaken, commandLineType, field, enumField);
                        }
                    }
                }
                else if (field.FieldType == typeof(bool))
                {
                    if (positional)
                        rep.Error(@"{0}.{1}: Fields of type bool cannot be positional.".Fmt(commandLineType.FullName, field.Name), "class " + commandLineType.Name, field.Name);

                    var options = field.GetCustomAttributes<OptionAttribute>();
                    if (!options.Any())
                        rep.Error(@"{0}.{1}: Boolean field must have at least one [Option] attribute.".Fmt(commandLineType.FullName, field.Name), "class " + commandLineType.Name, field.Name);

                    checkOptionsUnique(rep, options, optionTaken, commandLineType, field);
                    checkDocumentation(rep, field, commandLineType, applicationTrType, sensibleDocMethods);
                }
                else if (field.FieldType == typeof(string) || field.FieldType == typeof(string[]) ||
                    (ExactConvert.IsTrueIntegerType(field.FieldType) && !field.FieldType.IsEnum) ||
                    (ExactConvert.IsTrueIntegerNullableType(field.FieldType) && !field.FieldType.GetGenericArguments()[0].IsEnum))
                {
                    var options = field.GetCustomAttributes<OptionAttribute>();
                    if (!options.Any() && !positional)
                        rep.Error(@"{0}.{1}: Field of type string, string[] or an integer type must have either [IsPositional] or at least one [Option] attribute.".Fmt(commandLineType.FullName, field.Name), "class " + commandLineType.Name, field.Name);

                    checkOptionsUnique(rep, options, optionTaken, commandLineType, field);
                    checkDocumentation(rep, field, commandLineType, applicationTrType, sensibleDocMethods);
                }
                else if (field.FieldType.IsClass && field.FieldType.IsDefined<CommandGroupAttribute>())
                {
                    // Class-type fields must be positional parameters
                    if (!positional)
                        rep.Error(@"{0}.{1}: CommandGroup fields must be declared positional.".Fmt(commandLineType.FullName, field.Name), "class " + commandLineType.Name, field.Name);

                    // The class must have at least two subclasses with a [CommandName] attribute
                    var subclasses = field.FieldType.Assembly.GetTypes().Where(t => !t.IsAbstract && t.IsSubclassOf(field.FieldType) && t.IsDefined<CommandNameAttribute>());
                    if (subclasses.Count() < 2)
                        rep.Error(@"{0}.{1}: The CommandGroup class type must have at least two non-abstract subclasses with the [CommandName] attribute.".Fmt(commandLineType.FullName, field.Name), "class " + field.FieldType.Name);

                    var commandsTaken = new Dictionary<string, Type>();

                    foreach (var subclass in subclasses)
                    {
                        checkCommandNamesUnique(rep, subclass.GetCustomAttributes<CommandNameAttribute>(), commandsTaken, subclass);

                        // Recursively check this class
                        postBuildStep(rep, subclass, applicationTrType, true);
                    }

                    lastField = field;
                }
                else
                    rep.Error(@"{0}.{1} is not of a recognised type. Currently accepted types are: enum types, bool, string, integer types (sbyte, short, int, long and unsigned variants), nullable integer types, and classes with the [CommandGroup] attribute.".Fmt(commandLineType.FullName, field.Name), "class " + commandLineType.Name, field.Name);
            }

            // Warn if the method has unused documentation methods
            foreach (var meth in commandLineType.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Where(m => m.Name.EndsWith("Doc") && m.ReturnType == typeof(string) && m.GetParameters().Select(p => p.ParameterType).SequenceEqual(new Type[] { applicationTrType })))
                if (!sensibleDocMethods.Contains(meth))
                    rep.Error(@"{0}.{1} looks like a documentation method, but has no corresponding field.".Fmt(commandLineType.FullName, meth.Name), "class " + commandLineType.Name, meth.Name);
        }

        private static void checkOptionsUnique(IPostBuildReporter rep, IEnumerable<OptionAttribute> options, Dictionary<string, MemberInfo> optionTaken, Type type, FieldInfo field, FieldInfo enumField)
        {
            foreach (var option in options)
            {
                if (optionTaken.ContainsKey(option.Name))
                {
                    rep.Error(@"{0}.{1}: Option ""{2}"" is used more than once.".Fmt(field.FieldType.FullName, enumField.Name, option.Name), "enum " + field.FieldType.Name, enumField.Name);
                    rep.Error(@" -- It is used by {0}.{1}...".Fmt(type.FullName, field.Name), "class " + type.Name, field.Name);
                    rep.Error(@" -- ... and by {0}.{1}.".Fmt(optionTaken[option.Name].DeclaringType.FullName, optionTaken[option.Name].Name), "class " + optionTaken[option.Name].DeclaringType.Name, optionTaken[option.Name].Name);
                }
                optionTaken[option.Name] = enumField;
            }
        }

        private static void checkOptionsUnique(IPostBuildReporter rep, IEnumerable<OptionAttribute> options, Dictionary<string, MemberInfo> optionTaken, Type type, FieldInfo field)
        {
            foreach (var option in options)
            {
                if (optionTaken.ContainsKey(option.Name))
                {
                    rep.Error(@"Option ""{2}"" is used by {0}.{1}...".Fmt(type.FullName, field.Name, option.Name), "class " + type.Name, field.Name);
                    rep.Error(@" -- ... and by {0}.{1}.".Fmt(optionTaken[option.Name].DeclaringType.FullName, optionTaken[option.Name].Name), "class " + optionTaken[option.Name].DeclaringType.Name, optionTaken[option.Name].Name);
                }
                optionTaken[option.Name] = field;
            }
        }

        private static void checkCommandNamesUnique(IPostBuildReporter rep, IEnumerable<CommandNameAttribute> commands, Dictionary<string, Type> commandsTaken, Type subclass)
        {
            foreach (var cmd in commands)
            {
                if (commandsTaken.ContainsKey(cmd.Name))
                {
                    rep.Error(@"CommandName ""{1}"" is used by {0}...".Fmt(subclass.FullName, cmd.Name), "class " + subclass.Name);
                    rep.Error(@" -- ... and by {0}.".Fmt(commandsTaken[cmd.Name].FullName), "class " + commandsTaken[cmd.Name].Name);
                }
                commandsTaken[cmd.Name] = subclass;
            }
        }

        private static void checkCommandNamesUnique(IPostBuildReporter rep, IEnumerable<CommandNameAttribute> cmdNames, Dictionary<string, FieldInfo> commandsTaken, Type type, FieldInfo field, FieldInfo enumField)
        {
            foreach (var cmd in cmdNames)
            {
                if (commandsTaken.ContainsKey(cmd.Name))
                {
                    rep.Error(@"{0}.{1}: Option ""{2}"" is used more than once.".Fmt(field.FieldType.FullName, enumField.Name, cmd.Name), "enum " + field.FieldType.Name, enumField.Name);
                    rep.Error(@" -- It is used by {0}.{1}...".Fmt(type.FullName, field.Name), "class " + type.Name, field.Name);
                    rep.Error(@" -- ... and by {0}.{1}.".Fmt(commandsTaken[cmd.Name].DeclaringType.FullName, commandsTaken[cmd.Name].Name), "class " + commandsTaken[cmd.Name].DeclaringType.Name, commandsTaken[cmd.Name].Name);
                }
                commandsTaken[cmd.Name] = enumField;
            }
        }

        private static Dictionary<Type, object> _applicationTrCacheField = null;
        private static Dictionary<Type, object> _applicationTrCache
        {
            get
            {
                if (_applicationTrCacheField == null)
                    _applicationTrCacheField = new Dictionary<Type, object>();
                return _applicationTrCacheField;
            }
        }

        private static void checkDocumentation(IPostBuildReporter rep, MemberInfo member, Type inType, Type applicationTrType, List<MethodInfo> sensibleDocMethods)
        {
            if (member.IsDefined<UndocumentedAttribute>())
                return;

            if (!(member is Type) && inType.IsSubclassOf(member.DeclaringType))
                inType = member.DeclaringType;

            string toCheck = null;
            if (member.IsDefined<DocumentationLiteralAttribute>())
                toCheck = member.GetCustomAttributes<DocumentationLiteralAttribute>().First().Text;
            else if (applicationTrType != null)
            {
                var meth = inType.GetMethod(member.Name + "Doc", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { applicationTrType }, null);
                if (meth != null && meth.ReturnType == typeof(string))
                {
                    sensibleDocMethods.Add(meth);
                    if (!_applicationTrCache.ContainsKey(applicationTrType))
                        _applicationTrCache[applicationTrType] = Activator.CreateInstance(applicationTrType);
                    var appTr = _applicationTrCache[applicationTrType];
                    toCheck = (string) meth.Invoke(null, new object[] { appTr });
                    if (toCheck == null)
                    {
                        rep.Error(@"{0}." + member.Name + @"Doc() returned null.".Fmt(inType.FullName), "class " + inType.Name, member.Name + "Doc");
                        return;
                    }
                }
            }

            if (toCheck == null)
            {
                if (member is Type)
                {
                    rep.Error((@"{0} does not have any documentation. " +
                        (applicationTrType == null ? "U" : @"To provide localised documentation, declare a method ""static string {1}Doc({2})"" on {3}. Otherwise, u") +
                        @"se the [DocumentationLiteral] attribute to specify unlocalisable documentation. " +
                        @"Use [Undocumented] to completely hide an option or command from the help screen.").Fmt(((Type) member).FullName, member.Name, applicationTrType != null ? applicationTrType.FullName : null, inType.FullName),
                        "CommandName",
                        "class " + member.Name);
                }
                else
                {
                    rep.Error((@"{0}.{1} does not have any documentation. " +
                        (applicationTrType == null ? "U" : @"To provide localised documentation, declare a method ""static string {1}Doc({2})"" on {3}. Otherwise, u") +
                        @"se the [DocumentationLiteral] attribute to specify unlocalisable documentation. " +
                        @"Use [Undocumented] to completely hide an option or command from the help screen.").Fmt(member.DeclaringType.FullName, member.Name, applicationTrType != null ? applicationTrType.FullName : null, inType.FullName),
                        (member.DeclaringType.IsEnum ? "enum " : "class ") + member.DeclaringType.Name,
                        member.Name);
                }
                return;
            }

            string eggsError = null;
            try { var result = EggsML.Parse(toCheck); }
            catch (EggsMLParseException e) { eggsError = e.Message; }
            if (eggsError != null)
                rep.Error(@"{0}.{1}: Field documentation is not valid EggsML: {2}".Fmt(member.DeclaringType.FullName, member.Name, eggsError), "class " + member.DeclaringType.Name, member.Name);
        }
#endif

        #endregion
    }

    /// <summary>Contains methods to validate a set of parameters passed by the user on the command-line and parsed by <see cref="CommandLineParser&lt;T&gt;"/>.
    /// Use this class only in monolingual (unlocalisable) applications. Use <see cref="ICommandLineValidatable&lt;TTranslation&gt;"/> otherwise.</summary>
    public interface ICommandLineValidatable
    {
        /// <summary>When overridden in a derived class, returns an error message if the contents of the class are invalid, otherwise returns null.</summary>
        string Validate();
    }

    /// <summary>Contains methods to validate a set of parameters passed by the user on the command-line and parsed by <see cref="CommandLineParser&lt;T&gt;"/>.</summary>
    /// <typeparam name="TTranslation">A translation-string class containing the error messages that can occur during validation.</typeparam>
    public interface ICommandLineValidatable<TTranslation> where TTranslation : TranslationBase
    {
        /// <summary>When implemented in a class, returns an error message if the contents of the class are invalid, otherwise returns null.</summary>
        /// <param name="tr">Contains translations for the messages that may occur during validation.</param>
        string Validate(TTranslation tr);
    }

    /// <summary>Groups the translatable strings in the <see cref="Translation"/> class into categories.</summary>
    public enum TranslationGroup
    {
        /// <summary>Error messages produced by the command-line parser.</summary>
        [LingoGroup("Command-line errors", "Contains messages informing the user of invalid command-line syntax.")]
        CommandLineError,
        /// <summary>Messages used by the command-line parser to produce help pages.</summary>
        [LingoGroup("Command-line help", "Contains messages used to construct help pages for command-line options and parameters.")]
        CommandLineHelp
    }

    /// <summary>Contains translatable strings pertaining to the command-line parser, including error messages and usage help.</summary>
    public sealed class Translation : TranslationBase
    {
#pragma warning disable 1591    // Missing XML comment for publicly visible type or member
        public Translation() : base(Language.EnglishUS) { }

        [LingoInGroup(TranslationGroup.CommandLineError)]
        public TrString
            IncompatibleCommandOrOption = @"The command or option, {0}, cannot be used in conjunction with {1}. Please specify only one of the two.",
            IncompleteOption = @"The ""{0}"" option must be followed by an additional parameter.",
            InvalidInteger = @"The ""{0}"" option expects an integer. The specified parameter does not constitute a valid integer.",
            MissingOption = @"The option ""{0}"" is mandatory and must be specified.",
            MissingOptionBefore = @"The option ""{0}"" is mandatory and must be specified before the ""{1}"" parameter.",
            MissingParameter = @"The ""{0}"" parameter is missing.",
            UnexpectedParameter = @"Unexpected parameter.",
            UnrecognizedCommandOrOption = @"The specified command or option, {0}, is not recognized.",
            UnrecognizedType = @"{0}.{1} is not of a recognized type.";

        [LingoInGroup(TranslationGroup.CommandLineHelp)]
        public TrString
            AdditionalOptions = @"This command accepts further arguments on the command line. Type the command followed by -? to list them.",
            Error = @"Error:",
            OptionsHeader = @"Options:",
            ParametersHeader = @"Required parameters:",
            Usage = @"Usage:";

#pragma warning restore 1591    // Missing XML comment for publicly visible type or member
    }

    /// <summary>Use this on an abstract class to specify that its subclasses represent various commands.</summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class CommandGroupAttribute : Attribute
    {
        /// <summary>Constructor.</summary>
        public CommandGroupAttribute() { }
    }

    /// <summary>
    /// Use this on a sub-class of an abstract class to specify the command the user must use to invoke that class.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Field, Inherited = false, AllowMultiple = true)]
    public sealed class CommandNameAttribute : Attribute
    {
        /// <summary>Constructor.</summary>
        /// <param name="name">The command the user can specify to invoke this class.</param>
        public CommandNameAttribute(string name) { Name = name; }
        /// <summary>The command the user can specify to invoke this class.</summary>
        public string Name { get; private set; }
    }

    /// <summary>Use this to specify that a command-line parameter is mandatory.</summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    public sealed class IsMandatoryAttribute : Attribute
    {
        /// <summary>Constructor.</summary>
        public IsMandatoryAttribute() { }
    }

    /// <summary>
    /// Use this to specify that a command-line parameter is positional, i.e. is not invoked by an option that starts with "-".
    /// This automatically implies that the parameter is mandatory, so <see cref="IsMandatoryAttribute"/> is not necessary.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    public sealed class IsPositionalAttribute : Attribute
    {
        /// <summary>Constructor.</summary>
        public IsPositionalAttribute() { }
    }

    /// <summary>Use this on a command-line option of an enum type to specify the default value in case the option is not specified.</summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    public sealed class DefaultValueAttribute : Attribute
    {
        /// <summary>Constructor.</summary>
        /// <param name="defaultValue">The default value to be assigned to the field in case the command-line option is not specified.</param>
        public DefaultValueAttribute(object defaultValue) { DefaultValue = defaultValue; }
        /// <summary>The default value to be assigned to the field in case the command-line option is not specified.</summary>
        public object DefaultValue { get; private set; }
    }

    /// <summary>
    /// Use this to specify that a field in a class can be specified on the command line using an option, for example "-a" or "--option-name".
    /// The option name MUST begin with a "-".
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = true)]
    public sealed class OptionAttribute : Attribute
    {
        /// <summary>Constructor.</summary>
        /// <param name="name">The name of the option.</param>
        public OptionAttribute(string name) { Name = name; }
        /// <summary>The name of the option.</summary>
        public string Name { get; private set; }
    }

    /// <summary>Use this attribute in a non-internationalized (single-language) application to link a command-line option or command with the help text that describes (documents) it.</summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class DocumentationLiteralAttribute : Attribute
    {
        /// <summary>Retrieves the documentation for the corresponding member.</summary>
        public string Text { get; private set; }
        /// <summary>Constructor.</summary>
        /// <param name="text">Provides the documentation for the corresponding member.</param>
        public DocumentationLiteralAttribute(string text) { Text = text; }
    }

    /// <summary>Specifies that a specific command-line option should not be printed in help pages, i.e. the option should explicitly be undocumented.</summary>
    [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = true)]
    public sealed class UndocumentedAttribute : Attribute
    {
        /// <summary>Constructor.</summary>
        public UndocumentedAttribute() { }
    }

    /// <summary>Represents any error encountered while parsing a command line.</summary>
    [Serializable]
    public abstract class CommandLineParseException : TranslatableException<Translation>
    {
        /// <summary>Generates the help screen to be output to the user on the console. For non-internationalised (single-language) applications, pass null as the parameter.</summary>
        internal Func<Translation, int, ConsoleColoredString> GenerateHelpFunc { get; private set; }
        /// <summary>Generates the help screen to be output to the user on the console.</summary>
        /// <param name="tr">The translation class containing the translated text.</param>
        /// <param name="wrapWidth">The character width at which the output should be word-wrapped.</param>
        public ConsoleColoredString GenerateHelp(Translation tr, int wrapWidth) { return GenerateHelpFunc(tr, wrapWidth); }
        /// <summary>Generates a printable description of the error represented by this exception, typically used to tell the user what they did wrong.</summary>
        /// <param name="tr">The translation class containing the translated text.</param>
        /// <param name="wrapWidth">The character width at which the output should be word-wrapped.</param>
        public ConsoleColoredString GenerateErrorText(Translation tr, int wrapWidth)
        {
            if (tr == null)
                tr = new Translation();

            var strings = new List<ConsoleColoredString>();
            foreach (var line in (new ConsoleColoredString(tr.Error, ConsoleColor.Red) + " " + Message).WordWrap(wrapWidth, tr.Error.Translation.Length + 1))
            {
                strings.Add(line);
                strings.Add(Environment.NewLine);
            }
            return new ConsoleColoredString(strings);
        }
        /// <summary>Constructor.</summary>
        public CommandLineParseException(Func<Translation, string> getMessage, Func<Translation, int, ConsoleColoredString> helpGenerator) : this(getMessage, helpGenerator, null) { }
        /// <summary>Constructor.</summary>
        public CommandLineParseException(Func<Translation, string> getMessage, Func<Translation, int, ConsoleColoredString> helpGenerator, Exception inner) : base(getMessage, inner) { GenerateHelpFunc = helpGenerator; }

        /// <summary>
        /// Prints usage information, followed by an error message describing to the user what it was that the parser didn't
        /// understand. When the exception was caused by one of a list of common help switches, no error message is printed.
        /// </summary>
        /// <param name="tr">Contains translations for the messages used by the command-line parser. Set this to null
        /// only if your application is definitely monolingual (unlocalisable).</param>
        public void WriteUsageInfoToConsole(Translation tr = null)
        {
            if (tr == null)
                tr = new Translation();

            ConsoleUtil.Write(GenerateHelp(tr, ConsoleUtil.WrapToWidth()));

            if (!WasCausedByHelpRequest())
            {
                Console.WriteLine();
                ConsoleUtil.Write(GenerateErrorText(tr, ConsoleUtil.WrapToWidth()));
            }
        }

        /// <summary>Indicates whether this exception was caused by the user specifying an option that looks like a help switch.</summary>
        public bool WasCausedByHelpRequest()
        {
            var helps = new[] { "-?", "/?", "--?", "-h", "--help", "help" };
            return
                (this is UnrecognizedCommandOrOptionException && helps.Contains(((UnrecognizedCommandOrOptionException) this).CommandOrOptionName))
                || (this is UnexpectedParameterException && helps.Contains(((UnexpectedParameterException) this).UnexpectedParameters.FirstOrDefault()));
        }
    }

    /// <summary>Specifies that the parameters specified by the user on the command-line do not pass the custom validation checks.</summary>
    [Serializable]
    public sealed class CommandLineValidationException : CommandLineParseException
    {
        /// <summary>Constructor.</summary>
        public CommandLineValidationException(string message, Func<Translation, int, ConsoleColoredString> helpGenerator) : base(tr => message, helpGenerator) { }
    }

    /// <summary>Specifies that the command-line parser encountered a command or option that was not recognised (there was no <see cref="OptionAttribute"/> or <see cref="CommandNameAttribute"/> attribute with a matching option or command name).</summary>
    [Serializable]
    public sealed class UnrecognizedCommandOrOptionException : CommandLineParseException
    {
        /// <summary>The unrecognized command name or option name.</summary>
        public string CommandOrOptionName { get; private set; }
        /// <summary>Constructor.</summary>
        public UnrecognizedCommandOrOptionException(string commandOrOptionName, Func<Translation, int, ConsoleColoredString> helpGenerator) : this(commandOrOptionName, helpGenerator, null) { }
        /// <summary>Constructor.</summary>
        public UnrecognizedCommandOrOptionException(string commandOrOptionName, Func<Translation, int, ConsoleColoredString> helpGenerator, Exception inner)
            : base(tr => tr.UnrecognizedCommandOrOption.Fmt(commandOrOptionName), helpGenerator, inner)
        {
            CommandOrOptionName = commandOrOptionName;
        }
    }

    /// <summary>Specifies that the command-line parser encountered a command or option that is not allowed in conjunction with a previously-encountered command or option.</summary>
    [Serializable]
    public sealed class IncompatibleCommandOrOptionException : CommandLineParseException
    {
        /// <summary>The earlier option or command, which by itself is valid, but conflicts with the <see cref="LaterCommandOrOption"/>.</summary>
        public string EarlierCommandOrOption { get; private set; }
        /// <summary>The later option or command, which conflicts with the <see cref="EarlierCommandOrOption"/>.</summary>
        public string LaterCommandOrOption { get; private set; }
        /// <summary>Constructor.</summary>
        public IncompatibleCommandOrOptionException(string earlier, string later, Func<Translation, int, ConsoleColoredString> helpGenerator) : this(earlier, later, helpGenerator, null) { }
        /// <summary>Constructor.</summary>
        public IncompatibleCommandOrOptionException(string earlier, string later, Func<Translation, int, ConsoleColoredString> helpGenerator, Exception inner)
            : base(tr => tr.IncompatibleCommandOrOption.Fmt(later, earlier), helpGenerator, inner)
        {
            EarlierCommandOrOption = earlier;
            LaterCommandOrOption = later;
        }
    }

    /// <summary>Specifies that the command-line parser encountered an unsupported type in the class definition.</summary>
    [Serializable]
    public sealed class UnrecognizedTypeException : CommandLineParseException
    {
        /// <summary>The full name of the unsupported type that was encountered.</summary>
        public string TypeName { get; private set; }
        /// <summary>The name of the field that has the unsupported type.</summary>
        public string FieldName { get; private set; }
        /// <summary>Constructor.</summary>
        public UnrecognizedTypeException(string typeName, string fieldName, Func<Translation, int, ConsoleColoredString> helpGenerator) : this(typeName, fieldName, helpGenerator, null) { }
        /// <summary>Constructor.</summary>
        public UnrecognizedTypeException(string typeName, string fieldName, Func<Translation, int, ConsoleColoredString> helpGenerator, Exception inner)
            : base(tr => tr.UnrecognizedType.Fmt(typeName, fieldName), helpGenerator, inner)
        {
            TypeName = typeName;
            FieldName = fieldName;
        }
    }

    /// <summary>Specifies that the command-line parser encountered the end of the command line when it expected a parameter to an option.</summary>
    [Serializable]
    public sealed class IncompleteOptionException : CommandLineParseException
    {
        /// <summary>The name of the option that was missing a parameter.</summary>
        public string OptionName { get; private set; }
        /// <summary>Constructor.</summary>
        public IncompleteOptionException(string optionName, Func<Translation, int, ConsoleColoredString> helpGenerator) : this(optionName, helpGenerator, null) { }
        /// <summary>Constructor.</summary>
        public IncompleteOptionException(string optionName, Func<Translation, int, ConsoleColoredString> helpGenerator, Exception inner)
            : base(tr => tr.IncompleteOption.Fmt(optionName), helpGenerator, inner)
        {
            OptionName = optionName;
        }
    }

    /// <summary>Specifies that the command-line parser encountered additional command-line parameters when it expected the end of the command line.</summary>
    [Serializable]
    public sealed class UnexpectedParameterException : CommandLineParseException
    {
        /// <summary>Contains the first unexpected parameter and all of the subsequent arguments.</summary>
        public string[] UnexpectedParameters { get; private set; }
        /// <summary>Constructor.</summary>
        public UnexpectedParameterException(string[] unexpectedParams, Func<Translation, int, ConsoleColoredString> helpGenerator) : this(unexpectedParams, helpGenerator, null) { }
        /// <summary>Constructor.</summary>
        public UnexpectedParameterException(string[] unexpectedParams, Func<Translation, int, ConsoleColoredString> helpGenerator, Exception inner) : base(tr => tr.UnexpectedParameter, helpGenerator, inner) { UnexpectedParameters = unexpectedParams; }
    }

    /// <summary>Specifies that the command-line parser encountered the end of the command line when it expected additional positional parameters.</summary>
    [Serializable]
    public sealed class MissingParameterException : CommandLineParseException
    {
        /// <summary>Contains the name of the field pertaining to the parameter that was missing.</summary>
        public string FieldName { get; private set; }
        /// <summary>Constructor.</summary>
        public MissingParameterException(string fieldName, Func<Translation, int, ConsoleColoredString> helpGenerator) : this(fieldName, helpGenerator, null) { }
        /// <summary>Constructor.</summary>
        public MissingParameterException(string fieldName, Func<Translation, int, ConsoleColoredString> helpGenerator, Exception inner) : base(tr => tr.MissingParameter.Fmt(fieldName), helpGenerator, inner) { FieldName = fieldName; }
    }

    /// <summary>Specifies that a parameter that expected an integer was passed a string by the user that doesn't parse as an integer.</summary>
    [Serializable]
    public sealed class InvalidIntegerParameterException : CommandLineParseException
    {
        /// <summary>Contains the name of the field pertaining to the parameter that was missing.</summary>
        public string FieldName { get; private set; }
        /// <summary>Constructor.</summary>
        public InvalidIntegerParameterException(string fieldName, Func<Translation, int, ConsoleColoredString> helpGenerator) : this(fieldName, helpGenerator, null) { }
        /// <summary>Constructor.</summary>
        public InvalidIntegerParameterException(string fieldName, Func<Translation, int, ConsoleColoredString> helpGenerator, Exception inner) : base(tr => tr.InvalidInteger.Fmt(fieldName), helpGenerator, inner) { FieldName = fieldName; }
    }

    /// <summary>Specifies that the command-line parser encountered the end of the command line when it expected additional mandatory options.</summary>
    [Serializable]
    public sealed class MissingOptionException : CommandLineParseException
    {
        /// <summary>Contains the field pertaining to the parameter that was missing.</summary>
        public FieldInfo Field { get; private set; }
        /// <summary>Contains an optional reference to a field which the missing parameter must precede.</summary>
        public FieldInfo BeforeField { get; private set; }
        /// <summary>Constructor.</summary>
        public MissingOptionException(FieldInfo field, FieldInfo beforeField, Func<Translation, int, ConsoleColoredString> helpGenerator) : this(field, beforeField, helpGenerator, null) { }
        /// <summary>Constructor.</summary>
        public MissingOptionException(FieldInfo field, FieldInfo beforeField, Func<Translation, int, ConsoleColoredString> helpGenerator, Exception inner)
            : base(tr => getMessage(tr, field, beforeField), helpGenerator, inner) { Field = field; BeforeField = beforeField; }

        private static string getMessage(Translation tr, FieldInfo field, FieldInfo beforeField)
        {
            string fieldFormat;
            if (field.FieldType.IsEnum)
                fieldFormat = "{" + field.FieldType.GetFields().SelectMany(f => f.GetCustomAttributes<OptionAttribute>().Select(o => o.Name)).JoinString("|") + "}";
            else
            {
                var options = field.GetCustomAttributes<OptionAttribute>().Select(o => o.Name).ToArray();
                if (options.Length > 1)
                    fieldFormat = "{" + options.JoinString("|") + "}";
                else
                    fieldFormat = options[0];
                fieldFormat += " <" + field.Name + ">";
            }

            return beforeField == null ? tr.MissingOption.Fmt(fieldFormat) : tr.MissingOptionBefore.Fmt(fieldFormat, "<" + beforeField.Name + ">");
        }
    }
}
