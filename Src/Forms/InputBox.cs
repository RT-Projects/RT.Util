using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace RT.Util.Dialogs
{
    public partial class InputBox : Form
    {
        public InputBox()
        {
            InitializeComponent();
        }

        public static string GetLine(string Prompt)
        {
            return GetLine(Prompt, "", "Please enter text");
        }
        public static string GetLine(string Prompt, string Default)
        {
            return GetLine(Prompt, Default, "Please enter text");
        }
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