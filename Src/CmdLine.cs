using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using RT.Util.ExtensionMethods;
using RT.Util.Lingo;
using RT.Util.Text;

namespace RT.Util.CommandLine
{
    /// <summary>Implements a command-line parser that can turn the commands and options specified by the user on the command line into a strongly-typed instance of a specific class. See remarks for more details.</summary>
    /// <remarks><para>The following conditions must be met by the class wishing to receive the options and parameters:</para>
    /// <list type="bullet">
    /// <item><description>It must be a reference type (a class) and it must have a parameterless constructor.</description></item>
    /// <item><description>Each field in the class must be a string, a bool, an enum, or another class with the <see cref="CommandGroupAttribute"/>.</description></item>
    /// <item><description>A field of an enum type can be positional (marked with the <see cref="IsPositionalAttribute"/>) or not. If it is neither positional nor mandatory (see below), it must have a <see cref="DefaultValueAttribute"/>.</description></item>
    /// <item><description>Every value of such an enum must have an <see cref="OptionAttribute"/> if the field is optional, or a <see cref="CommandNameAttribute"/> if it is positional.</description></item>
    /// <item><description>A field of type bool must have an <see cref="OptionAttribute"/> and cannot be positional.</description></item>
    /// <item><description>A field of type string can be positional or optional. If it is optional, it must have an <see cref="OptionAttribute"/>.</description></item>
    /// <item><description>A field of any other type must be the last one, must be marked positional, and must be an abstract class with a <see cref="CommandGroupAttribute"/>. This class must have at least two derived classes with a <see cref="CommandNameAttribute"/>.</description></item>
    /// <item><description>Wherever an <see cref="OptionAttribute"/> or <see cref="CommandNameAttribute"/> is required, several such attributes are allowed.</description></item>
    /// <item><description>Any field that is not positional can be made mandatory by using the <see cref="IsMandatoryAttribute"/>.</description></item>
    /// <item><description>Every field must have a <see cref="DocumentationAttribute"/> or <see cref="DocumentationLiteralAttribute"/>, except for enum-typed fields, where the enum values must have those attributes instead.</description></item>
    /// </list>
    /// </remarks>
    public static class CommandLineParser
    {
        /// <summary>Parses the specified command-line arguments into an instance of the specified type. See the remarks section of the documentation for <see cref="CommandLineParser"/> for features and limitations.</summary>
        /// <typeparam name="T">The class containing the fields and attributes which define the command-line syntax.</typeparam>
        /// <param name="args">The command-line arguments to be parsed.</param>
        /// <param name="applicationTr">The application's translation class which contains the localised strings for the <see cref="DocumentationAttribute"/>s in the command-line syntax-defining class.</param>
        /// <returns>An instance of the class <typeparamref name="T"/> containing the options and parameters specified by the user on the command line.</returns>
        public static T Parse<T>(string[] args, TranslationBase applicationTr)
        {
            if (applicationTr == null)
                throw new ArgumentNullException("applicationTr");
            return (T) parseCommandLine(args, typeof(T), 0, applicationTr);
        }

        /// <summary>Parses the specified command-line arguments into an instance of the specified type. See the remarks section of the documentation for <see cref="CommandLineParser"/> for features and limitations.</summary>
        /// <typeparam name="T">The class containing the fields and attributes which define the command-line syntax.</typeparam>
        /// <param name="args">The command-line arguments to be parsed.</param>
        /// <returns>An instance of the class <typeparamref name="T"/> containing the options and parameters specified by the user on the command line.</returns>
        public static T Parse<T>(string[] args)
        {
            return (T) parseCommandLine(args, typeof(T), 0, null);
        }

        private class positionalParameterInfo
        {
            public Action ProcessParameter;
            public Action ProcessEndOfParameters;
        }

