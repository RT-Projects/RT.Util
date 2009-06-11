using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using RT.Util.Xml;
using RT.Util.Collections;
using System.IO;
using RT.Util.Dialogs;
using System.Diagnostics;

namespace RT.Util.Lingo
{
    /// <summary>Provides a dialog in which the user can create a new translation of the software.</summary>
    public class TranslationCreateForm : Form
    {
        private class LanguageListItem
        {
            public Language Language;
            public LanguageListItem(Language language) { Language = language; }
            public override string ToString()
            {
                return Language.GetEnglishName();
            }
        }
        private ComboBox _lstLanguages;

        /// <summary>Gets the language selected by the user.</summary>
        public Language SelectedLanguage { get { return ((LanguageListItem) _lstLanguages.SelectedItem).Language; } }

        /// <summary>Presents the user with a dialog to select a language from, and (if they click "OK") creates a new XML file for the new translation.</summary>
        /// <typeparam name="T">Class containing the translatable strings.</typeparam>
        /// <param name="programName">Name of the program (forms part of the translation's XML file).</param>
        /// <returns>If the user clicked OK, creates a new XML file and returns the language code. If the user clicked Cancel, returns null.</returns>
        public static Tuple<T, string> CreateTranslation<T>(string programName) where T : TranslationBase, new()
        {
            using (TranslationCreateForm tcf = new TranslationCreateForm())
            {
                if (tcf.ShowDialog() == DialogResult.OK)
                {
                    T trans = new T { Language = tcf.SelectedLanguage };
                    string iso = trans.Language.GetIsoLanguageCode();
                    string xmlFile = PathUtil.AppPathCombine("Translations", programName + "." + iso + ".xml");
                    if (!File.Exists(xmlFile))
                    {
                        XmlClassify.SaveObjectToXmlFile(trans, xmlFile);
                        return new Tuple<T, string>(trans, iso);
                    }
                    int result = DlgMessage.Show("A translation into the selected language already exists. If you wish to start this translation afresh, please delete the translation file first.\n\nThe translation file is: " + xmlFile,
                        "Error creating translation", DlgType.Error, "&Go to containing folder", "Cancel");
                    if (result == 0)
                        Process.Start(Path.GetDirectoryName(xmlFile));
                }
            }
            return new Tuple<T, string>(null, null);
        }

        /// <summary>Main constructor.</summary>
        public TranslationCreateForm()
        {
            TableLayoutPanel tlpMain = new TableLayoutPanel
            {
                ColumnCount = 2,
                RowCount = 2,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Fill,
                Margin = new Padding(5)
            };
            tlpMain.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tlpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            _lstLanguages = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Anchor = AnchorStyles.Left | AnchorStyles.Right, Margin = new Padding(5) };
            _lstLanguages.Items.AddRange(Enum.GetValues(typeof(Language)).Cast<Language>().Select(l => new LanguageListItem(l)).OrderBy(l => l.Language.GetEnglishName()).ToArray());
            _lstLanguages.ClientSize = new Size(_lstLanguages.Items.Cast<LanguageListItem>().Max(l => TextRenderer.MeasureText(l.Language.GetEnglishName(), _lstLanguages.Font).Width) + 32, _lstLanguages.ClientSize.Height);

            tlpMain.Controls.Add(new Label { Text = "&Language:", Anchor = AnchorStyles.Left, AutoSize = true, Margin = new Padding(5) }, 0, 0);
            tlpMain.Controls.Add(_lstLanguages, 1, 0);
            tlpMain.Controls.Add(new Label { Text = "Native name:", Anchor = AnchorStyles.Left, AutoSize = true, Margin = new Padding(5) }, 0, 1);
            Label lblNativeName = new Label { Text = "", Anchor = AnchorStyles.Left, AutoSize = true, Margin = new Padding(5) };
            tlpMain.Controls.Add(lblNativeName, 1, 1);

            TableLayoutPanel tlpButtons = new TableLayoutPanel
            {
                ColumnCount = 3,
                RowCount = 1,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Bottom,
                Margin = new Padding(5)
            };

            Button btnOK = new Button { Text = "OK", Margin = new Padding(5), Anchor = AnchorStyles.Left, Enabled = false };
            btnOK.Click += (s, v) => { DialogResult = DialogResult.OK; };
            Button btnCancel = new Button { Text = "Cancel", Margin = new Padding(5), Anchor = AnchorStyles.Left };
            btnCancel.Click += (s, v) => { DialogResult = DialogResult.Cancel; };
            tlpButtons.Controls.Add(btnOK, 1, 0);
            tlpButtons.Controls.Add(btnCancel, 2, 0);
            tlpButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tlpButtons.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tlpButtons.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tlpButtons.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            _lstLanguages.SelectedIndexChanged += (s, v) =>
            {
                btnOK.Enabled = _lstLanguages.SelectedItem != null;
                lblNativeName.Text = (_lstLanguages.SelectedItem != null) ? ((LanguageListItem) _lstLanguages.SelectedItem).Language.GetNativeName() : "";
            };
            _lstLanguages.SelectedIndex = 0;

            Controls.Add(tlpMain);
            Controls.Add(tlpButtons);

            Text = "Create new translation";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MinimizeBox = false;
            MaximizeBox = false;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;

            Load += (s, v) =>
            {
                Location = new Point(
                    (Screen.PrimaryScreen.WorkingArea.Width - Width) / 2 + Screen.PrimaryScreen.WorkingArea.X,
                    (Screen.PrimaryScreen.WorkingArea.Height - Height) / 2 + Screen.PrimaryScreen.WorkingArea.Y
                );
            };
            AcceptButton = btnOK;
            CancelButton = btnCancel;
        }
    }
}
