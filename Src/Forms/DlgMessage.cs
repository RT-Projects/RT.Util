using System;
using System.Drawing;
using System.Resources;
using System.Windows.Forms;
using RT.Util.ExtensionMethods;

namespace RT.Util.Dialogs
{
    /// <summary>
    /// Selects the type of DlgMessage dialog. This specifies the sound and, unless
    /// explicitly overridden, the image and caption used to show the dialog.
    /// </summary>
    public enum DlgType
    {
        /// <summary>Displays an information dialog.</summary>
        Info,
        /// <summary>Displays a questiondialog.</summary>
        Question,
        /// <summary>Displays a warning dialog.</summary>
        Warning,
        /// <summary>Displays an error dialog.</summary>
        Error,
        /// <summary>
        /// Displays a custom dialog. Custom defaults to no sound, no default image,
        /// and the caption defaults to the application name (all are overridable).
        /// Doesn't fully work, bug 15.
        /// </summary>
        Custom
    }

    /// <summary>
    /// This class is used by the <see cref="DlgMessage"/> class. This is the form used
    /// for displaying the message box.
    /// </summary>
    internal partial class DlgMessageForm : Form
    {
        #region Ugly hack, don't look (must come first so that static initialisers work well)

        /// Embedding resources in VS is fucked up. The name of the resource is hard-coded to
        /// be the default name of the assembly. There's no reason to generate a resource with
        /// the specified name (that I found). At the same time, the ResourceManager requires
        /// to be told the prefix for resource names as a string. These two factors make it
        /// impossible to use the same resource manager in different projects.
        /// 
        /// Workaround: use form's resources, because these are not mangled up by VS. But it's
        /// not that easy (of course not!) because the form's resources don't come with a
        /// strongly typed resource manager...
        private static ResourceManager RM;
        private static ResourceManager ResourceMgr
        {
            get
            {
                if (RM == null)
                    RM = new ResourceManager(typeof(DlgMessageForm));
                // Instead of the above, which works perfectly, the autogenerator produces
                // something more like the following (note the string!):
                // RM = new ResourceManager("RT.Util.Dialogs.DlgMessage", typeof(DlgMessage).Assembly);
                return RM;
            }
        }

        /// These are necessary because a certain scenario in VS2005 is totally impossible.
        /// ARGH without .NET Reflector there's no chance I could have figured out that VS
        /// adds some stupid .Image suffix to these images!...
        /// ARGH 2: and the bloody names are deduced based on which class is declared first
        /// in this source file, which is just absolutely un-fucking-believable. So merely
        /// declaring a new empty class at the start of this file breaks the fucking resources.
        private static Bitmap bmpInfo = (Bitmap) ResourceMgr.GetObject("info.Image");
        private static Bitmap bmpQuestion = (Bitmap) ResourceMgr.GetObject("question.Image");
        private static Bitmap bmpWarning = (Bitmap) ResourceMgr.GetObject("warning.Image");
        private static Bitmap bmpError = (Bitmap) ResourceMgr.GetObject("error.Image");

        #endregion

        /// <summary>
        /// Change this variable to i18n'ize the default captions
        /// </summary>
        internal static string[] DefaultCaption = new string[] {
            "Information", "Question",
            "Warning", "Error", "" // TODO: See bug 15
        };

        /// <summary>
        /// Change this variable to i18n'ize the default images
        /// </summary>
        internal static Bitmap[] DefaultImage = new Bitmap[] {
            bmpInfo, bmpQuestion, bmpWarning, bmpError, null
        };

        /// <summary>
        /// Change this variable to i18n'ize the default OK button text
        /// </summary>
        internal static string DefaultOKCaption = "&OK";

        /// <summary>Creates an instance of the DlgMessage form.</summary>
        internal DlgMessageForm()
        {
            InitializeComponent();
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
    }