        private static object parseCommandLine(string[] args, Type type, int i, TranslationBase applicationTr)
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
                            throw new UnrecognizedCommandOrOptionException(args[i], getHelpGenerator(type, applicationTr));
                        },
                        ProcessEndOfParameters = () => { throw new MissingParameterException(field.Name, getHelpGenerator(type, applicationTr)); }
                    });
                }
                else if (field.FieldType.IsEnum)   // not positional
                {
                    foreach (var e in field.FieldType.GetFields(BindingFlags.Static | BindingFlags.Public).Where(val => val != defaultValue))
                    {
                        foreach (var a in e.GetCustomAttributes<OptionAttribute>())
                        {
                            options[a.Name] = () =>
                            {
                                field.SetValue(ret, e.GetValue(null));
                                i++;
                                missingMandatories.Remove(field);
                                foreach (var e2 in field.FieldType.GetFields(BindingFlags.Static | BindingFlags.Public).Where(val => val != defaultValue))
                                    foreach (var a2 in e2.GetCustomAttributes<OptionAttribute>())
                                        options[a2.Name] = () => { throw new IncompatibleCommandOrOptionException(a.Name, a2.Name, getHelpGenerator(type, applicationTr)); };
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
                else if (field.FieldType == typeof(string))
                {
                    if (positional)
                    {
                        positionals.Add(new positionalParameterInfo
                        {
                            ProcessParameter = () => { positionals.RemoveAt(0); field.SetValue(ret, args[i]); i++; },
                            ProcessEndOfParameters = () => { throw new MissingParameterException(field.Name, getHelpGenerator(type, applicationTr)); }
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
                                    throw new IncompleteOptionException(e.Name, getHelpGenerator(type, applicationTr));
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
                                    throw new IncompleteOptionException(e.Name, getHelpGenerator(type, applicationTr));
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
                                    field.SetValue(ret, parseCommandLine(args, subclass, i + 1, applicationTr));
                                    i = args.Length;
                                    return;
                                }
                            throw new UnrecognizedCommandOrOptionException(args[i], getHelpGenerator(type, applicationTr));
                        },
                        ProcessEndOfParameters = () => { throw new MissingParameterException(field.Name, getHelpGenerator(type, applicationTr)); }
                    });
                }
                else    // This only happens if the post-build check didn't run
                    throw new UnrecognizedTypeException(type.FullName, field.Name, getHelpGenerator(type, applicationTr));
            }

            bool suppressOptions = false;

            while (i < args.Length)
            {
                if (args[i] == "--")
                    suppressOptions = true;
                else if (!suppressOptions && args[i][0] == '-')
                {
                    if (options.ContainsKey(args[i]))
                        options[args[i]]();
                    else
                        throw new UnrecognizedCommandOrOptionException(args[i], getHelpGenerator(type, applicationTr));
                }
                else
                {
                    if (positionals.Count == 0)
                        throw new UnexpectedParameterException(args.Subarray(i), getHelpGenerator(type, applicationTr));
                    positionals[0].ProcessParameter();
                }
            }

            if (positionals.Count > 0)
                positionals[0].ProcessEndOfParameters();

            if (missingMandatories.Count > 0)
                throw new MissingOptionException(missingMandatories[0], swallowingField, getHelpGenerator(type, applicationTr));

            return ret;
        }

        private static Func<Translation, ConsoleColoredString> getHelpGenerator(Type type, TranslationBase applicationTr)
        {
            return tr =>
            {
                if (tr == null)
                    tr = new Translation();

                var width = ConsoleUtil.WrapWidth();
                if (width == int.MaxValue)
                    width = 120;    // an arbitrary but sensible default value

                width--;   // outputting characters to the last column in the console causes a blank line, so need to keep the rightmost column clear

                int leftMargin = 3;

                var help = new List<ConsoleColoredString>();
                string commandName = type.GetCustomAttributes<CommandNameAttribute>().Select(c => c.Name).OrderByDescending(c => c.Length).FirstOrDefault();
                commandName = commandName == null ? Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location) : "... " + commandName;

                help.Add(new ConsoleColoredString(tr.Usage + " ", ConsoleColor.Green));
                help.Add(commandName);

                FieldInfo[] optional, required;
                getOptionalRequiredFieldsForHelp(type, out optional, out required);

                foreach (var opt in optional)
                {
                    IEnumerable<OptionAttribute> attrsRaw;
                    if (opt.FieldType.IsEnum)
                    {
                        var defAttr = opt.GetCustomAttributes<DefaultValueAttribute>().FirstOrDefault();
                        if (defAttr == null)
                            continue;
                        attrsRaw = opt.FieldType.GetFields(BindingFlags.Public | BindingFlags.Static).Where(f => !f.GetValue(null).Equals(defAttr.DefaultValue) && !f.IsDefined<UndocumentedAttribute>()).SelectMany(f => f.GetCustomAttributes<OptionAttribute>());
                    }
                    else
                        attrsRaw = opt.GetCustomAttributes<OptionAttribute>();

                    help.Add(new ConsoleColoredString(" [", ConsoleColor.DarkGray));
                    var attrs = attrsRaw.Any(a => !a.Name.StartsWith("--")) ? attrsRaw.Where(a => !a.Name.StartsWith("--")) : attrsRaw;
                    var c = new ConsoleColoredString(attrs.First().Name, ConsoleColor.Cyan);
                    foreach (var attr in attrs.Skip(1))
                    {
                        c = c + new ConsoleColoredString("|", ConsoleColor.DarkGray);
                        c = c + new ConsoleColoredString(attr.Name, ConsoleColor.Cyan);
                    }
                    if (opt.FieldType == typeof(string) || opt.FieldType == typeof(string[]))
                        c = c + new ConsoleColoredString(" <" + opt.Name + ">", ConsoleColor.Cyan);
                    help.Add(c);
                    if (opt.FieldType == typeof(string[]))
                    {
                        help.Add(new ConsoleColoredString(" [", ConsoleColor.DarkGray));
                        help.Add(c);
                        help.Add(new ConsoleColoredString(" [...]]", ConsoleColor.DarkGray));
                    }
                    help.Add(new ConsoleColoredString("]", ConsoleColor.DarkGray));
                }

                var anyCommandsWithSuboptions = false;
                var requiredParamsTable = new TextTable { MaxWidth = width - leftMargin, ColumnSpacing = 3, RowSpacing = 1, LeftMargin = leftMargin };
                int row = 0;

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

                    help.Add(new ConsoleColoredString(" <" + f.Name + ">", ConsoleColor.Cyan));

                    if (f.FieldType.IsDefined<CommandGroupAttribute>())
                    {
                        help.Add(" ...");
                        int origRow = row;
                        foreach (var ty in f.FieldType.Assembly.GetTypes().Where(t => t.IsSubclassOf(f.FieldType) && t.IsDefined<CommandNameAttribute>() && !t.IsAbstract && !t.IsDefined<UndocumentedAttribute>()))
                        {
                            FieldInfo[] subOptional, subRequired;
                            getOptionalRequiredFieldsForHelp(ty, out subOptional, out subRequired);
                            var cell1 = new ConsoleColoredString(ty.GetCustomAttributes<CommandNameAttribute>().Select(c => c.Name).OrderBy(c => c.Length).JoinString("\n"), ConsoleColor.White);
                            if (subOptional.Any() || subRequired.Any())
                            {
                                cell1 += new ConsoleColoredString("*", ConsoleColor.DarkYellow);
                                anyCommandsWithSuboptions = true;
                            }
                            requiredParamsTable.SetCell(1, row, cell1, true);
                            requiredParamsTable.SetCell(2, row, getDocumentation(ty, applicationTr));
                            row++;
                        }
                        requiredParamsTable.SetCell(0, origRow, new ConsoleColoredString("<" + f.Name + ">", ConsoleColor.Cyan), 1, row - origRow, true);
                    }
                    else if (f.FieldType.IsEnum)
                    {
                        var positional = f.IsDefined<IsPositionalAttribute>();
                        foreach (var el in f.FieldType.GetFields(BindingFlags.Static | BindingFlags.Public))
                        {
                            var str = positional
                                ? el.GetCustomAttributes<CommandNameAttribute>().Select(o => o.Name).OrderBy(c => c.Length).JoinString("\n")
                                : el.GetCustomAttributes<OptionAttribute>().Select(o => o.Name).OrderBy(c => c.Length).JoinString("\n");
                            requiredParamsTable.SetCell(0, row, new ConsoleColoredString(str, ConsoleColor.White), true);
                            requiredParamsTable.SetCell(1, row, getDocumentation(el, applicationTr), 2, 1);
                            row++;
                        }
                    }
                    else
                    {
                        requiredParamsTable.SetCell(0, row, new ConsoleColoredString("<" + f.Name + ">", ConsoleColor.Cyan), true);
                        requiredParamsTable.SetCell(1, row, getDocumentation(f, applicationTr), 2, 1);
                        row++;
                    }
                }

                var optionalParamsTable = new TextTable { MaxWidth = width - leftMargin, ColumnSpacing = 3, RowSpacing = 1, LeftMargin = leftMargin };
                row = 0;

                foreach (var f in optional)
                {
                    if (f.FieldType.IsEnum)
                    {
                        foreach (var el in f.FieldType.GetFields(BindingFlags.Static | BindingFlags.Public).Where(e => !e.GetValue(null).Equals(f.GetCustomAttributes<DefaultValueAttribute>().First().DefaultValue) && !e.IsDefined<UndocumentedAttribute>()))
                        {
                            optionalParamsTable.SetCell(0, row, new ConsoleColoredString(el.GetCustomAttributes<OptionAttribute>().Where(o => !o.Name.StartsWith("--")).Select(o => o.Name).OrderBy(c => c.Length).JoinString(", "), ConsoleColor.White), true);
                            optionalParamsTable.SetCell(1, row, new ConsoleColoredString(el.GetCustomAttributes<OptionAttribute>().Where(o => o.Name.StartsWith("--")).Select(o => o.Name).OrderBy(c => c.Length).JoinString(", "), ConsoleColor.White), true);
                            optionalParamsTable.SetCell(2, row, getDocumentation(el, applicationTr));
                            row++;
                        }
                    }
                    else
                    {
                        optionalParamsTable.SetCell(0, row, new ConsoleColoredString(f.GetCustomAttributes<OptionAttribute>().Where(o => !o.Name.StartsWith("--")).Select(o => o.Name).OrderBy(c => c.Length).JoinString(", "), ConsoleColor.White), true);
                        optionalParamsTable.SetCell(1, row, new ConsoleColoredString(f.GetCustomAttributes<OptionAttribute>().Where(o => o.Name.StartsWith("--")).Select(o => o.Name).OrderBy(c => c.Length).JoinString(", "), ConsoleColor.White), true);
                        optionalParamsTable.SetCell(2, row, getDocumentation(f, applicationTr));
                        row++;
                    }
                }

                var doc = getDocumentation(type, applicationTr);
                if (doc != null)
                {
                    help.Add(Environment.NewLine);
                    help.Add(Environment.NewLine);
                    help.Add(ConsoleColoredString.FromEggsNode(doc));
                }

                var helpString = new List<ConsoleColoredString>();
                foreach (var line in new ConsoleColoredString(help.ToArray()).WordWrap(width, 8))
                {
                    helpString.Add(line);
                    helpString.Add(Environment.NewLine);
                }
                if (required.Any())
                {
                    helpString.Add(Environment.NewLine);
                    helpString.Add(new ConsoleColoredString(tr.ParametersHeader, ConsoleColor.White));
                    helpString.Add(Environment.NewLine);
                    helpString.Add(Environment.NewLine);
                    helpString.Add(requiredParamsTable.ToColoredString());
                }
                if (optional.Any())
                {
                    helpString.Add(Environment.NewLine);
                    helpString.Add(new ConsoleColoredString(tr.OptionsHeader, ConsoleColor.White));
                    helpString.Add(Environment.NewLine);
                    helpString.Add(Environment.NewLine);
                    helpString.Add(optionalParamsTable.ToColoredString());
                }
                if (anyCommandsWithSuboptions)
                {
                    helpString.Add(Environment.NewLine);
                    helpString.Add(new ConsoleColoredString("* ", ConsoleColor.DarkYellow));
                    helpString.Add(new ConsoleColoredString("accepts further arguments on the command line", ConsoleColor.Gray));
                    helpString.Add(Environment.NewLine);
                }

                return new ConsoleColoredString(helpString.ToArray());
            };
        }

        private static void getOptionalRequiredFieldsForHelp(Type type, out FieldInfo[] optional, out FieldInfo[] required)
        {
            optional = type.GetAllFields().Where(f => !f.IsDefined<IsPositionalAttribute>() && !f.IsDefined<IsMandatoryAttribute>() && !f.IsDefined<UndocumentedAttribute>()).ToArray();
            required = type.GetAllFields().Where(f => f.IsDefined<IsPositionalAttribute>() || f.IsDefined<IsMandatoryAttribute>()).ToArray();
        }

        private static EggsNode getDocumentation(MemberInfo member, TranslationBase applicationTr)
        {
            if (member.IsDefined<DocumentationAttribute>())
                return member.GetCustomAttributes<DocumentationAttribute>().Select(d => EggsML.Parse(d.Translate(applicationTr))).First();

            if (member.IsDefined<DocumentationLiteralAttribute>())
                return member.GetCustomAttributes<DocumentationLiteralAttribute>().Select(d => EggsML.Parse(d.Text)).First();

            return null;
        }

        /// <summary>If compiled in DEBUG mode, this method performs safety checks to ensure that the structure of your command-line syntax defining class is valid.
        /// Run this method as a post-build step to ensure reliability of execution. In non-DEBUG mode, this method is a no-op.</summary>
        /// <param name="type">A reference to the class containing the command-line syntax which would be passed to <see cref="Parse(string[])"/>.</param>
        public static void PostBuildStep(Type type)
        {
#if DEBUG
            if (!type.IsClass)
                throw new PostBuildException(@"{0} is not a class.".Fmt(type.FullName));

            if (type.GetConstructor(Type.EmptyTypes) == null)
                throw new PostBuildException(@"{0} does not have a default constructor.".Fmt(type.FullName));

            var optionTaken = new Dictionary<string, MemberInfo>();
            FieldInfo lastField = null;

            foreach (var field in type.GetFields())
            {
                if (lastField != null)
                    throw new PostBuildException(@"The type of {0}.{1} necessitates that it is the last one in the class.".Fmt(lastField.DeclaringType.FullName, lastField.Name));
                var positional = field.IsDefined<IsPositionalAttribute>();

                if ((positional || field.IsDefined<IsMandatoryAttribute>()) && field.IsDefined<UndocumentedAttribute>())
                    throw new PostBuildException(@"{0}.{1}: Fields cannot simultaneously be mandatory/positional and also undocumented.".Fmt(field.DeclaringType.FullName, field.Name));

                // (1) if it's a field of type enum:
                if (field.FieldType.IsEnum)
                {
                    var defaultAttr = field.GetCustomAttributes<DefaultValueAttribute>().FirstOrDefault();
                    var commandsTaken = new Dictionary<string, FieldInfo>();

                    // check that it is either positional OR has a DefaultAttribute, but not both
                    if (positional && defaultAttr != null)
                        throw new PostBuildException(@"{0}.{1}: Fields of an enum type cannot both be positional and have a default value.".Fmt(type.FullName, field.Name));
                    if (!positional && defaultAttr == null)
                        throw new PostBuildException(@"{0}.{1}: Fields of an enum type must be either positional or have a default value.".Fmt(type.FullName, field.Name));

                    // check that it doesn't have an [Option] or [CommandName] attribute, because the enum values are supposed to have that instead
                    if (field.GetCustomAttributes<OptionAttribute>().Any() || field.GetCustomAttributes<CommandNameAttribute>().Any())
                        throw new PostBuildException(@"{0}.{1}: Fields of an enum type cannot have [Option] or [CommandName] attributes; these attributes should go on the enum values in the enum type instead.".Fmt(type.FullName, field.Name));

                    foreach (var enumField in field.FieldType.GetFields(BindingFlags.Static | BindingFlags.Public))
                    {
                        if (!positional && enumField.GetValue(null).Equals(defaultAttr.DefaultValue))
                            continue;

                        // check that the enum values all have documentation
                        checkDocumentation(enumField);

                        if (positional)
                        {
                            // check that the enum values all have at least one CommandName, and they do not clash
                            var cmdNames = enumField.GetCustomAttributes<CommandNameAttribute>();
                            if (!cmdNames.Any())
                                throw new PostBuildException(@"{0}.{1} (used by {2}.{3}): Enum value does not have a [CommandName] attribute.".Fmt(field.FieldType.FullName, enumField.Name, type.FullName, field.Name));
                            checkCommandNamesUnique(cmdNames, commandsTaken, type, field, enumField);
                        }
                        else
                        {
                            // check that the non-default enum values' Options are present and do not clash
                            var options = enumField.GetCustomAttributes<OptionAttribute>();
                            if (!options.Any())
                                throw new PostBuildException(@"{0}.{1} (used by {2}.{3}): Enum value must have at least one [Option] attribute.".Fmt(field.FieldType.FullName, enumField.Name, type.FullName, field.Name));
                            checkOptionsUnique(options, optionTaken, type, field, enumField);
                        }
                    }
                }
                else if (field.FieldType == typeof(bool))
                {
                    if (positional)
                        throw new PostBuildException(@"{0}.{1}: Fields of type bool cannot be positional.".Fmt(type.FullName, field.Name));

                    var options = field.GetCustomAttributes<OptionAttribute>();
                    if (!options.Any())
                        throw new PostBuildException(@"{0}.{1}: Boolean field must have at least one [Option] attribute.".Fmt(type.FullName, field.Name));

                    checkOptionsUnique(options, optionTaken, type, field);
                    checkDocumentation(field);
                }
                else if (field.FieldType == typeof(string) || field.FieldType == typeof(string[]))
                {
                    var options = field.GetCustomAttributes<OptionAttribute>();
                    if (!options.Any() && !positional)
                        throw new PostBuildException(@"{0}.{1}: Field of type string or string[] must have either [IsPositional] or at least one [Option] attribute.".Fmt(type.FullName, field.Name));

                    checkOptionsUnique(options, optionTaken, type, field);
                    checkDocumentation(field);
                }
                else if (field.FieldType.IsClass && field.FieldType.IsDefined<CommandGroupAttribute>())
                {
                    // Class-type fields must be positional parameters
                    if (!positional)
                        throw new PostBuildException(@"{0}.{1}: CommandGroup fields must be declared positional.".Fmt(type.FullName, field.Name));

                    // The class must have at least two subclasses with a [CommandName] attribute
                    var subclasses = field.FieldType.Assembly.GetTypes().Where(t => !t.IsAbstract && t.IsSubclassOf(field.FieldType) && t.IsDefined<CommandNameAttribute>());
                    if (subclasses.Count() < 2)
                        throw new PostBuildException(@"{0}.{1}: The CommandGroup class type must have at least two non-abstract subclasses with the [CommandName] attribute.".Fmt(type.FullName, field.Name));

                    var commandsTaken = new Dictionary<string, Type>();

                    foreach (var subclass in subclasses)
                    {
                        checkDocumentation(subclass);
                        checkCommandNamesUnique(subclass.GetCustomAttributes<CommandNameAttribute>(), commandsTaken, subclass);

                        // Recursively check this class
                        PostBuildStep(subclass);
                    }

                    lastField = field;
                }
                else
                    throw new PostBuildException(@"{0}.{1} is not of a recognised type. Currently accepted types are: enum types, bool, string, and classes with the [CommandGroup] attribute.".Fmt(type.FullName, field.Name));
            }
#endif
        }

