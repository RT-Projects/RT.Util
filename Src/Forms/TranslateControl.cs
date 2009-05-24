using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.ComponentModel;
using System.Reflection;
using System.IO;
using System.Text.RegularExpressions;
using RT.Util.Collections;

namespace RT.Util.Forms
{
    /// <summary>
    /// Use this attribute on a type that contains translations for a form. <see cref="Translation.TranslateControl"/> will automatically add missing fields to the class.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class TranslationDebugAttribute : Attribute
    {
        /// <summary>
        /// Specifies the relative path from the compiled assembly to the source file of the translation type.
        /// </summary>
        public string RelativePath { get; set; }
    }

    /// <summary>
    /// Static class with helper methods for automatically translating a form.
    /// </summary>
    public static class Translation
    {
        /// <summary>
        /// Translates the text of the specified control and all its sub-controls using the specified translation object.
        /// </summary>
        /// <param name="control">Control whose text is to be translated.</param>
        /// <param name="translation">Object containing the translations. Use [TranslationDebug] attribute on the class you use for this.</param>
        public static void TranslateControl(Control control, object translation)
        {
            translateControl(control, translation, "");
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

        private static void translateControl(Control control, object translation, string prefix)
        {
            if (control == null)
                return;

            string add = "";
            if (!string.IsNullOrEmpty(control.Name))
            {
                add = control.Name + "_";
                if (!string.IsNullOrEmpty(control.Text) && (!(control.Tag is string) || ((string) control.Tag != "notranslate")))
                {
                    string translated = translate(prefix + control.Name, translation, control);
                    if (translated != null)
                        control.Text = translated;
#if DEBUG
                    else
                        setMissingTranslation(translation, prefix + control.Name, control.Text);
#endif
                }
            }

            if (control is ToolStrip)
                foreach (ToolStripItem tsi in ((ToolStrip) control).Items)
                    translateToolStripItem(tsi, translation, prefix + add);
            foreach (Control subcontrol in control.Controls)
                translateControl(subcontrol, translation, prefix + add);
        }

        private static void translateToolStripItem(ToolStripItem tsi, object translation, string prefix)
        {
            string add = "";
            if (!string.IsNullOrEmpty(tsi.Name))
            {
                add = tsi.Name + "_";
                if (!string.IsNullOrEmpty(tsi.Text) && (!(tsi.Tag is string) || ((string) tsi.Tag != "notranslate")))
                {
                    string translated = translate(prefix + tsi.Name, translation, tsi);
                    if (translated != null)
                        tsi.Text = translated;
#if DEBUG
                    else
                        setMissingTranslation(translation, prefix + tsi.Name, tsi.Text);
#endif
                }
            }
            if (tsi is ToolStripDropDownItem)
            {
                foreach (ToolStripItem subitem in ((ToolStripDropDownItem) tsi).DropDownItems)
                    translateToolStripItem(subitem, translation, prefix + add);
            }
        }

        private static void setMissingTranslation(object translation, string key, string origText)
        {
            var translationType = translation.GetType();
            var attributes = translationType.GetCustomAttributes(typeof(TranslationDebugAttribute), false);
            if (!attributes.Any())
                throw new Exception("Your translation type must have a [TranslationDebug(...)] attribute which specifies the relative path from the compiled assembly to the source of that translation type.");

            var translationDebugAttribute = (TranslationDebugAttribute) attributes.First();
            var path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), translationDebugAttribute.RelativePath);
            string source = File.ReadAllText(path);
            var match = Regex.Match(source, @"^(\s*)#endregion", RegexOptions.Multiline);
            if (match.Success)
            {
                source = source.Substring(0, match.Index) + match.Groups[1].Value + "public string " + key + " = \"" + origText + "\";\n" + source.Substring(match.Index);
                File.WriteAllText(path, source);
            }
        }
    }
}
