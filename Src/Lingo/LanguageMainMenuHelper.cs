using System;
using System.Drawing;
using System.Windows.Forms;
using RT.Util.Dialogs;

namespace RT.Util.Lingo
{
    /// <summary>
    /// Helps an application using Lingo to display language selection UI in the main menu.
    /// </summary>
    /// <typeparam name="TTranslation">The type of the class holding the program's translation.</typeparam>
    public class LanguageMainMenuHelper<TTranslation> where TTranslation : TranslationBase, new()
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
        /// This defaults to false for release builds and true for debug builds.
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
        /// <param name="languageMenu">The menu item which, when opened, will display the list of languages.</param>
        /// <param name="getCurrentLanguage">A callback that returns the currently active language whenever called.</param>
        public LanguageMainMenuHelper(string programTitle, string moduleName, Language defaultLanguage,
            TranslationForm<TTranslation>.Settings trFormSettings, Icon trFormIcon, SetLanguage<TTranslation> setLanguage, ToolStripMenuItem languageMenu, Func<Language> getCurrentLanguage)
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
#if DEBUG
            TranslationEditingEnabled = true;
#endif

            _languageMenu.DropDownOpening += new EventHandler(_languageMenu_DropDownOpening);
        }

        void _languageMenu_DropDownOpening(object sender, EventArgs e)
        {
            _currentLanguage = _getCurrentLanguage();
            _languageMenu.DropDownItems.Clear();
            _languageMenu.DropDownItems.AddRange(Lingo.LanguageToolStripMenuItems<TTranslation>(_moduleName, _setLanguage, _currentLanguage));
            ToolStripItem miEdit = null;
            if (TranslationEditingEnabled)
            {
                _languageMenu.DropDownItems.Add(new ToolStripSeparator());
                _languageMenu.DropDownItems.Add(miEdit = new ToolStripMenuItem("&Edit current language", null, new EventHandler(editCurrentLanguage)));
                _languageMenu.DropDownItems.Add(new ToolStripMenuItem("&Create new language", null, new EventHandler(createNewLanguage)));
            }
            // Disable most items if a translation dialog is currently visible
            if (_translationDialog != null)
            {
                foreach (ToolStripItem mi in _languageMenu.DropDownItems)
                    if (mi != miEdit)
                        mi.Enabled = false;
            }
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
