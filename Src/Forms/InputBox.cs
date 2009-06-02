using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace RT.Util.Dialogs
{
    /// <summary>
    /// Provides a simple dialog containing a single-line text box,
    /// prompting the user to input some text.
    /// </summary>
    public partial class InputBox : Form
    {
        /// <summary>Initialises a new <see cref="InputBox"/> instance.</summary>
        public InputBox()
        {
            InitializeComponent();
        }

        /// <summary>Prompts the user for input.</summary>
        /// <param name="prompt">Message to display to prompt the user.</param>
        /// <returns>The text entered by the user, or null if the user selected the Cancel button.</returns>
        public static string GetLine(string prompt)
        {
            return GetLine(prompt, "", "Please enter text");
        }

        /// <summary>Prompts the user for input.</summary>
        /// <param name="prompt">Message to display to prompt the user.</param>
        /// <param name="default">Initial value to populate the input box with.</param>
        /// <returns>The text entered by the user, or null if the user selected the Cancel button.</returns>
        public static string GetLine(string prompt, string @default)
        {
            return GetLine(prompt, @default, "Please enter text");
        }

        /// <summary>Prompts the user for input.</summary>
        /// <param name="prompt">Message to display to prompt the user.</param>
        /// <param name="default">Initial value to populate the input box with.</param>
        /// <param name="caption">Caption to use in the title bar of the dialog.</param>
        /// <returns>The text entered by the user, or null if the user selected the Cancel button.</returns>
        public static string GetLine(string prompt, string @default, string caption)
        {
            InputBox dlg = new InputBox();
            dlg.Text = caption;
            dlg.PromptLabel.Text = prompt;
            dlg.EnterBox.Text = @default;
            if (dlg.ShowDialog() == DialogResult.OK)
                return dlg.EnterBox.Text;
            else
                return null;
        }

        /// <summary>Prompts the user for input.</summary>
        /// <param name="prompt">Message to display to prompt the user.</param>
        /// <param name="default">Initial value to populate the input box with.</param>
        /// <param name="caption">Caption to use in the title bar of the dialog.</param>
        /// <param name="okButtonText">Text to use on the OK button.</param>
        /// <param name="cancelButtonText">Text to use on the Cancel button.</param>
        /// <returns>The text entered by the user, or null if the user selected the Cancel button.</returns>
        public static string GetLine(string prompt, string @default, string caption, string okButtonText, string cancelButtonText)
        {
            InputBox dlg = new InputBox();
            dlg.Text = caption;
            dlg.PromptLabel.Text = prompt;
            dlg.EnterBox.Text = @default;
            dlg.BtnOK.Text = okButtonText;
            dlg.BtnCancel.Text = cancelButtonText;
            if (dlg.ShowDialog() == DialogResult.OK)
                return dlg.EnterBox.Text;
            else
                return null;
        }
    }
}