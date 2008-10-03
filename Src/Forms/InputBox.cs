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
        /// <param name="Prompt">Message to display to prompt the user.</param>
        /// <returns>The text entered by the user, or null if the user selected the Cancel button.</returns>
        public static string GetLine(string Prompt)
        {
            return GetLine(Prompt, "", "Please enter text");
        }

        /// <summary>Prompts the user for input.</summary>
        /// <param name="Prompt">Message to display to prompt the user.</param>
        /// <param name="Default">Initial value to populate the input box with.</param>
        /// <returns>The text entered by the user, or null if the user selected the Cancel button.</returns>
        public static string GetLine(string Prompt, string Default)
        {
            return GetLine(Prompt, Default, "Please enter text");
        }

        /// <summary>Prompts the user for input.</summary>
        /// <param name="Prompt">Message to display to prompt the user.</param>
        /// <param name="Default">Initial value to populate the input box with.</param>
        /// <param name="Caption">Caption to use in the title bar of the dialog.</param>
        /// <returns>The text entered by the user, or null if the user selected the Cancel button.</returns>
        public static string GetLine(string Prompt, string Default, string Caption)
        {
            InputBox Dlg = new InputBox();
            Dlg.Text = Caption;
            Dlg.PromptLabel.Text = Prompt;
            Dlg.EnterBox.Text = Default;
            if (Dlg.ShowDialog() == DialogResult.OK)
                return Dlg.EnterBox.Text;
            else
                return null;
        }
    }
}