#if DEBUG
        private static void checkOptionsUnique(IEnumerable<OptionAttribute> options, Dictionary<string, MemberInfo> optionTaken, Type type, FieldInfo field, FieldInfo enumField)
        {
            foreach (var option in options)
            {
                if (optionTaken.ContainsKey(option.Name))
                    throw new PostBuildException(@"{0}.{1} (used by {2}.{3}): Option ""{4}"" is already taken by {5}.{6}.".Fmt(field.FieldType.FullName, enumField.Name, type.FullName, field.Name, option.Name, optionTaken[option.Name].DeclaringType.FullName, optionTaken[option.Name].Name));
                optionTaken[option.Name] = enumField;
            }
        }

        private static void checkOptionsUnique(IEnumerable<OptionAttribute> options, Dictionary<string, MemberInfo> optionTaken, Type type, FieldInfo field)
        {
            foreach (var option in options)
            {
                if (optionTaken.ContainsKey(option.Name))
                    throw new PostBuildException(@"{0}.{1}: Option ""{2}"" is already taken by {3}.{4}.".Fmt(type.FullName, field.Name, option.Name, optionTaken[option.Name].DeclaringType.FullName, optionTaken[option.Name].Name));
                optionTaken[option.Name] = field;
            }
        }

        private static void checkCommandNamesUnique(IEnumerable<CommandNameAttribute> commands, Dictionary<string, Type> commandsTaken, Type subclass)
        {
            foreach (var cmd in commands)
            {
                if (commandsTaken.ContainsKey(cmd.Name))
                    throw new PostBuildException(@"{0}: CommandName ""{1}"" is already taken by {2}.".Fmt(subclass.FullName, cmd.Name, commandsTaken[cmd.Name].FullName));
                commandsTaken[cmd.Name] = subclass;
            }
        }

        private static void checkCommandNamesUnique(IEnumerable<CommandNameAttribute> cmdNames, Dictionary<string, FieldInfo> commandsTaken, Type type, FieldInfo field, FieldInfo enumField)
        {
            foreach (var cmd in cmdNames)
            {
                if (commandsTaken.ContainsKey(cmd.Name))
                    throw new PostBuildException(@"{0}.{1} (used by {2}.{3}): Enum value's CommandName ""{4}"" is already taken by {5}.".Fmt(field.FieldType.FullName, enumField.Name, type.FullName, field.Name, cmd.Name, commandsTaken[cmd.Name].Name));
                commandsTaken[cmd.Name] = enumField;
            }
        }

        private static void checkDocumentation(MemberInfo member)
        {
            // This automatically executes the constructor of the relevant DocumentationAttribute, which in turn will throw an exception if it is invalid.
            if (!member.IsDefined<DocumentationAttribute>() && !member.IsDefined<DocumentationLiteralAttribute>() && !member.IsDefined<UndocumentedAttribute>())
                throw new PostBuildException(@"{0}.{1}: Field does not have any documentation. Use the [Documentation] or [DocumentationLiteral] attribute to specify documentation for a command-line option or parameter. Use [Undocumented] to completely hide an option from the help screen.".Fmt(member.DeclaringType.FullName, member.Name));
        }
