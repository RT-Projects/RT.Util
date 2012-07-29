using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Windows.Forms;
using RT.Util.Dialogs;
using RT.Util.ExtensionMethods;
using RT.Util.Xml;

namespace RT.Util.Lingo
{
    /// <summary>
    /// Used by any function which may change the language of the program. The program must
    /// change the UI language in this callback and remember to use that language after restarting.
    /// </summary>
    /// <typeparam name="TTranslation">The type of the translation class.</typeparam>
    /// <param name="translation">The new translation to be used by the program.</param>
    public delegate void SetLanguage<in TTranslation>(TTranslation translation) where TTranslation : TranslationBase;

    /// <summary>
    /// Static class with helper methods to support multi-language applications.
    /// </summary>
    public static partial class Lingo
    {
        /// <summary>Attempts to load the translation for the specified module and language. The translation must exist in the application 
        /// executable directory under a subdirectory called "Translations". If the translation loaded successfully, returns the translation instance.
        /// Otherwise, the <typeparamref name="TTranslation"/> default constructor is called and the result returned, and
        /// <paramref name="language"/> is set to the default language returned by this constructor.</summary>
        /// <typeparam name="TTranslation">The type of the translation class to load the translation into.</typeparam>
        /// <param name="module">The name of the module whose translation to load.</param>
        /// <param name="language">The language code of the language to load. This is set to the default language if the specified language cannot be loaded.</param>
        /// <returns>The loaded or default translation.</returns>
        public static TTranslation LoadTranslationOrDefault<TTranslation>(string module, ref Language language) where TTranslation : TranslationBase, new()
        {
            try
            {
                return LoadTranslation<TTranslation>(module, language);
            }
            catch (Exception)
            {
                var trn = new TTranslation();
                language = trn.Language;
                return trn;
            }
        }

        /// <summary>Loads and returns the translation for the specified module and language. The translation must exist in the application 
        /// executable directory under a subdirectory called "Translations". If the translation cannot be loaded successfully, an exception is thrown.</summary>
        /// <typeparam name="TTranslation">The type of the translation class to load the translation into.</typeparam>
        /// <param name="module">The name of the module whose translation to load.</param>
        /// <param name="language">The language code of the language to load.</param>
        /// <returns>The loaded translation.</returns>
        public static TTranslation LoadTranslation<TTranslation>(string module, Language language) where TTranslation : TranslationBase, new()
        {
            string path = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "Translations", module + "." + language.GetIsoLanguageCode() + ".xml");
            var trans = XmlClassify.LoadObjectFromXmlFile<TTranslation>(path);
            trans.Language = language;
            return trans;
        }

