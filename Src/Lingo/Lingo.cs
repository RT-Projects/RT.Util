﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using RT.Util.Collections;
using RT.Util.ExtensionMethods;
using RT.Util.Xml;

namespace RT.Util
{
#if DEBUG
    /// <summary>
    /// Use this attribute on a type that contains translations for a form. <see cref="Lingo.TranslateControl"/> will automatically add missing fields to the class.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class LingoDebugAttribute : Attribute
    {
        /// <summary>
        /// Specifies the relative path from the compiled assembly to the source file of the translation type.
        /// </summary>
        public string RelativePath { get; set; }
    }
#endif

    /// <summary>
    /// Use this attribute on a field for a translatable string to specify notes to the translator, detailing the purpose, context, or format of a translatable string.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = true)]
    public sealed class LingoNotesAttribute : Attribute
    {
        /// <summary>
        /// Constructor for a <see cref="LingoNotesAttribute"/> attribute.
        /// </summary>
        /// <param name="notes">Specifies notes to the translator, detailing the purpose, context, or format of a translatable string.</param>
        public LingoNotesAttribute(string notes) { _notes = notes; }

        /// <summary>
        /// Specifies notes to the translator, detailing the purpose, context, or format of a translatable string.
        /// </summary>
        public string Notes { get { return _notes; } }
        private string _notes;
    }

    /// <summary>
    /// Static class with helper methods to support multi-language applications.
    /// </summary>
    public static class Lingo
    {
        /// <summary>
        /// Attempts to load the translation for the specified module and language. The translation must exist
        /// in the application executable directory under a subdirectory called "Translations". See remarks for
        /// more info.
        /// </summary>
        /// <remarks>
        /// If the translation can be loaded successfully, this function will return true and will store the translation
        /// in the specified variable. Otherwise, the variable will be unmodified. In DEBUG mode, any exception when
        /// loading the translation will be propagated, but in release mode the function will simply behave as if the
        /// file didn't exist.
        /// </remarks>
        /// <typeparam name="TTranslation">The type of the translation class to load the translation into.</typeparam>
        /// <param name="module">The name of the module whose translation is being loaded.</param>
        /// <param name="language">The language code of the language to be loaded.</param>
        /// <param name="translation">Upon success, the translation will be stored here. On failure, this will not be modified.</param>
        /// <returns>True if the translation has been loaded and stored in "translation"; false otherwise.</returns>
        public static bool TryLoadTranslation<TTranslation>(string module, string language, ref TTranslation translation) where TTranslation : new()
        {
            string path = PathUtil.Combine(Path.GetDirectoryName(Application.ExecutablePath), "Translations", module + "." + language + ".xml");
            if (language == null || !File.Exists(path))
                return false;
            try
            {
                translation = XmlClassify.LoadObjectFromXmlFile<TTranslation>(path);
            }
#if DEBUG
            catch (Exception e)
            {
                throw new RTException(@"Could not load translation for module ""{0}"", language ""{1}"", from file ""{2}""".Fmt(module, language, path), e);
            }
#else
            catch
            {
                return false;
            }
#endif
            return true;
        }

        /// <summary>
        /// Generates a list of <see cref="MenuItem"/>s for the user to select a language from. The list is generated from the set of available XML files in the application's directory.
        /// </summary>
        /// <typeparam name="TTranslation">The type in which translations are stored.</typeparam>
        /// <param name="filemask">A file mask, e.g. "Project.*.xml", narrowing down which files may be valid translation files. Files that match both this and <paramref name="fileregex"/> will be considered.</param>
        /// <param name="fileregex">A regular expression stating which file names are acceptable for valid translation files. Files that match both <paramref name="filemask"/> and this will be considered.</param>
        /// <param name="thisLanguageGetter">A function taking an instance of the <typeparamref name="TTranslation"/> class and returning a string specifying the name of the language (e.g. "English (GB)").</param>
        /// <param name="setLanguage">A callback function to call when the user clicks on a menu item. The first parameter to the callback function is the <typeparamref name="TTranslation"/>
        /// object for the selected language. The second parameter is the regular expression match object returned by <paramref name="fileregex"/> for this file's filename. The second parameter
        /// is null for the native language, which is generated by calling <typeparamref name="TTranslation"/>'s constructor rather than by loading a file.</param>
        /// <returns>An <see cref="IEnumerable&lt;MenuItem&gt;"/> containing the generated menu items.</returns>
        public static IEnumerable<MenuItem> LanguageMenuItems<TTranslation>(string filemask, string fileregex, Func<TTranslation, string> thisLanguageGetter, Action<TTranslation, Match> setLanguage) where TTranslation : new()
        {
            return languageMenuItems<TTranslation>(filemask, fileregex)
                .OrderBy(tup => thisLanguageGetter(tup.E1))
                .Select(tup => new MenuItem("&" + thisLanguageGetter(tup.E1), new EventHandler((snd, ev) =>
                {
                    var t = (Tuple<TTranslation, Match>) ((MenuItem) snd).Tag;
                    setLanguage(t.E1, t.E2);
                })) { Tag = tup });
        }