    /// <summary>
    /// Holds a number of settings that define a message dialog. Provides a method to show the
    /// dialog represented, as well as static methods to show some common dialog kinds with
    /// most settings at their defaults.
    /// </summary>
    public class DlgMessage
    {
        /// <summary>
        /// Specifies the message type. This selects a sound, an image and a caption to be used
        /// by default, unless explicitly overridden in the other fields of this class. Defaults
        /// to <see cref="DlgType.Info"/>.
        /// </summary>
        public DlgType Type = DlgType.Info;
        /// <summary>
        /// Specifies the labels of the buttons to be displayed in the dialog. Defaults to null,
        /// in which case a single "OK" button will appear. May contain at most four elements.
        /// The labels may include the ampersand to indicate a shortcut key, or double-ampersand
        /// to include an ampersand.
        /// </summary>
        public string[] Buttons;
        /// <summary>
        /// Specifies the message to be displayed in the main message area. Defaults to null,
        /// in which case the message area will be empty.
        /// </summary>
        public string Message;
        /// <summary>
        /// Specifies a caption to be displayed in the dialog's window title bar. Defaults to null,
        /// which means choose a title based on the <see cref="Type"/> field.
        /// </summary>
        public string Caption;
        /// <summary>
        /// Specifies an image to be displayed in the image area of the dialog. Defaults to null,
        /// which means use an image based on the <see cref="Type"/> field. If the type is <see cref="DlgType.Custom"/>,
        /// and this field is null, no image will be displayed.
        /// </summary>
        public Bitmap Image;    // null acceptable as final value
        /// <summary>
        /// Specifies a font to be used for the message text (but not the buttons). Defaults to
        /// null, which means the default system font should be used.
        /// </summary>
        public Font Font;       // null acceptable as final value
        /// <summary>
        /// Specifies the index of the accept button (the one selected if the user just presses Enter).
        /// Defaults to -1, which means the button with index 0.
        /// </summary>
        public int AcceptButton = -1;
        /// <summary>
        /// Specifies the index of the cancel button (the one selected if the user just presses Esc or Alt+F4).
        /// Defaults to -1, which means the button with the largest valid index.
        /// </summary>
        public int CancelButton = -1;

        /// <summary>
        /// Shows a message using all the settings specified in this class instance. Anything
        /// left at defaults will be modified to hold the appropriate value. Any invalid settings
        /// will be flagged with an exception.
        /// </summary>
        public int Show()
        {
            deduceAndCheckSettings();
            return showAsIs();
        }

        /// <summary>
        /// Shows a message of the specified message type, using an appropriate caption, sound and image.
        /// </summary>
        public static int Show(string Message, DlgType Type)
        {
            return new DlgMessage() { Message = Message, Type = Type }.Show();
        }

        /// <summary>
        /// Shows a message of the specified message type, using an appropriate sound and image.
        /// </summary>
        public static int Show(string Message, string Caption, DlgType Type, params string[] Buttons)
        {
            return new DlgMessage() { Message = Message, Caption = Caption, Type = Type, Buttons = Buttons }.Show();
        }

        /// <summary>
        /// Shows an informational message using the image, caption and sound appropriate for this message type.
        /// </summary>
        public static int ShowInfo(string Message, params string[] Buttons)
        {
            return new DlgMessage() { Message = Message, Buttons = Buttons, Type = DlgType.Info }.Show();
        }

        /// <summary>
        /// Shows a question message using the image, caption and sound appropriate for this message type.
        /// </summary>
        public static int ShowQuestion(string Message, params string[] Buttons)
        {
            return new DlgMessage() { Message = Message, Buttons = Buttons, Type = DlgType.Question }.Show();
        }

        /// <summary>
        /// Shows a warning message using the image, caption and sound appropriate for this message type.
        /// </summary>
        public static int ShowWarning(string Message, params string[] Buttons)
        {
            return new DlgMessage() { Message = Message, Buttons = Buttons, Type = DlgType.Warning }.Show();
        }

        /// <summary>
        /// Shows an error message using the image, caption and sound appropriate for this message type.
        /// </summary>
        public static int ShowError(string Message, params string[] Buttons)
        {
            return new DlgMessage() { Message = Message, Buttons = Buttons, Type = DlgType.Error }.Show();
        }

