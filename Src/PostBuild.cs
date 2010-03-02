using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using RT.Util.ExtensionMethods;

namespace RT.Util
{
#if DEBUG
    /// <summary>Provides the ability to run arbitrary checks on the compiled assembly as a post-build step (see remarks for details).</summary>
    /// <remarks><para>Intended use is as follows:</para>
    /// <list type="bullet">
    ///    <item><description><para>Add the following line to your project's post-build event:</para>
    ///        <code>"$(TargetPath)" --post-build-check "$(SolutionDir)."</code></description></item>
    ///    <item><description><para>Add the following code at the beginning of your project's Main() method:</para>
    ///        <code>
    ///            #if DEBUG
    ///                if (args.Length == 2 &amp;&amp; args[0] == "--post-build-check")
    ///                    return PostBuild.PostBuildChecks(args[1], Assembly.GetExecutingAssembly());
    ///            #endif
    ///        </code>
    ///        <para>If your project entails several assemblies, you can specify additional assemblies in the call to <see cref="PostBuild.PostBuildChecks"/>.
    ///        For example, you could specify <c>typeof(SomeTypeInMyLibrary).Assembly</c>.</para>
    ///        </description></item>
    ///    <item><description><para>Add post-build check methods to any type where they may be relevent. For example, for a command-line program that uses <see cref="RT.Util.CommandLine.CommandLineParser&lt;T&gt;"/>,
    ///                                                     you might use code similar to the following:</para>
    ///        <code>
    ///            #if DEBUG
    ///                private static IEnumerable&lt;PostBuildError&gt; PostBuildCheck()
    ///                {
    ///                    // Replace "CommandLine" with the name of your command-line type, and "Translation" with the name of your translation type (<see cref="RT.Util.Lingo.TranslationBase"/>)
    ///                    return CommandLineParser&lt;CommandLine&gt;.PostBuildStep(typeof(Translation));
    ///                }
    ///            #endif
    ///        </code>
    ///        <para>The method is expected to have no parameters, the return type shown above, and it is expected to be static and non-public.</para>
    ///    </description></item>
    /// </list></remarks>
    public static class PostBuild
    {
        /// <summary>Runs all post-build checks defined in the specified assemblies.</summary>
        /// <param name="sourcePath">Specifies the path to the folder containing the C# source files.</param>
        /// <param name="assemblies">Specifies the compiled assemblies from which to run post-build checks.</param>
        /// <returns>1 if any errors occurred, otherwise 0.</returns>
        /// <remarks>A post-build check is a static, non-public method called PostBuildCheck() with no parameters and a return type
        /// of IEnumerable&lt;<see cref="PostBuildError"/>&gt;. Such a method is expected to return each error encountered as a
        /// <see cref="PostBuildError"/> object, or an empty collection if no errors are found.</remarks>
        public static int PostBuildChecks(string sourcePath, params Assembly[] assemblies)
        {
            bool anyError = false;
            foreach (var ty in assemblies.SelectMany(asm => asm.GetTypes()))
            {
                var meth = ty.GetMethod("PostBuildCheck", BindingFlags.NonPublic | BindingFlags.Static);
                if (meth != null)
                {
                    if (meth.GetParameters().Length == 0 && meth.ReturnType == typeof(IEnumerable<PostBuildError>))
                    {
                        try
                        {
                            foreach (var pbe in (IEnumerable<PostBuildError>) meth.Invoke(null, null))
                            {
                                anyError = true;
                                pbe.OutputToConsole(sourcePath);
                            }
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
                            anyError = true;
                        }
                    }
                    else
                        PostBuildError.SequentialFindTextAndOutputError(sourcePath, "class " + ty.Name, "PostBuildCheck", "Error: The type {0} has a method called PostBuildCheck() that is not of the expected signature. There should be no parameters, and the return type should be IEnumerable<{1}>.".Fmt(ty.FullName, typeof(PostBuildError).FullName));
                }
            }

            return anyError ? 1 : 0;
        }
    }

    /// <summary>Specifies the way in which the <see cref="PostBuildError.Tokens"/> are used to find a position in the source code.</summary>
    public enum PostBuildTokenType
    {
        /// <summary>Outputs a separate copy of the error message for each token, and only for the first occurrence of each token within the source code. This is the default.</summary>
        MultipleErrors,
        /// <summary>Finds the first occurrence of the first token, and then starts searching there to find the first occurrence of the second token within the same file. Outputs a single copy of the error message. There must be exactly two tokens.</summary>
        SequentialFind
    }

