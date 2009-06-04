using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using RT.Util.Controls;
using RT.Util.Dialogs;
using RT.Util.ExtensionMethods;
using RT.Util.Forms;
using RT.Util.Xml;

namespace RT.Util.Lingo
{
    internal enum DismissButton { OK, Cancel, Apply };

    /// <summary>Provides a GUI for the user to edit a translation for the application.</summary>
    /// <typeparam name="T">The type containing the <see cref="TrString"/> fields to be translated.</typeparam>
    public partial class TranslationForm<T> : ManagedForm where T : new()
    {
        /// <summary>Used to fire <see cref="AcceptChanges"/>.</summary>
        public delegate void TranslationChangesEventHandler();
        /// <summary>Fires when the user clicks "Save changes" or "Apply changes".</summary>
        public event TranslationChangesEventHandler AcceptChanges;

        private Panel[] _translationPanels;
        private SplitContainer _pnlMain;
        private string _translationFile;
        private T _translation;
        private bool _anyChanges;

        private static Color outOfDateNormal = Color.FromArgb(0xff, 0xcc, 0xcc);
        private static Color upToDateNormal = Color.FromArgb(0xcc, 0xcc, 0xcc);
        private static Color outOfDateFocus = Color.FromArgb(0xff, 0xdd, 0xdd);
        private static Color upToDateFocus = Color.FromArgb(0xdd, 0xdd, 0xdd);
        private static Color outOfDateOldNormal = Color.FromArgb(0xff, 0xbb, 0xbb);
        private static Color upToDateOldNormal = Color.FromArgb(0xbb, 0xbb, 0xbb);
        private static Color outOfDateOldFocus = Color.FromArgb(0xff, 0xcc, 0xcc);
        private static Color upToDateOldFocus = Color.FromArgb(0xcc, 0xcc, 0xcc);

        /// <summary>Holds the settings of the <see cref="TranslationForm&lt;T&gt;"/>.</summary>
        public new class Settings : ManagedForm.Settings
        {
            /// <summary>Remembers the position of the horizontal splitter (between the tree view and the main interface).</summary>
            public int SplitterDistance = 200;
        }

        /// <summary>Main constructor.</summary>
        /// <param name="translationFile">Path and filename to the translation to be edited.</param>
        /// <param name="settings">Settings of the <see cref="TranslationForm&lt;T&gt;"/>.</param>
        public TranslationForm(string translationFile, Settings settings)
            : base(settings)
        {
            _translationFile = translationFile;
            _translation = XmlClassify.LoadObjectFromXmlFile<T>(translationFile);
            _anyChanges = false;

            // some defaults
            Text = "Translating";
            Height = Screen.PrimaryScreen.WorkingArea.Height * 9 / 10;

            // Start creating all the controls
            _pnlMain = new SplitContainerEx
            {
                Dock = DockStyle.Fill,
                FixedPanel = FixedPanel.Panel1,
                Orientation = Orientation.Vertical,
            };
            _pnlMain.Panel2.AutoScroll = true;
            _pnlMain.Panel2.HorizontalScroll.Enabled = false;

            TreeView tv = new TreeView { Dock = DockStyle.Fill, HideSelection = false };
            _pnlMain.Panel1.Controls.Add(tv);
            tv.Nodes.Add(createNodeWithPanels(typeof(T), new T(), _translation, "Main"));
            tv.ExpandAll();

            TableLayoutPanel tlp = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                Dock = DockStyle.Top,
                GrowStyle = TableLayoutPanelGrowStyle.AddRows,
            };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            _pnlMain.Panel2.Controls.Add(tlp);

