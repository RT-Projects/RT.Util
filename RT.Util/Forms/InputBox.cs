using System.Drawing;
using System.Windows.Forms;

namespace RT.Util.Forms
{
    /// <summary>
    /// Provides a simple dialog containing a single-line text box,
    /// prompting the user to input some text.
    /// </summary>
    public sealed partial class InputBox : Form
    {
        /// <summary>Initialises a new <see cref="InputBox"/> instance.</summary>
        public InputBox()
        {
            InitializeComponent();
            Font = SystemFonts.MessageBoxFont;
        }

        /// <summary>Prompts the user for input.</summary>
        /// <param name="prompt">Message to display to prompt the user.</param>
        /// <param name="default">Initial value to populate the input box with.</param>
        /// <param name="caption">Caption to use in the title bar of the dialog.</param>
        /// <param name="okButtonText">Caption for the OK button.</param>
        /// <param name="cancelButtonText">Caption for the Cancel button.</param>
        /// <param name="useMultilineBox">If true, a multi-line textbox is used, allowing the user to enter multiple lines of text.</param>
        /// <returns>The text entered by the user, or null if the user selected the Cancel button.</returns>
        public static string GetLine(string prompt, string @default = "", string caption = "Please enter text", string okButtonText = null, string cancelButtonText = null, bool useMultilineBox = false)
        {
            InputBox dlg = new InputBox();
            dlg.Text = caption;
            dlg.PromptLabel.Text = prompt;
            if (useMultilineBox)
            {
                dlg.EnterBox.Multiline = true;
                dlg.EnterBox.Height = 128;
            }
            dlg.EnterBox.Text = @default;
            if (okButtonText != null)
                dlg.BtnOK.Text = okButtonText;
            if (cancelButtonText != null)
                dlg.BtnCancel.Text = cancelButtonText;
            if (dlg.ShowDialog() == DialogResult.OK)
                return dlg.EnterBox.Text;
            else
                return null;
        }
    }
}
