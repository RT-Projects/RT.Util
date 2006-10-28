/// DlgMessage.cs  -  defines classes to display advanced message boxes

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace RT.Controls
{
    public enum DlgType
    {
        Info,
        Question,
        Warning,
        Error
    }

    public partial class DlgMessage : Form
    {
        public DlgMessage()
        {
            InitializeComponent();
        }

        public static void Show(string Message)
        {
            DoShow(Message, " ", DlgType.Info, new string[] {"OK"});
        }

        public static int Show(string Message, DlgType Type, params string[] Buttons)
        {
            if (Buttons.Length == 0)
                return DoShow(Message, " ", Type, new string[] { "OK" });
            else
                return DoShow(Message, " ", Type, Buttons);
        }

        public static int Show(string Message, string Caption, DlgType Type, params string[] Buttons)
        {
            if (Buttons.Length == 0)
                return DoShow(Message, Caption, Type, new string[] { "OK" });
            else
                return DoShow(Message, Caption, Type, Buttons);
        }

        public static int ShowError(string Message, params string[] Buttons)
        {
            if (Buttons.Length == 0)
                return DoShow(Message, " ", DlgType.Error, new string[] { "OK" });
            else
                return DoShow(Message, " ", DlgType.Error, Buttons);
        }

        public static int ShowWarning(string Message, params string[] Buttons)
        {
            if (Buttons.Length == 0)
                return DoShow(Message, " ", DlgType.Warning, new string[] { "OK" });
            else
                return DoShow(Message, " ", DlgType.Warning, Buttons);
        }

        public static int ShowInfo(string Message, params string[] Buttons)
        {
            if (Buttons.Length == 0)
                return DoShow(Message, " ", DlgType.Info, new string[] { "OK" });
            else
                return DoShow(Message, " ", DlgType.Info, Buttons);
        }

        public static int ShowQuestion(string Message, params string[] Buttons)
        {
            if (Buttons.Length == 0)
                return DoShow(Message, " ", DlgType.Question, new string[] { "OK" });
            else
                return DoShow(Message, " ", DlgType.Question, Buttons);
        }


        private static int DoShow(string Message, string Caption, DlgType Type, string[] Buttons)
        {
            DlgMessage M = new DlgMessage();

            switch (Type)
            {
            case DlgType.Error:
                M.imgError.Visible = true;
                if (Caption == " ") Caption = "Error";
                break;
            case DlgType.Info:
                M.imgInfo.Visible = true;
                if (Caption == " ") Caption = "Information";
                break;
            case DlgType.Question:
                M.imgQuestion.Visible = true;
                if (Caption == " ") Caption = "Question";
                break;
            case DlgType.Warning:
                M.imgWarning.Visible = true;
                if (Caption == " ") Caption = "Warning";
                break;
            }

            M.Text = Caption;
            M.Message.Text = Message;
            M.AcceptButton = null;
            M.CancelButton = null;

            Button[] Btn = new Button[4];
            Btn[0] = M.Btn0;
            Btn[1] = M.Btn1;
            Btn[2] = M.Btn2;
            Btn[3] = M.Btn3;

            for (int i=Buttons.Length-1; i>=0; i--)
            {
                Btn[i].Visible = true;
                if (Buttons[i].IndexOf(" ") == 0)
                    M.AcceptButton = Btn[i];
                if (Buttons[i].LastIndexOf(" ") == Buttons[i].Length-1)
                    M.CancelButton = Btn[i];
                Btn[i].Text = Buttons[i].Trim();
                Btn[i].SendToBack();
            }

            if (M.AcceptButton == null)
                M.AcceptButton = Btn[0];

            if (Buttons.Length == 0)
            {
                Btn[0].Visible = true;
                Btn[0].Text = "OK";
                M.CancelButton = Btn[0];
            }
            else
            {
                if (M.CancelButton == null)
                    M.CancelButton = Btn[Buttons.Length-1];
            }

            M.Message.MaximumSize = new Size(Screen.PrimaryScreen.WorkingArea.Width*3/4, Screen.PrimaryScreen.WorkingArea.Height*3/4);

            switch (M.ShowDialog())
            {
            case DialogResult.OK:
                return 0;
            case DialogResult.Cancel:
                return 1;
            case DialogResult.Yes:
                return 2;
            case DialogResult.No:
                return 3;
            }

            // Can't get here
            return 0;
        }
    }
}