            TableLayoutPanel pnlBottom = new TableLayoutPanel { Dock = DockStyle.Bottom, ColumnCount = 4, RowCount = 1, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
            Button btnOK = new Button { Text = "&Save and close", Width = 100 };
            Button btnCancel = new Button { Text = "&Discard changes", Width = 100 };
            Button btnApply = new Button { Text = "&Apply changes", Width = 100 };
            pnlBottom.Controls.Add(btnOK, 1, 0);
            pnlBottom.Controls.Add(btnCancel, 2, 0);
            pnlBottom.Controls.Add(btnApply, 3, 0);
            pnlBottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            pnlBottom.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            pnlBottom.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            pnlBottom.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            btnOK.Click += (s, e) => btnClick(DismissButton.OK);
            btnCancel.Click += (s, e) => btnClick(DismissButton.Cancel);
            btnApply.Click += (s, e) => btnClick(DismissButton.Apply);

            Controls.Add(_pnlMain);
            Controls.Add(pnlBottom);

            tv.AfterSelect += (s, e) =>
            {
                _pnlMain.Panel2.VerticalScroll.Value = 0;
                tlp.SuspendLayout();
                tlp.Controls.Clear();
                TranslationTreeNode tn = tv.SelectedNode as TranslationTreeNode;
                if (tn == null)
                    return;
                _translationPanels = tn.TranslationPanels;
                foreach (var pnl in _translationPanels)
                    tlp.Controls.Add(pnl);
                tlp.ResumeLayout(true);
            };

            Load += (s, e) => { _pnlMain.SplitterDistance = settings.SplitterDistance; };
        }

        private void btnClick(DismissButton btn)
        {
            if (btn == DismissButton.Cancel && _anyChanges && DlgMessage.Show("Are you sure you wish to discard all unsaved changes you made to the translation?", "Discard changes", DlgType.Warning, "&Discard", "&Cancel") == 1)
                return;

            if (btn != DismissButton.Cancel && _anyChanges)
            {
                XmlClassify.SaveObjectToXmlFile(_translation, _translationFile);
                if (AcceptChanges != null)
                    AcceptChanges();
            }

            if (btn != DismissButton.Apply)
                Close();
        }

        private TranslationTreeNode createNodeWithPanels(Type type, object original, object translation, string name)
        {
            var tn = new TranslationTreeNode
            {
                TranslationPanels = type.GetFields()
                    .Where(f => f.FieldType == typeof(TrString))
                    .OrderBy(f => f.Name, StringComparer.InvariantCultureIgnoreCase)
                    .Select(f => createFieldPanel(
                        f.GetCustomAttributes(typeof(LingoNotesAttribute), false).Select(a => ((LingoNotesAttribute) a).Notes).Where(s => s != null).JoinString("\n"),
                        (TrString) f.GetValue(original), (TrString) f.GetValue(translation), f.Name))
                    .ToArray(),
                Text = name
            };
            foreach (var f in type.GetFields().Where(f => f.FieldType != typeof(TrString)))
                tn.Nodes.Add(createNodeWithPanels(f.FieldType, f.GetValue(original), f.GetValue(translation), f.Name));
            return tn;
        }

