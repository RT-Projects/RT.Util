using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using RT.Util;
using RT.Util.ExtensionMethods;
using RT.Util.IL;

namespace RT.PostBuild
{
    public static class PostBuildChecker
    {
        /// <summary>Runs all post-build checks defined in the specified assemblies. This is intended to be run as a post-build event. See remarks for details.</summary>
        /// <remarks><para>In non-DEBUG mode, does nothing and returns 0.</para>
        /// <para>Intended use is as follows:</para>
        /// <list type="bullet">
        ///    <item><description><para>Add the following line to your project's post-build event:</para>
        ///        <code>"$(TargetPath)" --post-build-check "$(SolutionDir)."</code></description></item>
        ///    <item><description><para>Add the following code at the beginning of your project's Main() method:</para>
        ///        <code>
        ///            if (args.Length == 2 &amp;&amp; args[0] == "--post-build-check")
        ///                return Ut.RunPostBuildChecks(args[1], Assembly.GetExecutingAssembly());
        ///        </code>
        ///        <para>If your project entails several assemblies, you can specify additional assemblies in the call to <see cref="Ut.RunPostBuildChecks"/>.
        ///            For example, you could specify <c>typeof(SomeTypeInMyLibrary).Assembly</c>.</para>
        ///        </description></item>
        ///    <item><description>
        ///        <para>Add post-build check methods to any type where they may be relevant. For example, for a command-line program that uses
        ///            <see cref="RT.Util.CommandLine.CommandLineParser"/>, you might use code similar to the following:</para>
        ///        <code>
        ///            #if DEBUG
        ///                private static void PostBuildCheck(IPostBuildReporter rep)
        ///                {
        ///                    // Replace “CommandLine” with the name of your command-line type, and “Translation”
        ///                    // with the name of your translation type (<see cref="RT.Util.Lingo.TranslationBase"/>)
        ///                    CommandLineParser.PostBuildStep&lt;CommandLine&gt;(rep, typeof(Translation));
        ///                }
        ///            #endif
        ///        </code>
        ///        <para>The method is expected to have one parameter of type <see cref="IPostBuildReporter"/>, a return type of void, and it is expected
        ///            to be static and non-public. Errors and warnings can be reported by calling methods on said <see cref="IPostBuildReporter"/> object.
        ///            Alternatively, throwing an exception will also report an error.</para>
        ///    </description></item>
        /// </list></remarks>
        /// <param name="sourcePath">Specifies the path to the folder containing the C# source files.</param>
        /// <param name="assemblies">Specifies the compiled assemblies from which to run post-build checks.</param>
        /// <returns>1 if any errors occurred, otherwise 0.</returns>
        public static int RunPostBuildChecks(string sourcePath, params Assembly[] assemblies)
        {
            int countMethods = 0;
            var rep = new postBuildReporter(sourcePath);
            var attempt = Ut.Lambda((Action action) =>
            {
                try
                {
                    action();
                }
                catch (Exception e)
                {
                    rep.AnyErrors = true;
                    string indent = "";
                    while (e != null)
                    {
                        var st = new StackTrace(e, true);
                        string fileLine = null;
                        for (int i = 0; i < st.FrameCount; i++)
                        {
                            var frame = st.GetFrame(i);
                            if (frame.GetFileName() != null)
                            {
                                fileLine = frame.GetFileName() + "(" + frame.GetFileLineNumber() + "," + frame.GetFileColumnNumber() + "): ";
                                break;
                            }
                        }

                        Console.Error.WriteLine($"{fileLine}Error: {indent}{e.Message.Replace("\n", " ").Replace("\r", "")} ({e.GetType().FullName})");
                        Console.Error.WriteLine(e.StackTrace);
                        e = e.InnerException;
                        indent += "---- ";
                    }
                }
            });

            // Step 1: Run all the custom-defined PostBuildCheck methods
            foreach (var ty in assemblies.SelectMany(asm => asm.GetTypes()))
            {
                attempt(() =>
                {
                    var meth = ty.GetMethod("PostBuildCheck", BindingFlags.NonPublic | BindingFlags.Static);
                    if (meth != null)
                    {
                        if (meth.GetParameters().Select(p => p.ParameterType).SequenceEqual(new Type[] { typeof(IPostBuildReporter) }) && meth.ReturnType == typeof(void))
                        {
                            countMethods++;
                            meth.Invoke(null, new object[] { rep });
                        }
                        else
                            rep.Error(
                                $"The type {ty.FullName} has a method called PostBuildCheck() that is not of the expected signature. There should be one parameter of type {typeof(IPostBuildReporter).FullName}, and the return type should be void.",
                                (ty.IsValueType ? "struct " : "class ") + ty.Name, "PostBuildCheck");
                    }
                });
            }

            // Step 2: Run all the built-in checks on IL code
            foreach (var asm in assemblies)
                foreach (var type in asm.GetTypes())
                    foreach (var meth in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                        attempt(() =>
                        {
                            var instructions = ILReader.ReadIL(meth, type).ToArray();
                            for (int i = 0; i < instructions.Length; i++)
                            {
                                // Check that “throw new ArgumentNullException(...)” statements refer to an actual parameter
                                if (instructions[i].OpCode.Value == OpCodes.Newobj.Value)
                                {
                                    var constructor = (ConstructorInfo) instructions[i].Operand;
                                    string wrong = null;
                                    string wrongException = "ArgumentNullException";
                                    if (constructor.DeclaringType == typeof(ArgumentNullException) && constructor.GetParameters().Select(p => p.ParameterType).SequenceEqual(typeof(string)))
                                        if (instructions[i - 1].OpCode.Value == OpCodes.Ldstr.Value)
                                            if (!meth.GetParameters().Any(p => p.Name == (string) instructions[i - 1].Operand))
                                                wrong = (string) instructions[i - 1].Operand;
                                    if (constructor.DeclaringType == typeof(ArgumentNullException) && constructor.GetParameters().Select(p => p.ParameterType).SequenceEqual(typeof(string), typeof(string)))
                                        if (instructions[i - 1].OpCode.Value == OpCodes.Ldstr.Value && instructions[i - 2].OpCode.Value == OpCodes.Ldstr.Value)
                                            if (!meth.GetParameters().Any(p => p.Name == (string) instructions[i - 2].Operand))
                                                wrong = (string) instructions[i - 2].Operand;
                                    if (constructor.DeclaringType == typeof(ArgumentException) && constructor.GetParameters().Select(p => p.ParameterType).SequenceEqual(typeof(string), typeof(string)))
                                        if (instructions[i - 1].OpCode.Value == OpCodes.Ldstr.Value)
                                            if (!meth.GetParameters().Any(p => p.Name == (string) instructions[i - 1].Operand))
                                            {
                                                wrong = (string) instructions[i - 1].Operand;
                                                wrongException = "ArgumentException";
                                            }

                                    if (wrong != null)
                                    {
                                        rep.Error(
                                            Regex.IsMatch(meth.DeclaringType.Name, @"<.*>d__\d")
                                                ? $@"The iterator method ""{type.FullName}.{meth.Name}"" constructs a {wrongException}. Move this argument check outside the iterator."
                                                : $@"The method ""{type.FullName}.{meth.Name}"" constructs an {wrongException} with a parameter name ""{wrong}"" which doesn't appear to be a parameter in that method.",
                                            getDebugClassName(meth),
                                            getDebugMethodName(meth),
                                            wrongException,
                                            wrong
                                        );
                                    }

                                    if (constructor.DeclaringType == typeof(ArgumentException) && constructor.GetParameters().Select(p => p.ParameterType).SequenceEqual(typeof(string)))
                                        rep.Error(
                                            Regex.IsMatch(meth.DeclaringType.Name, @"<.*>d__\d")
                                                ? $@"The iterator method ""{type.FullName}.{meth.Name}"" constructs an ArgumentException. Move this argument check outside the iterator."
                                                : $@"The method ""{type.FullName}.{meth.Name}"" uses the single-argument constructor to ArgumentException. Please use the two-argument constructor and specify the parameter name. If there is no parameter involved, use InvalidOperationException.",
                                            getDebugClassName(meth),
                                            getDebugMethodName(meth),
                                            "ArgumentException");
                                }
                                else if (i < instructions.Length - 1 && (instructions[i].OpCode.Value == OpCodes.Call.Value || instructions[i].OpCode.Value == OpCodes.Callvirt.Value) && instructions[i + 1].OpCode.Value == OpCodes.Pop.Value)
                                {
                                    var method = (MethodInfo) instructions[i].Operand;
                                    var mType = method.DeclaringType;
                                    if (postBuildGetNoPopMethods().Contains(method))
                                        rep.Error(
                                            $@"Useless call to ""{mType.FullName}.{method.Name}"" (the return value is discarded).",
                                            getDebugClassName(meth),
                                            getDebugMethodName(meth),
                                            method.Name
                                        );
                                }
                            }
                        });

            Console.WriteLine($"Post-build checks ran on {assemblies.Length} assemblies, {countMethods} methods and completed {(rep.AnyErrors ? "with ERRORS" : "SUCCESSFULLY")}.");

            return rep.AnyErrors ? 1 : 0;
        }

        private static MethodInfo[] _postBuild_NoPopMethods;
        private static MethodInfo[] postBuildGetNoPopMethods()
        {
            if (_postBuild_NoPopMethods == null)
            {
                var list = new List<MethodInfo>();
                var stringMethods = new[] { "Normalize", "PadLeft", "PadRight", "Remove", "Replace", "ToLower", "ToLowerInvariant", "ToUpper", "ToUpperInvariant", "Trim", "TrimEnd", "TrimStart" };
                list.AddRange(typeof(string).GetMethods(BindingFlags.Public | BindingFlags.Instance).Where(m => stringMethods.Contains(m.Name)));
                //list.AddRange(typeof(ConsoleColoredString).GetMethods(BindingFlags.Public | BindingFlags.Instance).Where(m => stringMethods.Contains(m.Name)));
                list.AddRange(typeof(DateTime).GetMethods(BindingFlags.Public | BindingFlags.Instance).Where(m => m.Name.StartsWith("Add") || m.Name == "Subtract"));
                _postBuild_NoPopMethods = list.ToArray();
            }
            return _postBuild_NoPopMethods;
        }

        private static string getDebugClassName(MethodInfo meth)
        {
            var m = Regex.Match(meth.DeclaringType.Name, @"<(.*)>d__\d");
            if (m.Success)
                return (meth.DeclaringType.DeclaringType.IsValueType ? "struct " : "class ") + meth.DeclaringType.DeclaringType.Name;
            return (meth.DeclaringType.IsValueType ? "struct " : "class ") + genericsConvert(meth.DeclaringType.Name);
        }

        private static string getDebugMethodName(MethodInfo meth)
        {
            var m = Regex.Match(meth.DeclaringType.Name, @"<(.*)>d__\d");
            if (m.Success)
                return m.Groups[1].Value;
            return genericsConvert(meth.Name);
        }

        private static string genericsConvert(string memberName)
        {
            var p = memberName.IndexOf('`');
            if (p != -1)
                return memberName.Substring(0, p) + "<";
            return memberName;
        }

        private sealed class postBuildReporter : IPostBuildReporter
        {
            private string _path;
            public bool AnyErrors { get; set; }
            public postBuildReporter(string path) { _path = path; AnyErrors = false; }
            public void Error(string message, params string[] tokens) { AnyErrors = true; output("Error", message, tokens); }
            public void Warning(string message, params string[] tokens) { output("Warning", message, tokens); }

            public void Error(string message, string filename, int lineNumber, int? columnNumber = null)
            {
                AnyErrors = true;
                outputLine("Error", filename, columnNumber == null ? lineNumber.ToString() : $"{lineNumber},{columnNumber}", message);
            }

            public void Warning(string message, string filename, int lineNumber, int? columnNumber = null)
            {
                outputLine("Warning", filename, columnNumber == null ? lineNumber.ToString() : $"{lineNumber},{columnNumber}", message);
            }

            private void outputLine(string errorOrWarning, string filename, string lineOrLineAndColumn, string message)
            {
                Console.Error.WriteLine("{0}({1}): {2} CS9999: {3}", filename, lineOrLineAndColumn, errorOrWarning, message);
            }

            private void output(string errorOrWarning, string message, params string[] tokens)
            {
                if (tokens == null || tokens.Length == 0 || tokens.All(t => t == null))
                {
                    var frame = new StackFrame(2, true);
                    outputLine(errorOrWarning, frame.GetFileName(), $"{frame.GetFileLineNumber()},{frame.GetFileColumnNumber()}", message);
                    return;
                }
                try
                {
                    var tokenRegexes = tokens.Select(tok => tok == null ? null : new Regex(@"\b" + Regex.Escape(tok) + @"\b")).ToArray();
                    foreach (var f in new DirectoryInfo(_path).GetFiles("*.cs", SearchOption.AllDirectories))
                    {
                        var lines = File.ReadAllLines(f.FullName);
                        var tokenIndex = tokens.IndexOf(t => t != null);
                        for (int i = 0; i < lines.Length; i++)
                        {
                            Match match;
                            var charIndex = 0;
                            while ((match = tokenRegexes[tokenIndex].Match(lines[i], charIndex)).Success)
                            {
                                do { tokenIndex++; } while (tokenIndex < tokens.Length && tokens[tokenIndex] == null);
                                if (tokenIndex == tokens.Length)
                                {
                                    Console.Error.WriteLine(@"{0}({1},{2},{1},{3}): {4} CS9999: {5}",
                                        f.FullName,
                                        i + 1,
                                        match.Index + 1,
                                        match.Index + match.Length + 1,
                                        errorOrWarning,
                                        message
                                    );
                                    return;
                                }
                                charIndex = match.Index + match.Length;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine("Error: " + e.Message + " (" + e.GetType().FullName + ")");
                }
                Console.Error.WriteLine("{0} CS9999: {1}", errorOrWarning, message);
            }
        }
    }
}
