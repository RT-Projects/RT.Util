﻿using System;
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
    /// <summary>Provides a GUI for the user to edit a translation for the application.</summary>
    /// <typeparam name="T">The type containing the <see cref="TrString"/> and <see cref="TrStringNumbers"/> fields to be translated.</typeparam>
    public partial class TranslationForm<T> : ManagedForm where T : TranslationBase, new()
    {
        /// <summary>Used to fire <see cref="AcceptChanges"/>.</summary>
        public delegate void TranslationChangesEventHandler();
        /// <summary>Fires when the user clicks "Save &amp; Close" or "Apply changes".</summary>
        public event TranslationChangesEventHandler AcceptChanges;

        private TranslationPanel[] _currentlyVisibleTranslationPanels;
        private TranslationPanel[] _allTranslationPanels;
        private Panel _pnlRight;
        private ToolStripMenuItem _mnuFindNext;
        private ToolStripMenuItem _mnuFindPrev;
        private TreeView _ctTreeView;
        private Label _lblGroupInfo;
        private Button _btnOK;
        private Button _btnCancel;
        private Button _btnApply;

        private TranslationPanel _lastFocusedPanel;
        private string _translationFile;
        private T _translation;
        private bool _anyChanges;
        private Settings _settings;

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
            var lstPanels = new List<TranslationPanel>();
            _ctTreeView.Nodes.Add(createNodeWithPanels(typeof(T), new T(), _translation, lstPanels));
            _allTranslationPanels = lstPanels.ToArray();
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
            _btnOK.Click += new EventHandler(btnClick);
            _btnCancel.Click += new EventHandler(btnClick);
            _btnApply.Click += new EventHandler(btnClick);

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
                    _currentlyVisibleTranslationPanels[0].FocusTranslationBox();
                _ctTreeView.Focus();
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

            if (_settings.FontName != null)
                setFont(new Font(_settings.FontName, _settings.FontSize, FontStyle.Regular));
        }

        private void setButtonSizes()
        {
            Size f = TextRenderer.MeasureText(_btnOK.Text, Font);
            int w = f.Width;
            w = Math.Max(w, TextRenderer.MeasureText(_btnCancel.Text, Font).Width);
            w = Math.Max(w, TextRenderer.MeasureText(_btnApply.Text, Font).Width);
            _btnOK.Width = _btnCancel.Width = _btnApply.Width = w + 20;
            _btnOK.Height = _btnCancel.Height = _btnApply.Height = f.Height + 10;
        }

        private void setFont(object sender, EventArgs e)
        {
            using (FontDialog fd = new FontDialog())
            {
                fd.Font = Font;
                if (fd.ShowDialog() == DialogResult.OK)
                    setFont(fd.Font);
            }
        }

        private void setFont(Font font)
        {
            // Calculate the new size of the "OK" buttons in each panel
            var f = TextRenderer.MeasureText("OK", font) + new Size(10, 10);

            _pnlRight.SuspendLayout();
            Font = new Font(font, FontStyle.Regular);
            _lblGroupInfo.Font = new Font(font, FontStyle.Bold);
            foreach (var pnl in _allTranslationPanels)
            {
                pnl.SuspendLayout();
                pnl.SetFont(font, f);
            }
            setButtonSizes();
            foreach (var pnl in _allTranslationPanels)
                pnl.ResumeLayout(true);
            _settings.FontName = font.Name;
            _settings.FontSize = font.Size;
            _pnlRight.ResumeLayout(true);
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
                if (_allTranslationPanels[i].Contains(_settings.LastFindQuery, _settings.LastFindOrig, _settings.LastFindTrans))
                {
                    _ctTreeView.SelectedNode = _allTranslationPanels[i].TreeNode;
                    _allTranslationPanels[i].FocusTranslationBox();
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
                if (_allTranslationPanels[i].Contains(_settings.LastFindQuery, _settings.LastFindOrig, _settings.LastFindTrans))
                {
                    _ctTreeView.SelectedNode = _allTranslationPanels[i].TreeNode;
                    _allTranslationPanels[i].FocusTranslationBox();
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
                if (_allTranslationPanels[i].OutOfDate)
                {
                    _ctTreeView.SelectedNode = _allTranslationPanels[i].TreeNode;
                    _allTranslationPanels[i].FocusTranslationBox();
                    return;
                }
            }
            MessageBox.Show("All strings are up to date.", "Next out-of-date string");
        }

        private void btnClick(object sender, EventArgs e)
        {
            if (sender == _btnCancel && _anyChanges && DlgMessage.Show("Are you sure you wish to discard all unsaved changes you made to the translation?", "Discard changes", DlgType.Warning, "&Discard", "&Cancel") == 1)
                return;

            if (sender != _btnCancel && _anyChanges)
            {
                XmlClassify.SaveObjectToXmlFile(_translation, _translationFile);
                if (AcceptChanges != null)
                    AcceptChanges();
            }
            _anyChanges = false;

            if (sender != _btnApply)
                Close();
        }

        private TranslationTreeNode createNodeWithPanels(Type type, object original, object translation, List<TranslationPanel> lstPanels)
        {
            var attrs = type.GetCustomAttributes(typeof(LingoGroupAttribute), true);
            if (!attrs.Any())
                throw new ArgumentException("Classes with translatable strings are not allowed to contain any fields other than fields of type TrString, TrStringNumbers, and types with the [LingoGroup] attribute.", "type");
            LingoGroupAttribute lga = (LingoGroupAttribute) attrs.First();
            var tn = new TranslationTreeNode { Text = lga.Label, Notes = lga.Description };
            tn.TranslationPanels = type.GetFields()
                .Where(f => f.FieldType == typeof(TrString))
                .Select(f => createTranslationPanel(
                    f.GetCustomAttributes(typeof(LingoNotesAttribute), false).Select(a => ((LingoNotesAttribute) a).Notes).Where(s => s != null).JoinString("\n"),
                    f.GetValue(original), f.GetValue(translation), f.Name, tn))
                .ToArray();
            lstPanels.AddRange(tn.TranslationPanels);
            foreach (var f in type.GetFields().Where(f => f.FieldType != typeof(TrString) && f.FieldType != typeof(TrStringNumbers) && f.Name != "Language"))
                tn.Nodes.Add(createNodeWithPanels(f.FieldType, f.GetValue(original), f.GetValue(translation), lstPanels));
            return tn;
        }

        private TranslationPanel createTranslationPanel(string notes, object orig, object trans, string fieldname, TranslationTreeNode tn)
        {
            if (orig is TrString && trans is TrString)
            {
                TranslationPanelTrString pnl = new TranslationPanelTrString(notes, (TrString) orig, (TrString) trans, fieldname, tn);
                pnl.ChangeMade += (s, e) => { _anyChanges = true; };
                pnl.EnterPanel += new EventHandler(enterPanel);
                pnl.CtrlUp += new EventHandler(ctrlUp);
                pnl.CtrlDown += new EventHandler(ctrlDown);
                pnl.CtrlHome += new EventHandler(ctrlHome);
                pnl.CtrlEnd += new EventHandler(ctrlEnd);
                pnl.PageUp += new EventHandler(pageUp);
                pnl.PageDown += new EventHandler(pageDown);
                return pnl;
            }
            if (orig is TrStringNumbers && trans is TrStringNumbers)
            {
                /*
                TranslationPanelTrStringNumbers pnl = new TranslationPanelTrStringNumbers(notes, (TrStringNumbers) orig, (TrStringNumbers) trans, fieldname, tn);
                pnl.ChangeMade += (s, e) => { _anyChanges = true; };
                pnl.EnterPanel += new EventHandler(enterPanel);
                pnl.CtrlUp += new EventHandler(ctrlUp);
                pnl.CtrlDown += new EventHandler(ctrlDown);
                pnl.CtrlHome += new EventHandler(ctrlHome);
                pnl.CtrlEnd += new EventHandler(ctrlEnd);
                pnl.PageUp += new EventHandler(pageUp);
                pnl.PageDown += new EventHandler(pageDown);
                return pnl;
                 * */
            }
            return null;
        }

        private void ctrlUp(object sender, EventArgs e)
        {
            TranslationPanel pnl = (TranslationPanel) sender;
            int index = Array.IndexOf(_currentlyVisibleTranslationPanels, pnl);
            if (index > 0)
                _currentlyVisibleTranslationPanels[index - 1].FocusTranslationBox();
        }

        private void ctrlDown(object sender, EventArgs e)
        {
            TranslationPanel pnl = (TranslationPanel) sender;
            int index = Array.IndexOf(_currentlyVisibleTranslationPanels, pnl);
            if (index >= 0 && index < _currentlyVisibleTranslationPanels.Length - 1)
                _currentlyVisibleTranslationPanels[index + 1].FocusTranslationBox();
        }

        private void ctrlHome(object sender, EventArgs e)
        {
            TranslationPanel pnl = (TranslationPanel) sender;
            int index = Array.IndexOf(_currentlyVisibleTranslationPanels, pnl);
            if (index >= 0 && _currentlyVisibleTranslationPanels.Length > 0)
                _currentlyVisibleTranslationPanels[0].FocusTranslationBox();
        }

        private void ctrlEnd(object sender, EventArgs e)
        {
            TranslationPanel pnl = (TranslationPanel) sender;
            int index = Array.IndexOf(_currentlyVisibleTranslationPanels, pnl);
            if (index >= 0 && _currentlyVisibleTranslationPanels.Length > 0)
                _currentlyVisibleTranslationPanels[_currentlyVisibleTranslationPanels.Length - 1].FocusTranslationBox();
        }

        private void pageUp(object sender, EventArgs e)
        {
            TranslationPanel pnl = (TranslationPanel) sender;
            int index = Array.IndexOf(_currentlyVisibleTranslationPanels, pnl);
            if (index >= 0 && _currentlyVisibleTranslationPanels.Length > 0)
                _currentlyVisibleTranslationPanels[Math.Max(index - 10, 0)].FocusTranslationBox();
        }

        private void pageDown(object sender, EventArgs e)
        {
            TranslationPanel pnl = (TranslationPanel) sender;
            int index = Array.IndexOf(_currentlyVisibleTranslationPanels, pnl);
            if (index >= 0 && _currentlyVisibleTranslationPanels.Length > 0)
                _currentlyVisibleTranslationPanels[Math.Min(index + 10, _currentlyVisibleTranslationPanels.Length - 1)].FocusTranslationBox();
        }

        private void enterPanel(object sender, EventArgs e)
        {
            _lastFocusedPanel = (TranslationPanel) sender;
            int index = Array.IndexOf(_currentlyVisibleTranslationPanels, _lastFocusedPanel);
            if (index < 0) return;
            if (index > 0)
                _pnlRight.ScrollControlIntoView(_currentlyVisibleTranslationPanels[index - 1]);
            if (index < _currentlyVisibleTranslationPanels.Length - 1)
                _pnlRight.ScrollControlIntoView(_currentlyVisibleTranslationPanels[index + 1]);
            _pnlRight.ScrollControlIntoView(_lastFocusedPanel);
        }

        private class TranslationTreeNode : TreeNode
        {
            public TranslationPanel[] TranslationPanels;
            public string Notes;
        }

        private abstract class TranslationPanel : TableLayoutPanel
        {
            protected static readonly int margin = 3;

            private TranslationTreeNode _treeNode;
            public TranslationTreeNode TreeNode { get { return _treeNode; } }

            protected Button _btnAccept;
            private Label _lblOldEnglishLbl;
            private Label _lblNewEnglishLbl;
            private Label _lblStringCode;
            private Label _lblNotes;

            private bool _outOfDate;
            public bool OutOfDate
            {
                get { return _outOfDate; }
                set { _outOfDate = value; setBackColor(); }
            }

            protected bool _anythingFocused;
            public bool AnythingFocused
            {
                get { return _anythingFocused; }
                set { _anythingFocused = value; setBackColor(); }
            }

            public TranslationPanel(TranslationTreeNode tn, string notes, string fieldname, bool outOfDate, bool needOldRow)
                : base()
            {
                _treeNode = tn;

                // Calculate number of rows
                int rows = 3;
                if (!string.IsNullOrEmpty(notes)) rows++;
                _outOfDate = outOfDate;
                if (needOldRow)
                    rows++;

                Anchor = AnchorStyles.Left | AnchorStyles.Right;
                AutoSize = true;
                AutoSizeMode = AutoSizeMode.GrowAndShrink;
                BorderStyle = BorderStyle.Fixed3D;
                ColumnCount = 3;
                Height = 1;
                Margin = new Padding(1);
                Padding = new Padding(0);
                Width = 1;
                RowCount = rows;

                _lblStringCode = new Label { Text = fieldname.Replace("&", "&&"), Font = new Font(Font, FontStyle.Bold), AutoSize = true, Margin = new Padding(margin), Anchor = AnchorStyles.Left };
                _lblStringCode.Click += new EventHandler(focusTranslationBox);
                Controls.Add(_lblStringCode, 0, 0);
                SetColumnSpan(_lblStringCode, 3);
                RowStyles.Add(new RowStyle(SizeType.AutoSize));

                int currow = 1;
                if (!string.IsNullOrEmpty(notes))
                {
                    _lblNotes = new Label { Text = notes.Replace("&", "&&"), AutoSize = true, Font = new Font(Font, FontStyle.Italic), Margin = new Padding(margin), Anchor = AnchorStyles.Left | AnchorStyles.Right };
                    _lblNotes.Click += new EventHandler(focusTranslationBox);
                    Controls.Add(_lblNotes, 0, currow);
                    SetColumnSpan(_lblNotes, 3);
                    RowStyles.Add(new RowStyle(SizeType.AutoSize));
                    currow++;
                }
                bool haveOldEnglish = false;
                if (needOldRow)
                {
                    haveOldEnglish = true;
                    _lblOldEnglishLbl = new Label { Text = "Old English:", AutoSize = true, Margin = new Padding(margin), Anchor = AnchorStyles.Left };
                    _lblOldEnglishLbl.Click += new EventHandler(focusTranslationBox);
                    Controls.Add(_lblOldEnglishLbl, 0, currow);
                    RowStyles.Add(new RowStyle(SizeType.AutoSize));
                    currow++;
                }
                _lblNewEnglishLbl = new Label { Text = haveOldEnglish ? "New English:" : "English:", AutoSize = true, Margin = new Padding(margin), Anchor = AnchorStyles.Left };
                _lblNewEnglishLbl.Click += new EventHandler(focusTranslationBox);
                Controls.Add(_lblNewEnglishLbl, 0, currow);
                RowStyles.Add(new RowStyle(SizeType.AutoSize));
                currow++;

                Label lblTranslation = new Label { Text = "Translation:", AutoSize = true, Margin = new Padding(margin), Anchor = AnchorStyles.Left };
                lblTranslation.Click += new EventHandler(focusTranslationBox);
                Controls.Add(lblTranslation, 0, currow);
                _btnAccept = new Button { Text = "OK", Anchor = AnchorStyles.None, Margin = new Padding(margin), BackColor = Color.FromKnownColor(KnownColor.ButtonFace) };

                // assign events
                Click += new EventHandler(focusTranslationBox);
                _btnAccept.Enter += (s, e) => { AnythingFocused = true; fireEnterPanel(e); };
                _btnAccept.Leave += (s, e) => { AnythingFocused = false; };
                _btnAccept.Click += new EventHandler(acceptTranslation);

                Controls.Add(_btnAccept, 2, currow);
                RowStyles.Add(new RowStyle(SizeType.AutoSize));
                ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
                ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
                ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
                currow++;

                _btnAccept.Tag = this;
                setBackColor();
            }

            protected static Color outOfDateNormal = Color.FromArgb(0xff, 0xcc, 0xcc);
            protected static Color upToDateNormal = Color.FromArgb(0xcc, 0xcc, 0xcc);
            protected static Color outOfDateFocus = Color.FromArgb(0xff, 0xdd, 0xdd);
            protected static Color upToDateFocus = Color.FromArgb(0xdd, 0xdd, 0xdd);
            protected static Color outOfDateOldNormal = Color.FromArgb(0xff, 0xbb, 0xbb);
            protected static Color upToDateOldNormal = Color.FromArgb(0xbb, 0xbb, 0xbb);
            protected static Color outOfDateOldFocus = Color.FromArgb(0xff, 0xcc, 0xcc);
            protected static Color upToDateOldFocus = Color.FromArgb(0xcc, 0xcc, 0xcc);

            public abstract bool Contains(string substring, bool inOriginal, bool inTranslation);
            public abstract void FocusTranslationBox();
            public virtual void SetFont(Font font, Size f)
            {
                _lblStringCode.Font = new Font(font, FontStyle.Bold);
                if (_lblNotes != null)
                    _lblNotes.Font = new Font(font, FontStyle.Italic);
                _btnAccept.Size = f;
            }

            protected abstract void fireEnterPanel(EventArgs e);
            protected virtual void focusTranslationBox(object sender, EventArgs e) { FocusTranslationBox(); }
            protected virtual void setBackColor() { BackColor = _anythingFocused ? (_outOfDate ? outOfDateFocus : upToDateFocus) : (_outOfDate ? outOfDateNormal : upToDateNormal); }
            protected virtual void acceptTranslation(object sender, EventArgs e)
            {
                OutOfDate = false;
                if (_lblOldEnglishLbl != null)
                    _lblOldEnglishLbl.Visible = false;
                _lblNewEnglishLbl.Text = "English:";
            }
        }

        private class TranslationPanelTrString : TranslationPanel
        {
            public event EventHandler ChangeMade;
            public event EventHandler EnterPanel;
            public event EventHandler CtrlUp;
            public event EventHandler CtrlDown;
            public event EventHandler PageUp;
            public event EventHandler PageDown;
            public event EventHandler CtrlHome;
            public event EventHandler CtrlEnd;

            #region X
            private TrString _translation;
            private TrString _original;

            private TextBoxAutoHeight _txtTranslation;
            private Label _lblOldEnglish;
            #endregion

            public TranslationPanelTrString(string notes, TrString orig, TrString trans, string fieldname, TranslationTreeNode tn)
                : base(tn, notes, fieldname,
                    // outOfDate
                    string.IsNullOrEmpty(trans.OldEnglish) || trans.OldEnglish != orig.Translation,
                    // needOldRow
                    !string.IsNullOrEmpty(trans.OldEnglish) && trans.OldEnglish != orig.Translation
                )
            {
                _translation = trans;
                _original = orig;

                // Create the textbox a bit early so that we can assign the events which focus it
                _txtTranslation = new TextBoxAutoHeight()
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

                int currow = 1;
                if (!string.IsNullOrEmpty(notes))
                    currow++;
                if (!string.IsNullOrEmpty(trans.OldEnglish) && trans.OldEnglish != orig.Translation)
                {
                    _lblOldEnglish = new Label { Text = trans.OldEnglish.Replace("&", "&&"), AutoSize = true, Margin = new Padding(margin), Anchor = AnchorStyles.Left | AnchorStyles.Right };
                    _lblOldEnglish.Click += new EventHandler(focusTranslationBox);
                    Controls.Add(_lblOldEnglish, 1, currow);
                    SetColumnSpan(_lblOldEnglish, 2);
                    setBackColor();
                    currow++;
                }
                Label lblNewEnglish = new Label { Text = orig.Translation.Replace("&", "&&"), AutoSize = true, Margin = new Padding(margin), Anchor = AnchorStyles.Left | AnchorStyles.Right };
                lblNewEnglish.Click += new EventHandler(focusTranslationBox);
                Controls.Add(lblNewEnglish, 1, currow);
                SetColumnSpan(lblNewEnglish, 2);
                currow++;
                Controls.Add(_txtTranslation, 1, currow);

                _txtTranslation.TextChanged += (s, e) =>
                {
                    OutOfDate = true;
                    if (ChangeMade != null)
                        ChangeMade(s, e);
                };
                _txtTranslation.Enter += (s, e) => { AnythingFocused = true; fireEnterPanel(e); };
                _txtTranslation.Leave += (s, e) => { AnythingFocused = false; };
                _txtTranslation.Tag = this;
                _txtTranslation.KeyDown += new KeyEventHandler(keyDown);
                _btnAccept.KeyDown += new KeyEventHandler(keyDown);
                _txtTranslation.TabIndex = 0;
                _btnAccept.TabIndex = 1;
            }

            protected override void acceptTranslation(object sender, EventArgs e)
            {
                SuspendLayout();
                base.acceptTranslation(sender, e);
                _translation.Translation = _txtTranslation.Text;
                _translation.OldEnglish = _original.Translation;
                if (_lblOldEnglish != null)
                    _lblOldEnglish.Visible = false;
                if (ChangeMade != null)
                    ChangeMade(this, e);
                if (CtrlDown != null)
                    CtrlDown(this, e);
                ResumeLayout(true);
            }

            private void keyDown(object sender, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Up && e.Control && !e.Alt && !e.Shift && CtrlUp != null)
                {
                    CtrlUp(this, e);
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.Down && e.Control && !e.Alt && !e.Shift && CtrlDown != null)
                {
                    CtrlDown(this, e);
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.Home && e.Control && !e.Alt && !e.Shift && CtrlHome != null)
                {
                    CtrlHome(this, e);
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.End && e.Control && !e.Alt && !e.Shift && CtrlEnd != null)
                {
                    CtrlEnd(this, e);
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.PageUp && !e.Control && !e.Alt && !e.Shift && PageUp != null)
                {
                    PageUp(this, e);
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.PageDown && !e.Control && !e.Alt && !e.Shift && PageDown != null)
                {
                    PageDown(this, e);
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.A && e.Control && !e.Alt && !e.Shift && sender is TextBoxAutoHeight)
                {
                    ((TextBoxAutoHeight) sender).SelectAll();
                    e.Handled = true;
                }
            }

            public override bool Contains(string substring, bool inOriginal, bool inTranslation)
            {
                return (inOriginal && _original.Translation.Contains(substring)) || (inTranslation && _translation.Translation.Contains(substring));
            }

            public override void FocusTranslationBox()
            {
                _txtTranslation.Focus();
                _txtTranslation.SelectAll();
            }

            protected override void setBackColor()
            {
                base.setBackColor();
                if (_lblOldEnglish != null)
                    _lblOldEnglish.BackColor = _anythingFocused ? (OutOfDate ? outOfDateOldFocus : upToDateOldFocus) : (OutOfDate ? outOfDateOldNormal : upToDateOldNormal);
            }

            protected override void fireEnterPanel(EventArgs e)
            {
                if (EnterPanel != null)
                    EnterPanel(this, e);
            }
        }
    }
}