        private TableLayoutPanel createFieldPanel(string notes, TrString orig, TrString trans, string fieldname)
        {
            // A default value
            int margin = 3;

            TableLayoutPanel pnlField = new TableLayoutPanel()
            {
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BorderStyle = BorderStyle.Fixed3D,
                ColumnCount = 3,
                Height = 1,
                Margin = new Padding(1),
                Padding = new Padding(0),
                Width = 1,
            };
            int rows = 3;
            if (!string.IsNullOrEmpty(notes)) rows++;
            bool outOfDate = string.IsNullOrEmpty(trans.OldEnglish);
            if (!string.IsNullOrEmpty(trans.OldEnglish) && trans.OldEnglish != orig.Translation)
            {
                rows++;
                outOfDate = true;
            }
            pnlField.RowCount = rows;

            // Create the textbox a bit early so that we can assign the events which focus it
            TextBoxAutoHeight txtTranslation = new TextBoxAutoHeight()
            {
                Text = trans.Translation,
                Margin = new Padding(margin),
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Multiline = true,
                WordWrap = true,
                AcceptsReturn = true,
                AcceptsTab = false,
                ShortcutsEnabled = true
            };
            pnlField.Click += (s, e) => txtTranslation.Focus();
            pnlField.Tag = new FieldPanelInfo { Translation = trans, Original = orig, TranslationBox = txtTranslation, OutOfDate = outOfDate };

            Label lblStringCode = new Label { Text = fieldname.Replace("&", "&&"), Font = new Font(Font, FontStyle.Bold), AutoSize = true, Margin = new Padding(margin), Anchor = AnchorStyles.Left };
            lblStringCode.Click += (s, e) => txtTranslation.Focus();
            pnlField.Controls.Add(lblStringCode, 0, 0);
            pnlField.SetColumnSpan(lblStringCode, 3);
            pnlField.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            int currow = 1;
            if (!string.IsNullOrEmpty(notes))
            {
                Label lblNotes = new Label { Text = notes.Replace("&", "&&"), AutoSize = true, Font = new Font(Font, FontStyle.Italic), Margin = new Padding(margin), Anchor = AnchorStyles.Left | AnchorStyles.Right };
                lblNotes.Click += (s, e) => txtTranslation.Focus();
                pnlField.Controls.Add(lblNotes, 0, currow);
                pnlField.SetColumnSpan(lblNotes, 3);
                pnlField.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                currow++;
            }
            bool haveOldEnglish = false;
            if (!string.IsNullOrEmpty(trans.OldEnglish) && trans.OldEnglish != orig.Translation)
            {
                haveOldEnglish = true;
                Label lblOldEnglishLbl = new Label { Text = "Old English:", AutoSize = true, Margin = new Padding(margin), Anchor = AnchorStyles.Left };
                lblOldEnglishLbl.Click += (s, e) => txtTranslation.Focus();
                pnlField.Controls.Add(lblOldEnglishLbl, 0, currow);
                Label lblOldEnglish = new Label { Text = trans.OldEnglish.Replace("&", "&&"), AutoSize = true, Margin = new Padding(margin), Anchor = AnchorStyles.Left | AnchorStyles.Right, BackColor = outOfDate ? outOfDateOldNormal : outOfDateNormal };
                lblOldEnglish.Click += (s, e) => txtTranslation.Focus();
                pnlField.Controls.Add(lblOldEnglish, 1, currow);
                pnlField.SetColumnSpan(lblOldEnglish, 2);
                pnlField.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                ((FieldPanelInfo) pnlField.Tag).OldEnglish = lblOldEnglish;
                ((FieldPanelInfo) pnlField.Tag).OldEnglishLabel = lblOldEnglishLbl;
                currow++;
            }
            Label lblNewEnglishLbl = new Label { Text = haveOldEnglish ? "New English:" : "English:", AutoSize = true, Margin = new Padding(margin), Anchor = AnchorStyles.Left };
            lblNewEnglishLbl.Click += (s, e) => txtTranslation.Focus();
            pnlField.Controls.Add(lblNewEnglishLbl, 0, currow);
            Label lblNewEnglish = new Label { Text = orig.Translation.Replace("&", "&&"), AutoSize = true, Margin = new Padding(margin), Anchor = AnchorStyles.Left | AnchorStyles.Right };
            lblNewEnglish.Click += (s, e) => txtTranslation.Focus();
            pnlField.Controls.Add(lblNewEnglish, 1, currow);
            pnlField.SetColumnSpan(lblNewEnglish, 2);
            pnlField.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            ((FieldPanelInfo) pnlField.Tag).NewEnglishLabel = lblNewEnglishLbl;
            currow++;

            Label lblTranslation = new Label { Text = "Translation:", AutoSize = true, Margin = new Padding(margin), Anchor = AnchorStyles.Left };
            lblTranslation.Click += (s, e) => txtTranslation.Focus();
            pnlField.Controls.Add(lblTranslation, 0, currow);
            pnlField.Controls.Add(txtTranslation, 1, currow);
            Button btnAccept = new Button { Text = "OK", Anchor = AnchorStyles.None, Margin = new Padding(margin), BackColor = Color.FromKnownColor(KnownColor.ButtonFace), Width = 30 };
            ((FieldPanelInfo) pnlField.Tag).AcceptButton = btnAccept;

            // assign events
            txtTranslation.Enter += new EventHandler(enter);
            txtTranslation.Leave += new EventHandler(leave);

            btnAccept.Enter += new EventHandler(enter);
            btnAccept.Leave += new EventHandler(leave);
            btnAccept.Click += new EventHandler(acceptTranslation);

            txtTranslation.KeyDown += new KeyEventHandler(keyDown);
            btnAccept.KeyDown += new KeyEventHandler(keyDown);

            pnlField.BackColor = outOfDate ? outOfDateNormal : upToDateNormal;
            pnlField.Controls.Add(btnAccept, 2, currow);
            pnlField.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            pnlField.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            pnlField.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            pnlField.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            currow++;

            btnAccept.Tag = pnlField;
            txtTranslation.Tag = pnlField;

            return pnlField;
        }

