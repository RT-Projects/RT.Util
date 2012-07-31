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
    /// <summary>Helps an application using Lingo to display language selection UI.</summary>
    /// <typeparam name="TTranslation">The type of the class holding the program’s translation.</typeparam>
    public abstract class LanguageHelper<TTranslation> where TTranslation : TranslationBase, new()
    {
        private readonly string _programTitle;
        private readonly string _moduleName;
        private readonly bool _editable;
        private readonly TranslationForm<TTranslation>.Settings _trFormSettings;
        private readonly Icon _trFormIcon;
        private readonly Func<Language> _getCurrentLanguage;

        private readonly Language _defaultLanguage = new TTranslation().Language;
        private TranslationForm<TTranslation> _translationDialog;

        /// <summary>Constructor.</summary>
        /// <param name="programTitle">The title of the program - to be displayed in the translation UI.</param>
        /// <param name="moduleName">Name of the module being translated - used to construct the filename for the translation file.</param>
        /// <param name="editable">Whether translation editing UI should be included.</param>
        /// <param name="trFormSettings">Translation window settings, such as window position/size.</param>
        /// <param name="trFormIcon">The icon to use on the translation window.</param>
        /// <param name="getCurrentLanguage">A callback that returns the currently active language whenever called.</param>
        public LanguageHelper(string programTitle, string moduleName, bool editable,
            TranslationForm<TTranslation>.Settings trFormSettings, Icon trFormIcon, Func<Language> getCurrentLanguage)
        {
            if (programTitle == null) throw new ArgumentNullException("programTitle");
            if (moduleName == null) throw new ArgumentNullException("moduleName");
            if (trFormSettings == null) throw new ArgumentNullException("trFormSettings");
            if (trFormIcon == null) throw new ArgumentNullException("trFormIcon");
            if (getCurrentLanguage == null) throw new ArgumentNullException("getCurrentLanguage");
            _programTitle = programTitle;
            _moduleName = moduleName;
            _editable = editable;
            _trFormSettings = trFormSettings;
            _trFormIcon = trFormIcon;
            _getCurrentLanguage = getCurrentLanguage;
        }

        /// <summary>Changes the currently selected language in exactly the same way as using one of the UI elements would.</summary>
        /// <param name="language">The desired language. If not available, the default language will be set instead.</param>
        public void SetLanguage(Language language)
        {
            TranslationChanged(language == _defaultLanguage ? new TTranslation() : Lingo.LoadTranslationOrDefault<TTranslation>(_moduleName, ref language));
        }

        /// <summary>
        /// Occurs whenever the translation has been changed. The application must respond by updating *all* visible UI
        /// and storing the language of the selected translation for use on next program start.
        /// </summary>
        public event SetTranslation<TTranslation> TranslationChanged = delegate { };

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

        /// <summary>Describes a UI entry exposed by the language helper.</summary>
        protected class Entry
        {
            /// <summary>The label that the control should have. Hotkeys are prefixed with an ampersand; ampersands are escaped by doubling.</summary>
            public string Text;
            /// <summary>If this UI entry activates a language, this is the language code. Otherwise null.</summary>
            public Language? Language { get; private set; }
            /// <summary>Activating the control should invoke this action.</summary>
            public Action Action { get; private set; }
            /// <summary>If true, a separator should be displayed before this control, if possible.</summary>
            public bool SeparatorBefore;
            /// <summary>The UI control should appear highlighted or checked, because it represents the current language.</summary>
            public bool IsCurrentLanguage;
            /// <summary>Whether the UI control should be enabled (that is, possible to trigger the <see cref="Action"/>).</summary>
            public bool Enabled = true;

            /// <summary>Constructor for language entries.</summary>
            public Entry(LanguageHelper<TTranslation> helper, Language language)
            {
                Language = language;
                Text = language.GetNativeName();
                Action = () => helper.SetLanguage(Language.Value);
            }

            /// <summary>Constructor for arbitrary entries.</summary>
            public Entry(string text, Action action)
            {
                Text = text;
                Action = action;
            }

            /// <summary>Returns the UI control text.</summary>
            public override string ToString()
            {
                return Text;
            }
        }

        /// <summary>
        /// Returns a list of UI control descriptors whose state is applicable at the time of invocation. This method should be invoked
        /// every time prior to displaying UI, to obtain the up-to-date UI state. The entries returned are not "live" and will not be updated
        /// to reflect any changes.
        /// </summary>
        /// <remarks>
        /// The list is guaranteed to have at least one item, the default language. Exactly one item will have <see cref="Entry.IsCurrentLanguage"/> set to true.
        /// </remarks>
        protected List<Entry> ListCurrentEntries()
        {
            // Enumerate available languages
            var result = new List<Entry>();
            var defaultLanguage = new Entry(this, _defaultLanguage);
            result.Add(defaultLanguage);
            var path = PathUtil.AppPathCombine("Translations");
            if (Directory.Exists(path))
            {
                foreach (var file in new DirectoryInfo(path).GetFiles(_moduleName + ".*.xml"))
                {
                    Match match = Regex.Match(file.Name, "^" + Regex.Escape(_moduleName) + @"\.(.*)\.xml$");
                    if (!match.Success) continue;
                    var l = Lingo.LanguageFromIsoCode(match.Groups[1].Value);
                    if (l == null) continue;
                    result.Add(new Entry(this, l.Value));
                }
                result.Sort((l1, l2) => l1.Language.Value.GetNativeName().CompareTo(l2.Language.Value.GetNativeName()));
            }

            // Remove the English variant suffix if only one such language
            var english = result.Where(e => e.Text.StartsWith("English ")).ToArray();
            if (english.Length == 1)
                english[0].Text = "English";

            // Mark the current language as checked
            (result.SingleOrDefault(e => e.Language == _getCurrentLanguage()) ?? defaultLanguage).IsCurrentLanguage = true;

            // Add the editing UI
            Entry editEntry = null;
            if (_editable)
            {
                result.Add(editEntry = new Entry("&Edit current language", editCurrentLanguage) { SeparatorBefore = true });
                result.Add(new Entry("&Create new language", createNewLanguage));
            }

            // Disable most items if a translation dialog is currently visible
            if (_translationDialog != null)
                foreach (var entry in result)
                    if (entry != editEntry)
                        entry.Enabled = false;

            return result;
        }

        private void editCurrentLanguage()
        {
            if (_getCurrentLanguage() == _defaultLanguage)
            {
                DlgMessage.Show("The currently selected language is the native language of this application and cannot be edited.", "Edit current language", DlgType.Info);
                return;
            }
            if (_translationDialog == null)
            {
                try
                {
                    _translationDialog = new TranslationForm<TTranslation>(_trFormSettings, _trFormIcon, _programTitle, _moduleName, _getCurrentLanguage());
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Translation could not be loaded: " + ex.Message);
                    return;
                }
                _translationDialog.TranslationChanged += tr => { TranslationChanged(tr); };
                _translationDialog.FormClosed += delegate { _translationDialog = null; };
            }
            _translationDialog.Show();
        }

        private void createNewLanguage()
        {
            var newTranslation = TranslationCreateForm.CreateTranslation<TTranslation>(_moduleName, _trFormSettings.FontName, _trFormSettings.FontSize);
            if (newTranslation != null)
            {
                SetLanguage(newTranslation.Language);
                editCurrentLanguage();
            }
        }
    }

    /// <summary>Helps an application using Lingo to display language selection UI using WinForms controls.</summary>
    /// <typeparam name="TTranslation">The type of the class holding the program’s translation.</typeparam>
    public class LanguageHelperWinForms<TTranslation> : LanguageHelper<TTranslation> where TTranslation : TranslationBase, new()
    {
        /// <summary>Constructor.</summary>
        /// <param name="programTitle">The title of the program - to be displayed in the translation UI.</param>
        /// <param name="moduleName">Name of the module being translated - used to construct the filename for the translation file.</param>
        /// <param name="editable">Whether translation editing UI should be included.</param>
        /// <param name="trFormSettings">Translation window settings, such as window position/size.</param>
        /// <param name="trFormIcon">The icon to use on the translation window.</param>
        /// <param name="getCurrentLanguage">A callback that returns the currently active language whenever called.</param>
        public LanguageHelperWinForms(string programTitle, string moduleName, bool editable,
            TranslationForm<TTranslation>.Settings trFormSettings, Icon trFormIcon, Func<Language> getCurrentLanguage)
            : base(programTitle, moduleName, editable, trFormSettings, trFormIcon, getCurrentLanguage)
        {
        }

        private void populateToolStripItems(ToolStripItemCollection items)
        {
            foreach (var entry in ListCurrentEntries())
            {
                if (entry.SeparatorBefore)
                    items.Add(new ToolStripSeparator());
                var item = new ToolStripMenuItem();
                item.Text = entry.Text;
                item.Checked = entry.IsCurrentLanguage;
                item.Enabled = entry.Enabled;
                var action = entry.Action; // to capture the right thing into lambda
                item.Click += delegate { action(); };
                items.Add(item);
            }
        }

        /// <summary>Displays a context menu listing all available languages, and optionally controls to edit the translations.</summary>
        /// <param name="positionControl">The menu's position is specified relative to this control.</param>
        /// <param name="position">The menu's desired position relative to <paramref name="positionControl"/>.</param>
        public void ShowContextMenu(Control positionControl, Point position)
        {
            var menu = new ContextMenuStrip() { Renderer = new NativeToolStripRenderer() };
            populateToolStripItems(menu.Items);
            menu.Show(positionControl, position);
        }

        /// <summary>Initialises the combo box to list all available languages and change the application translation when one gets selected.</summary>
        /// <remarks>Do not call multiple times for the same combo box.</remarks>
        public void MakeLanguageComboBox(ComboBox comboBox)
        {
            comboBox.Items.Clear();
            var entries = ListCurrentEntries().Where(e => e.Language != null).ToArray();
            foreach (var entry in entries)
                comboBox.Items.Add(entry);

            comboBox.SelectedItem = entries.Single(e => e.IsCurrentLanguage);
            comboBox.SelectedIndexChanged += delegate
            {
                if (comboBox.SelectedIndex < 0)
                    return;
                var entry = entries[comboBox.SelectedIndex];
                SetLanguage(entry.Language.Value);
            };
        }

        /// <summary>Initialises the menu item to provide language selection UI and any translation editing UI.</summary>
        /// <remarks>Do not call multiple times for the same combo box.</remarks>
        public void MakeLanguageMenu(ToolStripMenuItem menuItem)
        {
            menuItem.DropDownOpening += delegate
            {
                menuItem.DropDownItems.Clear();
                populateToolStripItems(menuItem.DropDownItems);
            };
        }
    }
}