        /// <summary>Writes the specified translation for the specified module to an XML file in the application directory.</summary>
        /// <typeparam name="TTranslation">Translation class to write.</typeparam>
        /// <param name="moduleName">Name of the module for which this is a translation.</param>
        /// <param name="translation">The translation to save.</param>
        public static void SaveTranslation<TTranslation>(string moduleName, TTranslation translation) where TTranslation : TranslationBase, new()
        {
            XmlClassify.SaveObjectToXmlFile(translation, Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "Translations", moduleName + "." + translation.Language.GetIsoLanguageCode() + ".xml"));
        }

        /// <summary>Returns the language from the <see cref="Language"/> enum which corresponds to the specified ISO code, or null if none matches.</summary>
        public static Language? LanguageFromIsoCode(string isoCode)
        {
            foreach (var f in typeof(Language).GetFields(BindingFlags.Static | BindingFlags.Public))
                if (f.GetCustomAttributes<LanguageInfoAttribute>().Any(l => l.LanguageCode == isoCode))
                    return (Language) f.GetValue(null);
            return null;
        }

        private sealed class trStringInfo
        {
            public bool NoTranslate;
            public string FieldName;
            public string Translation;
            public string LingoNotes;
            public object[] LingoInGroups;
            public string Namespace;
            public void AppendTo(StringBuilder sb)
            {
                if (FieldName == null || NoTranslate)
                {
                    sb.AppendLine("        // The following control was not added because {0}: {1}".Fmt(NoTranslate ? "its tag is \"notranslate\"" : "it has no name", Translation));
                    return;
                }

                sb.AppendLine("        [LingoAutoGenerated]");
                var attribs = new List<string>();
                if (LingoInGroups != null)
                {
                    foreach (var inGroup in LingoInGroups)
                    {
                        var fn = inGroup.GetType().Namespace == Namespace ? inGroup.GetType().Name : inGroup.GetType().FullName;
                        attribs.Add("LingoInGroup(" + fn + "." + inGroup.ToString() + ")");
                    }
                }
                if (LingoNotes != null)
                    attribs.Add("LingoNotes(\"" + LingoNotes.CsharpEscape() + "\")");
                if (attribs.Count > 0)
                    sb.AppendLine("        [" + attribs.JoinString(", ") + "]");
                sb.AppendLine("        public TrString " + FieldName + " = \"" + Translation.CsharpEscape() + "\";");
            }
        }

        /// <summary>Contains utility methods to generate C# code for translation strings required for automated translation of forms and controls.</summary>
        public sealed partial class TranslationFileGenerator : IDisposable
        {
            private string _filename;
            private Dictionary<string, List<string>> _codeSoFar;

            /// <summary>Constructor.</summary>
            /// <param name="filename">Path and filename where to store the auto-generated code.</param>
            public TranslationFileGenerator(string filename)
            {
                _filename = filename;
                _codeSoFar = new Dictionary<string, List<string>>();
            }

            /// <summary>Translates the text of the specified control and all its sub-controls using the specified translation object, while simultaneously generating C# code for the required translation strings.</summary>
            /// <param name="control">Control whose text is to be translated.</param>
            /// <param name="translation">Object containing the translations.</param>
            public void TranslateControl(Control control, object translation)
            {
                if (control == null)
                    throw new ArgumentNullException("control");
                if (translation == null)
                    throw new ArgumentNullException("translation");

                var lst = new List<trStringInfo>();
                translateControl(control, translation, lst);
                generateCode(control, translation, lst);
            }

            /// <summary>Translates the text of the specified WPF window and all its sub-controls using the specified translation object, while simultaneously generating C# code for the required translation strings.</summary>
            /// <param name="window">Window whose controls are to be translated.</param>
            /// <param name="translation">Object containing the translations.</param>
            public void TranslateWindow(System.Windows.Window window, object translation)
            {
                if (window == null)
                    throw new ArgumentNullException("window");
                if (translation == null)
                    throw new ArgumentNullException("translation");

                var lst = new List<trStringInfo>();
                translateControlWpf(window, translation, lst);
                generateCode(window, translation, lst);
            }

            private void generateCode(object control, object translation, List<trStringInfo> lst)
            {
                var ns = translation.GetType().Namespace;
                if (!_codeSoFar.ContainsKey(ns))
                    _codeSoFar[ns] = new List<string>();

                var sb = new StringBuilder();
                sb.AppendLine("    [LingoStringClass" + translation.GetType().GetCustomAttributes<LingoInGroupAttribute>()
                    .Select(lig => ", LingoInGroup(" + (lig.Group.GetType().Namespace == ns ? lig.Group.GetType().Name : lig.Group.GetType().FullName) + "." + lig.Group.ToString() + ")").JoinString() + "]");
                sb.AppendLine("    sealed partial class " + translation.GetType().Name);
                sb.AppendLine("    {");
                bool first = true;
                foreach (var item in lst)
                {
                    if (!first)
                        sb.AppendLine();
                    first = false;
                    item.AppendTo(sb);
                }
                sb.AppendLine("    }");
                _codeSoFar[ns].Add(sb.ToString());
            }

            /// <summary>Generates the file.</summary>
            public void Dispose()
            {
                if (!Directory.Exists(Path.GetDirectoryName(_filename)))
                    // silently ignore this error because we might be running the Debug build in some other environment
                    return;

                var f = new StringBuilder();

                f.AppendLine("using RT.Util.Lingo;");
                f.AppendLine("");
                bool first = true;
                foreach (var kvp in _codeSoFar)
                {
                    if (!first)
                        f.AppendLine();
                    first = false;
                    f.AppendLine("namespace " + kvp.Key);
                    f.AppendLine("{");
                    bool first2 = true;
                    foreach (var val in kvp.Value)
                    {
                        if (!first2)
                            f.AppendLine();
                        first2 = false;
                        f.Append(val);
                    }
                    f.AppendLine("}");
                }

                var newOutput = f.ToString();
                var prevOutput = File.Exists(_filename) ? File.ReadAllText(_filename) : string.Empty;
                string filenameGenerated = null;

                if (newOutput != prevOutput)
                {
                    // Replace the destination file with the newly generated stuff, but only if there are any differences.
                    int choice;
                    while ((choice = DlgMessage.ShowWarning("The file \"{0}\" will be modified by Lingo.".Fmt(Path.GetFullPath(_filename)), "&OK", "&Diff", "&Break")) != 0)
                    {
                        if (choice == 1)
                        {
                            if (Environment.GetEnvironmentVariable("LINGO_DIFF") == null)
                                DlgMessage.ShowInfo("The \"LINGO_DIFF\" environment variable is not defined. To enable diffing, set it to a full path to your preferred diff program.");
                            else
                            {
                                filenameGenerated = Path.GetTempFileName();
                                File.WriteAllText(filenameGenerated, newOutput);
                                Process.Start(Environment.GetEnvironmentVariable("LINGO_DIFF"), @"""{0}"" ""{1}""".Fmt(_filename, filenameGenerated));
                            }
                        }
                        else if (choice == 2)
                            Debugger.Break();
                    }
                    if (File.Exists(_filename))
                        File.SetAttributes(_filename, File.GetAttributes(_filename) & ~FileAttributes.ReadOnly);
                    File.WriteAllText(_filename, newOutput);
                    if (filenameGenerated != null)
                        File.Delete(filenameGenerated);
                    // Warn the user that some things may not behave properly until rebuild (e.g. newly generated strings won't show in translation UI yet).
                    DlgMessage.ShowWarning("The file \"{0}\" has been modified by Lingo.\n\nPlease rebuild the application to avoid unexpected behaviour, or click \"OK\" at your own risk.".Fmt(Path.GetFullPath(_filename)));
                }
            }
        }

        private static string translate(string key, object tag, object translation, string origText, object control, List<trStringInfo> generateFields)
        {
            if (string.IsNullOrWhiteSpace(key) || "notranslate".Equals(tag))
            {
                if (generateFields != null)
                    generateFields.Add(new trStringInfo { FieldName = null, Translation = origText, NoTranslate = "notranslate".Equals(tag) });
                return null;
            }

            var translationType = translation.GetType();

            FieldInfo field = translationType.GetField(key);
            if (field != null)
            {
                if (generateFields != null)
                {
                    generateFields.Add(new trStringInfo
                    {
                        FieldName = key,
                        Translation = origText,
                        LingoNotes = field.IsDefined<LingoNotesAttribute>() ? field.GetCustomAttributes<LingoNotesAttribute>().First().Notes : null,
                        LingoInGroups = field.GetCustomAttributes<LingoInGroupAttribute>().Select(lig => lig.Group).ToArray(),
                        Namespace = translation.GetType().Namespace
                    });
                }
                return field.GetValue(translation).ToString();
            }

            PropertyInfo property = translationType.GetProperty(key);
            if (property != null)
                return property.GetValue(translation, null).ToString();

            MethodInfo method = translationType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(m => m.Name == key && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType.IsAssignableFrom(control.GetType()));
            if (method != null)
                return method.Invoke(translation, new object[] { control }).ToString();

            if (generateFields != null)
            {
                generateFields.Add(new trStringInfo
                {
                    FieldName = key,
                    Translation = origText,
                    Namespace = translation.GetType().Namespace
                });
            }
            return null;
        }

        /// <summary>Translates the text of the specified control and all its sub-controls using the specified translation object.</summary>
        /// <param name="control">Control whose text is to be translated.</param>
        /// <param name="translation">Object containing the translations.</param>
        public static void TranslateControl(Control control, object translation)
        {
            if (control == null)
                throw new ArgumentNullException("control");
            if (translation == null)
                throw new ArgumentNullException("translation");

            translateControl(control, translation, null);
        }

        private static void translateControl(Control control, object translation, List<trStringInfo> generateFields)
        {
            if (control == null)
                return;

            if (!string.IsNullOrEmpty(control.Name) && !string.IsNullOrEmpty(control.Text) && !"notranslate".Equals(control.Tag))
            {
                string translated = translate(control.Name, control.Tag, translation, control.Text, control, generateFields);
                if (translated != null)
                    control.Text = translated;
            }

            if (control is ToolStrip)
                foreach (ToolStripItem tsi in ((ToolStrip) control).Items)
                    translateToolStripItem(tsi, translation, generateFields);
            foreach (Control subcontrol in control.Controls)
                translateControl(subcontrol, translation, generateFields);
        }

        private static void translateToolStripItem(ToolStripItem tsi, object translation, List<trStringInfo> generateFields)
        {
            if (!string.IsNullOrEmpty(tsi.Name) && !string.IsNullOrEmpty(tsi.Text) && !"notranslate".Equals(tsi.Tag))
            {
                string translated = translate(tsi.Name, tsi.Tag, translation, tsi.Text, tsi, generateFields);
                if (translated != null)
                    tsi.Text = translated;
            }
            if (tsi is ToolStripDropDownItem)
            {
                foreach (ToolStripItem subitem in ((ToolStripDropDownItem) tsi).DropDownItems)
                    translateToolStripItem(subitem, translation, generateFields);
            }
        }

        private static string CsharpEscape(this string str)
        {
            return str
                .Replace("\\", "\\\\")
                .Replace("'", "\\'")
                .Replace("\"", "\\\"")
                .Replace("\0", "\\0")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n")
                .Replace("\a", "\\a")
                .Replace("\b", "\\b")
                .Replace("\f", "\\f")
                .Replace("\t", "\\t")
                .Replace("\v", "\\v");
        }

        /// <summary>Outputs a <see cref="DlgMessage"/> containing a list of translation-string fields (<see cref="TrString"/> or <see cref="TrStringNum"/>) which are not referenced in any of the IL code in the specified assembly or assemblies. 
        /// Displays only those unused fields from the specified type as well as types referenced by that type that have the <see cref="LingoStringClassAttribute"/>.</summary>
        /// <param name="type">Top-level translation-string type whose fields to examine.</param>
        /// <param name="assemblies">Collection of assemblies whose IL code to examine.</param>
        public static void WarnOfUnusedStrings(Type type, params Assembly[] assemblies)
        {
            var fields = allTrStringFields(type).ToList();
            var reader = new ilReader();

            foreach (var mod in assemblies.SelectMany(a => a.GetModules(false)))
            {
                foreach (var typ in mod.GetTypes())
                {
                    foreach (var meth in
                        typ.GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance).Where(m => m.DeclaringType == typ).Cast<MethodBase>().Concat(
                        typ.GetConstructors(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance).Where(c => c.DeclaringType == typ).Cast<MethodBase>()))
                    {
                        foreach (var instr in reader.ReadIL(meth))
                        {
                            if (instr.OpCode == OpCodes.Ldfld || instr.OpCode == OpCodes.Ldflda)
                            {
                                var field = mod.ResolveField((int) instr.Argument.Value, typ.GetGenericArguments(), meth.IsConstructor ? Type.EmptyTypes : meth.GetGenericArguments());
                                if (field != null)
                                {
                                    fields.Remove(field);
                                    if (fields.Count == 0)
                                        return;
                                }
                            }
                        }
                    }
                }
            }

            if (DlgMessage.ShowWarning("Unused strings found:\n\n • " + fields.Select(f => f.DeclaringType.FullName + "." + f.Name).JoinString("\n • "), "Ignore", "Break") == 1)
                Debugger.Break();
        }

        private static IEnumerable<FieldInfo> allTrStringFields(Type type)
        {
            var fields = type.GetAllFields().Where(f => (f.FieldType == typeof(TrString) || f.FieldType == typeof(TrStringNum)) && !f.IsDefined<LingoAutoGeneratedAttribute>());
            foreach (var nested in type.GetAllFields().Where(f => f.FieldType.IsDefined<LingoStringClassAttribute>()))
                fields = fields.Concat(allTrStringFields(nested.FieldType));
            return fields;
        }

        private sealed class ilReader
        {
            public sealed class Instruction
            {
                public int StartOffset { get; private set; }
                public OpCode OpCode { get; private set; }
                public long? Argument { get; private set; }
                public Instruction(int startOffset, OpCode opCode, long? argument)
                {
                    StartOffset = startOffset;
                    OpCode = opCode;
                    Argument = argument;
                }
                public override string ToString()
                {
                    return OpCode.ToString() + (Argument == null ? string.Empty : " " + Argument.Value);
                }
            }

            private Dictionary<short, OpCode> _opCodeList;

            public ilReader()
            {
                _opCodeList = typeof(OpCodes).GetFields().Where(f => f.FieldType == typeof(OpCode)).Select(f => (OpCode) f.GetValue(null)).ToDictionary(o => o.Value);
            }

            public IEnumerable<Instruction> ReadIL(MethodBase method)
            {
                MethodBody body = method.GetMethodBody();
                if (body == null)
                    yield break;

                int offset = 0;
                byte[] il = body.GetILAsByteArray();
                while (offset < il.Length)
                {
                    int startOffset = offset;
                    byte opCodeByte = il[offset];
                    short opCodeValue = opCodeByte;
                    offset++;

                    // If it's an extended opcode then grab the second byte. The 0xFE prefix codes aren't marked as prefix operators though.
                    if (opCodeValue == 0xFE || _opCodeList[opCodeValue].OpCodeType == OpCodeType.Prefix)
                    {
                        opCodeValue = (short) ((opCodeValue << 8) + il[offset]);
                        offset++;
                    }

                    OpCode code = _opCodeList[opCodeValue];

                    Int64? argument = null;

                    int argumentSize = 4;
                    if (code.OperandType == OperandType.InlineNone)
                        argumentSize = 0;
                    else if (code.OperandType == OperandType.ShortInlineBrTarget || code.OperandType == OperandType.ShortInlineI || code.OperandType == OperandType.ShortInlineVar)
                        argumentSize = 1;
                    else if (code.OperandType == OperandType.InlineVar)
                        argumentSize = 2;
                    else if (code.OperandType == OperandType.InlineI8 || code.OperandType == OperandType.InlineR)
                        argumentSize = 8;
                    else if (code.OperandType == OperandType.InlineSwitch)
                    {
                        long num = il[offset] + (il[offset + 1] << 8) + (il[offset + 2] << 16) + (il[offset + 3] << 24);
                        argumentSize = (int) (4 * num + 4);
                    }

                    if (argumentSize > 0)
                    {
                        Int64 arg = 0;
                        for (int i = 0; i < argumentSize; ++i)
                        {
                            Int64 v = il[offset + i];
                            arg += v << (i * 8);
                        }
                        argument = arg;
                        offset += argumentSize;
                    }

                    yield return new Instruction(startOffset, code, argument);
                }
            }
        }
    }
}
