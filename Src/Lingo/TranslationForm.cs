using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using RT.Util.Controls;
using RT.Util.Collections;
using RT.Util.Dialogs;
using RT.Util.ExtensionMethods;
using RT.Util.Forms;
using RT.Util.Xml;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace RT.Util.Lingo
{
    /// <summary>Provides a GUI for the user to edit a translation for the application.</summary>
    /// <typeparam name="T">The type containing the <see cref="TrString"/> and <see cref="TrStringNum"/> fields to be translated.</typeparam>
    public class TranslationForm<T> : ManagedForm where T : TranslationBase, new()
    {
        private TranslationPanel[] _currentlyVisibleTranslationPanels;
        private TranslationPanel[] _allTranslationPanels;
        private Panel _pnlRightOuter;
        private TableLayoutPanel _pnlRightInner;
        private ToolStripMenuItem _mnuFindNext;
        private ToolStripMenuItem _mnuFindPrev;
        private TranslationGroupListBox _lstGroups;
        private Button _btnOK;
        private Button _btnCancel;
        private Button _btnApply;

        private TranslationPanel _lastFocusedPanel;
        private string _translationFile;
        private T _translation;
        private bool _anyChanges;
        private Settings _settings;
        private NumberSystem _origNumberSystem;

        /// <summary>Holds the settings of the <see cref="TranslationForm&lt;T&gt;"/>.</summary>
        public new class Settings : ManagedForm.Settings
        {
            /// <summary>Remembers the position of the horizontal splitter (between the tree view and the main interface).</summary>
            public int SplitterDistance = 300;
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

        /// <summary>
        /// Fires every time the translation is updated on the disk (i.e. when the user clicks either "Save &amp; Close" or "Apply changes").
        /// </summary>
        public event SetLanguage<T> TranslationChanged;

        /// <summary>Main constructor.</summary>
        /// <param name="settings">Settings of the <see cref="TranslationForm&lt;T&gt;"/>.</param>
        /// <param name="icon">Application icon to use.</param>
        /// <param name="programTitle">Title of the program. Used in the title bar.</param>
        /// <param name="moduleName">Used for locating the translation file to be edited under the Translations directory.</param>
        /// <param name="language">The language to be edited.</param>
        public TranslationForm(Settings settings, Icon icon, string programTitle, string moduleName, Language language)
            : base(settings)
        {
            if (icon != null)
                Icon = icon;

            _settings = settings;
            _translationFile = PathUtil.Combine(PathUtil.AppPath, "Translations", moduleName + "." + language.GetIsoLanguageCode() + ".xml");
            _translation = XmlClassify.LoadObjectFromXmlFile<T>(_translationFile);
            _anyChanges = false;

            if (_settings.FontName != null)
                Font = new Font(_settings.FontName, _settings.FontSize, FontStyle.Regular);

            // some defaults
            Text = "Translating " + programTitle;
            Width = Screen.PrimaryScreen.WorkingArea.Width / 2;
            Height = Screen.PrimaryScreen.WorkingArea.Height * 9 / 10;

            // Start creating all the controls
            SplitContainerEx pnlSplit = new SplitContainerEx
            {
                Dock = DockStyle.Fill,
                FixedPanel = FixedPanel.Panel1,
                Orientation = Orientation.Vertical,
            };

            _lstGroups = new TranslationGroupListBox { Dock = DockStyle.Fill };
            pnlSplit.Panel1.Controls.Add(_lstGroups);

            T orig = new T();
            _origNumberSystem = orig.Language.GetNumberSystem();

            // Create all the translation panels
            var dicPanels = new Dictionary<object, List<TranslationPanel>>();
            var lstAllPanels = new List<TranslationPanel>();
            var lstUngroupedPanels = new List<TranslationPanel>();
            createPanelsForType(null, typeof(T), typeof(T), orig, _translation, dicPanels, lstUngroupedPanels, lstAllPanels);

            // Discover all the group types, their enum values, and then their attributes
            Dictionary<object, Tuple<string, string>> dic = new Dictionary<object, Tuple<string, string>>();
            foreach (var type in dicPanels.Select(kvp => kvp.Key.GetType()).Distinct())
                foreach (var f in type.GetFields(BindingFlags.Static | BindingFlags.Public))
                    foreach (var attr in f.GetCustomAttributes(typeof(LingoGroupAttribute), false))
                        dic.Add(f.GetValue(null), new Tuple<string, string>(((LingoGroupAttribute) attr).Name, ((LingoGroupAttribute) attr).Description));

            // Create all the list items
            foreach (var kvp in dic)
                if (dicPanels.ContainsKey(kvp.Key))
                {
                    var li = new TranslationGroupListItem { Label = kvp.Value.E1, Notes = kvp.Value.E2, TranslationPanels = dicPanels[kvp.Key].ToArray() };
                    _lstGroups.Items.Add(li);
                    foreach (var tp in dicPanels[kvp.Key])
                        tp.ListItems.Add(li);
                }
            if (lstUngroupedPanels.Count > 0)
            {
                var li = new TranslationGroupListItem { Label = "Ungrouped strings", Notes = "This group contains strings not found in any other group.", TranslationPanels = lstUngroupedPanels.ToArray() };
                _lstGroups.Items.Add(li);
                foreach (var tp in lstUngroupedPanels)
                    tp.ListItems.Add(li);
            }
            _allTranslationPanels = lstAllPanels.ToArray();

            _pnlRightInner = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                Dock = DockStyle.Top,
                GrowStyle = TableLayoutPanelGrowStyle.AddRows,
            };
            _pnlRightInner.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            _pnlRightOuter = new Panel { AutoScroll = true, Dock = DockStyle.Fill };
            _pnlRightOuter.Controls.Add(_pnlRightInner);
            pnlSplit.Panel2.Controls.Add(_pnlRightOuter);

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

            _lstGroups.SelectedValueChanged += (s, e) =>
            {
                if (_lstGroups.Tag == null || (int) _lstGroups.Tag != _lstGroups.SelectedIndex)
                {
                    _pnlRightOuter.VerticalScroll.Value = 0;
                    _pnlRightInner.SuspendLayout();
                    _pnlRightInner.Controls.Clear();
                    TranslationGroupListItem li = _lstGroups.SelectedItem as TranslationGroupListItem;
                    if (li == null)
                        return;
                    _currentlyVisibleTranslationPanels = li.TranslationPanels;
                    foreach (var pnl in _currentlyVisibleTranslationPanels)
                        pnl.SwitchToGroup(li);
                    _pnlRightInner.Controls.AddRange(_currentlyVisibleTranslationPanels);
                    _pnlRightInner.ResumeLayout(true);
                    if (_currentlyVisibleTranslationPanels.Length > 0)
                        _lastFocusedPanel = _currentlyVisibleTranslationPanels[0];
                }
                _lstGroups.Tag = _lstGroups.SelectedIndex;
            };
            _lstGroups.SelectedIndex = 0;

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
                    new ToolStripMenuItem("Fon&t...", null, new EventHandler(setFont)) { ShortcutKeys = Keys.Control | Keys.T },
                    new ToolStripMenuItem("&Mark all strings as up to date", null, new EventHandler(markAllUpToDate)),
                    new ToolStripMenuItem("M&ark all strings as out of date", null, new EventHandler(markAllOutOfDate))
                )
            ) { Dock = DockStyle.Top };

            Controls.Add(pnlSplit);
            Controls.Add(pnlBottom);
            Controls.Add(ts);

            setFont(_settings.FontName != null ? new Font(_settings.FontName, _settings.FontSize, FontStyle.Regular) : Font);
        }

        private void markAllUpToDate(object sender, EventArgs e)
        {
            if (DlgMessage.Show("Are you absolutely sure that you want to mark all strings as up to date? If you have not translated all strings yet, this will cause you to lose track of which strings you have not yet translated.",
                "Mark all as up to date", DlgType.Question, "&Yes", "&Cancel") == 1)
                return;
            _pnlRightInner.SuspendLayout();
            foreach (var p in _allTranslationPanels)
                p.SetUpToDate();
            _pnlRightInner.ResumeLayout(true);
            _anyChanges = true;
        }

        private void markAllOutOfDate(object sender, EventArgs e)
        {
            if (DlgMessage.Show("Are you absolutely sure that you want to mark all strings as out of date? This will mean that you will need to attend to all strings again before the translation can be considered up to date again.",
                "Mark all as out of date", DlgType.Question, "&Yes", "&Cancel") == 1)
                return;
            _pnlRightInner.SuspendLayout();
            foreach (var p in _allTranslationPanels)
                p.SetOutOfDate();
            _pnlRightInner.ResumeLayout(true);
            _anyChanges = true;
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

            _pnlRightInner.SuspendLayout();
            Font = new Font(font, FontStyle.Regular);
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
            _pnlRightInner.ResumeLayout(true);
        }

        private void find(object sender, EventArgs e)
        {
            using (Form ff = new Form())
            {
                ff.AutoSize = true;
                ff.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                ff.Text = "Find";
                ff.FormBorderStyle = FormBorderStyle.FixedDialog;
                ff.MinimizeBox = false;
                ff.MaximizeBox = false;
                ff.ShowInTaskbar = false;
                ff.Font = Font;
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
                DlgMessage.Show("You unchecked both \"Search English text\" and \"Search Translations\". That leaves nothing to be searched.", "Nothing to search", DlgType.Info);
                return;
            }
            int start = _lastFocusedPanel == null ? 0 : Array.IndexOf(_allTranslationPanels, _lastFocusedPanel) + 1;
            int finish = _lastFocusedPanel == null ? _allTranslationPanels.Length - 1 : start - 1;
            for (int i = start % _allTranslationPanels.Length; i != finish; i = (i + 1) % _allTranslationPanels.Length)
            {
                if (_allTranslationPanels[i].Contains(_settings.LastFindQuery, _settings.LastFindOrig, _settings.LastFindTrans))
                {
                    _lstGroups.SelectedItem = _allTranslationPanels[i].ListItems.First();
                    _allTranslationPanels[i].FocusFirstTranslationBox();
                    return;
                }
            }
            DlgMessage.Show("No matching strings found.", "Find", DlgType.Info);
        }

        private void findPrev(object sender, EventArgs e)
        {
            if (!_settings.LastFindOrig && !_settings.LastFindTrans)
            {
                DlgMessage.Show("You unchecked both \"Search English text\" and \"Search Translations\". That leaves nothing to be searched.", "Nothing to search", DlgType.Info);
                return;
            }
            int start = _lastFocusedPanel == null ? _allTranslationPanels.Length - 1 : Array.IndexOf(_allTranslationPanels, _lastFocusedPanel) - 1;
            int finish = _lastFocusedPanel == null ? 0 : start + 1;
            for (int i = (start + _allTranslationPanels.Length) % _allTranslationPanels.Length; i != finish; i = (i + _allTranslationPanels.Length - 1) % _allTranslationPanels.Length)
            {
                if (_allTranslationPanels[i].Contains(_settings.LastFindQuery, _settings.LastFindOrig, _settings.LastFindTrans))
                {
                    _lstGroups.SelectedItem = _allTranslationPanels[i].ListItems.First();
                    _allTranslationPanels[i].FocusFirstTranslationBox();
                    return;
                }
            }
            DlgMessage.Show("No matching strings found.", "Find", DlgType.Info);
        }

        private void nextOutOfDate(object sender, EventArgs e)
        {
            var refList = _lstGroups.Items.Cast<TranslationGroupListItem>().SelectMany(it => it.TranslationPanels.Select(tp => new { ListItem = it, TranslationPanel = tp })).ToArray();
            int start = _lastFocusedPanel == null || _lstGroups.SelectedIndex == -1 ? 0 : refList.TakeWhile(r => r.TranslationPanel != _lastFocusedPanel || r.ListItem != _lstGroups.Items[_lstGroups.SelectedIndex]).Count() + 1;
            int finish = _lastFocusedPanel == null ? refList.Length - 1 : start - 1;
            for (int i = start % refList.Length; i != finish; i = (i + 1) % refList.Length)
            {
                if (refList[i].TranslationPanel.State != TranslationPanelState.UpToDateAndSaved)
                {
                    _lstGroups.SelectedItem = refList[i].ListItem;
                    refList[i].TranslationPanel.FocusFirstTranslationBox();
                    return;
                }
            }
            if (_lastFocusedPanel != null && _lastFocusedPanel.State != TranslationPanelState.UpToDateAndSaved)
                DlgMessage.Show("All other strings are up to date.", "Next out-of-date string", DlgType.Info);
            else
                DlgMessage.Show("All strings are up to date.", "Next out-of-date string", DlgType.Info);
        }

        private void btnClick(object sender, EventArgs e)
        {
            if (sender == _btnCancel && _anyChanges && DlgMessage.Show("Are you sure you wish to discard all unsaved changes you made to the translation?", "Discard changes", DlgType.Warning, "&Discard", "&Cancel") == 1)
                return;

            if (sender != _btnCancel && _anyChanges)
            {
                XmlClassify.SaveObjectToXmlFile(_translation, _translationFile);
                if (TranslationChanged != null)
                {
                    try
                    {
                        var loadAgain = XmlClassify.LoadObjectFromXmlFile<T>(_translationFile);
                        TranslationChanged(loadAgain);
                    }
                    catch (Exception x)
                    {
                        DlgMessage.Show("Saving and re-loading the translation failed for the following reason:\n\n" + x.Message + "\n\nPlease ensure that the file is writable and readable and try again.", "Save translation failed", DlgType.Error);
                    }
                }
            }
            _anyChanges = false;

            if (sender != _btnApply)
                Close();
        }

        private void createPanelsForType(string chkName, Type chkType, Type type, object original, object translation, Dictionary<object, List<TranslationPanel>> dicPanels, List<TranslationPanel> lstUngroupedPanels, List<TranslationPanel> lstAllPanels)
        {
            var attrs = type.GetCustomAttributes(typeof(LingoStringClassAttribute), true);
            if (!attrs.Any())
            {
                if (chkName == null)
                    throw new ArgumentException(@"Type ""{0}"" must be marked with the [LingoStringClass] attribute.".Fmt(chkType.Name), "type");
                else
                    throw new ArgumentException(@"Field ""{0}.{1}"" must either be marked with the [LingoIgnore] attribute, or be of type TrString, TrStringNumbers, or a type with the [LingoStringClass] attribute.".Fmt(chkType.FullName, chkName), "type");
            }

            foreach (var f in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (f.FieldType == typeof(TrString) || f.FieldType == typeof(TrStringNum))
                {
                    string notes = f.GetCustomAttributes(typeof(LingoNotesAttribute), true).Cast<LingoNotesAttribute>().Select(lna => lna.Notes).FirstOrDefault();
                    var pnl = createTranslationPanel(notes, f.GetValue(original), f.GetValue(translation), f.Name);
                    lstAllPanels.Add(pnl);
                    var groups = f.GetCustomAttributes(typeof(LingoInGroupAttribute), true).Cast<LingoInGroupAttribute>().Select(liga => liga.Group);
                    if (groups.Any())
                        foreach (var group in groups)
                            dicPanels.AddSafe(group, pnl);
                    else
                        lstUngroupedPanels.Add(pnl);
                }
                else if (!f.GetCustomAttributes(typeof(LingoIgnoreAttribute), true).Any())
                    createPanelsForType(f.Name, type, f.FieldType, f.GetValue(original), f.GetValue(translation), dicPanels, lstUngroupedPanels, lstAllPanels);
            }
        }

        private TranslationPanel createTranslationPanel(string notes, object orig, object trans, string fieldname)
        {
            TranslationPanel pnl = (orig is TrString)
                ? (TranslationPanel) new TranslationPanelTrString(notes, (TrString) orig, (TrString) trans, fieldname)
                : (TranslationPanel) new TranslationPanelTrStringNum(notes, (TrStringNum) orig, (TrStringNum) trans, fieldname, _origNumberSystem, _translation.Language.GetNumberSystem());

            pnl.ChangeMade += new EventHandler(changeMade);
            pnl.EnterPanel += new EventHandler(enterPanel);
            pnl.CtrlUp += new EventHandler(ctrlUp);
            pnl.CtrlDown += new EventHandler(ctrlDown);
            pnl.CtrlPageUp += new EventHandler(ctrlPageUp);
            pnl.CtrlPageDown += new EventHandler(ctrlPageDown);
            pnl.PageUp += new EventHandler(pageUp);
            pnl.PageDown += new EventHandler(pageDown);
            pnl.GroupSwitch += new GroupSwitchEventHandler(groupSwitch);
            return pnl;
        }

        private void changeMade(object sender, EventArgs e)
        {
            _anyChanges = true;
            _lstGroups.Invalidate();
        }

        private void ctrlUp(object sender, EventArgs e)
        {
            TranslationPanel pnl = (TranslationPanel) sender;
            int index = Array.IndexOf(_currentlyVisibleTranslationPanels, pnl);
            if (index > 0)
                _currentlyVisibleTranslationPanels[index - 1].FocusLastTranslationBox();
        }

        private void ctrlDown(object sender, EventArgs e)
        {
            TranslationPanel pnl = (TranslationPanel) sender;
            int index = Array.IndexOf(_currentlyVisibleTranslationPanels, pnl);
            if (index >= 0 && index < _currentlyVisibleTranslationPanels.Length - 1)
                _currentlyVisibleTranslationPanels[index + 1].FocusFirstTranslationBox();
        }

        private void ctrlPageUp(object sender, EventArgs e)
        {
            int selIndex = _lstGroups.SelectedIndex;
            if (selIndex > 0)
                _lstGroups.SelectedIndex = selIndex - 1;
            else
                _lstGroups.SelectedIndex = _lstGroups.Items.Count - 1;
            if (_currentlyVisibleTranslationPanels.Length > 0)
                _currentlyVisibleTranslationPanels[0].FocusFirstTranslationBox();
        }

        private void ctrlPageDown(object sender, EventArgs e)
        {
            int selIndex = _lstGroups.SelectedIndex;
            if (selIndex < _lstGroups.Items.Count - 1)
                _lstGroups.SelectedIndex = selIndex + 1;
            else
                _lstGroups.SelectedIndex = 0;
            if (_currentlyVisibleTranslationPanels.Length > 0)
                _currentlyVisibleTranslationPanels[0].FocusFirstTranslationBox();
        }

        private void pageUp(object sender, EventArgs e)
        {
            TranslationPanel pnl = (TranslationPanel) sender;
            int index = Array.IndexOf(_currentlyVisibleTranslationPanels, pnl);
            if (index >= 0 && _currentlyVisibleTranslationPanels.Length > 0)
                _currentlyVisibleTranslationPanels[Math.Max(index - 10, 0)].FocusFirstTranslationBox();
        }

        private void pageDown(object sender, EventArgs e)
        {
            TranslationPanel pnl = (TranslationPanel) sender;
            int index = Array.IndexOf(_currentlyVisibleTranslationPanels, pnl);
            if (index >= 0 && _currentlyVisibleTranslationPanels.Length > 0)
                _currentlyVisibleTranslationPanels[Math.Min(index + 10, _currentlyVisibleTranslationPanels.Length - 1)].FocusFirstTranslationBox();
        }

        private void groupSwitch(object sender, GroupSwitchEventArgs e)
        {
            _lstGroups.SelectedItem = e.ListItem;
            ((TranslationPanel) sender).FocusFirstTranslationBox();
        }

        private void enterPanel(object sender, EventArgs e)
        {
            _lastFocusedPanel = (TranslationPanel) sender;
            int index = Array.IndexOf(_currentlyVisibleTranslationPanels, _lastFocusedPanel);
            if (index < 0)
                return;
            if (index > 0)
                _pnlRightOuter.ScrollControlIntoView(_currentlyVisibleTranslationPanels[index - 1]);
            if (index < _currentlyVisibleTranslationPanels.Length - 1)
                _pnlRightOuter.ScrollControlIntoView(_currentlyVisibleTranslationPanels[index + 1]);
            _pnlRightOuter.ScrollControlIntoView(_lastFocusedPanel);
        }

        private class GroupSwitchEventArgs : EventArgs
        {
            public TranslationGroupListItem ListItem;
            public GroupSwitchEventArgs(TranslationGroupListItem listItem) { ListItem = listItem; }
        }

        private delegate void GroupSwitchEventHandler(object sender, GroupSwitchEventArgs e);

        private enum TranslationPanelState
        {
            UpToDateAndSaved,
            OutOfDate,
            Unsaved
        }

        private abstract class TranslationPanel : TableLayoutPanel
        {
            public event EventHandler ChangeMade;
            public event EventHandler EnterPanel;
            public event EventHandler CtrlUp;
            public event EventHandler CtrlDown;
            public event EventHandler PageUp;
            public event EventHandler PageDown;
            public event EventHandler CtrlPageUp;
            public event EventHandler CtrlPageDown;
            public event GroupSwitchEventHandler GroupSwitch;

            protected void fireChangeMade() { if (ChangeMade != null) ChangeMade(this, new EventArgs()); }
            protected void fireEnterPanel() { if (EnterPanel != null) EnterPanel(this, new EventArgs()); }
            protected void fireCtrlUp() { if (CtrlUp != null) CtrlUp(this, new EventArgs()); }
            protected void fireCtrlDown() { if (CtrlDown != null) CtrlDown(this, new EventArgs()); }
            protected void firePageUp() { if (PageUp != null) PageUp(this, new EventArgs()); }
            protected void firePageDown() { if (PageDown != null) PageDown(this, new EventArgs()); }
            protected void fireCtrlPageUp() { if (CtrlPageUp != null) CtrlPageUp(this, new EventArgs()); }
            protected void fireCtrlPageDown() { if (CtrlPageDown != null) CtrlPageDown(this, new EventArgs()); }

            protected static readonly int margin = 3;

            public List<TranslationGroupListItem> ListItems = new List<TranslationGroupListItem>();

            protected Button _btnAccept;
            private Label _lblOldEnglishLbl;
            private Label _lblNewEnglishLbl;
            private Label _lblStringCode;
            private Label _lblNotes;
            private Label _lblOtherGroups;
            private TableLayoutPanel _pnlTopRow;

            private TranslationPanelState _state;
            public TranslationPanelState State
            {
                get { return _state; }
                protected set { _state = value; setBackColor(); }
            }

            protected bool _anythingFocused;
            public bool AnythingFocused
            {
                get { return _anythingFocused; }
                set { _anythingFocused = value; setBackColor(); }
            }

            public TranslationPanel(string notes, string fieldname, bool outOfDate, bool needOldRow)
                : base()
            {
                // Calculate number of rows
                int rows = 3;
                if (!string.IsNullOrEmpty(notes)) rows++;
                _state = outOfDate ? TranslationPanelState.OutOfDate : TranslationPanelState.UpToDateAndSaved;
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

                _lblStringCode = new Label { Text = fieldname, UseMnemonic = false, Font = new Font(Font, FontStyle.Bold), AutoSize = true, Margin = new Padding(margin), Anchor = AnchorStyles.Left };
                _lblStringCode.Click += new EventHandler(focusTranslationBox);
                Controls.Add(_lblStringCode, 0, 0);
                SetColumnSpan(_lblStringCode, 3);
                RowStyles.Add(new RowStyle(SizeType.AutoSize));

                int currow = 1;
                if (!string.IsNullOrEmpty(notes))
                {
                    _lblNotes = new Label { Text = notes, UseMnemonic = false, AutoSize = true, Font = new Font(Font, FontStyle.Italic), Margin = new Padding(margin), Anchor = AnchorStyles.Left | AnchorStyles.Right };
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
                _btnAccept.Enter += (s, e) => { AnythingFocused = true; fireEnterPanel(); };
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

            protected static Color upToDateNormal = Color.FromArgb(0xcc, 0xcc, 0xcc);
            protected static Color outOfDateNormal = Color.FromArgb(0xcc, 0xcc, 0xff);
            protected static Color unsavedNormal = Color.FromArgb(0xff, 0xcc, 0xcc);
            protected static Color upToDateFocus = Color.FromArgb(0xdd, 0xdd, 0xdd);
            protected static Color outOfDateFocus = Color.FromArgb(0xdd, 0xdd, 0xff);
            protected static Color unsavedFocus = Color.FromArgb(0xff, 0xdd, 0xdd);
            protected static Color upToDateOldNormal = Color.FromArgb(0xbb, 0xbb, 0xbb);
            protected static Color outOfDateOldNormal = Color.FromArgb(0xbb, 0xbb, 0xff);
            protected static Color unsavedOldNormal = Color.FromArgb(0xff, 0xbb, 0xbb);
            protected static Color upToDateOldFocus = Color.FromArgb(0xcc, 0xcc, 0xcc);
            protected static Color outOfDateOldFocus = Color.FromArgb(0xcc, 0xcc, 0xff);
            protected static Color unsavedOldFocus = Color.FromArgb(0xff, 0xcc, 0xcc);

            public abstract bool Contains(string substring, bool inOriginal, bool inTranslation);
            public abstract void FocusFirstTranslationBox();
            public abstract void FocusLastTranslationBox();
            public abstract void SetUpToDate();
            public abstract void SetOutOfDate();
            public virtual void SetFont(Font font, Size f)
            {
                _lblStringCode.Font = new Font(font, FontStyle.Bold);
                if (_lblNotes != null)
                    _lblNotes.Font = new Font(font, FontStyle.Italic);
                _btnAccept.Size = f;
            }

            protected virtual void focusTranslationBox(object sender, EventArgs e) { FocusFirstTranslationBox(); }
            protected virtual void setBackColor()
            {
                switch (State)
                {
                    case TranslationPanelState.UpToDateAndSaved:
                        BackColor = _anythingFocused ? upToDateFocus : upToDateNormal;
                        break;
                    case TranslationPanelState.OutOfDate:
                        BackColor = _anythingFocused ? outOfDateFocus : outOfDateNormal;
                        break;
                    case TranslationPanelState.Unsaved:
                        BackColor = _anythingFocused ? unsavedFocus : unsavedNormal;
                        break;
                }
            }
            protected virtual void acceptTranslation(object sender, EventArgs e)
            {
                State = TranslationPanelState.UpToDateAndSaved;
                if (_lblOldEnglishLbl != null)
                    _lblOldEnglishLbl.Visible = false;
                _lblNewEnglishLbl.Text = "English:";
            }

            public void SwitchToGroup(TranslationGroupListItem li)
            {
                if (ListItems.Count < 2)
                {
                    if (_pnlTopRow != null)
                    {
                        _pnlTopRow.Controls.Remove(_lblStringCode);
                        Controls.Remove(_pnlTopRow);
                        _pnlTopRow = null;
                        _lblStringCode.Margin = new Padding(margin);
                        Controls.Add(_lblStringCode, 0, 0);
                        SetColumnSpan(_lblStringCode, 3);
                    }
                    return;
                }

                if (_pnlTopRow != null)
                    Controls.Remove(_pnlTopRow);

                _pnlTopRow = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    AutoSize = true,
                    AutoSizeMode = AutoSizeMode.GrowAndShrink,
                    ColumnCount = 2,
                    RowCount = 1,
                    Margin = new Padding(margin)
                };
                _pnlTopRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
                _pnlTopRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
                _pnlTopRow.RowStyles.Add(new RowStyle(SizeType.AutoSize));

                _lblOtherGroups = new Label
                {
                    AutoSize = true,
                    Anchor = AnchorStyles.Right,
                    TextAlign = ContentAlignment.MiddleRight,
                    ForeColor = Color.Navy,
                    Font = new Font(Font.Name, Font.Size * 0.8f, FontStyle.Underline),
                    Cursor = Cursors.Hand,
                    Margin = new Padding(0),
                };
                _lblStringCode.Margin = new Padding(0);

                if (ListItems.Count == 2)
                {
                    TranslationGroupListItem otherLi = ListItems.First(l => l != li);
                    _lblOtherGroups.Text = @"This string is also in ""{0}"".".Fmt(otherLi.Label);
                    _lblOtherGroups.Click += (s, e) => fireGroupSwitch(otherLi);
                }
                else
                {
                    _lblOtherGroups.Text = "This string is also in other groups.";
                    _lblOtherGroups.Click += (s, e) =>
                    {
                        ContextMenuStrip dropDownMenu = new ContextMenuStrip();
                        foreach (var li2 in ListItems.Where(l => l != li))
                            dropDownMenu.Items.Add(new ToolStripMenuItem(li2.Label, null, (s2, e2) => fireGroupSwitch((TranslationGroupListItem) ((ToolStripMenuItem) s2).Tag)) { Tag = li2 });
                        dropDownMenu.Show(_lblOtherGroups, new Point(_lblOtherGroups.Width, _lblOtherGroups.Height), ToolStripDropDownDirection.BelowLeft);
                    };
                }

                _pnlTopRow.Controls.Add(_lblStringCode, 0, 0);
                _pnlTopRow.SetColumnSpan(_lblStringCode, 1);
                _pnlTopRow.Controls.Add(_lblOtherGroups, 1, 0);
                Controls.Add(_pnlTopRow, 0, 0);
                SetColumnSpan(_pnlTopRow, 3);
            }

            private void fireGroupSwitch(TranslationGroupListItem listItem)
            {
                if (GroupSwitch != null)
                    GroupSwitch(this, new GroupSwitchEventArgs(listItem));
            }
        }

        private class TranslationPanelTrString : TranslationPanel
        {
            private TrString _translation;
            private TrString _original;
            private TextBoxAutoHeight _txtTranslation;
            private Label _lblOldEnglish;

            public TranslationPanelTrString(string notes, TrString orig, TrString trans, string fieldname)
                : base(notes, fieldname,
                    // outOfDate
                    string.IsNullOrEmpty(trans.Old) || trans.Old != orig.Translation,
                    // needOldRow
                    !string.IsNullOrEmpty(trans.Old) && trans.Old != orig.Translation
                )
            {
                _translation = trans;
                _original = orig;

                _txtTranslation = new TextBoxAutoHeight()
                {
                    Text = trans.Translation.UnifyLineEndings(),
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
                if (!string.IsNullOrEmpty(trans.Old) && trans.Old != orig.Translation)
                {
                    _lblOldEnglish = new Label { Text = trans.Old, UseMnemonic = false, AutoSize = true, Margin = new Padding(margin), Anchor = AnchorStyles.Left | AnchorStyles.Right };
                    _lblOldEnglish.Click += new EventHandler(focusTranslationBox);
                    Controls.Add(_lblOldEnglish, 1, currow);
                    SetColumnSpan(_lblOldEnglish, 2);
                    setBackColor();
                    currow++;
                }
                Label lblNewEnglish = new Label { Text = orig.Translation, UseMnemonic = false, AutoSize = true, Margin = new Padding(margin), Anchor = AnchorStyles.Left | AnchorStyles.Right };
                lblNewEnglish.Click += new EventHandler(focusTranslationBox);
                Controls.Add(lblNewEnglish, 1, currow);
                SetColumnSpan(lblNewEnglish, 2);
                currow++;
                Controls.Add(_txtTranslation, 1, currow);

                _txtTranslation.TextChanged += (s, e) => { if (State != TranslationPanelState.Unsaved) { State = TranslationPanelState.Unsaved; fireChangeMade(); } };
                _txtTranslation.Enter += (s, e) => { _txtTranslation.SelectAll(); AnythingFocused = true; fireEnterPanel(); };
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
                _translation.Old = _original.Translation;
                if (_lblOldEnglish != null)
                    _lblOldEnglish.Visible = false;
                fireChangeMade();
                fireCtrlDown();
                ResumeLayout(true);
            }

            private void keyDown(object sender, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Up && e.Control && !e.Alt && !e.Shift)
                {
                    fireCtrlUp();
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.Down && e.Control && !e.Alt && !e.Shift)
                {
                    fireCtrlDown();
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.PageUp && e.Control && !e.Alt && !e.Shift)
                {
                    fireCtrlPageUp();
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.PageDown && e.Control && !e.Alt && !e.Shift)
                {
                    fireCtrlPageDown();
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.PageUp && !e.Control && !e.Alt && !e.Shift)
                {
                    firePageUp();
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.PageDown && !e.Control && !e.Alt && !e.Shift)
                {
                    firePageDown();
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.A && e.Control && !e.Alt && !e.Shift && sender is TextBoxAutoHeight)
                {
                    ((TextBoxAutoHeight) sender).SelectAll();
                    e.Handled = true;
                }
                else if ((e.KeyCode == Keys.Return || e.KeyCode == Keys.Enter) && !e.Control && !e.Alt && !e.Shift && sender is TextBoxAutoHeight)
                {
                    acceptTranslation(_btnAccept, e);
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
            }

            public override bool Contains(string substring, bool inOriginal, bool inTranslation)
            {
                CompareOptions co = CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreSymbols | CompareOptions.IgnoreWidth;
                return (inOriginal && CultureInfo.InvariantCulture.CompareInfo.IndexOf(_original.Translation, substring, co) != -1) ||
                    (inTranslation && CultureInfo.InvariantCulture.CompareInfo.IndexOf(_translation.Translation, substring, co) != -1);
            }

            public override void FocusFirstTranslationBox()
            {
                _txtTranslation.Focus();
                _txtTranslation.SelectAll();
            }

            public override void FocusLastTranslationBox()
            {
                _txtTranslation.Focus();
                _txtTranslation.SelectAll();
            }

            public override void SetUpToDate()
            {
                SuspendLayout();
                base.acceptTranslation(this, new EventArgs());
                _translation.Translation = _txtTranslation.Text;
                _translation.Old = _original.Translation;
                if (_lblOldEnglish != null)
                    _lblOldEnglish.Visible = false;
                State = TranslationPanelState.UpToDateAndSaved;
                ResumeLayout(true);
            }

            public override void SetOutOfDate()
            {
                SuspendLayout();
                base.acceptTranslation(this, new EventArgs());
                _translation.Translation = _txtTranslation.Text;
                _translation.Old = null;
                if (_lblOldEnglish != null)
                    _lblOldEnglish.Visible = false;
                State = TranslationPanelState.OutOfDate;
                ResumeLayout(true);
            }

            protected override void setBackColor()
            {
                base.setBackColor();
                if (_lblOldEnglish != null)
                {
                    switch (State)
                    {
                        case TranslationPanelState.UpToDateAndSaved:
                            _lblOldEnglish.BackColor = _anythingFocused ? upToDateOldFocus : upToDateOldNormal;
                            break;
                        case TranslationPanelState.OutOfDate:
                            _lblOldEnglish.BackColor = _anythingFocused ? outOfDateOldFocus : outOfDateOldNormal;
                            break;
                        case TranslationPanelState.Unsaved:
                            _lblOldEnglish.BackColor = _anythingFocused ? unsavedOldFocus : unsavedOldNormal;
                            break;
                    }
                }
            }
        }

        private class TranslationPanelTrStringNum : TranslationPanel
        {
            private TrStringNum _original;
            private TrStringNum _translation;
            private Panel _pnlOldEnglish;
            private TextBoxAutoHeight[] _txtTranslation;
            private NumberSystem _origNumberSystem;
            private NumberSystem _transNumberSystem;
            private int _lastFocusedTextbox;
            private List<Label> _smallLabels = new List<Label>();

            public TranslationPanelTrStringNum(string notes, TrStringNum orig, TrStringNum trans, string fieldname, NumberSystem origNumberSystem, NumberSystem transNumberSystem)
                : base(notes, fieldname,
                    // outOfDate
                    trans.Old == null || !trans.Old.SequenceEqual(orig.Translations),
                    // needOldRow
                    trans.Old != null && !trans.Old.SequenceEqual(orig.Translations)
                )
            {
                int nn = trans.IsNumber.Where(b => b).Count();
                int rowsOrig = (int) Math.Pow(origNumberSystem.NumStrings, nn);
                int rowsTrans = (int) Math.Pow(transNumberSystem.NumStrings, nn);

                _translation = trans;
                _original = orig;
                _origNumberSystem = origNumberSystem;
                _transNumberSystem = transNumberSystem;

                Panel pnlTranslation = createTablePanel(trans, trans.Translations, transNumberSystem, true, nn, rowsTrans);
                pnlTranslation.TabIndex = 0;

                int currow = 1;
                if (!string.IsNullOrEmpty(notes))
                    currow++;
                if (trans.Old != null && !trans.Old.SequenceEqual(orig.Translations))
                {
                    _pnlOldEnglish = createTablePanel(orig, trans.Old, origNumberSystem, false, nn, rowsOrig);
                    Controls.Add(_pnlOldEnglish, 1, currow);
                    SetColumnSpan(_pnlOldEnglish, 2);
                    setBackColor();
                    currow++;
                }
                Panel pnlNewEnglish = createTablePanel(orig, orig.Translations, origNumberSystem, false, nn, rowsOrig);
                Controls.Add(pnlNewEnglish, 1, currow);
                SetColumnSpan(pnlNewEnglish, 2);
                currow++;
                Controls.Add(pnlTranslation, 1, currow);

                _btnAccept.KeyDown += new KeyEventHandler(keyDown);
                _btnAccept.TabIndex = 1;
            }

            private Panel createTablePanel(TrStringNum str, string[] display, NumberSystem ns, bool textBoxes, int nn, int rows)
            {
                if (textBoxes)
                    _txtTranslation = new TextBoxAutoHeight[rows];

                TableLayoutPanel pnlTranslations = new TableLayoutPanel
                {
                    AutoSize = true,
                    AutoSizeMode = AutoSizeMode.GrowAndShrink,
                    Anchor = AnchorStyles.Left | AnchorStyles.Right,
                    Dock = DockStyle.Fill,
                    ColumnCount = 1 + nn,
                    RowCount = 1 + rows
                };
                pnlTranslations.Click += new EventHandler(focusTranslationBox);
                for (int i = 0; i < nn; i++)
                    pnlTranslations.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
                pnlTranslations.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
                for (int i = 0; i <= rows; i++)
                    pnlTranslations.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                int column = 0;
                for (int i = 0; i < str.IsNumber.Length; i++)
                {
                    if (str.IsNumber[i])
                    {
                        Label lbl = new Label
                        {
                            Anchor = AnchorStyles.Left,
                            AutoSize = true,
                            Margin = new Padding(margin),
                            Text = "{" + i + "}",
                        };
                        lbl.Click += new EventHandler(focusTranslationBox);
                        pnlTranslations.Controls.Add(lbl, column, 0);
                        column++;
                    }
                }
                for (int row = 0; row < rows; row++)
                {
                    int col = 0;
                    int r = row;
                    for (int i = 0; i < str.IsNumber.Length; i++)
                    {
                        if (str.IsNumber[i])
                        {
                            Label lbl = new Label
                            {
                                Anchor = AnchorStyles.Left,
                                AutoSize = true,
                                Margin = new Padding(margin),
                                Text = ns.GetDescription(r % ns.NumStrings),
                                Font = new Font(Font.Name, Font.Size * 0.8f, FontStyle.Regular)
                            };
                            lbl.Click += new EventHandler(focusTranslationBox);
                            pnlTranslations.Controls.Add(lbl, col, row + 1);
                            _smallLabels.Add(lbl);
                            col++;
                            r /= ns.NumStrings;
                        }
                    }
                    if (textBoxes)
                    {
                        TextBoxAutoHeight tba = new TextBoxAutoHeight
                        {
                            Anchor = AnchorStyles.Left | AnchorStyles.Right,
                            Margin = new Padding(margin),
                            Text = (display != null && row < display.Length) ? display[row].UnifyLineEndings() : "",
                            Multiline = true,
                            WordWrap = true,
                            AcceptsReturn = true,
                            AcceptsTab = false,
                            ShortcutsEnabled = true
                        };
                        tba.TextChanged += (s, e) => { if (State != TranslationPanelState.Unsaved) { State = TranslationPanelState.Unsaved; fireChangeMade(); } };
                        tba.Enter += new EventHandler(textBoxEnter);
                        tba.Leave += (s, e) => { AnythingFocused = false; };
                        tba.Tag = this;
                        tba.KeyDown += new KeyEventHandler(keyDown);
                        _txtTranslation[row] = tba;
                        pnlTranslations.Controls.Add(tba, col, row + 1);
                    }
                    else
                    {
                        Label lbl = new Label
                        {
                            Anchor = AnchorStyles.Left | AnchorStyles.Right,
                            Margin = new Padding(margin),
                            Text = (display != null && row < display.Length) ? display[row] : "",
                            UseMnemonic = false,
                            AutoSize = true,
                        };
                        lbl.Click += new EventHandler(focusTranslationBox);
                        pnlTranslations.Controls.Add(lbl, col, row + 1);
                    }
                }
                return pnlTranslations;
            }

            private void textBoxEnter(object sender, EventArgs e)
            {
                ((TextBoxAutoHeight) sender).SelectAll();
                AnythingFocused = true;
                fireEnterPanel();
                _lastFocusedTextbox = Array.IndexOf(_txtTranslation, sender);
            }

            protected override void acceptTranslation(object sender, EventArgs e)
            {
                SuspendLayout();
                base.acceptTranslation(sender, e);
                _translation.Translations = _txtTranslation.Select(t => t.Text).ToArray();
                _translation.Old = _original.Translations;
                if (_pnlOldEnglish != null)
                    _pnlOldEnglish.Visible = false;
                fireChangeMade();
                fireCtrlDown();
                ResumeLayout(true);
            }

            private void keyDown(object sender, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Up && e.Control && !e.Alt && !e.Shift)
                {
                    if (_lastFocusedTextbox > 0)
                        _txtTranslation[_lastFocusedTextbox - 1].Focus();
                    else
                        fireCtrlUp();
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.Down && e.Control && !e.Alt && !e.Shift)
                {
                    if (_lastFocusedTextbox < _txtTranslation.Length - 1)
                        _txtTranslation[_lastFocusedTextbox + 1].Focus();
                    else
                        fireCtrlDown();
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.PageUp && e.Control && !e.Alt && !e.Shift)
                {
                    fireCtrlPageUp();
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.PageDown && e.Control && !e.Alt && !e.Shift)
                {
                    fireCtrlPageDown();
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.PageUp && !e.Control && !e.Alt && !e.Shift)
                {
                    firePageUp();
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.PageDown && !e.Control && !e.Alt && !e.Shift)
                {
                    firePageDown();
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.A && e.Control && !e.Alt && !e.Shift && sender is TextBoxAutoHeight)
                {
                    ((TextBoxAutoHeight) sender).SelectAll();
                    e.Handled = true;
                }
                else if ((e.KeyCode == Keys.Return || e.KeyCode == Keys.Enter) && !e.Control && !e.Alt && !e.Shift && sender is TextBoxAutoHeight)
                {
                    if (sender == _txtTranslation[_txtTranslation.Length - 1])
                        acceptTranslation(_btnAccept, e);
                    else
                        _txtTranslation[Array.IndexOf(_txtTranslation, sender) + 1].Focus();
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
            }

            public override bool Contains(string substring, bool inOriginal, bool inTranslation)
            {
                CompareOptions co = CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreSymbols | CompareOptions.IgnoreWidth;
                return
                    (inOriginal && _original.Translations.Any(t => CultureInfo.InvariantCulture.CompareInfo.IndexOf(t, substring, co) != -1)) ||
                    (inTranslation && _translation.Translations.Any(t => CultureInfo.InvariantCulture.CompareInfo.IndexOf(t, substring, co) != -1));
            }

            public override void FocusFirstTranslationBox()
            {
                _txtTranslation[0].Focus();
                _txtTranslation[0].SelectAll();
            }

            public override void FocusLastTranslationBox()
            {
                _txtTranslation[_txtTranslation.Length - 1].Focus();
                _txtTranslation[_txtTranslation.Length - 1].SelectAll();
            }

            public override void SetUpToDate()
            {
                SuspendLayout();
                _translation.Translations = _txtTranslation.Select(t => t.Text).ToArray();
                _translation.Old = _original.Translations;
                if (_pnlOldEnglish != null)
                    _pnlOldEnglish.Visible = false;
                State = TranslationPanelState.UpToDateAndSaved;
                ResumeLayout(true);
            }

            public override void SetOutOfDate()
            {
                SuspendLayout();
                _translation.Translations = _txtTranslation.Select(t => t.Text).ToArray();
                _translation.Old = null;
                if (_pnlOldEnglish != null)
                    _pnlOldEnglish.Visible = false;
                State = TranslationPanelState.OutOfDate;
                ResumeLayout(true);
            }

            protected override void setBackColor()
            {
                base.setBackColor();
                if (_pnlOldEnglish != null)
                {
                    switch (State)
                    {
                        case TranslationPanelState.UpToDateAndSaved:
                            _pnlOldEnglish.BackColor = _anythingFocused ? upToDateOldFocus : upToDateOldNormal;
                            break;
                        case TranslationPanelState.OutOfDate:
                            _pnlOldEnglish.BackColor = _anythingFocused ? outOfDateOldFocus : outOfDateOldNormal;
                            break;
                        case TranslationPanelState.Unsaved:
                            _pnlOldEnglish.BackColor = _anythingFocused ? unsavedOldFocus : unsavedOldNormal;
                            break;
                    }
                }
            }

            public override void SetFont(Font font, Size f)
            {
                base.SetFont(font, f);
                foreach (var l in _smallLabels)
                    l.Font = new Font(font.Name, font.Size * 0.8f, FontStyle.Regular);
            }
        }

        private class TranslationGroupListItem
        {
            public TranslationPanel[] TranslationPanels;
            public string Label;
            public string Notes;
            public override string ToString() { return Label; }
        }

        private class TranslationGroupListBox : ListBox
        {
            private const int vertMargin = 3;
            private const int horizMargin = 5;
            private const int indent = 10;

            public TranslationGroupListBox()
            {
                DrawMode = DrawMode.OwnerDrawVariable;
                IntegralHeight = false;
                ScrollAlwaysVisible = true;

                MeasureItem += new MeasureItemEventHandler(measureItem);
                DrawItem += new DrawItemEventHandler(drawItem);
                FontChanged += (s, e) => RefreshItems();

                int prevWidth = 0;
                Resize += (s, e) =>
                {
                    if (Width != prevWidth)
                    {
                        prevWidth = Width;
                        RefreshItems();
                        Update();
                    }
                };

                int selectedIndex = -2;
                SelectedIndexChanged += (s, e) =>
                {
                    if (SelectedIndex != selectedIndex)
                    {
                        selectedIndex = SelectedIndex;
                        RefreshItems();
                        Update();
                    }
                };
            }

            private void drawItem(object sender, DrawItemEventArgs e)
            {
                if (e.Index < 0 || e.Index >= Items.Count || DesignMode)
                    return;
                TranslationGroupListItem tgli = Items[e.Index] as TranslationGroupListItem;
                if (tgli == null)
                    return;
                e.DrawBackground();
                Color textColor = e.Index == SelectedIndex ? SystemColors.HighlightText : SystemColors.WindowText;
                e.Graphics.DrawString(tgli.Label, new Font(Font, FontStyle.Bold), new SolidBrush(textColor), new PointF(e.Bounds.Left + horizMargin, e.Bounds.Top + vertMargin));
                if (tgli.Notes != null)
                {
                    int h = (int) e.Graphics.MeasureString(tgli.Label, new Font(Font, FontStyle.Bold)).Height + 2 * vertMargin;
                    e.Graphics.DrawString(tgli.Notes, new Font(Font.Name, Font.Size * 0.8f, FontStyle.Regular), new SolidBrush(textColor), new RectangleF(e.Bounds.Left + horizMargin + indent, e.Bounds.Top + h, /*e.Bounds.Width*/ClientSize.Width - indent - 2 * horizMargin, e.Bounds.Height - h - vertMargin));
                }
                if (tgli.TranslationPanels.Any(t => t.State != TranslationPanelState.UpToDateAndSaved))
                {
                    Color red = Color.FromArgb((textColor.R + 256) / 2, textColor.G / 2, textColor.B / 2);
                    e.Graphics.DrawString("!", new Font(Font, FontStyle.Bold), new SolidBrush(red), new PointF(e.Bounds.Right - horizMargin, e.Bounds.Top + vertMargin), new StringFormat { Alignment = StringAlignment.Far });
                }
                if (e.Index == SelectedIndex)
                    e.DrawFocusRectangle();
            }

            private void measureItem(object sender, MeasureItemEventArgs e)
            {
                if (e.Index < 0 || e.Index >= Items.Count || DesignMode)
                    return;
                TranslationGroupListItem tgli = Items[e.Index] as TranslationGroupListItem;
                if (tgli == null)
                    return;
                int h = (int) e.Graphics.MeasureString(tgli.Label, new Font(Font, FontStyle.Bold)).Height + 2 * vertMargin;
                if (tgli.Notes != null)
                    h += (int) e.Graphics.MeasureString(tgli.Notes, new Font(Font.Name, Font.Size * 0.8f, FontStyle.Regular), ClientSize.Width - 2 * horizMargin - indent).Height + vertMargin;
                e.ItemHeight = h;
            }
        }
    }
}