        private void acceptTranslation(object sender, EventArgs e)
        {
            _anyChanges = true;
            Panel pnl = (Panel) ((sender is Panel) ? sender : ((Control) sender).Tag);
            pnl.SuspendLayout();
            FieldPanelInfo fpi = (FieldPanelInfo) pnl.Tag;
            fpi.Translation.Translation = fpi.TranslationBox.Text;
            fpi.Translation.OldEnglish = fpi.Original.Translation;
            fpi.OutOfDate = false;
            if (fpi.OldEnglish != null)
            {
                fpi.OldEnglish.Visible = false;
                fpi.OldEnglishLabel.Visible = false;
            }
            fpi.NewEnglishLabel.Text = "English:";
            int index = Array.IndexOf(_translationPanels, pnl);
            if (index < _translationPanels.Length - 1)
                ((FieldPanelInfo) _translationPanels[index + 1].Tag).TranslationBox.Focus();
            else
                fpi.TranslationBox.Focus();
            pnl.ResumeLayout(true);
        }

        private void enter(object sender, EventArgs e)
        {
            Panel pnl = (Panel) ((sender is Panel) ? sender : ((Control) sender).Tag);
            int index = Array.IndexOf(_translationPanels, pnl);
            if (index < 0) return;
            if (index > 0)
                _pnlMain.Panel2.ScrollControlIntoView(_translationPanels[index - 1]);
            if (index < _translationPanels.Length - 1)
                _pnlMain.Panel2.ScrollControlIntoView(_translationPanels[index + 1]);
            _pnlMain.Panel2.ScrollControlIntoView(pnl);
            pnl.BackColor = ((FieldPanelInfo) pnl.Tag).OutOfDate ? outOfDateFocus : upToDateFocus;
            if (((FieldPanelInfo) pnl.Tag).OldEnglish != null)
                ((FieldPanelInfo) pnl.Tag).OldEnglish.BackColor = ((FieldPanelInfo) pnl.Tag).OutOfDate ? outOfDateOldFocus : upToDateOldFocus;
        }

        private void leave(object sender, EventArgs e)
        {
            Panel pnl = (Panel) ((sender is Panel) ? sender : ((Control) sender).Tag);
            pnl.BackColor = ((FieldPanelInfo) pnl.Tag).OutOfDate ? outOfDateNormal : upToDateNormal;
            if (((FieldPanelInfo) pnl.Tag).OldEnglish != null)
                ((FieldPanelInfo) pnl.Tag).OldEnglish.BackColor = ((FieldPanelInfo) pnl.Tag).OutOfDate ? outOfDateOldNormal : upToDateOldNormal;
        }

        private void keyDown(object sender, KeyEventArgs e)
        {
            Panel pnl = (Panel) (sender is Panel ? sender : ((Control) sender).Tag);
            int index = Array.IndexOf(_translationPanels, pnl);
            if (index < 0) return;
            if (e.Control && !e.Alt && !e.Shift)
            {
                if (e.KeyCode == Keys.Up && index > 0)
                {
                    ((FieldPanelInfo) _translationPanels[index - 1].Tag).TranslationBox.Focus();
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.Down && index < _translationPanels.Length - 1)
                {
                    ((FieldPanelInfo) _translationPanels[index + 1].Tag).TranslationBox.Focus();
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.A && sender is TextBoxAutoHeight)
                    ((TextBoxAutoHeight) sender).SelectAll();
            }
            else if (!e.Control && !e.Alt && !e.Shift)
            {
                if (e.KeyCode == Keys.PageUp)
                {
                    ((FieldPanelInfo) _translationPanels[Math.Max(index - 10, 0)].Tag).TranslationBox.Focus();
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.PageDown)
                {
                    ((FieldPanelInfo) _translationPanels[Math.Min(index + 10, _translationPanels.Length - 1)].Tag).TranslationBox.Focus();
                    e.Handled = true;
                }
            }
        }

        private class FieldPanelInfo
        {
            public TrString Translation;
            public TrString Original;
            public TextBoxAutoHeight TranslationBox;
            public Button AcceptButton;
            public Label OldEnglish;
            public Label OldEnglishLabel;
            public Label NewEnglishLabel;
            public bool OutOfDate;
        }

        private class TranslationTreeNode : TreeNode
        {
            public Panel[] TranslationPanels;
        }
    }
}
