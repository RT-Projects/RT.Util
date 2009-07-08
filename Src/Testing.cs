using System;
using RT.Util.ExtensionMethods;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace RT.Util
{
    /// <summary>
    /// Provides utility methods used in unit testing.
    /// </summary>
    public static class Testing
    {
        /// <summary>
        /// Generates C# code to run all the tests contained in a specified list of types, then modifies an existing source file to contain this code.
        /// </summary>
        /// <param name="fpath">The path and filename of the source file where to store the generated C# code.</param>
        /// <param name="regionName">The name of a #region within the source file. If the source file does not contain a #region with this name, nothing happens.</param>
        /// <param name="testTypes">List of types to check for tests.</param>
        /// <param name="testFixtureAttribute">Type of custom attribute which designates a type as containing tests. Types contained in <paramref name="testTypes"/> which do not have this attribute are silently ignored.</param>
        /// <param name="testFixtureSetUpAttribute">Type of custom attribute which designates a method as a set-up method. The generated code calls these methods before running the tests contained in the relevant type.</param>
        /// <param name="testAttribute">Type of custom attribute which designates a method as a test.</param>
        /// <param name="testFixtureTearDownAttribute">Type of custom attribute which designates a method as a tear-down method. The generated code calls these methods after running the tests contained in the relevant type.</param>
        public static void GenerateTestingCode(string fpath, string regionName, IEnumerable<Type> testTypes, Type testFixtureAttribute, Type testFixtureSetUpAttribute, Type testAttribute, Type testFixtureTearDownAttribute)
        {
            string origCode = File.ReadAllText(fpath);
            var startMatch = Regex.Match(origCode, @"^(\s*)#region " + regionName + "\r?$", RegexOptions.Multiline);
            var endMatch = Regex.Match(origCode, @"^\s*#endregion\r?$", RegexOptions.Multiline);
            if (!startMatch.Success || !endMatch.Success)
                return;

            string newCode = origCode.Substring(0, startMatch.Index + startMatch.Length + 1);
            string indent = startMatch.Groups[1].Value;
            foreach (var ty in testTypes.Where(t => t.GetCustomAttributes(testFixtureAttribute, true).Any()).OrderBy(t => t.Name))
            {
                string varName = "do" + ty.Name;
                newCode += indent + "Console.WriteLine(\"\");\n";
                newCode += indent + "Console.WriteLine(\"Testing type: {0}\");\n".Fmt(ty.FullName);
                newCode += indent + "var {0} = new {1}();\n".Fmt(varName, ty.FullName);

                foreach (var meth in ty.GetMethods().Where(m => m.GetCustomAttributes(testFixtureSetUpAttribute, false).Any()))
                {
                    newCode += indent + "Console.WriteLine(\"-- Running setup: {0}\");\n".Fmt(meth.Name);
                    newCode += indent + "{0}.{1}();\n".Fmt(varName, meth.Name);
                }
                foreach (var meth in ty.GetMethods().Where(m => m.GetCustomAttributes(testAttribute, false).Any()))
                {
                    newCode += indent + "Console.WriteLine(\"-- Running test: {0}\");\n".Fmt(meth.Name);
                    newCode += indent + "{0}.{1}();\n".Fmt(varName, meth.Name);
                }
                foreach (var meth in ty.GetMethods().Where(m => m.GetCustomAttributes(testFixtureTearDownAttribute, false).Any()))
                {
                    newCode += indent + "Console.WriteLine(\"-- Running teardown: {0}\");\n".Fmt(meth.Name);
                    newCode += indent + "{0}.{1}();\n".Fmt(varName, meth.Name);
                }
            }
            newCode += origCode.Substring(endMatch.Index);
            if (newCode != origCode)
                File.WriteAllText(fpath, newCode);
        }
    }
}
