using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using System.Windows.Forms;
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
    public delegate void SetLanguage<TTranslation>(TTranslation translation) where TTranslation : TranslationBase;

    /// <summary>
    /// Static class with helper methods to support multi-language applications.
    /// </summary>
    public static class Lingo
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
            string path = PathUtil.Combine(Path.GetDirectoryName(Application.ExecutablePath), "Translations", module + "." + language.GetIsoLanguageCode() + ".xml");
            var trans = XmlClassify.LoadObjectFromXmlFile<TTranslation>(path);
            trans.Language = language;
            return trans;
        }

        /// <summary>
        /// Generates a list of <see cref="MenuItem"/>s for the user to select a language from. The list is generated from the set of available XML files in the application's directory.
        /// </summary>
        /// <typeparam name="TTranslation">The type in which translations are stored.</typeparam>
        /// <param name="moduleName">The name of the module. XML files considered valid translation files are those that match moduleName+"."+languagecode+".xml".</param>
        /// <param name="setLanguage">A callback function to call when the user clicks on a menu item. The first parameter to the callback function is the <typeparamref name="TTranslation"/>
        /// object for the selected language. The second parameter is the string identifying the language, or null for the application's native language.</param>
        /// <param name="curLanguage">The currently-selected language. (The relevant menu item is automatically checked.)</param>
        /// <returns>A <see cref="MenuItem"/>[] containing the generated menu items.</returns>
        public static MenuItem[] LanguageMenuItems<TTranslation>(string moduleName, SetLanguage<TTranslation> setLanguage, Language curLanguage) where TTranslation : TranslationBase, new()
        {
            MenuItem selected = null;

            var arr = languageMenuItems<TTranslation>(moduleName)
                .OrderBy(trn => trn.Language.GetNativeName())
                .Select(trn => new MenuItem(trn.Language.GetNativeName(), new EventHandler((snd, ev) =>
                {
                    try
                    {
                        var trInf = (translationInfo) ((MenuItem) snd).Tag;
                        setLanguage(trInf.IsDefault ? new TTranslation() : LoadTranslation<TTranslation>(moduleName, trInf.Language));
                        if (selected != null) selected.Checked = false;
                        selected = (MenuItem) snd;
                        selected.Checked = true;
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show("The specified translation could not be opened: " + e.Message);
                    }
                })) { Tag = trn, Checked = trn.Language == curLanguage }).ToArray();
            selected = arr.FirstOrDefault(m => ((translationInfo) m.Tag).Language == curLanguage);
            return arr;
        }

        /// <summary>
        /// Generates a list of <see cref="ToolStripMenuItem"/>s for the user to select a language from. The list is generated from the set of available XML files in the application's directory.
        /// </summary>
        /// <typeparam name="TTranslation">The type in which translations are stored.</typeparam>
        /// <param name="moduleName">The name of the program. XML files considered valid translation files are those that match moduleName+".&lt;languagecode&gt;.xml".</param>
        /// <param name="setLanguage">A callback function to call when the user clicks on a menu item. The first parameter to the callback function is the <typeparamref name="TTranslation"/>
        /// object for the selected language. The second parameter is the string identifying the language, or null for the application's native language.</param>
        /// <param name="curLanguage">The currently-selected language. (The relevant menu item is automatically checked.)</param>
        /// <returns>A <see cref="ToolStripMenuItem"/>[] containing the generated menu items.</returns>
        public static ToolStripMenuItem[] LanguageToolStripMenuItems<TTranslation>(string moduleName, SetLanguage<TTranslation> setLanguage, Language curLanguage) where TTranslation : TranslationBase, new()
        {
            ToolStripMenuItem selected = null;

            var arr = languageMenuItems<TTranslation>(moduleName)
                .OrderBy(trn => trn.Language.GetNativeName())
                .Select(trn => new ToolStripMenuItem(trn.Language.GetNativeName(), null, new EventHandler((snd, ev) =>
                {
                    try
                    {
                        var trInf = ((translationInfo) ((ToolStripMenuItem) snd).Tag);
                        setLanguage(trInf.IsDefault ? new TTranslation() : LoadTranslation<TTranslation>(moduleName, trInf.Language));
                        if (selected != null) selected.Checked = false;
                        selected = (ToolStripMenuItem) snd;
                        selected.Checked = true;
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show("The specified translation could not be opened: " + e.Message);
                    }
                })) { Tag = trn, Checked = trn.Language == curLanguage }).ToArray();
            selected = arr.FirstOrDefault(m => ((translationInfo) m.Tag).Language == curLanguage);
            return arr;
        }

        /// <summary>Writes the specified translation for the specified module to an XML file in the application directory.</summary>
        /// <typeparam name="TTranslation">Translation class to write.</typeparam>
        /// <param name="moduleName">Name of the module for which this is a translation.</param>
        /// <param name="translation">The translation to save.</param>
        public static void SaveTranslation<TTranslation>(string moduleName, TTranslation translation) where TTranslation : TranslationBase, new()
        {
            XmlClassify.SaveObjectToXmlFile(translation, PathUtil.Combine(Path.GetDirectoryName(Application.ExecutablePath), "Translations", moduleName + "." + translation.Language.GetIsoLanguageCode() + ".xml"));
        }

        private class translationInfo
        {
            public Language Language;
            public bool IsDefault;
        }

        private static IEnumerable<translationInfo> languageMenuItems<TTranslation>(string moduleName) where TTranslation : TranslationBase, new()
        {
            yield return new translationInfo { IsDefault = true, Language = new TTranslation().Language };
            var path = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "Translations");
            if (!Directory.Exists(path))
                yield break;
            foreach (var file in new DirectoryInfo(path).GetFiles(moduleName + ".*.xml"))
            {
                Match match = Regex.Match(file.Name, "^" + Regex.Escape(moduleName) + @"\.(.*)\.xml$");
                if (!match.Success) continue;
                var l = LanguageFromIsoCode(match.Groups[1].Value);
                if (l == null) continue;
                yield return new translationInfo { Language = l.Value, IsDefault = false };
            }
        }

        /// <summary>Returns the language from the <see cref="Language"/> enum which corresponds to the specified ISO code, or null if none matches.</summary>
        public static Language? LanguageFromIsoCode(string isoCode)
        {
            foreach (var f in typeof(Language).GetFields(BindingFlags.Static | BindingFlags.Public))
                if (f.GetCustomAttributes<LanguageInfoAttribute>().Any(l => l.LanguageCode == isoCode))
                    return (Language) f.GetValue(null);
            return null;
        }

        /// <summary>Translates the text of the specified control and all its sub-controls using the specified translation object.</summary>
        /// <param name="control">Control whose text is to be translated.</param>
        /// <param name="translation">Object containing the translations.</param>
        public static void TranslateControl(Control control, object translation)
        {
            translateControl(control, translation, null);
        }

        private class trStringInfo
        {
#if DEBUG
            public string Key;
            public string Translation;
            public string LingoNotes;
            public object[] LingoInGroups;
            public string Namespace;
            public void WriteTo(TextWriter f)
            {
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
                    f.WriteLine("        [" + attribs.JoinString(", ") + "]");
                f.WriteLine("        public TrString " + Key + " = \"" + Translation.CsharpEscape() + "\";");
            }
#endif
        }