#endif
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
    public class Translation : TranslationBase
    {
#pragma warning disable 1591    // Missing XML comment for publicly visible type or member
        public Translation() : base(Language.EnglishUS) { }

        [LingoInGroup(TranslationGroup.CommandLineError)]
        public TrString
            UnrecognizedCommandOrOption = @"The specified command or option, {0}, is not recognized.",
            IncompatibleCommandOrOption = @"The command or option, {0}, cannot be used in conjunction with {1}. Please specify only one of the two.",
            UnrecognizedType = @"{0}.{1} is not of a recognized type.",
            IncompleteOption = @"The ""{0}"" option must be followed by an additional parameter.",
            UnexpectedParameter = @"Unexpected parameter.",
            MissingParameter = @"The ""{0}"" parameter is missing.",
            MissingOption = @"The option ""{0}"" is mandatory and must be specified.",
            MissingOptionBefore = @"The option ""{0}"" is mandatory and must be specified before the ""{1}"" parameter.";

        [LingoInGroup(TranslationGroup.CommandLineHelp)]
        public TrString
            Usage = @"Usage:",
            ParametersHeader = @"Required parameters:",
            OptionsHeader = @"Options:";

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

    /// <summary>
    /// Use this to specify that an enum type is used to identify documentation text in an internationalized (multi-language) application.
    /// </summary>
    [AttributeUsage(AttributeTargets.Enum, Inherited = false, AllowMultiple = true)]
    public sealed class DocumentationCodesAttribute : Attribute
    {
        /// <summary>Constructor.</summary>
        /// <param name="staticMethods">Points to a static class containing static methods which provide translations for the documentation strings. Those methods must be marked with the <see cref="DocumentationTranslationAttribute"/>.</param>
        public DocumentationCodesAttribute(Type staticMethods) { Type = staticMethods; }
        /// <summary>Returns the static class containing static methods which provide translations for the documentation strings.</summary>
        public Type Type { get; private set; }
    }

    /// <summary>
    /// Use this attribute on each static method that provides a translation for a documentation string.
    /// These static methods must all be in a static class which is pointed to by the <see cref="DocumentationCodesAttribute"/> on an enum type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public class DocumentationTranslationAttribute : Attribute
    {
        /// <summary>Constructor.</summary>
        /// <param name="code">A code that identifies the translation. This must be a value from an enum type with the <see cref="DocumentationCodesAttribute"/>.</param>
        public DocumentationTranslationAttribute(object code) { Code = code; }
        /// <summary>The enum value that represents the documentation text.</summary>
        public object Code { get; private set; }
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

    /// <summary>Use this attribute in an internationalized (multi-language) application to link a command-line option or command with the help text that describes (documents) it.</summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public class DocumentationAttribute : Attribute
    {
        /// <summary>Constructor.</summary>
        /// <param name="code">A code that identifies the translation. This must be a value from an enum type with the <see cref="DocumentationCodesAttribute"/>.</param>
        public DocumentationAttribute(object code)
        {
            var t = code.GetType();
            var attrs = t.GetCustomAttributes<DocumentationCodesAttribute>();
            if (!t.IsEnum || !attrs.Any())
            {
#if DEBUG
                throw new PostBuildException("The type parameter to DocumentationAttribute must be an enum type; {0} was passed instead. Additionally, the enum type must have a [DocumentationCodes] attribute.".Fmt(t.FullName));
#else
                return;
#endif
            }

            foreach (var meth in attrs.First().Type.GetMethods(BindingFlags.Static | BindingFlags.Public))
            {
                if (meth.ReturnType != typeof(string))
                    continue;
                var p = meth.GetParameters();
                if (p.Length != 1 || !typeof(TranslationBase).IsAssignableFrom(p[0].ParameterType))
                    continue;
                foreach (var attr in meth.GetCustomAttributes<DocumentationTranslationAttribute>().Where(a => a.Code.Equals(code)))
                {
#if DEBUG
                    if (_translate == null)
                        _translate = meth;
                    else
                        throw new PostBuildException(@"The documentation code ""{0}"" has more than one documentation method: {1}, {2}".Fmt(code, _translate.Name, meth.Name));
#else
                    _translate = meth;
                    return;
#endif
                }
            }
#if DEBUG
            if (_translate == null)
                throw new PostBuildException(@"Documentation method for documentation code ""{0}"" not found.".Fmt(code));
#endif
        }
        private MethodInfo _translate = null;
        /// <summary>Returns the translated documentation text identified by this attribute.</summary>
        /// <param name="tr">The translation class containing the translated text.</param>
        public string Translate(TranslationBase tr) { return _translate == null ? null : (string) _translate.Invoke(null, new object[] { tr }); }
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
        internal Func<Translation, ConsoleColoredString> GenerateHelpFunc { get; private set; }
        /// <summary>Generates the help screen to be output to the user on the console.</summary>
        public ConsoleColoredString GenerateHelp() { return GenerateHelpFunc(null); }
        /// <summary>Generates the help screen to be output to the user on the console.</summary>
        /// <param name="tr">The translation class containing the translated text.</param>
        public ConsoleColoredString GenerateHelp(Translation tr) { return GenerateHelpFunc(tr); }
        /// <summary>Constructor.</summary>
        public CommandLineParseException(Func<Translation, string> getMessage, Func<Translation, ConsoleColoredString> helpGenerator) : this(getMessage, helpGenerator, null) { }
        /// <summary>Constructor.</summary>
        public CommandLineParseException(Func<Translation, string> getMessage, Func<Translation, ConsoleColoredString> helpGenerator, Exception inner) : base(getMessage, inner) { GenerateHelpFunc = helpGenerator; }
        /// <summary>
        /// Prints usage infofmation, followed by an error message describing to the user what it was that the parser didn't
        /// understand. When the exception was caused by one of a list of common help switches, no error message is printed.
        /// </summary>
        public void WriteUsageInfoToConsole()
        {
            ConsoleUtil.Write(GenerateHelp());

            var helps = new[] { "-?", "/?", "--?", "-h", "--help", "help" };
            bool requestedHelp =
                (this is UnrecognizedCommandOrOptionException && helps.Contains(((UnrecognizedCommandOrOptionException) this).CommandOrOptionName))
                || (this is UnexpectedParameterException && helps.Contains(((UnexpectedParameterException) this).UnexpectedParameters.FirstOrDefault()));

            if (!requestedHelp)
            {
                Console.WriteLine();
                ConsoleUtil.WriteLine(new ConsoleColoredString("Error: ", ConsoleColor.Red) + Message);
            }
        }
    }

    /// <summary>Specifies that the command-line parser encountered a command or option that was not recognised (there was no <see cref="OptionAttribute"/> or <see cref="CommandNameAttribute"/> attribute with a matching option or command name).</summary>
    [Serializable]
    public class UnrecognizedCommandOrOptionException : CommandLineParseException
    {
        /// <summary>The unrecognized command name or option name.</summary>
        public string CommandOrOptionName { get; private set; }
        /// <summary>Constructor.</summary>
        public UnrecognizedCommandOrOptionException(string commandOrOptionName, Func<Translation, ConsoleColoredString> helpGenerator) : this(commandOrOptionName, helpGenerator, null) { }
        /// <summary>Constructor.</summary>
        public UnrecognizedCommandOrOptionException(string commandOrOptionName, Func<Translation, ConsoleColoredString> helpGenerator, Exception inner)
            : base(tr => tr.UnrecognizedCommandOrOption.Fmt(commandOrOptionName), helpGenerator, inner)
        {
            CommandOrOptionName = commandOrOptionName;
        }
    }

    /// <summary>Specifies that the command-line parser encountered a command or option that is not allowed in conjunction with a previously-encountered command or option.</summary>
    [Serializable]
    public class IncompatibleCommandOrOptionException : CommandLineParseException
    {
        /// <summary>The earlier option or command, which by itself is valid, but conflicts with the <see cref="LaterCommandOrOption"/>.</summary>
        public string EarlierCommandOrOption { get; private set; }
        /// <summary>The later option or command, which conflicts with the <see cref="EarlierCommandOrOption"/>.</summary>
        public string LaterCommandOrOption { get; private set; }
        /// <summary>Constructor.</summary>
        public IncompatibleCommandOrOptionException(string earlier, string later, Func<Translation, ConsoleColoredString> helpGenerator) : this(earlier, later, helpGenerator, null) { }
        /// <summary>Constructor.</summary>
        public IncompatibleCommandOrOptionException(string earlier, string later, Func<Translation, ConsoleColoredString> helpGenerator, Exception inner)
            : base(tr => tr.IncompatibleCommandOrOption.Fmt(later, earlier), helpGenerator, inner)
        {
            EarlierCommandOrOption = earlier;
            LaterCommandOrOption = later;
        }
    }

    /// <summary>Specifies that the command-line parser encountered an unsupported type in the class definition.</summary>
    /// <remarks>This exception can only occur if the post-build check didn't run; see <see cref="CommandLineParser.PostBuildStep(Type)"/></remarks>
    [Serializable]
    public class UnrecognizedTypeException : CommandLineParseException
    {
        /// <summary>The full name of the unsupported type that was encountered.</summary>
        public string TypeName { get; private set; }
        /// <summary>The name of the field that has the unsupported type.</summary>
        public string FieldName { get; private set; }
        /// <summary>Constructor.</summary>
        public UnrecognizedTypeException(string typeName, string fieldName, Func<Translation, ConsoleColoredString> helpGenerator) : this(typeName, fieldName, helpGenerator, null) { }
        /// <summary>Constructor.</summary>
        public UnrecognizedTypeException(string typeName, string fieldName, Func<Translation, ConsoleColoredString> helpGenerator, Exception inner)
            : base(tr => tr.UnrecognizedType.Fmt(typeName, fieldName), helpGenerator, inner)
        {
            TypeName = typeName;
            FieldName = fieldName;
        }
    }

    /// <summary>Specifies that the command-line parser encountered the end of the command line when it expected a parameter to an option.</summary>
    [Serializable]
    public class IncompleteOptionException : CommandLineParseException
    {
        /// <summary>The name of the option that was missing a parameter.</summary>
        public string OptionName { get; private set; }
        /// <summary>Constructor.</summary>
        public IncompleteOptionException(string optionName, Func<Translation, ConsoleColoredString> helpGenerator) : this(optionName, helpGenerator, null) { }
        /// <summary>Constructor.</summary>
        public IncompleteOptionException(string optionName, Func<Translation, ConsoleColoredString> helpGenerator, Exception inner)
            : base(tr => tr.IncompleteOption.Fmt(optionName), helpGenerator, inner)
        {
            OptionName = optionName;
        }
    }

    /// <summary>Specifies that the command-line parser encountered additional command-line parameters when it expected the end of the command line.</summary>
    [Serializable]
    public class UnexpectedParameterException : CommandLineParseException
    {
        /// <summary>Contains the first unexpected parameter and all of the subsequent arguments.</summary>
        public string[] UnexpectedParameters { get; private set; }
        /// <summary>Constructor.</summary>
        public UnexpectedParameterException(string[] unexpectedParams, Func<Translation, ConsoleColoredString> helpGenerator) : this(unexpectedParams, helpGenerator, null) { }
        /// <summary>Constructor.</summary>
        public UnexpectedParameterException(string[] unexpectedParams, Func<Translation, ConsoleColoredString> helpGenerator, Exception inner) : base(tr => tr.UnexpectedParameter, helpGenerator, inner) { UnexpectedParameters = unexpectedParams; }
    }

    /// <summary>Specifies that the command-line parser encountered the end of the command line when it expected additional positional parameters.</summary>
    [Serializable]
    public class MissingParameterException : CommandLineParseException
    {
        /// <summary>Contains the name of the field pertaining to the parameter that was missing.</summary>
        public string FieldName { get; private set; }
        /// <summary>Constructor.</summary>
        public MissingParameterException(string fieldName, Func<Translation, ConsoleColoredString> helpGenerator) : this(fieldName, helpGenerator, null) { }
        /// <summary>Constructor.</summary>
        public MissingParameterException(string fieldName, Func<Translation, ConsoleColoredString> helpGenerator, Exception inner) : base(tr => tr.MissingParameter.Fmt(fieldName), helpGenerator, inner) { FieldName = fieldName; }
    }

    /// <summary>Specifies that the command-line parser encountered the end of the command line when it expected additional mandatory options.</summary>
    [Serializable]
    public class MissingOptionException : CommandLineParseException
    {
        /// <summary>Contains the field pertaining to the parameter that was missing.</summary>
        public FieldInfo Field { get; private set; }
        /// <summary>Contains an optional reference to a field which the missing parameter must precede.</summary>
        public FieldInfo BeforeField { get; private set; }
        /// <summary>Constructor.</summary>
        public MissingOptionException(FieldInfo field, FieldInfo beforeField, Func<Translation, ConsoleColoredString> helpGenerator) : this(field, beforeField, helpGenerator, null) { }
        /// <summary>Constructor.</summary>
        public MissingOptionException(FieldInfo field, FieldInfo beforeField, Func<Translation, ConsoleColoredString> helpGenerator, Exception inner)
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

#if DEBUG
    /// <summary>Specifies that the post-build check (<see cref="CommandLineParser.PostBuildStep(Type)"/>) discovered an error.</summary>
    [Serializable]
    public class PostBuildException : RTException
    {
        /// <summary>Constructor.</summary>
        public PostBuildException(string message) : this(message, null) { }
        /// <summary>Constructor.</summary>
        public PostBuildException(string message, Exception inner) : base(message, inner) { }
    }

    static class CommandLineParsingReflectionExtensions
    {
        public static string GetNamespace(this MemberInfo member)
        {
            if (member is Type)
                return ((Type) member).Namespace;
            return member.DeclaringType.Namespace;
        }

        public static string GetTypeName(this MemberInfo member)
        {
            if (member is Type)
                return ((Type) member).Name;
            return member.DeclaringType.Name;
        }
    }
#endif
}
