using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using RT.Util.Dialogs;
using RT.Util.Xml;

namespace RT.Util.Lingo
{
    /// <summary>Provides a dialog in which the user can create a new translation of the software.</summary>
    public sealed class TranslationCreateForm : Form
    {
        private sealed class LanguageListItem
        {
            public Language Language;
            public LanguageListItem(Language language) { Language = language; }
        }

        private ComboBox _lstLanguages;
        private Button _btnOk, _btnCancel;

        /// <summary>Gets the language selected by the user.</summary>
        public Language SelectedLanguage { get { return ((LanguageListItem) _lstLanguages.SelectedItem).Language; } }

        /// <summary>Presents the user with a dialog to select a language from, and (if they click "OK") creates a new XML file for the new translation.</summary>
        /// <typeparam name="TTranslation">Class containing the translatable strings.</typeparam>
        /// <param name="moduleName">Name of the module being translated (forms part of the translation's XML file).</param>
        /// <param name="fontName">Specifies the name of the font to use in this dialog, or null for the default font.</param>
        /// <param name="fontSize">Specifies the size of the font to use in this dialog. Ignored if <paramref name="fontName"/> is null.</param>
        /// <returns>If the user clicked OK, creates a new XML file and returns the translation. If the user clicked Cancel, returns null.</returns>
        public static TTranslation CreateTranslation<TTranslation>(string moduleName, string fontName, float fontSize) where TTranslation : TranslationBase, new()
        {
            using (TranslationCreateForm tcf = new TranslationCreateForm())
            {
                tcf.Font = fontName != null ? new Font(fontName, fontSize, FontStyle.Regular) : SystemFonts.MessageBoxFont;
                if (tcf.ShowDialog() != DialogResult.OK)
                    return null;

                if (tcf.SelectedLanguage == new TTranslation().Language)
                {
                    DlgMessage.Show("This is the native language of the application. This translation cannot be edited, and you cannot create a new translation for this language.",
                        "Error creating translation", DlgType.Error, "OK");
                    return null;
                }

                var trans = new TTranslation { Language = tcf.SelectedLanguage };
                string iso = trans.Language.GetIsoLanguageCode();
                string xmlFile = PathUtil.AppPathCombine("Translations", moduleName + "." + iso + ".xml");
                if (File.Exists(xmlFile))
                {
                    int result = DlgMessage.Show("A translation into the selected language already exists. If you wish to start this translation afresh, please delete the translation file first.\n\nThe translation file is: " + xmlFile,
                        "Error creating translation", DlgType.Error, "&Go to containing folder", "Cancel");
                    if (result == 0)
                        Process.Start(Path.GetDirectoryName(xmlFile));
                    return null;
                }
                XmlClassify.SaveObjectToXmlFile(trans, xmlFile);
                return trans;
            }
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
            };
            tlpMain.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tlpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            _lstLanguages = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Anchor = AnchorStyles.Left | AnchorStyles.Right, Margin = new Padding(5), FormattingEnabled = true };
            _lstLanguages.Items.AddRange(Enum.GetValues(typeof(Language)).Cast<Language>().Select(l => new LanguageListItem(l)).OrderBy(l => l.Language.GetEnglishName()).ToArray());
            _lstLanguages.ClientSize = new Size(_lstLanguages.Items.Cast<LanguageListItem>().Max(l => TextRenderer.MeasureText(l.Language.GetEnglishName(), _lstLanguages.Font).Width) + 32, _lstLanguages.ClientSize.Height);
            _lstLanguages.Format += (s, e) => { e.Value = ((LanguageListItem) e.ListItem).Language.GetEnglishName(); };

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
            };

            _btnOk = new Button { Text = "OK", Margin = new Padding(5), Anchor = AnchorStyles.Left, Enabled = false, AutoSize = true, MinimumSize = new Size(75, 20) };
            _btnOk.Click += (s, v) => { DialogResult = DialogResult.OK; };
            _btnCancel = new Button { Text = "Cancel", Margin = new Padding(5), Anchor = AnchorStyles.Left, AutoSize = true, MinimumSize = new Size(75, 20) };
            _btnCancel.Click += (s, v) => { DialogResult = DialogResult.Cancel; };
            tlpButtons.Controls.Add(_btnOk, 1, 0);
            tlpButtons.Controls.Add(_btnCancel, 2, 0);
            tlpButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tlpButtons.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tlpButtons.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tlpButtons.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            _lstLanguages.SelectedIndexChanged += (s, v) =>
            {
                _btnOk.Enabled = _lstLanguages.SelectedItem != null;
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
            ShowInTaskbar = false;
            Font = SystemFonts.MessageBoxFont;
            Padding = new Padding(5);

            Load += (s, v) =>
            {
                Location = new Point(
                    (Screen.PrimaryScreen.WorkingArea.Width - Width) / 2 + Screen.PrimaryScreen.WorkingArea.X,
                    (Screen.PrimaryScreen.WorkingArea.Height - Height) / 2 + Screen.PrimaryScreen.WorkingArea.Y
                );
            };
            AcceptButton = _btnOk;
            CancelButton = _btnCancel;
        }
    }
}