#if DEBUG
        /// <summary>Translates the text of the specified control and all its sub-controls using the specified translation object.</summary>
        /// <param name="control">Control whose text is to be translated.</param>
        /// <param name="translation">Object containing the translations.</param>
        /// <param name="csFilename">A C# source file containing a definition for the translation strings required to translate this control is generated in the given path/filename.</param>
        public static void TranslateControl(Control control, object translation, string csFilename)
        {
            var lst = new List<trStringInfo>();
            translateControl(control, translation, lst);

            using (var file = File.Open(csFilename, FileMode.Create, FileAccess.Write, FileShare.Write))
            using (var f = new StreamWriter(file))
            {
                var ns = translation.GetType().Namespace;
                f.WriteLine("using RT.Util.Lingo;");
                f.WriteLine("");
                f.WriteLine("namespace " + ns);
                f.WriteLine("{");
                f.WriteLine("#if DEBUG");
                f.WriteLine("    [LingoAutoGenerated]");
                f.WriteLine("#endif");
                f.WriteLine("    [LingoStringClass" + translation.GetType().GetCustomAttributes<LingoInGroupAttribute>()
                    .Select(lig => ", LingoInGroup(" + (lig.Group.GetType().Namespace == ns ? lig.Group.GetType().Name : lig.Group.GetType().FullName) + "." + lig.Group.ToString() + ")").JoinString() + "]");
                f.WriteLine("    public partial class " + translation.GetType().Name);
                f.WriteLine("    {");
                if (lst.Any())
                    lst.First().WriteTo(f);
                foreach (var item in lst.Skip(1))
                {
                    f.WriteLine();
                    item.WriteTo(f);
                }
                f.WriteLine("    }");
                f.WriteLine("}");
            }
        }
