using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using RT.Util.Controls;
using RT.Util.Forms;

namespace RT.Lingo
{
    /// <summary>Helps an application using Lingo to display language selection UI using WinForms controls.</summary>
    /// <typeparam name="TTranslation">The type of the class holding the program’s translation.</typeparam>
    public class LanguageHelperWinForms<TTranslation> : LanguageHelper<TTranslation> where TTranslation : TranslationBase, new()
    {
        private readonly TranslationForm<TTranslation>.Settings _settings;
        private readonly Icon _icon;

        private TranslationForm<TTranslation> _translationForm;
        internal override ITranslationDialog TranslationDialog { get { return _translationForm; } }

        /// <summary>Constructor.</summary>
        /// <param name="programTitle">The title of the program - to be displayed in the translation UI.</param>
        /// <param name="moduleName">Name of the module being translated - used to construct the filename for the translation file.</param>
        /// <param name="editable">Whether translation editing UI should be included.</param>
        /// <param name="settings">Translation window settings, such as window position/size.</param>
        /// <param name="icon">The icon to use on the translation window.</param>
        /// <param name="getCurrentLanguage">A callback that returns the currently active language whenever called.</param>
        public LanguageHelperWinForms(string programTitle, string moduleName, bool editable,
            TranslationForm<TTranslation>.Settings settings, Icon icon, Func<Language> getCurrentLanguage)
            : base(programTitle, moduleName, editable, getCurrentLanguage)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (icon == null) throw new ArgumentNullException(nameof(icon));
            _settings = settings;
            _icon = icon;
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
    }
}