        /// <summary>
        /// Generates a list of <see cref="ToolStripMenuItem"/>s for the user to select a language from. The list is generated from the set of available XML files in the application's directory.
        /// </summary>
        /// <typeparam name="TTranslation">The type in which translations are stored.</typeparam>
        /// <param name="filemask">A file mask, e.g. "Project.*.xml", narrowing down which files may be valid translation files. Files that match both this and <paramref name="fileregex"/> will be considered.</param>
        /// <param name="fileregex">A regular expression stating which file names are acceptable for valid translation files. Files that match both <paramref name="filemask"/> and this will be considered.</param>
        /// <param name="thisLanguageGetter">A function taking an instance of the <typeparamref name="TTranslation"/> class and returning a string specifying the name of the language (e.g. "English (GB)").</param>
        /// <param name="setLanguage">A callback function to call when the user clicks on a menu item. The first parameter to the callback function is the <typeparamref name="TTranslation"/>
        /// object for the selected language. The second parameter is the regular expression match object returned by <paramref name="fileregex"/> for this file's filename. The second parameter
        /// is null for the native language, which is generated by calling <typeparamref name="TTranslation"/>'s constructor rather than by loading a file.</param>
        /// <returns>An <see cref="IEnumerable&lt;ToolStripMenuItem&gt;"/> containing the generated menu items.</returns>
        public static IEnumerable<ToolStripMenuItem> LanguageToolStripMenuItems<TTranslation>(string filemask, string fileregex, Func<TTranslation, string> thisLanguageGetter, Action<TTranslation, Match> setLanguage) where TTranslation : new()
        {
            return languageMenuItems<TTranslation>(filemask, fileregex)
                .OrderBy(tup => thisLanguageGetter(tup.E1))
                .Select(tup => new ToolStripMenuItem("&" + thisLanguageGetter(tup.E1), null, new EventHandler((snd, ev) =>
                {
                    var t = (Tuple<TTranslation, Match>) ((ToolStripMenuItem) snd).Tag;
                    setLanguage(t.E1, t.E2);
                })) { Tag = tup });
        }

        private static IEnumerable<Tuple<TTranslation, Match>> languageMenuItems<TTranslation>(string filemask, string fileregex) where TTranslation : new()
        {
            yield return new Tuple<TTranslation, Match>(new TTranslation(), null);
            foreach (var file in new DirectoryInfo(Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "Translations")).GetFiles(filemask))
            {
                Match match = Regex.Match(file.Name, fileregex);
                if (!match.Success) continue;
                TTranslation transl;
                try { transl = XmlClassify.LoadObjectFromXmlFile<TTranslation>(file.FullName); }
                catch { continue; }
                yield return new Tuple<TTranslation, Match>(transl, match);
            }
        }

        /// <summary>
        /// Translates the text of the specified control and all its sub-controls using the specified translation object.
        /// </summary>
        /// <param name="control">Control whose text is to be translated.</param>
        /// <param name="translation">Object containing the translations. Use [TranslationDebug] attribute on the class you use for this.</param>
        public static void TranslateControl(Control control, object translation)
        {
            translateControl(control, translation);
        }

        private static string translate(string key, object translation, object control)
        {
            var translationType = translation.GetType();

            FieldInfo field = translationType.GetField(key);
            if (field != null)
                return field.GetValue(translation).ToString();

            PropertyInfo property = translationType.GetProperty(key);
            if (property != null)
                return property.GetValue(translation, null).ToString();

            MethodInfo method = translationType.GetMethod(key, new Type[] { typeof(Control) });
            if (method != null)
                return method.Invoke(translation, new object[] { control }).ToString();

            return null;
        }

        private static void translateControl(Control control, object translation)
        {
            if (control == null)
                return;

            if (!string.IsNullOrEmpty(control.Name))
            {
                if (!string.IsNullOrEmpty(control.Text) && (!(control.Tag is string) || ((string) control.Tag != "notranslate")))
                {
                    string translated = translate(control.Name, translation, control);
                    if (translated != null)
                        control.Text = translated;
#if DEBUG
                    else
                        setMissingTranslation(translation, control.Name, control.Text);
#endif
                }
            }

            if (control is ToolStrip)
                foreach (ToolStripItem tsi in ((ToolStrip) control).Items)
                    translateToolStripItem(tsi, translation);
            foreach (Control subcontrol in control.Controls)
                translateControl(subcontrol, translation);
        }

        private static void translateToolStripItem(ToolStripItem tsi, object translation)
        {
            if (!string.IsNullOrEmpty(tsi.Name))
            {
                if (!string.IsNullOrEmpty(tsi.Text) && (!(tsi.Tag is string) || ((string) tsi.Tag != "notranslate")))
                {
                    string translated = translate(tsi.Name, translation, tsi);
                    if (translated != null)
                        tsi.Text = translated;
#if DEBUG
                    else
                        setMissingTranslation(translation, tsi.Name, tsi.Text);
#endif
                }
            }
            if (tsi is ToolStripDropDownItem)
            {
                foreach (ToolStripItem subitem in ((ToolStripDropDownItem) tsi).DropDownItems)
                    translateToolStripItem(subitem, translation);
            }
        }

#if DEBUG
        private static void setMissingTranslation(object translation, string key, string origText)
        {
            var translationType = translation.GetType();
            var attributes = translationType.GetCustomAttributes(typeof(LingoDebugAttribute), false);
            if (!attributes.Any())
                throw new Exception("Your translation type must have a [LingoDebug(...)] attribute which specifies the relative path from the compiled assembly to the source of that translation type.");

            var translationDebugAttribute = (LingoDebugAttribute) attributes.First();
            var path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), translationDebugAttribute.RelativePath);
            string source = File.ReadAllText(path);
            var match = Regex.Match(source, @"^\s*#region " + translationType.Name + @"\s*$", RegexOptions.Multiline);
            if (!match.Success)
                return;
            string beforeRegion = source.Substring(0, match.Index);
            var afterRegion = source.Substring(match.Index);
            match = Regex.Match(afterRegion, @"^(\s*)#endregion", RegexOptions.Multiline);
            if (!match.Success)
                return;
            var newSource = beforeRegion + afterRegion.Substring(0, match.Index) + match.Groups[1].Value + "public string " + key + " = \"" + origText + "\";\n" + afterRegion.Substring(match.Index);
            File.WriteAllText(path, newSource);
        }
#endif
    }
}
