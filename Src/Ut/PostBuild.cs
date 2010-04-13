using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using RT.Util.ExtensionMethods;

namespace RT.Util
{
#if DEBUG
    public static partial class Ut
    {
        /// <summary>Runs all post-build checks defined in the specified assemblies. This is intended to be run as a post-build event. See remarks for details.</summary>
        /// <remarks><para>Intended use is as follows:</para>
        /// <list type="bullet">
        ///    <item><description><para>Add the following line to your project's post-build event:</para>
        ///        <code>"$(TargetPath)" --post-build-check "$(SolutionDir)."</code></description></item>
        ///    <item><description><para>Add the following code at the beginning of your project's Main() method:</para>
        ///        <code>
        ///            #if DEBUG
        ///                if (args.Length == 2 &amp;&amp; args[0] == "--post-build-check")
        ///                    return Ut.RunPostBuildChecks(args[1], Assembly.GetExecutingAssembly());
        ///            #endif
        ///        </code>
        ///        <para>If your project entails several assemblies, you can specify additional assemblies in the call to <see cref="Ut.RunPostBuildChecks"/>.
        ///        For example, you could specify <c>typeof(SomeTypeInMyLibrary).Assembly</c>.</para>
        ///        </description></item>
        ///    <item><description><para>Add post-build check methods to any type where they may be relevant. For example, for a command-line program that uses <see cref="RT.Util.CommandLine.CommandLineParser&lt;T&gt;"/>,
        ///                                                     you might use code similar to the following:</para>
        ///        <code>
        ///            #if DEBUG
        ///                private static void PostBuildCheck(IPostBuildReporter rep)
        ///                {
        ///                    // Replace "CommandLine" with the name of your command-line type, and "Translation" with the name of your translation type (<see cref="RT.Util.Lingo.TranslationBase"/>)
        ///                    CommandLineParser&lt;CommandLine&gt;.PostBuildStep(rep, typeof(Translation));
        ///                }
        ///            #endif
        ///        </code>
        ///        <para>The method is expected to have one parameter of type <see cref="IPostBuildReporter"/>, a return type if void, and it is expected to be static and non-public.
        ///                    Errors are reported by calling methods on said <see cref="IPostBuildReporter"/> object.</para>
        ///    </description></item>
        /// </list></remarks>
        /// <param name="sourcePath">Specifies the path to the folder containing the C# source files.</param>
        /// <param name="assemblies">Specifies the compiled assemblies from which to run post-build checks.</param>
        /// <returns>1 if any errors occurred, otherwise 0.</returns>
        public static int RunPostBuildChecks(string sourcePath, params Assembly[] assemblies)
        {
            var rep = new PostBuildReporter(sourcePath);
            foreach (var ty in assemblies.SelectMany(asm => asm.GetTypes()))
            {
                var meth = ty.GetMethod("PostBuildCheck", BindingFlags.NonPublic | BindingFlags.Static);
                if (meth != null)
                {
                    if (meth.GetParameters().Select(p => p.ParameterType).SequenceEqual(new Type[] { typeof(IPostBuildReporter) }) && meth.ReturnType == typeof(void))
                    {
                        try
                        {
                            meth.Invoke(null, new object[] { rep });
                        }
                        catch (Exception e)
                        {
                            var realException = e;
                            while (realException is TargetInvocationException && realException.InnerException != null)
                                realException = realException.InnerException;

                            var st = new StackTrace(realException, true);
                            string fileLine = null;
                            if (st.FrameCount > 0)
                                fileLine = st.GetFrame(0).GetFileName() + "(" + st.GetFrame(0).GetFileLineNumber() + "," + st.GetFrame(0).GetFileColumnNumber() + "): ";

                            Console.Error.WriteLine(fileLine + "Error: " + realException.Message.Replace("\n", " ").Replace("\r", "") + " (" + realException.GetType().FullName + ")");
                        }
                    }
                    else
                        rep.Error(
                            "The type {0} has a method called PostBuildCheck() that is not of the expected signature. There should be one parameter of type {1}, and the return type should be void.".Fmt(ty.FullName, typeof(IPostBuildReporter).FullName),
                            (ty.IsValueType ? "struct " : "class ") + ty.Name, "PostBuildCheck");
                }
            }

            return rep.AnyErrors ? 1 : 0;
        }

        sealed class PostBuildReporter : IPostBuildReporter
        {
            private string _path;
            public bool AnyErrors { get; set; }
            public PostBuildReporter(string path) { _path = path; AnyErrors = false; }
            public void Error(string message, params string[] tokens) { output("Error: ", message, tokens); }
            public void Warning(string message, params string[] tokens) { output("Warning: ", message, tokens); }

            private void output(string errorOrWarning, string message, params string[] tokens)
            {
                AnyErrors = true;
                if (tokens == null || tokens.Length == 0)
                {
                    Console.Error.WriteLine(errorOrWarning + message);
                    return;
                }
                try
                {
                    foreach (var f in new DirectoryInfo(_path).GetFiles("*.cs", SearchOption.AllDirectories))
                    {
                        var lines = File.ReadAllLines(f.FullName);
                        var tokenIndex = 0;
                        for (int i = 0; i < lines.Length; i++)
                        {
                            var match = Regex.Match(lines[i], "\\b" + Regex.Escape(tokens[tokenIndex]) + "\\b");
                            if (match.Success)
                            {
                                tokenIndex++;
                                if (tokenIndex == tokens.Length)
                                {
                                    Console.Error.WriteLine(f.FullName + "(" + (i + 1) + "," + (match.Index + 1) + "): " + errorOrWarning + message);
                                    return;
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine("Error: " + e.Message + " (" + e.GetType().FullName + ")");
                }
                Console.Error.WriteLine(errorOrWarning + message);
            }
        }
    }

    /// <summary>Provides the ability to output post-build messages (with filename and line number) to Console.Error. This interface is used by <see cref="Ut.RunPostBuildChecks"/>.</summary>
    public interface IPostBuildReporter
    {
        /// <summary>When implemented in a class, finds the first occurrence of the first token in <paramref name="tokens"/>, and then starts searching there to find the first occurrence of each of the subsequent
        /// <paramref name="tokens"/> within the same file. When found, outputs the error <paramref name="message"/> including the filename and line number where the last token was found.</summary>
        void Error(string message, params string[] tokens);

        /// <summary>When implemented in a class, finds the first occurrence of the first token in <paramref name="tokens"/>, and then starts searching there to find the first occurrence of each of the subsequent
        /// <paramref name="tokens"/> within the same file. When found, outputs the warning <paramref name="message"/> including the filename and line number where the last token was found.</summary>
        void Warning(string message, params string[] tokens);
    }
#endif
}