    /// <summary>Encapsulates information about an error discovered by the post-build check (<see cref="PostBuild.PostBuildChecks"/>).</summary>
    [Serializable]
    public class PostBuildError
    {
        /// <summary>Contains the error message.</summary>
        public string Message { get; private set; }

        /// <summary>Specifies any set of strings for which the source code will be searched in order to locate the relevant line(s) of code.</summary>
        public string[] Tokens { get; private set; }

        /// <summary>Specifies the way in which the <see cref="Tokens"/> are used to find a position in the source code.</summary>
        public PostBuildTokenType TokenType { get; private set; }

        /// <summary>Constructor.</summary>
        /// <param name="message">Error message.</param>
        /// <param name="tokens">Specifies any set of strings for which the source code will be searched in order to locate the relevant line(s) of code.</param>
        public PostBuildError(string message, params string[] tokens)
        {
            Message = message;
            TokenType = PostBuildTokenType.MultipleErrors; // default value
            Tokens = tokens;
        }

        /// <summary>Constructor.</summary>
        /// <param name="message">Error message.</param>
        /// <param name="tokenType">Specifies the way in which the <paramref name="tokens"/> parameter is used to find a position in the source code.</param>
        /// <param name="tokens">Specifies any set of strings for which the source code will be searched in order to locate the relevant line(s) of code.</param>
        public PostBuildError(string message, PostBuildTokenType tokenType, params string[] tokens)
        {
            if (tokenType == PostBuildTokenType.SequentialFind && tokens.Length != 2)
                throw new ArgumentException("If 'tokenType' is 'SequentialFind', 'tokens' must have exactly 2 elements.", "tokens");
            Message = message;
            TokenType = tokenType;
            Tokens = tokens;
        }

        /// <summary>Outputs the error message to Console.Error, including source file name and line number if any.</summary>
        /// <param name="path">Path to the source code in which to find the file and line number.</param>
        public void OutputToConsole(string path)
        {
            if (Tokens.Length == 0)
                Console.Error.WriteLine("Error: " + Message);
            else if (TokenType == PostBuildTokenType.SequentialFind)
                SequentialFindTextAndOutputError(path, Tokens[0], Tokens[1], "Error: " + Message);
            else
                foreach (var token in Tokens)
                    findTextAndOutputError(path, token);
        }

        private void findTextAndOutputError(string path, string textToFind)
        {
            try
            {
                foreach (var f in new DirectoryInfo(path).GetFiles("*.cs", SearchOption.AllDirectories))
                {
                    var lines = File.ReadAllLines(f.FullName);
                    for (int i = 0; i < lines.Length; i++)
                    {
                        var match = Regex.Match(lines[i], "\\b" + Regex.Escape(textToFind) + "\\b");
                        if (match.Success)
                        {
                            Console.Error.WriteLine(f.FullName + "(" + (i + 1) + "," + (match.Index + 1) + "): Error: " + Message);
                            return;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Error: " + e.Message + " (" + e.GetType().FullName + ") (" + path + ")");
            }
            Console.Error.WriteLine("Error: " + Message);
        }

        /// <summary>Outputs the specified <paramref name="errorMessage"/> (which should start with either "Error: " or "Warning: ") to Console.Error, possibly augmented with a source file name and line number.
        /// To determine that source file and line number, searches the C# source files in the specified path for <paramref name="text1"/>, and then searches from that point onwards for <paramref name="text2"/> within the same file.
        /// The first match is used, if any.</summary>
        public static void SequentialFindTextAndOutputError(string path, string text1, string text2, string errorMessage)
        {
            try
            {
                foreach (var f in new DirectoryInfo(path).GetFiles("*.cs", SearchOption.AllDirectories))
                {
                    var lines = File.ReadAllLines(f.FullName);
                    for (int i = 0; i < lines.Length; i++)
                        if (Regex.IsMatch(lines[i], "\\b" + Regex.Escape(text1) + "\\b"))
                        {
                            for (int j = i; j < lines.Length; j++)
                            {
                                var match = Regex.Match(lines[j], "\\b" + Regex.Escape(text2) + "\\b");
                                if (match.Success)
                                {
                                    Console.Error.WriteLine(f.FullName + "(" + (j + 1) + "," + (match.Index + 1) + "): " + errorMessage);
                                    return;
                                }
                            }
                            break;
                        }
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Error: " + e.Message + " (" + e.GetType().FullName + ") (" + path + ")");
            }
            Console.Error.WriteLine(errorMessage);
        }
    }
#endif
}
