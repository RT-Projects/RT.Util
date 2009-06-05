using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using RT.Util.Controls;
using RT.Util.Dialogs;
using RT.Util.ExtensionMethods;
using RT.Util.Forms;
using RT.Util.Xml;
using System.Collections.Generic;

namespace RT.Util.Lingo
{
    internal enum DismissButton { OK, Cancel, Apply };

    /// <summary>Provides a GUI for the user to edit a translation for the application.</summary>
    /// <typeparam name="T">The type containing the <see cref="TrString"/> and <see cref="TrStringNumbers"/> fields to be translated.</typeparam>
    public partial class TranslationForm<T> : ManagedForm where T : TranslationBase, new()
    {
        /// <summary>Used to fire <see cref="AcceptChanges"/>.</summary>
        public delegate void TranslationChangesEventHandler();
        /// <summary>Fires when the user clicks "Save changes" or "Apply changes".</summary>
        public event TranslationChangesEventHandler AcceptChanges;

        private Panel[] _currentlyVisibleTranslationPanels;
        private Panel[] _allTranslationPanels;
        private Panel _pnlRight;
        private ToolStripMenuItem _mnuFindNext;
        private ToolStripMenuItem _mnuFindPrev;
        private TreeView _ctTreeView;
        private Label _lblGroupInfo;
        private Button _btnOK;
        private Button _btnCancel;
        private Button _btnApply;

        private Panel _lastFocusedPanel;
        private string _translationFile;
        private T _translation;
        private bool _anyChanges;
        private Settings _settings;

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
            /// <summary>Remembers the string last typed in the Find dialog.</summary>
            public string LastFindQuery = "";
            /// <summary>Remembers the last settings of the "Search English text" option in the Find dialog.</summary>
            public bool LastFindOrig = true;
            /// <summary>Remembers the last settings of the "Search Translation" option in the Find dialog.</summary>
            public bool LastFindTrans = true;
            /// <summary>Remembers the name of the last font used.</summary>
            public string FontName;
            /// <summary>Remembers the size of the last font used.</summary>
            public float FontSize;
        }

