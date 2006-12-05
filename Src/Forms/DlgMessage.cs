/// DlgMessage.cs  -  defines classes to display advanced message boxes

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Reflection;

namespace RT.Util.Dialogs
{
    /// <summary>
    /// Selects the type of DlgMessage dialog. This specifies the sound and, unless
    /// explicitly overridden, the image and caption used to show the dialog.
    /// </summary>
    public enum DlgType
    {
        Info,
        Question,
        Warning,
        Error,
        /// <summary>
        /// Custom means no sound, no default image, and default caption equal to
        /// application name.
        /// </summary>
        Custom
    }

    /// <summary>
    /// The following applies to all variants of Show functions:
    /// - If no buttons are specified, an "OK" button is displayed.
    /// - Unless explicitly overridden, the accept & cancel buttons are the first and
    ///   the last button, respectively. Prefixing a button title with a space will make
    ///   that button the Accept button; suffix does the same for cancel button.
    ///   Explicitly specifying a button overrides this.
    /// </summary>
    public partial class DlgMessage : Form
    {
        /// <summary>
        /// Change this variable to i18n'ize the default captions
        /// </summary>
        public static string[] DefaultCaption = new string[] {
            "Information", "Question",
            "Warning", "Error", "" // TODO: See B-15
        };
        /// <summary>
        /// Change this variable to i18n'ize the default images
        /// </summary>
        public static Bitmap[] DefaultImage = new Bitmap[] {
            Resources.Resources.info, Resources.Resources.question,
            Resources.Resources.warning, Resources.Resources.error, null
        };
        /// <summary>
        /// Change this variable to i18n'ize the default OK button text
        /// </summary>
        public static string DefaultOKCaption = "&OK";

        public DlgMessage()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Shows a dialog with the specified message and buttons. Dialog type depends
        /// on the number of buttons: 0 or 1 are info, 2 or more are questions. Caption
        /// depends on dialog type.
        /// </summary>
        public static int Show(string Message, params string[] Buttons)
        {
            return DoShow(Message, null, MakeType(Buttons), MakeImage(Buttons), Buttons, -1, -1);
        }

        /// <summary>
        /// Shows a dialog with the specified message and buttons. Caption is constructed
        /// automatically depending on dialog type.
        /// </summary>
        public static int Show(string Message, DlgType Type, params string[] Buttons)
        {
            return DoShow(Message, null, Type, MakeImage(Type), Buttons, -1, -1);
        }

        /// <summary>
        /// Shows a dialog with the specified message, buttons and image. Custom type
        /// is assumed (i.e. no sound, assembly title as caption).
        /// </summary>
        public static int Show(string Message, Bitmap Image, params string[] Buttons)
        {
            return DoShow(Message, null, DlgType.Custom, Image, Buttons, -1, -1);
        }

        /// <summary>
        /// Shows a dialog with the specified message, caption, image and buttons.
        /// No sound will be played.
        /// </summary>
        public static int Show(string Message, string Caption, Bitmap Image, params string[] Buttons)
        {
            return DoShow(Message, Caption, DlgType.Custom, Image, Buttons, -1, -1);
        }

        /// <summary>
        /// Shows a dialog with the specified message, caption, type and buttons.
        /// Image is selected depending on type.
        /// </summary>
        public static int Show(string Message, string Caption, DlgType Type, params string[] Buttons)
        {
            return DoShow(Message, Caption, Type, MakeImage(Type), Buttons, -1, -1);
        }

        /// <summary>
        /// Shows a dialog with all the specified parameters.
        /// </summary>
        public static int Show(string Message, string Caption, DlgType Type, int AcceptButton, int CancelButton, params string[] Buttons)
        {
            return DoShow(Message, Caption, Type, MakeImage(Type), Buttons, AcceptButton, CancelButton);
        }

        /// <summary>
        /// Shows an Info-type message. Caption and image for info dialogs are used.
        /// </summary>
        public static int ShowInfo(string Message, params string[] Buttons)
        {
            return DoShow(Message, null, DlgType.Info, MakeImage(DlgType.Info), Buttons, -1, -1);
        }

        /// <summary>
        /// Shows an Question-type message. Caption and image for question dialogs are used.
        /// </summary>
        public static int ShowQuestion(string Message, params string[] Buttons)
        {
            return DoShow(Message, null, DlgType.Question, MakeImage(DlgType.Question), Buttons, -1, -1);
        }

        /// <summary>
        /// Shows an Warning-type message. Caption and image for warning dialogs are used.
        /// </summary>
        public static int ShowWarning(string Message, params string[] Buttons)
        {
            return DoShow(Message, null, DlgType.Warning, MakeImage(DlgType.Warning), Buttons, -1, -1);
        }

        /// <summary>
        /// Shows an Error-type message. Caption and image for error dialogs are used.
        /// </summary>
        public static int ShowError(string Message, params string[] Buttons)
        {
            return DoShow(Message, null, DlgType.Error, MakeImage(DlgType.Error), Buttons, -1, -1);
        }

        /// <summary>
        /// Shows an Info-type message. Image for info dialogs are used.
        /// </summary>
        public static int ShowInfo(string Message, string Caption, params string[] Buttons)
        {
            return DoShow(Message, Caption, DlgType.Info, MakeImage(DlgType.Info), Buttons, -1, -1);
        }

