using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace RT.Util.Lingo
{
    public static partial class Lingo
    {
        /// <summary>Translates the text of the specified control and all its sub-controls using the specified translation object.</summary>
        /// <param name="window">Control whose text is to be translated.</param>
        /// <param name="translation">Object containing the translations.</param>
        public static void TranslateWindow(Window window, object translation)
        {
            if (window == null)
                throw new ArgumentNullException("window");
            if (translation == null)
                throw new ArgumentNullException("translation");

            translateControlWpf((dynamic) window, translation, null);
        }

        private static void translateWpf(object translation, FrameworkElement control, object controlContent, Action<string> setter, List<trStringInfo> generateFields)
        {
            if (controlContent == null)
                return;

            if (!(controlContent is string))
            {
                translateControlWpf((dynamic) controlContent, translation, generateFields);
                return;
            }

            var translated = translate(control.Name, control.Tag, translation, (string) controlContent, control, generateFields);
            if (translated != null)
                setter(translated);
        }

        private static void translateControlWpf(Window control, object translation, List<trStringInfo> generateFields)
        {
            if (control == null)
                return;

            translateControlWpf((ContentControl) control, translation, generateFields);

            if (!string.IsNullOrEmpty(control.Title))
                translateWpf(translation, control, control.Title, newTitle => { control.Title = newTitle; }, generateFields);
        }

        private static void translateControlWpf(ContentControl control, object translation, List<trStringInfo> generateFields)
        {
            if (control == null)
                return;

            translateControlWpf((FrameworkElement) control, translation, generateFields);
            translateWpf(translation, control, control.Content, newContent => { control.Content = newContent; }, generateFields);
        }

        private static void translateControlWpf(AccessText control, object translation, List<trStringInfo> generateFields)
        {
            if (control == null)
                return;

            translateControlWpf((FrameworkElement) control, translation, generateFields);
            translateWpf(translation, control, control.Text, newText => { control.Text = newText; }, generateFields);
        }

        private static void translateControlWpf(FrameworkElement control, object translation, List<trStringInfo> generateFields)
        {
            if (control == null)
                return;

            translateWpf(translation, control, control.ToolTip, newTooltip => { control.ToolTip = newTooltip; }, generateFields);
        }

        private static void translateControlWpf(Panel control, object translation, List<trStringInfo> generateFields)
        {
            if (control == null)
                return;

            translateControlWpf((FrameworkElement) control, translation, generateFields);

            foreach (dynamic child in control.Children)
                translateControlWpf(child, translation, generateFields);
        }
    }
}
