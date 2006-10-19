using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace RT.Util
{
    public partial class InputBox : Form
    {
        public InputBox()
        {
            InitializeComponent();
        }

        public static string GetLine(string Prompt)
        {
            return GetLine(Prompt, "");
        }
        public static string GetLine(string Prompt, string Default)
        {
            InputBox Dlg = new InputBox();
            Dlg.PromptLabel.Text = Prompt;
            Dlg.EnterBox.Text = Default;
            if (Dlg.ShowDialog() == DialogResult.OK)
                return Dlg.EnterBox.Text;
            else
                return null;
        }
    }
}