        /// <summary>
        /// Shows an Question-type message. Image for question dialogs are used.
        /// </summary>
        public static int ShowQuestion(string Message, string Caption, params string[] Buttons)
        {
            return DoShow(Message, Caption, DlgType.Question, MakeImage(DlgType.Question), Buttons, -1, -1);
        }

        /// <summary>
        /// Shows an Warning-type message. Image for warning dialogs are used.
        /// </summary>
        public static int ShowWarning(string Message, string Caption, params string[] Buttons)
        {
            return DoShow(Message, Caption, DlgType.Warning, MakeImage(DlgType.Warning), Buttons, -1, -1);
        }

        /// <summary>
        /// Shows an Error-type message. Image for error dialogs are used.
        /// </summary>
        public static int ShowError(string Message, string Caption, params string[] Buttons)
        {
            return DoShow(Message, Caption, DlgType.Error, MakeImage(DlgType.Error), Buttons, -1, -1);
        }

        /// <summary>
        /// Internal routine to show the dialog and return the index of the button pressed.
        /// Has some conventions; please make sure you read the info about each parameter.
        /// </summary>
        /// <param name="Message">Message to be shown. Cannot be null.</param>
        /// <param name="Caption">Caption to be set. If null, the caption will be selected depending on dialog type.</param>
        /// <param name="Type">Determines the sound to be played (not the image!)</param>
        /// <param name="image">Image to be displayed. Can be null, in which case there will be no image.</param>
        /// <param name="Buttons">An array of buttons. If empty, an "OK" button will be assumed.</param>
        /// <param name="AcceptButton">Index of the accept button. If not valid, the last button whose title begins with a space is used. If none, first button is used.</param>
        /// <param name="CancelButton">Index of the cancel button. If not valid, the last button whose title ends with a space is used. If none, last button is used.</param>
        private static int DoShow(string Message, string Caption, DlgType Type, Bitmap image, string[] Buttons, int AcceptButton, int CancelButton)
        {
            DlgMessage M = new DlgMessage();

            if (Caption == null)
                Caption = DefaultCaption[(int)Type];

            if (image!=null)
            {
                M.img.Image = image;
                M.img.Visible = true;
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

            if (Buttons.Length == 0)
                Buttons = new string[] { DefaultOKCaption };
            if (Buttons.Length >= Btn.Length)
                throw new Exception("Internal exception in Util: too many buttons");

            Button space_accept_btn = null;
            Button space_cancel_btn = null;

            for (int i=Buttons.Length-1; i>=0; i--)
            {
                Btn[i].Visible = true;
                Btn[i].Text = Buttons[i].Trim();
                Btn[i].SendToBack(); // otherwise table layout ordering is messed up

                if (Buttons[i].Length > 0 && Buttons[i][0] == ' ')
                    space_accept_btn = Btn[i];
                if (Buttons[i].Length > 0 && Buttons[i][Buttons[i].Length-1] == ' ')
                    space_cancel_btn = Btn[i];
            }

            if (AcceptButton >= 0 && AcceptButton < Buttons.Length)
                M.AcceptButton = Btn[AcceptButton];
            else if (space_accept_btn != null)
                M.AcceptButton = space_accept_btn;
            else
                M.AcceptButton = Btn[0];

            if (CancelButton >= 0 && CancelButton < Buttons.Length)
                M.CancelButton = Btn[CancelButton];
            else if (space_cancel_btn != null)
                M.CancelButton = space_cancel_btn;
            else
                M.CancelButton = Btn[Buttons.Length-1];

            M.Message.MaximumSize = new Size(Screen.PrimaryScreen.WorkingArea.Width*3/4, Screen.PrimaryScreen.WorkingArea.Height*3/4);

            switch (Type)
            {
                case DlgType.Info:
                    WinAPI.MessageBeep(MessageBeepType.Information);
                    break;
                case DlgType.Question:
                    WinAPI.MessageBeep(MessageBeepType.Question);
                    break;
                case DlgType.Warning:
                    WinAPI.MessageBeep(MessageBeepType.Warning);
                    break;
                case DlgType.Error:
                    WinAPI.MessageBeep(MessageBeepType.Error);
                    break;
            }

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
            throw new Exception("Internal exception in Util: unreachable code");
        }

        private bool ButtonPressed = false;

        /// <summary>
        /// This takes care of the user closing the dialog. This is equivalent to pressing
        /// the Cancel button, whichever one it happens to be.
        /// </summary>
        private void DlgMessage_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!ButtonPressed)
                DialogResult = CancelButton.DialogResult;
        }

        private void Btn_Click(object sender, EventArgs e)
        {
            ButtonPressed = true;
        }

        private static DlgType MakeType(string[] Buttons)
        {
            if (Buttons.Length < 2)
                return DlgType.Info;
            else
                return DlgType.Question;
        }

        private static Bitmap MakeImage(string[] Buttons)
        {
            return DefaultImage[(int)MakeType(Buttons)];
        }

        private static Bitmap MakeImage(DlgType Type)
        {
            return DefaultImage[(int)Type];
        }

    }
}