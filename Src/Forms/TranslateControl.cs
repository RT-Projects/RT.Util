using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.ComponentModel;
using System.Reflection;
using System.IO;
using System.Text.RegularExpressions;

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

        private static void translateControl(Control control, object translation, string prefix)
        {
            var translationType = translation.GetType();
            if (!string.IsNullOrEmpty(control.Name))
            {
                if (!string.IsNullOrEmpty(control.Text) && (!(control.Tag is string) || ((string) control.Tag != "notranslate")))
                {
                    FieldInfo field = translationType.GetField(prefix + control.Name);
                    if (field != null)
                        control.Text = field.GetValue(translation).ToString();
                    else
                    {
                        PropertyInfo property = translationType.GetProperty(prefix + control.Name);
                        if (property != null)
                            control.Text = property.GetValue(translation, null).ToString();
                        else
                        {
                            MethodInfo method = translationType.GetMethod(prefix + control.Name, new Type[] { typeof(Control) });
                            if (method != null)
                                control.Text = method.Invoke(translation, new object[] { control }).ToString();
#if DEBUG
                            else
                            {
                                var attributes = translationType.GetCustomAttributes(typeof(TranslationDebugAttribute), false);
                                if (!attributes.Any())
                                    throw new Exception("Your translation type must have a [TranslationDebug(...)] attribute which specifies the relative path from the compiled assembly to the source of that translation type.");

                                var translationDebugAttribute = (TranslationDebugAttribute) attributes.First();
                                var path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), translationDebugAttribute.RelativePath);
                                string source = File.ReadAllText(path);
                                var match = Regex.Match(source, @"^(\s*)#endregion", RegexOptions.Multiline);
                                if (match.Success)
                                {
                                    source = source.Substring(0, match.Index) + match.Groups[1].Value + "public string " + prefix + control.Name + " = \"" + control.Text + "\";\n" + source.Substring(match.Index);
                                    File.WriteAllText(path, source);
                                }
                            }
#endif
                        }
                    }
                }
                foreach (Control subcontrol in control.Controls)
                    translateControl(subcontrol, translation, control.Name + "_");
            }
        }
    }
}
