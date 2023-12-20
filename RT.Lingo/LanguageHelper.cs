using System.IO;
using System.Text.RegularExpressions;
using RT.Util;
using RT.Util.Forms;

namespace RT.Lingo;

/// <summary>
///     Helps an application using Lingo to display language selection UI.</summary>
/// <typeparam name="TTranslation">
///     The type of the class holding the program’s translation.</typeparam>
public abstract class LanguageHelper<TTranslation> where TTranslation : TranslationBase, new()
{
    internal readonly string _programTitle;
    internal readonly string _moduleName;
    internal readonly bool _editable;
    internal readonly Func<Language> _getCurrentLanguage;
    internal readonly Language _defaultLanguage = new TTranslation().Language;

    /// <summary>
    ///     Constructor.</summary>
    /// <param name="programTitle">
    ///     The title of the program - to be displayed in the translation UI.</param>
    /// <param name="moduleName">
    ///     Name of the module being translated - used to construct the filename for the translation file.</param>
    /// <param name="editable">
    ///     Whether translation editing UI should be included.</param>
    /// <param name="getCurrentLanguage">
    ///     A callback that returns the currently active language whenever called.</param>
    public LanguageHelper(string programTitle, string moduleName, bool editable, Func<Language> getCurrentLanguage)
    {
        if (programTitle == null) throw new ArgumentNullException(nameof(programTitle));
        if (moduleName == null) throw new ArgumentNullException(nameof(moduleName));
        if (getCurrentLanguage == null) throw new ArgumentNullException(nameof(getCurrentLanguage));
        _programTitle = programTitle;
        _moduleName = moduleName;
        _editable = editable;
        _getCurrentLanguage = getCurrentLanguage;
    }

    /// <summary>
    ///     Changes the currently selected language in exactly the same way as using one of the UI elements would.</summary>
    /// <param name="language">
    ///     The desired language. If not available, the default language will be set instead.</param>
    public void SetLanguage(Language language)
    {
        TranslationChanged(language == _defaultLanguage ? new TTranslation() : Lingo.LoadTranslationOrDefault<TTranslation>(_moduleName, ref language));
    }

    /// <summary>
    ///     Occurs whenever the translation has been changed. The application must respond by updating *all* visible UI and
    ///     storing the language of the selected translation for use on next program start.</summary>
    public event SetTranslation<TTranslation> TranslationChanged = delegate { };

    /// <summary>Triggers the <see cref="TranslationChanged"/> event with the specified translation.</summary>
    protected void FireTranslationChanged(TTranslation translation) { TranslationChanged(translation); }

    internal abstract ITranslationDialog TranslationDialog { get; }

    /// <summary>
    ///     Returns a value indicating whether it is okay to close the application. The user is asked if there are any unsaved
    ///     changes.</summary>
    public bool MayExitApplication()
    {
        if (TranslationDialog == null || !TranslationDialog.AnyChanges)
            return true;
        var result = DlgMessage.Show("Would you like to save the changes to made to the translation you are currently editing?",
                "Exit Application", DlgType.Warning, "Save changes", "Discard changes", "Cancel");
        if (result == 2)
            return false;
        if (result == 0)
            TranslationDialog.SaveChanges(false);
        return true;
    }

    /// <summary>Closes the translation dialog (if it is visible) without any prompts regarding unsaved changes.</summary>
    public void CloseWithoutPrompts()
    {
        if (TranslationDialog != null)
            TranslationDialog.CloseWithoutPrompts();
    }

    /// <summary>Describes a UI entry exposed by the language helper.</summary>
    protected class Entry
    {
        /// <summary>
        ///     The label that the control should have. Hotkeys are prefixed with an ampersand; ampersands are escaped by
        ///     doubling.</summary>
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
    ///     Returns a list of UI control descriptors whose state is applicable at the time of invocation. This method should
    ///     be invoked every time prior to displaying UI, to obtain the up-to-date UI state. The entries returned are not
    ///     "live" and will not be updated to reflect any changes.</summary>
    /// <remarks>
    ///     The list is guaranteed to have at least one item, the default language. Exactly one item will have <see
    ///     cref="Entry.IsCurrentLanguage"/> set to true.</remarks>
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
            result.Add(editEntry = new Entry("&Edit current language", EditCurrentLanguage) { SeparatorBefore = true });
            result.Add(new Entry("&Create new language", CreateNewLanguage));
        }

        // Disable most items if a translation dialog is currently visible
        if (TranslationDialog != null)
            foreach (var entry in result)
                if (entry != editEntry)
                    entry.Enabled = false;

        return result;
    }

    /// <summary>Opens the dialog for creating a new language.</summary>
    protected abstract void CreateNewLanguage();
    /// <summary>Opens the GUI for editing the translation in the current language.</summary>
    protected abstract void EditCurrentLanguage();
}
