using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using RT.Util.Dialogs;

namespace RT.Util.Lingo
{
    /// <summary>
    /// Helps an application using Lingo display a language selection context menu.
    /// </summary>
    /// <typeparam name="TTranslation">The type of the class holding the program's translation.</typeparam>
    public class LanguageContextMenuHelper<TTranslation> where TTranslation : TranslationBase, new()
    {
        private string _programTitle;
        private string _moduleName;
        private Language _defaultLanguage;
        private TranslationForm<TTranslation>.Settings _trFormSettings;
        private Icon _trFormIcon;
        private SetLanguage<TTranslation> _setLanguage;

        private Language _currentLanguage;

        private ContextMenu _menu;
        private TranslationForm<TTranslation> _translationDialog;

        /// <summary>
        /// Gets or sets a value indicating whether the menu items to edit or create a language should be displayed.
        /// This defaults to false for release builds and true for debug builds.
        /// </summary>
        public bool TranslationEditingEnabled { get; set; }

        /// <summary>
        /// Creates a new language selection context menu helper.
        /// </summary>
        /// <param name="programTitle">The title of the program - to be displayed in the translation UI.</param>
        /// <param name="moduleName">Name of the module being translated - used to construct the filename for the translation file.</param>
        /// <param name="defaultLanguage">The language of the default translation (this translation cannot be edited as it's compiled into the program).</param>
        /// <param name="trFormSettings">Translation window settings, such as window position/size.</param>
        /// <param name="trFormIcon">The icon to use on the translation window.</param>
        /// <param name="setLanguage">A callback to invoke in order to change the program language. See <see cref="SetLanguage&lt;T&gt;"/> for more details.</param>
        public LanguageContextMenuHelper(string programTitle, string moduleName, Language defaultLanguage,
            TranslationForm<TTranslation>.Settings trFormSettings, System.Drawing.Icon trFormIcon, SetLanguage<TTranslation> setLanguage)
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
#if DEBUG
            TranslationEditingEnabled = true;
#endif
        }

        /// <summary>
        /// Displays a context menu listing all available languages, and optionally controls to edit the translations.
        /// </summary>
        /// <param name="curLanguage">The language currently in use by the program.</param>
        /// <param name="positionControl">The menu's position is specified relative to this control.</param>
        /// <param name="position">The menu's desired position relative to <paramref name="positionControl"/>.</param>
        public void ShowContextMenu(Language curLanguage, Control positionControl, Point position)
        {
            _currentLanguage = curLanguage;
            _menu = new ContextMenu(Lingo.LanguageMenuItems<TTranslation>(_moduleName, _setLanguage, curLanguage).ToArray());
            MenuItem miEdit = null;
            if (TranslationEditingEnabled)
            {
                _menu.MenuItems.Add(new MenuItem("-"));
                _menu.MenuItems.Add(miEdit = new MenuItem("&Edit current translation", new EventHandler(editCurrentLanguage)));
                _menu.MenuItems.Add(new MenuItem("&Create new translation", new EventHandler(createNewLanguage)));
            }
            // Disable most items if a translation dialog is currently visible
            if (_translationDialog != null)
            {
                foreach (MenuItem mi in _menu.MenuItems)
                    if (mi != miEdit)
                        mi.Enabled = false;
            }

            _menu.Show(positionControl, position);
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
                _translationDialog = new TranslationForm<TTranslation>(_trFormSettings, _trFormIcon, _programTitle, _moduleName, _currentLanguage);
                _translationDialog.TranslationChanged += _setLanguage;
                _translationDialog.FormClosed += (s, v) => { _translationDialog = null; };
            }
            _translationDialog.Show();
        }

        private void createNewLanguage(object sender, EventArgs e)
        {
            var newTranslation = TranslationCreateForm.CreateTranslation<TTranslation>(_moduleName, _setLanguage);
            if (newTranslation != null)
            {
                _currentLanguage = newTranslation.Language;
                editCurrentLanguage(sender, e);
            }
        }
    }
}
