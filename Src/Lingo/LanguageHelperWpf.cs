using System;
using System.Windows.Controls;
using System.Windows.Media;
using RT.Util.Dialogs;

namespace RT.Util.Lingo
{
    /// <summary>Helps an application using Lingo to display language selection UI using WPF controls.</summary>
    /// <typeparam name="TTranslation">The type of the class holding the program’s translation.</typeparam>
    public class LanguageHelperWpf<TTranslation> : LanguageHelper<TTranslation> where TTranslation : TranslationBase, new()
    {
        private readonly TranslationWindow.Settings _settings;
        private readonly ImageSource _icon;

        private TranslationWindow _translationWindow;
        internal override ITranslationDialog TranslationDialog { get { return _translationWindow; } }

        /// <summary>Constructor.</summary>
        /// <param name="programTitle">The title of the program - to be displayed in the translation UI.</param>
        /// <param name="moduleName">Name of the module being translated - used to construct the filename for the translation file.</param>
        /// <param name="editable">Whether translation editing UI should be included.</param>
        /// <param name="settings">Translation window settings, such as window position/size.</param>
        /// <param name="icon">The icon to use on the translation window.</param>
        /// <param name="getCurrentLanguage">A callback that returns the currently active language whenever called.</param>
        public LanguageHelperWpf(string programTitle, string moduleName, bool editable,
            TranslationWindow.Settings settings, ImageSource icon, Func<Language> getCurrentLanguage)
            : base(programTitle, moduleName, editable, getCurrentLanguage)
        {
            if (settings == null) throw new ArgumentNullException("settings");
            if (icon == null) throw new ArgumentNullException("icon");
            _settings = settings;
            _icon = icon;
        }

        /// <summary>Appends menu items for changing languages and, optionally, translation editing. The menu items are hooked with appropriate click handlers.</summary>
        public void PopulateMenuItems(ItemCollection items)
        {
            foreach (var entry in ListCurrentEntries())
            {
                if (entry.SeparatorBefore)
                    items.Add(new Separator());
                var item = new MenuItem();
                item.Header = entry.Text.Replace("&&", "&").Replace("_", "__").Replace("&", "_");
                item.IsChecked = entry.IsCurrentLanguage;
                item.IsEnabled = entry.Enabled;
                var action = entry.Action; // to capture the right thing into lambda
                item.Click += delegate { action(); };
                items.Add(item);
            }
        }

        /// <summary>Override; see base.</summary>
        protected override void CreateNewLanguage()
        {
            var newTranslation = TranslationCreateForm.CreateTranslation<TTranslation>(_moduleName, _settings.FontName, _settings.FontSize);
            if (newTranslation != null)
            {
                SetLanguage(newTranslation.Language);
                EditCurrentLanguage();
            }
        }

        /// <summary>Override; see base.</summary>
        protected override void EditCurrentLanguage()
        {
            if (_getCurrentLanguage() == _defaultLanguage)
            {
                DlgMessage.Show("The currently selected language is the native language of this application and cannot be edited.", "Edit current language", DlgType.Info);
                return;
            }
            if (_translationWindow == null)
            {
#if !DEBUG
                try
#endif
                {
                    _translationWindow = new TranslationWindow(typeof(TTranslation), _settings, _icon, _programTitle, _moduleName, _getCurrentLanguage());
                }
#if !DEBUG
                catch (Exception ex)
                {
                    DlgMessage.Show("Translation could not be loaded: " + ex.Message, "Edit current language", DlgType.Info);
                    return;
                }
#endif
                _translationWindow.TranslationChanged += tr => { FireTranslationChanged((TTranslation) tr); };
                _translationWindow.Closed += delegate { _translationWindow = null; };
            }
            _translationWindow.Show();
        }
    }

    /// <summary>Helps an application using Lingo to display language selection UI using WPF controls but the WinForms translation UI.</summary>
    /// <remarks>This class is required for as long as the WPF translation UI is unfinished.</remarks>
    /// <typeparam name="TTranslation">The type of the class holding the program’s translation.</typeparam>
    public class LanguageHelperWpfOld<TTranslation> : LanguageHelper<TTranslation> where TTranslation : TranslationBase, new()
    {
        private readonly TranslationForm<TTranslation>.Settings _settings;
        private readonly System.Drawing.Icon _icon;

        private TranslationForm<TTranslation> _translationForm;
        internal override ITranslationDialog TranslationDialog { get { return _translationForm; } }

        /// <summary>Constructor.</summary>
        /// <param name="programTitle">The title of the program - to be displayed in the translation UI.</param>
        /// <param name="moduleName">Name of the module being translated - used to construct the filename for the translation file.</param>
        /// <param name="editable">Whether translation editing UI should be included.</param>
        /// <param name="settings">Translation window settings, such as window position/size.</param>
        /// <param name="icon">The icon to use on the translation window.</param>
        /// <param name="getCurrentLanguage">A callback that returns the currently active language whenever called.</param>
        public LanguageHelperWpfOld(string programTitle, string moduleName, bool editable,
            TranslationForm<TTranslation>.Settings settings, System.Drawing.Icon icon, Func<Language> getCurrentLanguage)
            : base(programTitle, moduleName, editable, getCurrentLanguage)
        {
            if (settings == null) throw new ArgumentNullException("settings");
            if (icon == null) throw new ArgumentNullException("icon");
            _settings = settings;
            _icon = icon;
        }

        /// <summary>Override; see base.</summary>
        protected override void CreateNewLanguage()
        {
            var newTranslation = TranslationCreateForm.CreateTranslation<TTranslation>(_moduleName, _settings.FontName, _settings.FontSize);
            if (newTranslation != null)
            {
                SetLanguage(newTranslation.Language);
                EditCurrentLanguage();
            }
        }

        /// <summary>Override; see base.</summary>
        protected override void EditCurrentLanguage()
        {
            if (_getCurrentLanguage() == _defaultLanguage)
            {
                DlgMessage.Show("The currently selected language is the native language of this application and cannot be edited.", "Edit current language", DlgType.Info);
                return;
            }
            if (_translationForm == null)
            {
                try
                {
                    _translationForm = new TranslationForm<TTranslation>(_settings, _icon, _programTitle, _moduleName, _getCurrentLanguage());
                }
                catch (Exception ex)
                {
                    DlgMessage.Show("Translation could not be loaded: " + ex.Message, "Edit current language", DlgType.Info);
                    return;
                }
                _translationForm.TranslationChanged += tr => { FireTranslationChanged(tr); };
                _translationForm.FormClosed += delegate { _translationForm = null; };
            }
            _translationForm.Show();
        }

        /// <summary>Appends menu items for changing languages and, optionally, translation editing. The menu items are hooked with appropriate click handlers.</summary>
        public void PopulateMenuItems(ItemCollection items)
        {
            foreach (var entry in ListCurrentEntries())
            {
                if (entry.SeparatorBefore)
                    items.Add(new Separator());
                var item = new MenuItem();
                item.Header = entry.Text.Replace("&&", "&").Replace("_", "__").Replace("&", "_");
                item.IsChecked = entry.IsCurrentLanguage;
                item.IsEnabled = entry.Enabled;
                var action = entry.Action; // to capture the right thing into lambda
                item.Click += delegate { action(); };
                items.Add(item);
            }
        }
    }
}
