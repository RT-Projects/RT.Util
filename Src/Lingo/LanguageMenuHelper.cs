using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using RT.Util.Controls;
using RT.Util.Dialogs;

namespace RT.Util.Lingo
{
    /// <summary>Helps an application using Lingo to display language selection UI in a submenu, a context menu, or a drop-down box.</summary>
    /// <typeparam name="TTranslation">The type of the class holding the program’s translation.</typeparam>
    public sealed class LanguageMenuHelper<TTranslation> where TTranslation : TranslationBase, new()
    {
        private string _programTitle;
        private string _moduleName;
        private Language _defaultLanguage;
        private Language _currentLanguage;
        private TranslationForm<TTranslation>.Settings _trFormSettings;
        private Icon _trFormIcon;
        private SetLanguage<TTranslation> _setLanguage;
        private ToolStripMenuItem _languageMenu;
        private Func<Language> _getCurrentLanguage;

        private TranslationForm<TTranslation> _translationDialog;

        /// <summary>
        /// Gets or sets a value indicating whether the menu items to edit or create a language should be displayed.
        /// </summary>
        public bool TranslationEditingEnabled { get; set; }

        /// <summary>Returns a value indicating whether it is okay to close the application. The user is asked if there are any unsaved changes.</summary>
        public bool MayExitApplication()
        {
            if (_translationDialog == null || !_translationDialog.AnyChanges)
                return true;
            var result = DlgMessage.Show("Would you like to save the changes to made to the translation you are currently editing?",
                    "Exit Application", DlgType.Warning, "Save changes", "Discard changes", "Cancel");
            if (result == 2)
                return false;
            if (result == 0)
                _translationDialog.SaveChanges(false);
            return true;
        }

        /// <summary>Closes the translation dialog (if it is visible) without any prompts regarding unsaved changes.</summary>
        public void CloseWithoutPrompts()
        {
            if (_translationDialog != null)
                _translationDialog.CloseWithoutPrompts();
        }

        /// <summary>Creates a new language selection menu helper.</summary>
        /// <param name="programTitle">The title of the program - to be displayed in the translation UI.</param>
        /// <param name="moduleName">Name of the module being translated - used to construct the filename for the translation file.</param>
        /// <param name="defaultLanguage">The language of the default translation (this translation cannot be edited as it's compiled into the program).</param>
        /// <param name="trFormSettings">Translation window settings, such as window position/size.</param>
        /// <param name="trFormIcon">The icon to use on the translation window.</param>
        /// <param name="setLanguage">A callback to invoke in order to change the program language. See <see cref="SetLanguage&lt;T&gt;"/> for more details.</param>
        /// <param name="getCurrentLanguage">A callback that returns the currently active language whenever called.</param>
        /// <param name="languageMenu">The menu item which, when opened, will display the list of languages in a submenu.
        /// May be null if you intend to use only a context menu and/or drop-down box.</param>
        public LanguageMenuHelper(string programTitle, string moduleName, Language defaultLanguage,
            TranslationForm<TTranslation>.Settings trFormSettings, Icon trFormIcon, SetLanguage<TTranslation> setLanguage,
            Func<Language> getCurrentLanguage, ToolStripMenuItem languageMenu = null)
        {
            if (programTitle == null) throw new ArgumentNullException("programTitle");
            if (moduleName == null) throw new ArgumentNullException("moduleName");
            if (trFormSettings == null) throw new ArgumentNullException("trFormSettings");
            if (trFormIcon == null) throw new ArgumentNullException("trFormIcon");
            if (setLanguage == null) throw new ArgumentNullException("setLanguage");
            _programTitle = programTitle;
            _moduleName = moduleName;
            _defaultLanguage = defaultLanguage;
            _trFormSettings = trFormSettings;
            _trFormIcon = trFormIcon;
            _setLanguage = setLanguage;
            _languageMenu = languageMenu;
            _getCurrentLanguage = getCurrentLanguage;
            TranslationEditingEnabled = true;

            if (_languageMenu != null)
                _languageMenu.DropDownOpening += new EventHandler(dropDownOpening);
        }

        private IEnumerable<ToolStripItem> createDropDownItems()
        {
            _currentLanguage = _getCurrentLanguage();
            var items = new List<ToolStripItem>();
            items.AddRange(generateToolStripMenuItems());
            ToolStripItem miEdit = null;
            if (TranslationEditingEnabled)
            {
                items.Add(new ToolStripSeparator());
                items.Add(miEdit = new ToolStripMenuItem("&Edit current language", null, new EventHandler(editCurrentLanguage)));
                items.Add(new ToolStripMenuItem("&Create new language", null, new EventHandler(createNewLanguage)));
            }

            // Disable most items if a translation dialog is currently visible
            if (_translationDialog != null)
                foreach (ToolStripItem mi in _languageMenu.DropDownItems)
                    if (mi != miEdit)
                        mi.Enabled = false;

            return items;
        }