        /// <summary>
        /// Verifies that all the settings are correct, throwing an exception if not. For all
        /// settings left at defaults, stores a value deemed appropriate based on the other settings.
        /// </summary>
        private void deduceAndCheckSettings()
        {
            if (Buttons == null || Buttons.Length == 0)
                Buttons = new[] { DlgMessageForm.DefaultOKCaption };
            if (Buttons.Length > 4)
                throw new RTException("The number of message buttons must not exceed 4. Actual number: {0}".Fmt(Buttons.Length));

            if (Message == null)
                Message = "";

            if (Caption == null)
                Caption = DlgMessageForm.DefaultCaption[(int) Type];

            if (Image == null && Type != DlgType.Custom)
                Image = DlgMessageForm.DefaultImage[(int) Type];

            if (AcceptButton < 0)
                AcceptButton = 0;

            if (CancelButton < 0)
                CancelButton = Buttons.Length - 1;

            if (AcceptButton > Buttons.Length - 1 || CancelButton > Buttons.Length - 1)
                throw new RTException("AcceptButton or CancelButton is too large for the specified number of buttons.");
        }

        /// <summary>
        /// Creates a message form and fills in the controls using the current settings. Does not
        /// verify whether the settings are valid, so must only be used internally and only after the
        /// settings have been verified.
        /// </summary>
        /// <returns>The index of the button pressed.</returns>
        private int showAsIs()
        {
            DlgMessageForm Form = new DlgMessageForm();

            Form.Font = SystemFonts.MessageBoxFont;
            if (Font != null)
                Form.Message.Font = Font;

            if (Image != null)
            {
                Form.img.Image = Image;
                Form.img.Visible = true;
            }

            Form.Text = Caption;
            Form.Message.Text = Message;

            // --- Buttons - captions, visibility, accept/cancel

            Button[] Btn = new Button[4];
            Btn[0] = Form.Btn0;
            Btn[1] = Form.Btn1;
            Btn[2] = Form.Btn2;
            Btn[3] = Form.Btn3;

            Form.AcceptButton = null;
            Form.CancelButton = null;

            for (int i = Buttons.Length - 1; i >= 0; i--)
            {
                Btn[i].Visible = true;
                Btn[i].Text = Buttons[i];
                Btn[i].SendToBack(); // otherwise table layout ordering is messed up
            }

            Form.AcceptButton = Btn[AcceptButton];
            Form.CancelButton = Btn[CancelButton];

            // --- Ding

            if (Environment.OSVersion.Platform != PlatformID.Unix && Environment.OSVersion.Platform != PlatformID.MacOSX)
            {
                switch (Type)
                {
                    case DlgType.Info:
                        WinAPI.MessageBeep(WinAPI.MessageBeepType.Information);
                        break;
                    case DlgType.Question:
                        WinAPI.MessageBeep(WinAPI.MessageBeepType.Question);
                        break;
                    case DlgType.Warning:
                        WinAPI.MessageBeep(WinAPI.MessageBeepType.Warning);
                        break;
                    case DlgType.Error:
                        WinAPI.MessageBeep(WinAPI.MessageBeepType.Error);
                        break;
                }
            }

            // --- Show

            Form.Message.MaximumSize = new Size(Math.Min(600, Screen.PrimaryScreen.WorkingArea.Width * 3 / 4), Screen.PrimaryScreen.WorkingArea.Height * 3 / 4);

            var result = Form.ShowDialog();
            Form.Dispose();

            // --- Return button index

            switch (result)
            {
                case DialogResult.OK:
                    return 0;
                case DialogResult.Cancel:
                    return 1;
                case DialogResult.Yes:
                    return 2;
                case DialogResult.No:
                    return 3;
                default:
                    // Should be unable to get here
                    throw new Exception("Internal exception in Util: unreachable code");
            }
        }
    }


}