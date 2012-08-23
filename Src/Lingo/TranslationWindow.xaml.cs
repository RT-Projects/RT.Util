using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using RT.Util.Forms;
using System.Collections.ObjectModel;
using System.Reflection;
using RT.Util.ExtensionMethods;

namespace RT.Util.Lingo
{
    /// <summary>
    /// Interaction logic for TranslationWindow.xaml
    /// </summary>
    public partial class TranslationWindow : ManagedWindow, ITranslationDialog
    {
        private bool _anyChanges = false;
        private ObservableCollection<TranslationGroup> _groups = new ObservableCollection<TranslationGroup>();

        /// <summary>Holds the settings of the <see cref="TranslationForm&lt;T&gt;"/>.</summary>
        public new sealed class Settings : ManagedWindow.Settings
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

        public TranslationWindow(Type translationType, Settings settings, ImageSource icon, string programTitle, string moduleName, Language language)
            : base(settings)
        {
            InitializeComponent();
            if (icon != null)
                Icon = icon;
            Title = "Translating " + programTitle;

            CommandBindings.Add(new CommandBinding(TranslationCommands.SaveAndApply, delegate { SaveChanges(true); }));
            CommandBindings.Add(new CommandBinding(TranslationCommands.Close, delegate { Close(); }));
            CommandBindings.Add(new CommandBinding(TranslationCommands.Find, find));
            CommandBindings.Add(new CommandBinding(TranslationCommands.FindNext, findNext));
            CommandBindings.Add(new CommandBinding(TranslationCommands.FindPrevious, findPrevious));
            CommandBindings.Add(new CommandBinding(TranslationCommands.GotoNextOutOfDateString, gotoNextOutOfDateString));
            CommandBindings.Add(new CommandBinding(TranslationCommands.MarkCurrentStringOutOfDate, markCurrentStringOutOfDate));
            CommandBindings.Add(new CommandBinding(TranslationCommands.MarkAllStringsOutOfDate, markAllStringsOutOfDate));
            CommandBindings.Add(new CommandBinding(TranslationCommands.MarkAllStringsUpToDate, markAllStringsUpToDate));
            CommandBindings.Add(new CommandBinding(TranslationCommands.Font, font));

            var original = (TranslationBase) Activator.CreateInstance(translationType);
            var translation = Lingo.LoadTranslation(translationType, moduleName, language);

            foreach (var group in TranslationDialogHelper.GetGroups(translationType, original, translation))
                _groups.Add(group);

            ctGroups.Items.Clear();
            ctGroups.ItemsSource = _groups;
        }

        //private void generateTranslationPanels(Type type, Dictionary<object, TranslationGroup> groups, string path)
        //{
        //    foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
        //    {
        //        if (field.IsDefined<LingoIgnoreAttribute>())
        //            continue;
        //        if (field.FieldType.IsDefined<LingoStringClassAttribute>())
        //            generateTranslationPanels(field.FieldType, groups, path + field.Name + " / ");
        //        else if (field.FieldType == typeof(TrString) || field.FieldType == typeof(TrStringNum))
        //        {

        //        }
        //    }
        //}

        //private void createPanelsForType(string chkName, Type chkType, Type type, object original, object translation, Dictionary<object, List<TranslationInfo>> dicPanels, List<TranslationInfo> lstUngroupedPanels, List<TranslationInfo> lstAllPanels, IEnumerable<object> classGroups, string path)
        //{
        //    if (!type.IsDefined<LingoStringClassAttribute>(true))
        //    {
        //        if (chkName == null)
        //            throw new ArgumentException(@"Type ""{0}"" must be marked with the [LingoStringClass] attribute.".Fmt(chkType.FullName), "type");
        //        else
        //            throw new ArgumentException(@"Field ""{0}.{1}"" must either be marked with the [LingoIgnore] attribute, or be of type TrString, TrStringNumbers, or a type with the [LingoStringClass] attribute.".Fmt(chkType.FullName, chkName), "type");
        //    }

        //    var thisClassGroups = type.GetCustomAttributes(true).OfType<LingoInGroupAttribute>().Select(attr => attr.Group);
        //    if (classGroups != null)
        //        thisClassGroups = thisClassGroups.Concat(classGroups);

        //    foreach (var f in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
        //    {
        //        if (f.FieldType == typeof(TrString) || f.FieldType == typeof(TrStringNum))
        //        {
        //            string notes = f.GetCustomAttributes(true).OfType<LingoNotesAttribute>().Select(lna => lna.Notes).FirstOrDefault();
        //            var pnl = createTranslationPanel(notes, f.GetValue(original), f.GetValue(translation), path + f.Name);
        //            lstAllPanels.Add(pnl);
        //            var groups = f.GetCustomAttributes(true).OfType<LingoInGroupAttribute>().Select(attr => attr.Group).Concat(thisClassGroups);
        //            if (!groups.Any())
        //                lstUngroupedPanels.Add(pnl);
        //            else
        //                foreach (var group in groups)
        //                    dicPanels.AddSafe(group, pnl);
        //        }
        //        else if (!f.IsDefined<LingoIgnoreAttribute>(true))
        //            createPanelsForType(f.Name, type, f.FieldType, f.GetValue(original), f.GetValue(translation), dicPanels, lstUngroupedPanels, lstAllPanels, thisClassGroups, path + f.Name + " / ");
        //    }
        //}

        /// <summary>
        /// Fires every time the translation is updated on the disk (i.e. when the user clicks either "Save &amp; Close" or "Apply changes").
        /// </summary>
        public event Action<object> TranslationChanged;

        public bool AnyChanges
        {
            get { return _anyChanges; }
        }

        public void SaveChanges(bool fireTranslationChanged)
        {
            throw new NotImplementedException();
        }

        public void CloseWithoutPrompts()
        {
            throw new NotImplementedException();
        }

        private void find(object _, ExecutedRoutedEventArgs __)
        {
            throw new NotImplementedException();
        }

        private void findNext(object _, ExecutedRoutedEventArgs __)
        {
            throw new NotImplementedException();
        }

        private void findPrevious(object _, ExecutedRoutedEventArgs __)
        {
            throw new NotImplementedException();
        }

        private void gotoNextOutOfDateString(object _, ExecutedRoutedEventArgs __)
        {
            throw new NotImplementedException();
        }

        private void markCurrentStringOutOfDate(object _, ExecutedRoutedEventArgs __)
        {
            throw new NotImplementedException();
        }

        private void markAllStringsOutOfDate(object _, ExecutedRoutedEventArgs __)
        {
            throw new NotImplementedException();
        }

        private void markAllStringsUpToDate(object _, ExecutedRoutedEventArgs __)
        {
            throw new NotImplementedException();
        }

        private void font(object _, ExecutedRoutedEventArgs __)
        {
            throw new NotImplementedException();
        }
    }

    static class TranslationCommands
    {
        public static RoutedCommand SaveAndApply = new RoutedCommand();
        public static RoutedCommand Close = new RoutedCommand();
        public static RoutedCommand Find = new RoutedCommand();
        public static RoutedCommand FindNext = new RoutedCommand();
        public static RoutedCommand FindPrevious = new RoutedCommand();
        public static RoutedCommand GotoNextOutOfDateString = new RoutedCommand();
        public static RoutedCommand MarkCurrentStringOutOfDate = new RoutedCommand();
        public static RoutedCommand MarkAllStringsOutOfDate = new RoutedCommand();
        public static RoutedCommand MarkAllStringsUpToDate = new RoutedCommand();
        public static RoutedCommand Font = new RoutedCommand();
    }
}
