using System;
using RT.Util.ExtensionMethods;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace RT.Util
{
    public static class Testing
    {
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
