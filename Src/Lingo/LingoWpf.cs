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

            translateControlWpf(window, translation, null);
        }

        private static void translateWpf(object translation, FrameworkElement control, object controlContent, Action<string> setter, List<trStringInfo> generateFields)
        {
            if (controlContent is FrameworkElement)
            {
                translateControlWpf((FrameworkElement) controlContent, translation, generateFields);
            }
            else if (controlContent is string)
            {
                var translated = translate(control.Name, control.Tag, translation, (string) controlContent, control, generateFields);
                if (translated != null)
                    setter(translated);
            }
            // else it could be null
        }

        private static void translateControlWpf(FrameworkElement control, object translation, List<trStringInfo> generateFields)
        {
            if (control == null)
                return;

            if (control is Window)
            {
                var window = (Window) control;
                if (!string.IsNullOrEmpty(window.Title))
                    translateWpf(translation, window, window.Title, newTitle => { window.Title = newTitle; }, generateFields);
            }

            if (control is AccessText)
            {
                var access = (AccessText) control;
                translateWpf(translation, access, access.Text, newText => { access.Text = newText; }, generateFields);
            }

            if (control is ContentControl)
            {
                var cc = (ContentControl) control;
                if (cc is HeaderedContentControl)
                {
                    var hc = (HeaderedContentControl) cc;
                    translateWpf(translation, hc, hc.Header, newHeader => { hc.Header = newHeader; }, generateFields);
                }
                translateWpf(translation, cc, cc.Content, newContent => { cc.Content = newContent; }, generateFields);
            }

            if (control is ItemsControl)
            {
                var ic = (ItemsControl) control;
                if (ic is HeaderedItemsControl)
                {
                    var hc = (HeaderedItemsControl) ic;
                    translateWpf(translation, hc, hc.Header, newHeader => { hc.Header = newHeader; }, generateFields);
                }
                foreach (var item in ic.Items.OfType<FrameworkElement>())
                    translateControlWpf(item, translation, generateFields);
            }

            if (control is Panel)
            {
                var panel = (Panel) control;
                foreach (var child in panel.Children.OfType<FrameworkElement>())
                    translateControlWpf(child, translation, generateFields);
            }

            translateControlWpf(control.ContextMenu, translation, generateFields);
            translateWpf(translation, control, control.ToolTip, newTooltip => { control.ToolTip = newTooltip; }, generateFields);
        }
    }
}