#endif

        private static string translate(string key, object translation, string origText, object control, List<trStringInfo> generateFields)
        {
            var translationType = translation.GetType();

            FieldInfo field = translationType.GetField(key);
            if (field != null)
            {
#if DEBUG
                if (generateFields != null)
                {
                    generateFields.Add(new trStringInfo
                    {
                        Key = key,
                        Translation = origText,
                        LingoNotes = field.IsDefined<LingoNotesAttribute>() ? field.GetCustomAttributes<LingoNotesAttribute>().First().Notes : null,
                        LingoInGroups = field.GetCustomAttributes<LingoInGroupAttribute>().Select(lig => lig.Group).ToArray(),
                        Namespace = translation.GetType().Namespace
                    });
                }
#endif
                return field.GetValue(translation).ToString();
            }

            PropertyInfo property = translationType.GetProperty(key);
            if (property != null)
                return property.GetValue(translation, null).ToString();

            MethodInfo method = translationType.GetMethod(key, new Type[] { typeof(Control) });
            if (method != null)
                return method.Invoke(translation, new object[] { control }).ToString();

#if DEBUG
            if (generateFields != null)
            {
                generateFields.Add(new trStringInfo
                {
                    Key = key,
                    Translation = origText,
                    Namespace = translation.GetType().Namespace
                });
            }
#endif
            return null;
        }

        private static void translateControl(Control control, object translation, List<trStringInfo> generateFields)
        {
            if (control == null)
                return;

            if (!string.IsNullOrEmpty(control.Name))
            {
                if (!string.IsNullOrEmpty(control.Text) && (!(control.Tag is string) || ((string) control.Tag != "notranslate")))
                {
                    string translated = translate(control.Name, translation, control.Text, control, generateFields);
                    if (translated != null)
                        control.Text = translated;
                }
            }

            if (control is ToolStrip)
                foreach (ToolStripItem tsi in ((ToolStrip) control).Items)
                    translateToolStripItem(tsi, translation, generateFields);
            foreach (Control subcontrol in control.Controls)
                translateControl(subcontrol, translation, generateFields);
        }

        private static void translateToolStripItem(ToolStripItem tsi, object translation, List<trStringInfo> generateFields)
        {
            if (!string.IsNullOrEmpty(tsi.Name))
            {
                if (!string.IsNullOrEmpty(tsi.Text) && (!(tsi.Tag is string) || ((string) tsi.Tag != "notranslate")))
                {
                    string translated = translate(tsi.Name, translation, tsi.Text, tsi, generateFields);
                    if (translated != null)
                        tsi.Text = translated;
                }
            }
            if (tsi is ToolStripDropDownItem)
            {
                foreach (ToolStripItem subitem in ((ToolStripDropDownItem) tsi).DropDownItems)
                    translateToolStripItem(subitem, translation, generateFields);
            }
        }

#if DEBUG
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

        /// <summary>Returns a list of translation-string fields (<see cref="TrString"/> or <see cref="TrStringNum"/>) which are not referenced in any of the IL code in the specified assembly or assemblies. 
        /// Returns only those unused fields from the specified type as well as types referenced by that type that have the <see cref="LingoStringClassAttribute"/>.</summary>
        /// <param name="type">Top-level translation-string type whose fields to examine.</param>
        /// <param name="assemblies">Collection of assemblies whose IL code to examine.</param>
        public static IEnumerable<FieldInfo> FindUnusedStrings(Type type, IEnumerable<Assembly> assemblies)
        {
            var fields = allTrStringFields(type).ToList();
            var reader = new ilReader();

            foreach (var mod in assemblies.SelectMany(a => a.GetModules(false)))
            {
                foreach (var meth in mod.GetTypes().SelectMany(t =>
                    t.GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance).Where(m => m.DeclaringType == t).Cast<MethodBase>().Concat(
                    t.GetConstructors(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance).Where(c => c.DeclaringType == t).Cast<MethodBase>())))
                {
                    foreach (var instr in reader.ReadIL(meth))
                    {
                        if (instr.OpCode == OpCodes.Ldfld || instr.OpCode == OpCodes.Ldflda)
                        {
                            var field = mod.ResolveField((int) instr.Argument.Value);
                            if (field != null)
                                fields.Remove(field);
                        }
                    }
                }
            }

            return fields;
        }

        private static IEnumerable<FieldInfo> allTrStringFields(Type type)
        {
            var fields = type.GetAllFields().Where(f => f.FieldType == typeof(TrString) || f.FieldType == typeof(TrStringNum));
            foreach (var nested in type.GetAllFields().Where(f => f.FieldType.IsDefined<LingoStringClassAttribute>() && !f.FieldType.IsDefined<LingoAutoGeneratedAttribute>()))
                fields = fields.Concat(allTrStringFields(nested.FieldType));
            return fields;
        }

        private class ilReader
        {
            public class Instruction
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

#endif
    }
}
