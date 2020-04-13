using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Windows.Forms;
using RT.PostBuild;
using RT.Serialization;
using RT.Util.Forms;
using RT.Util.ExtensionMethods;
using RT.Util.IL;

namespace RT.Util.Lingo
{
    /// <summary>Used for functions which change the language of the program.</summary>
    /// <typeparam name="TTranslation">The type of the translation class.</typeparam>
    /// <param name="translation">The new translation to be used by the program.</param>
    public delegate void SetTranslation<in TTranslation>(TTranslation translation) where TTranslation : TranslationBase;

    /// <summary>
    /// Static class with helper methods to support multi-language applications.
    /// </summary>
    public static partial class Lingo
    {
        /// <summary>
        /// If not null, whenever a translation is saved, Lingo will also attempt to save it in this directory. Use an absolute path. Lingo will
        /// quietly ignore any errors when saving here, and will do nothing if the path is missing. Lingo will overwrite read-only files without prompts.
        /// </summary>
        public static string AlsoSaveTranslationsTo = null;

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
            return (TTranslation) LoadTranslation(typeof(TTranslation), module, language);
        }

        /// <summary>Loads and returns the translation for the specified module and language. The translation must exist in the application 
        /// executable directory under a subdirectory called "Translations". If the translation cannot be loaded successfully, an exception is thrown.</summary>
        /// <param name="translationType">The type of the translation class to load the translation into.</param>
        /// <param name="module">The name of the module whose translation to load.</param>
        /// <param name="language">The language code of the language to load.</param>
        /// <returns>The loaded translation.</returns>
        public static TranslationBase LoadTranslation(Type translationType, string module, Language language)
        {
            string path = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "Translations", module + "." + language.GetIsoLanguageCode() + ".xml");
            var trans = (TranslationBase) ClassifyXml.DeserializeFile(translationType, path);
            trans.Language = language;
            return trans;
        }

        /// <summary>Writes the specified translation for the specified module to an XML file in the application directory.</summary>
        /// <typeparam name="TTranslation">Translation class to write.</typeparam>
        /// <param name="moduleName">Name of the module for which this is a translation.</param>
        /// <param name="translation">The translation to save.</param>
        public static void SaveTranslation<TTranslation>(string moduleName, TTranslation translation) where TTranslation : TranslationBase, new()
        {
            SaveTranslation(typeof(TTranslation), moduleName, translation);
        }

        /// <summary>Writes the specified translation for the specified module to an XML file in the application directory.</summary>
        /// <param name="translationType">Translation class to write.</param>
        /// <param name="moduleName">Name of the module for which this is a translation.</param>
        /// <param name="translation">The translation to save.</param>
        public static void SaveTranslation(Type translationType, string moduleName, TranslationBase translation)
        {
            ClassifyXml.SerializeToFile(translationType, translation, Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "Translations", moduleName + "." + translation.Language.GetIsoLanguageCode() + ".xml"));
            if (AlsoSaveTranslationsTo != null && Directory.Exists(AlsoSaveTranslationsTo))
            {
                try
                {
                    var filename = Path.Combine(AlsoSaveTranslationsTo, moduleName + "." + translation.Language.GetIsoLanguageCode() + ".xml");
                    File.SetAttributes(filename, File.GetAttributes(filename) & ~FileAttributes.ReadOnly);
                    ClassifyXml.SerializeToFile(translationType, translation, filename);
                }
                catch { }
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
                    sb.AppendLine("        // The following control was not added because {0}: {1}".Fmt(NoTranslate ? "its tag is \"notranslate\"" : "it has no name", Translation.CLiteralEscape()));
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
                    attribs.Add("LingoNotes(\"" + LingoNotes.CLiteralEscape() + "\")");
                if (attribs.Count > 0)
                    sb.AppendLine("        [" + attribs.JoinString(", ") + "]");
                sb.AppendLine("        public TrString " + FieldName + " = \"" + Translation.CLiteralEscape() + "\";");
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
                    throw new ArgumentNullException(nameof(control));
                if (translation == null)
                    throw new ArgumentNullException(nameof(translation));

                var lst = new List<trStringInfo>();
                translateControl(control, translation, lst);
                generateCode(translation, lst);
            }

            /// <summary>Translates the text of the specified WPF window and all its sub-controls using the specified translation object, while simultaneously generating C# code for the required translation strings.</summary>
            /// <param name="window">Window whose controls are to be translated.</param>
            /// <param name="translation">Object containing the translations.</param>
            public void TranslateWindow(System.Windows.Window window, object translation)
            {
                if (window == null)
                    throw new ArgumentNullException(nameof(window));
                if (translation == null)
                    throw new ArgumentNullException(nameof(translation));

                var lst = new List<trStringInfo>();
                translateControlWpf(window, translation, lst);
                generateCode(translation, lst);
            }

            private void generateCode(object translation, List<trStringInfo> lst)
            {
                var ns = translation.GetType().Namespace;
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
                _codeSoFar.AddSafe(ns, sb.ToString());
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
                throw new ArgumentNullException(nameof(control));
            if (translation == null)
                throw new ArgumentNullException(nameof(translation));

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

            if (control is MenuStrip || control is ContextMenuStrip)
            {
                translateMenu(((ToolStrip) control).Items, () =>
                {
                    foreach (ToolStripItem tsi in ((ToolStrip) control).Items)
                        translateToolStripItem(tsi, translation, generateFields);
                });
            }
            else if (control is ToolStrip)
            {
                foreach (ToolStripItem tsi in ((ToolStrip) control).Items)
                    translateToolStripItem(tsi, translation, generateFields);
            }
            foreach (Control subcontrol in control.Controls)
                translateControl(subcontrol, translation, generateFields);
        }

        private static void translateMenu(ToolStripItemCollection items, Action action)
        {
#if DEBUG
            var toRemove = items.Cast<ToolStripItem>().Where(tsi => object.Equals(tsi.Tag, "dup/av")).ToArray();
            foreach (var item in toRemove)
                items.Remove(item);
#endif
            action();
#if DEBUG
            var used = items.Cast<ToolStripItem>().Where(tsi => tsi.Text.Contains('&')).Select(tsi => tsi.Text[tsi.Text.IndexOf('&') + 1]).GroupBy(c => char.ToLowerInvariant(c)).Select(gr => new { Ch = gr.Key, Count = gr.Count() }).ToArray();
            if (used.Any(i => i.Count > 1) || items.Cast<ToolStripItem>().Any(tsi => object.Equals(tsi.Tag, "no_hotkey")))
                items.Add(new ToolStripMenuItem($"duplicates: {used.Where(i => i.Count > 1).Select(i => i.Ch).Order().JoinString().Apply(s => s.Length == 0 ? "(none)" : s)}; available: {"abcdefghijklmnopqrstuvwxyz".Except(used.Select(i => i.Ch)).Order().JoinString()}") { Tag = "dup/av" });
#endif
        }

        private static void translateToolStripItem(ToolStripItem tsItem, object translation, List<trStringInfo> generateFields)
        {
            if (!string.IsNullOrEmpty(tsItem.Name) && !string.IsNullOrEmpty(tsItem.Text) && !"notranslate".Equals(tsItem.Tag))
            {
                string translated = translate(tsItem.Name, tsItem.Tag, translation, tsItem.Text, tsItem, generateFields);
                if (translated != null)
                {
                    tsItem.Text = translated;
                    if (!translated.Contains('&'))
                    {
                        tsItem.Text = "[!X] " + translated;
                        tsItem.Tag = "no_hotkey";
                    }
                }
            }
            if (tsItem is ToolStripDropDownItem)
            {
                var items = ((ToolStripDropDownItem) tsItem).DropDownItems;
                if (items.Count > 0)
                {
                    translateMenu(items, () =>
                    {
                        foreach (ToolStripItem subitem in items)
                            translateToolStripItem(subitem, translation, generateFields);
                    });
                }
            }
        }

        /// <summary>Checks the specified assemblies for any obvious Lingo-related problems, including unused strings, mismatched enum translations.</summary>
        /// <typeparam name="TTranslation">The type of the translation class.</typeparam>
        /// <param name="rep">Post-build step reporter.</param>
        /// <param name="assemblies">A list of assemblies to check. The Lingo assembly is included automatically to ensure correct operation.</param>
        public static void PostBuildStep<TTranslation>(IPostBuildReporter rep, params Assembly[] assemblies)
        {
            if (!assemblies.Contains(Assembly.GetExecutingAssembly()))
                assemblies = assemblies.Concat(Assembly.GetExecutingAssembly()).ToArray();

            // Check that all enum translations are sensible
            var allEnumTrs = allEnumTranslations(assemblies).ToList();
            foreach (var tr in allEnumTrs)
                checkEnumTranslation(rep, tr.EnumType, tr.TranslationType);

            // Check all component model member translations
            foreach (var type in assemblies.SelectMany(a => a.GetTypes()))
            {
                // All functions returning MemberTr and accepting a TranslationBase descendant must conform
                var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(p => p.Name).ToHashSet();
                foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                    if (method.ReturnType == typeof(MemberTr) && method.GetParameters().Length == 1 && typeof(TranslationBase).IsAssignableFrom(method.GetParameters()[0].ParameterType))
                    {
                        if (!method.IsStatic)
                            rep.Error("A member translation method must be static. Translation method: {0}".Fmt(method.DeclaringType.FullName + "." + method.Name), "class " + method.DeclaringType.Name, typeof(MemberTr).Name + " " + method.Name);
                        if (!method.Name.EndsWith("Tr"))
                            rep.Error("The name of a member translation method must end with the letters \"Tr\". Translation method: {0}".Fmt(method.DeclaringType.FullName + "." + method.Name), "class " + method.DeclaringType.Name, typeof(MemberTr).Name + " " + method.Name);
                        var propertyName = method.Name.Substring(0, method.Name.Length - 2);
                        if (!properties.Contains(propertyName))
                            rep.Error("Member translation method has no corresponding property named \"{1}\". Translation method: {0}".Fmt(method.DeclaringType.FullName + "." + method.Name, propertyName), "class " + method.DeclaringType.Name, typeof(MemberTr).Name + " " + method.Name);
                    }
            }

            // Find unused strings
            var fields = new HashSet<FieldInfo>();
            addAllLingoRelevantFields(typeof(TTranslation), fields);

            // Treat all fields used for enum translations as used
            foreach (var f in allEnumTrs.SelectMany(et => et.TranslationType.GetAllFields()))
                fields.Remove(f);

            // Treat all fields that occur in a ldfld / ldflda instruction as used
            foreach (var mod in assemblies.SelectMany(a => a.GetModules(false)))
            {
                foreach (var typ in mod.GetTypes())
                {
                    foreach (var meth in
                        typ.GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance).Where(m => m.DeclaringType == typ).Cast<MethodBase>().Concat(
                        typ.GetConstructors(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance).Where(c => c.DeclaringType == typ).Cast<MethodBase>()))
                    {
                        foreach (var instr in ILReader.ReadIL(meth, typ))
                        {
                            if (instr.OpCode == OpCodes.Ldfld || instr.OpCode == OpCodes.Ldflda)
                            {
                                fields.Remove((FieldInfo) instr.Operand);
                                if (fields.Count == 0)
                                    goto done; // don't have to break the loop early, but there's really no point in searching the rest of the code now
                            }
                        }
                    }
                }
            }

            // Report warnings for all unused strings (not errors so that the developer can test things in the presence of unused strings)
            done:
            foreach (var field in fields)
                rep.Warning("Unused Lingo field: " + field.DeclaringType.FullName + "." + field.Name, "class " + field.DeclaringType.Name, field.FieldType.Name, field.Name);
        }

        private static void addAllLingoRelevantFields(Type type, HashSet<FieldInfo> sofar)
        {
            foreach (var field in type.GetAllFields())
            {
                if ((field.FieldType == typeof(TrString) || field.FieldType == typeof(TrStringNum)) && !field.IsDefined<LingoAutoGeneratedAttribute>())
                    sofar.Add(field);
                else if (field.FieldType.IsDefined<LingoStringClassAttribute>())
                {
                    sofar.Add(field);
                    addAllLingoRelevantFields(field.FieldType, sofar);
                }
            }
        }

        private class enumTrInfo { public Type EnumType; public Type TranslationType; }

        private static IEnumerable<enumTrInfo> allEnumTranslations(IEnumerable<Assembly> assemblies)
        {
            foreach (var type in assemblies.SelectMany(a => a.GetTypes()))
                if (type.IsEnum)
                {
                    var attr = type.GetCustomAttributes<TypeConverterAttribute>().FirstOrDefault();
                    if (attr == null)
                        continue;
                    var converter = Type.GetType(attr.ConverterTypeName);
                    var lingoConverter = converter.SelectChain(t => t.BaseType == typeof(object) ? null : t.BaseType)
                        .FirstOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(LingoEnumConverter<,>));
                    if (lingoConverter == null)
                        continue;
                    var args = lingoConverter.GetGenericArguments();
                    yield return new enumTrInfo { EnumType = args[0], TranslationType = args[1] };
                }
        }

        /// <summary>Checks that the enum values declared in the specified enum type and the TrString fields declared in the specified translation type match exactly.</summary>
        private static void checkEnumTranslation(IPostBuildReporter rep, Type enumType, Type translationType)
        {
            var set = translationType.GetAllFields().Where(f => f.FieldType == typeof(TrString)).Select(f => f.Name).ToHashSet();
            foreach (var enumValue in Enum.GetValues(enumType))
                if (!set.Contains(enumValue.ToString()))
                {
                    rep.Error(@"The translation type ""{0}"" does not contain a field of type {1} with the name ""{2}"" declared in enum type ""{3}"".".Fmt(translationType.FullName, typeof(TrString).Name, enumValue, enumType.FullName), "class " + translationType.Name);
                    rep.Error(@"---- Enum type is here.", "enum " + enumType.Name);
                }
            foreach (var value in set)
            {
                try { Enum.Parse(enumType, value, ignoreCase: false); }
                catch (ArgumentException)
                {
                    rep.Error(@"The enum type ""{0}"" does not contain a value with the name ""{1}"" declared in translation type ""{2}"".".Fmt(enumType.FullName, value, translationType.FullName), "enum " + enumType.Name);
                    rep.Error(@"---- Translation type is here.", "class " + translationType.Name);
                }
            }
        }
    }
}