        /// <summary>Main constructor.</summary>
        /// <param name="translationFile">Path and filename to the translation to be edited.</param>
        /// <param name="settings">Settings of the <see cref="TranslationForm&lt;T&gt;"/>.</param>
        public TranslationForm(string translationFile, Settings settings)
            : base(settings)
        {
            _settings = settings;
            _translationFile = translationFile;
            _translation = XmlClassify.LoadObjectFromXmlFile<T>(translationFile);
            _anyChanges = false;

            if (_settings.FontName != null)
                Font = new Font(_settings.FontName, _settings.FontSize, FontStyle.Regular);

            // some defaults
            Text = "Translating";
            Height = Screen.PrimaryScreen.WorkingArea.Height * 9 / 10;

            // Start creating all the controls
            SplitContainerEx pnlSplit = new SplitContainerEx
            {
                Dock = DockStyle.Fill,
                FixedPanel = FixedPanel.Panel1,
                Orientation = Orientation.Vertical,
            };

            _ctTreeView = new TreeView { Dock = DockStyle.Fill, HideSelection = false };
            pnlSplit.Panel1.Controls.Add(_ctTreeView);
            var pnlList = new List<Panel>();
            _ctTreeView.Nodes.Add(createNodeWithPanels(typeof(T), new T(), _translation, pnlList));
            _allTranslationPanels = pnlList.ToArray();
            _ctTreeView.ExpandAll();

            TableLayoutPanel tlp = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                Dock = DockStyle.Top,
                GrowStyle = TableLayoutPanelGrowStyle.AddRows,
            };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            TableLayoutPanel topInfo = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                Dock = DockStyle.Top,
                RowCount = 1,
                BackColor = Color.Navy
            };
            topInfo.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            topInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            _lblGroupInfo = new Label
            {
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                AutoSize = true,
                Dock = DockStyle.Top,
                Padding = new Padding(5),
                ForeColor = Color.White,
                Font = new Font(Font, FontStyle.Bold)
            };
            topInfo.Controls.Add(_lblGroupInfo);

            _pnlRight = new Panel { AutoScroll = true, Dock = DockStyle.Fill };
            _pnlRight.Controls.Add(tlp);
            pnlSplit.Panel2.Controls.Add(_pnlRight);
            pnlSplit.Panel2.Controls.Add(topInfo);

            TableLayoutPanel pnlBottom = new TableLayoutPanel { Dock = DockStyle.Bottom, ColumnCount = 4, RowCount = 1, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
            _btnOK = new Button { Text = "&Save and close" };
            _btnCancel = new Button { Text = "&Discard changes" };
            _btnApply = new Button { Text = "&Apply changes" };
            pnlBottom.Controls.Add(_btnOK, 1, 0);
            pnlBottom.Controls.Add(_btnCancel, 2, 0);
            pnlBottom.Controls.Add(_btnApply, 3, 0);
            pnlBottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            pnlBottom.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            pnlBottom.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            pnlBottom.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            _btnOK.Click += (s, e) => btnClick(DismissButton.OK);
            _btnCancel.Click += (s, e) => btnClick(DismissButton.Cancel);
            _btnApply.Click += (s, e) => btnClick(DismissButton.Apply);

            _ctTreeView.AfterSelect += (s, e) =>
            {
                _pnlRight.VerticalScroll.Value = 0;
                tlp.SuspendLayout();
                tlp.Controls.Clear();
                TranslationTreeNode tn = _ctTreeView.SelectedNode as TranslationTreeNode;
                if (tn == null)
                    return;
                _currentlyVisibleTranslationPanels = tn.TranslationPanels;
                tlp.Controls.AddRange(_currentlyVisibleTranslationPanels);
                tlp.ResumeLayout(true);
                if (_currentlyVisibleTranslationPanels.Length > 0)
                    ((FieldPanelInfo) _currentlyVisibleTranslationPanels[0].Tag).TranslationBox.Focus();
                _lblGroupInfo.Text = tn.Notes;
            };

            Load += (s, e) => { pnlSplit.SplitterDistance = settings.SplitterDistance; setButtonSizes(); };
            FormClosing += (s, e) =>
            {
                settings.SplitterDistance = pnlSplit.SplitterDistance;
                if (_anyChanges && DlgMessage.Show("Are you sure you wish to discard all unsaved changes you made to the translation?", "Discard changes", DlgType.Warning, "&Discard", "&Cancel") == 1)
                    e.Cancel = true;
            };

            ToolStrip ts = new ToolStrip(
                new ToolStripMenuItem("&Translation", null,
                    new ToolStripMenuItem("&Find...", null, new EventHandler(find)) { ShortcutKeys = Keys.Control | Keys.F },
                    _mnuFindNext = new ToolStripMenuItem("F&ind next", null, new EventHandler(findNext)) { ShortcutKeys = Keys.F3, Enabled = _settings.LastFindQuery != null },
                    _mnuFindPrev = new ToolStripMenuItem("Find &previous", null, new EventHandler(findPrev)) { ShortcutKeys = Keys.Shift | Keys.F3, Enabled = _settings.LastFindQuery != null },
                    new ToolStripMenuItem("Go to &next out-of-date string", null, new EventHandler(nextOutOfDate)) { ShortcutKeys = Keys.Control | Keys.N },
                    new ToolStripMenuItem("Fon&t...", null, new EventHandler(setFont)) { ShortcutKeys = Keys.Control | Keys.T }
                )
            ) { Dock = DockStyle.Top };

            Controls.Add(pnlSplit);
            Controls.Add(pnlBottom);
            Controls.Add(ts);
        }

        private void setButtonSizes()
        {
            Size f = TextRenderer.MeasureText(_btnOK.Text, Font);
            int w = f.Width;
            w = Math.Max(w, TextRenderer.MeasureText(_btnCancel.Text, Font).Width);
            w = Math.Max(w, TextRenderer.MeasureText(_btnApply.Text, Font).Width);
            _btnOK.Width = _btnCancel.Width = _btnApply.Width = w + 20;
            _btnOK.Height = _btnCancel.Height = _btnApply.Height = f.Height + 10;
            f = TextRenderer.MeasureText("OK", Font) + new Size(10, 10);
            foreach (var pnl in _allTranslationPanels)
                ((FieldPanelInfo) pnl.Tag).AcceptButton.Size = f;
        }

        private void setFont(object sender, EventArgs e)
        {
            using (FontDialog fd = new FontDialog())
            {
                fd.Font = Font;
                if (fd.ShowDialog() == DialogResult.OK)
                {
                    _pnlRight.SuspendLayout();
                    Font = new Font(fd.Font, FontStyle.Regular);
                    _lblGroupInfo.Font = new Font(fd.Font, FontStyle.Bold);
                    foreach (var pnl in _allTranslationPanels)
                    {
                        pnl.SuspendLayout();
                        ((FieldPanelInfo) pnl.Tag).StringCodeLabel.Font = new Font(fd.Font, FontStyle.Bold);
                        if (((FieldPanelInfo) pnl.Tag).NotesLabel != null)
                            ((FieldPanelInfo) pnl.Tag).NotesLabel.Font = new Font(fd.Font, FontStyle.Italic);
                    }
                    setButtonSizes();
                    foreach (var pnl in _allTranslationPanels)
                        pnl.ResumeLayout(true);
                    _settings.FontName = fd.Font.Name;
                    _settings.FontSize = fd.Font.Size;
                    _pnlRight.ResumeLayout(true);
                }
            }
        }

        private void find(object sender, EventArgs e)
        {
            using (Form ff = new Form())
            {
                ff.AutoSize = true;
                ff.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                ff.Text = "Find";
                ff.FormBorderStyle = FormBorderStyle.FixedDialog;
                TableLayoutPanel tp = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 5, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(5) };
                tp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
                tp.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
                tp.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
                tp.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                tp.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                tp.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                tp.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                tp.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                Label lbl = new Label { Text = "&Find text:", AutoSize = true, Anchor = AnchorStyles.Left };
                tp.Controls.Add(lbl, 0, 0); tp.SetColumnSpan(lbl, 3);
                TextBox txt = new TextBox { Text = _settings.LastFindQuery, Anchor = AnchorStyles.Left | AnchorStyles.Right };
                tp.Controls.Add(txt, 0, 1); tp.SetColumnSpan(txt, 3);
                CheckBox optOrig = new CheckBox { Text = "Search &English text", Checked = _settings.LastFindOrig, AutoSize = true, Anchor = AnchorStyles.Left };
                tp.Controls.Add(optOrig, 0, 2); tp.SetColumnSpan(optOrig, 3);
                CheckBox optTrans = new CheckBox { Text = "Search &Translations", Checked = _settings.LastFindTrans, AutoSize = true, Anchor = AnchorStyles.Left };
                tp.Controls.Add(optTrans, 0, 3); tp.SetColumnSpan(optTrans, 3);
                Button btnOK = new Button { Text = "OK", Anchor = AnchorStyles.Right };
                EventHandler evh = (s, v) => { btnOK.Enabled = (optOrig.Checked || optTrans.Checked) && txt.TextLength > 0; };
                optOrig.CheckedChanged += evh;
                optTrans.CheckedChanged += evh;
                txt.TextChanged += evh;
                btnOK.Click += (s, v) => { ff.DialogResult = DialogResult.OK; };
                tp.Controls.Add(btnOK, 1, 4);
                Button btnCancel = new Button { Text = "Cancel", Anchor = AnchorStyles.Right };
                tp.Controls.Add(btnCancel, 2, 4);
                ff.Controls.Add(tp);
                ff.AcceptButton = btnOK;
                ff.CancelButton = btnCancel;
                ff.Load += (s, v) => { ff.Location = new Point((Screen.PrimaryScreen.WorkingArea.Width - ff.Width) / 2 + Screen.PrimaryScreen.WorkingArea.X, (Screen.PrimaryScreen.WorkingArea.Height - ff.Height) / 2 + Screen.PrimaryScreen.WorkingArea.Y); };
                if (ff.ShowDialog() == DialogResult.OK)
                {
                    _settings.LastFindQuery = txt.Text;
                    _settings.LastFindOrig = optOrig.Checked;
                    _settings.LastFindTrans = optTrans.Checked;
                    _mnuFindNext.Enabled = true;
                    findNext(sender, e);
                }
            }
        }

        private void findNext(object sender, EventArgs e)
        {
            if (!_settings.LastFindOrig && !_settings.LastFindTrans)
            {
                MessageBox.Show("You unchecked both \"Search English text\" and \"Search Translations\". That leaves nothing to be searched.", "Nothing to search");
                return;
            }
            int start = _lastFocusedPanel == null ? 0 : Array.IndexOf(_allTranslationPanels, _lastFocusedPanel) + 1;
            int finish = _lastFocusedPanel == null ? _allTranslationPanels.Length - 1 : start - 1;
            for (int i = start % _allTranslationPanels.Length; i != finish; i = (i + 1) % _allTranslationPanels.Length)
            {
                if ((_settings.LastFindOrig && ((FieldPanelInfo) _allTranslationPanels[i].Tag).Original.Translation.Contains(_settings.LastFindQuery)) ||
                    (_settings.LastFindTrans && ((FieldPanelInfo) _allTranslationPanels[i].Tag).Translation.Translation.Contains(_settings.LastFindQuery)))
                {
                    _ctTreeView.SelectedNode = ((FieldPanelInfo) _allTranslationPanels[i].Tag).TreeNode;
                    ((FieldPanelInfo) _allTranslationPanels[i].Tag).TranslationBox.Focus();
                    return;
                }
            }
            MessageBox.Show("No matching strings found.", "Find");
        }

        private void findPrev(object sender, EventArgs e)
        {
            if (!_settings.LastFindOrig && !_settings.LastFindTrans)
            {
                MessageBox.Show("You unchecked both \"Search English text\" and \"Search Translations\". That leaves nothing to be searched.", "Nothing to search");
                return;
            }
            int start = _lastFocusedPanel == null ? _allTranslationPanels.Length - 1 : Array.IndexOf(_allTranslationPanels, _lastFocusedPanel) - 1;
            int finish = _lastFocusedPanel == null ? 0 : start + 1;
            for (int i = (start + _allTranslationPanels.Length) % _allTranslationPanels.Length; i != finish; i = (i + _allTranslationPanels.Length - 1) % _allTranslationPanels.Length)
            {
                if ((_settings.LastFindOrig && ((FieldPanelInfo) _allTranslationPanels[i].Tag).Original.Translation.Contains(_settings.LastFindQuery)) ||
                    (_settings.LastFindTrans && ((FieldPanelInfo) _allTranslationPanels[i].Tag).Translation.Translation.Contains(_settings.LastFindQuery)))
                {
                    _ctTreeView.SelectedNode = ((FieldPanelInfo) _allTranslationPanels[i].Tag).TreeNode;
                    ((FieldPanelInfo) _allTranslationPanels[i].Tag).TranslationBox.Focus();
                    return;
                }
            }
            MessageBox.Show("No matching strings found.", "Find");
        }

        private void nextOutOfDate(object sender, EventArgs e)
        {
            int start = _lastFocusedPanel == null ? 0 : Array.IndexOf(_allTranslationPanels, _lastFocusedPanel) + 1;
            int finish = _lastFocusedPanel == null ? _allTranslationPanels.Length - 1 : start - 1;
            for (int i = start % _allTranslationPanels.Length; i != finish; i = (i + 1) % _allTranslationPanels.Length)
            {
                if (((FieldPanelInfo) _allTranslationPanels[i].Tag).OutOfDate)
                {
                    _ctTreeView.SelectedNode = ((FieldPanelInfo) _allTranslationPanels[i].Tag).TreeNode;
                    ((FieldPanelInfo) _allTranslationPanels[i].Tag).TranslationBox.Focus();
                    return;
                }
            }
            MessageBox.Show("All strings are up to date.", "Next out-of-date string");
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
            _anyChanges = false;

            if (btn != DismissButton.Apply)
                Close();
        }

        private TranslationTreeNode createNodeWithPanels(Type type, object original, object translation, List<Panel> pnlList)
        {
            var attrs = type.GetCustomAttributes(typeof(LingoGroupAttribute), true);
            if (!attrs.Any())
                throw new ArgumentException("Classes with translatable strings are not allowed to contain any fields other than fields of type TrString, TrStringNumbers, and types with the [LingoGroup] attribute.", "type");
            LingoGroupAttribute lga = (LingoGroupAttribute) attrs.First();
            var tn = new TranslationTreeNode { Text = lga.Label, Notes = lga.Description };
            tn.TranslationPanels = type.GetFields()
                .Where(f => f.FieldType == typeof(TrString))
                // .OrderBy(f => f.Name, StringComparer.InvariantCultureIgnoreCase)
                .Select(f => createFieldPanel(
                    f.GetCustomAttributes(typeof(LingoNotesAttribute), false).Select(a => ((LingoNotesAttribute) a).Notes).Where(s => s != null).JoinString("\n"),
                    (TrString) f.GetValue(original), (TrString) f.GetValue(translation), f.Name, tn))
                .ToArray();
            pnlList.AddRange(tn.TranslationPanels);
            foreach (var f in type.GetFields().Where(f => f.FieldType != typeof(TrString) && f.FieldType != typeof(TrStringNumbers) && f.Name != "Language"))
                tn.Nodes.Add(createNodeWithPanels(f.FieldType, f.GetValue(original), f.GetValue(translation), pnlList));
            return tn;
        }

        private TableLayoutPanel createFieldPanel(string notes, TrString orig, TrString trans, string fieldname, TranslationTreeNode tn)
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
            txtTranslation.TextChanged += new EventHandler(modifyTranslation);
            pnlField.Click += (s, e) => txtTranslation.Focus();
            pnlField.Tag = new FieldPanelInfo { Translation = trans, Original = orig, TranslationBox = txtTranslation, OutOfDate = outOfDate, TreeNode = tn };

            Label lblStringCode = new Label { Text = fieldname.Replace("&", "&&"), Font = new Font(Font, FontStyle.Bold), AutoSize = true, Margin = new Padding(margin), Anchor = AnchorStyles.Left };
            lblStringCode.Click += (s, e) => txtTranslation.Focus();
            pnlField.Controls.Add(lblStringCode, 0, 0);
            pnlField.SetColumnSpan(lblStringCode, 3);
            pnlField.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            ((FieldPanelInfo) pnlField.Tag).StringCodeLabel = lblStringCode;

            int currow = 1;
            if (!string.IsNullOrEmpty(notes))
            {
                Label lblNotes = new Label { Text = notes.Replace("&", "&&"), AutoSize = true, Font = new Font(Font, FontStyle.Italic), Margin = new Padding(margin), Anchor = AnchorStyles.Left | AnchorStyles.Right };
                lblNotes.Click += (s, e) => txtTranslation.Focus();
                pnlField.Controls.Add(lblNotes, 0, currow);
                pnlField.SetColumnSpan(lblNotes, 3);
                pnlField.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                ((FieldPanelInfo) pnlField.Tag).NotesLabel = lblNotes;
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
            Button btnAccept = new Button { Text = "OK", Anchor = AnchorStyles.None, Margin = new Padding(margin), BackColor = Color.FromKnownColor(KnownColor.ButtonFace) };
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
            int index = Array.IndexOf(_currentlyVisibleTranslationPanels, pnl);
            if (index < _currentlyVisibleTranslationPanels.Length - 1)
                ((FieldPanelInfo) _currentlyVisibleTranslationPanels[index + 1].Tag).TranslationBox.Focus();
            else
                fpi.TranslationBox.Focus();
            pnl.ResumeLayout(true);
        }

        private void modifyTranslation(object sender, EventArgs e)
        {
            _anyChanges = true;
            Panel pnl = (Panel) ((sender is Panel) ? sender : ((Control) sender).Tag);
            ((FieldPanelInfo) pnl.Tag).OutOfDate = true;
            enter(sender, e);
        }

        private void enter(object sender, EventArgs e)
        {
            Panel pnl = (Panel) ((sender is Panel) ? sender : ((Control) sender).Tag);
            _lastFocusedPanel = pnl;
            int index = Array.IndexOf(_currentlyVisibleTranslationPanels, pnl);
            if (index < 0) return;
            if (index > 0)
                _pnlRight.ScrollControlIntoView(_currentlyVisibleTranslationPanels[index - 1]);
            if (index < _currentlyVisibleTranslationPanels.Length - 1)
                _pnlRight.ScrollControlIntoView(_currentlyVisibleTranslationPanels[index + 1]);
            _pnlRight.ScrollControlIntoView(pnl);
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
            int index = Array.IndexOf(_currentlyVisibleTranslationPanels, pnl);
            if (index < 0) return;
            if (e.Control && !e.Alt && !e.Shift)
            {
                if (e.KeyCode == Keys.Up && index > 0)
                {
                    ((FieldPanelInfo) _currentlyVisibleTranslationPanels[index - 1].Tag).TranslationBox.Focus();
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.Down && index < _currentlyVisibleTranslationPanels.Length - 1)
                {
                    ((FieldPanelInfo) _currentlyVisibleTranslationPanels[index + 1].Tag).TranslationBox.Focus();
                    e.Handled = true;
                }
                else if ((e.KeyCode == Keys.Home || e.KeyCode == Keys.End) && _currentlyVisibleTranslationPanels.Length > 0)
                {
                    ((FieldPanelInfo) _currentlyVisibleTranslationPanels[e.KeyCode == Keys.Home ? 0 : _currentlyVisibleTranslationPanels.Length - 1].Tag).TranslationBox.Focus();
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.A && sender is TextBoxAutoHeight)
                    ((TextBoxAutoHeight) sender).SelectAll();
            }
            else if (!e.Control && !e.Alt && !e.Shift)
            {
                if (e.KeyCode == Keys.PageUp)
                {
                    ((FieldPanelInfo) _currentlyVisibleTranslationPanels[Math.Max(index - 10, 0)].Tag).TranslationBox.Focus();
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.PageDown)
                {
                    ((FieldPanelInfo) _currentlyVisibleTranslationPanels[Math.Min(index + 10, _currentlyVisibleTranslationPanels.Length - 1)].Tag).TranslationBox.Focus();
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
            public Label StringCodeLabel;
            public Label NotesLabel;
            public bool OutOfDate;
            public TranslationTreeNode TreeNode;
        }

        private class TranslationTreeNode : TreeNode
        {
            public Panel[] TranslationPanels;
            public string Notes;
        }
    }
}