        private IEnumerable<ToolStripMenuItem> generateToolStripMenuItems()
        {
            ToolStripMenuItem selected = null;
            var curLanguage = _getCurrentLanguage();

            var arr = getLanguageInfos()
                .Select(trn => new ToolStripMenuItem(trn.Language.GetNativeName(), null, new EventHandler((sender, _) =>
                {
                    try
                    {
                        var trInf = ((languageItemInfo) ((ToolStripMenuItem) sender).Tag);
                        _setLanguage(trInf.IsDefault ? new TTranslation() : Lingo.LoadTranslation<TTranslation>(_moduleName, trInf.Language));
                        if (selected != null) selected.Checked = false;
                        selected = (ToolStripMenuItem) sender;
                        selected.Checked = true;
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show("The specified translation could not be opened: " + e.Message);
                    }
                })) { Tag = trn, Checked = trn.Language == curLanguage }).ToArray();
            selected = arr.FirstOrDefault(m => ((languageItemInfo) m.Tag).Language == curLanguage);
            return arr;
        }

        private void dropDownOpening(object sender, EventArgs e)
        {
            _languageMenu.DropDownItems.Clear();
            foreach (var item in createDropDownItems())
                _languageMenu.DropDownItems.Add(item);
        }

        /// <summary>Displays a context menu listing all available languages, and optionally controls to edit the translations.</summary>
        /// <param name="positionControl">The menu's position is specified relative to this control.</param>
        /// <param name="position">The menu's desired position relative to <paramref name="positionControl"/>.</param>
        public void ShowContextMenu(Control positionControl, Point position)
        {
            var menu = new ContextMenuStrip() { Renderer = new NativeToolStripRenderer() };
            foreach (var item in createDropDownItems())
                menu.Items.Add(item);
            menu.Show(positionControl, position);
        }

        /// <summary>Populates a combo-box with items that identify the available languages for the user to choose one.</summary>
        /// <param name="comboBox">The combobox to populate.</param>
        /// <param name="additionalSetLanguage">An optional delegate that is called for setting the language in addition to the one passed in via the constructor.</param>
        public void PopulateComboBox(ComboBox comboBox, Action<TTranslation> additionalSetLanguage)
        {
            _currentLanguage = _getCurrentLanguage();
            comboBox.Items.Clear();
            var list = getLanguageInfos();
            foreach (var languageInfo in list)
                comboBox.Items.Add(languageInfo);
            var defaultItem = list.FirstOrDefault(item => item.Language == _currentLanguage) ?? list.FirstOrDefault(item => item.IsDefault);
            comboBox.SelectedItem = defaultItem;
            comboBox.SelectedIndexChanged += (s, e) =>
            {
                var trInf = (languageItemInfo) comboBox.SelectedItem;
                var translation = trInf.IsDefault ? new TTranslation() : Lingo.LoadTranslation<TTranslation>(_moduleName, trInf.Language);
                _setLanguage(translation);
                if (additionalSetLanguage != null)
                    additionalSetLanguage(translation);
            };
        }

        private sealed class languageItemInfo
        {
            public Language Language;
            public bool IsDefault;
            public override string ToString() { return Language.GetNativeName(); }
        }

        private IEnumerable<languageItemInfo> getLanguageInfos()
        {
            var list = new List<languageItemInfo>();
            list.Add(new languageItemInfo { IsDefault = true, Language = new TTranslation().Language });
            var path = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "Translations");
            if (!Directory.Exists(path))
                return list;
            foreach (var file in new DirectoryInfo(path).GetFiles(_moduleName + ".*.xml"))
            {
                Match match = Regex.Match(file.Name, "^" + Regex.Escape(_moduleName) + @"\.(.*)\.xml$");
                if (!match.Success) continue;
                var l = Lingo.LanguageFromIsoCode(match.Groups[1].Value);
                if (l == null) continue;
                list.Add(new languageItemInfo { Language = l.Value, IsDefault = false });
            }
            list.Sort((l1, l2) => l1.Language.GetNativeName().CompareTo(l2.Language.GetNativeName()));
            return list;
        }

        private void editCurrentLanguage(object sender, EventArgs e)
        {
            if (_currentLanguage == _defaultLanguage)
            {
                DlgMessage.Show("The currently selected language is the native language of this application and cannot be edited.", "Edit current language", DlgType.Info);
                return;
            }
            if (_translationDialog == null)
            {
                try
                {
                    _translationDialog = new TranslationForm<TTranslation>(_trFormSettings, _trFormIcon, _programTitle, _moduleName, _currentLanguage);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Translation could not be loaded: " + ex.Message);
                    return;
                }
                _translationDialog.TranslationChanged += _setLanguage;
                _translationDialog.FormClosed += (s, v) => { _translationDialog = null; };
            }
            _translationDialog.Show();
        }

        private void createNewLanguage(object sender, EventArgs e)
        {
            var newTranslation = TranslationCreateForm.CreateTranslation<TTranslation>(_moduleName, _setLanguage, _trFormSettings.FontName, _trFormSettings.FontSize);
            if (newTranslation != null)
            {
                _currentLanguage = newTranslation.Language;
                editCurrentLanguage(sender, e);
            }
        }
    }
}
