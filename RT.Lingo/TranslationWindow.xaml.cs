using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using RT.Util.ExtensionMethods;
using RT.Util.Forms;

namespace RT.Lingo
{
    /// <summary>
    /// Interaction logic for TranslationWindow.xaml
    /// </summary>
    public partial class TranslationWindow : ManagedWindow, ITranslationDialog
    {
        private bool _anyChanges = false;
        private TranslationBase _translation;
        private Type _translationType;
        private string _moduleName;
        private Language _language;
        private ObservableCollection<TranslationGroup> _groups = new ObservableCollection<TranslationGroup>();

        /// <summary>Holds the settings of the <see cref="TranslationForm&lt;T&gt;"/>.</summary>
        public new sealed class Settings : ManagedWindow.Settings
        {
            /// <summary>Remembers the position of the horizontal splitter (between the tree view and the main interface).</summary>
            public int SplitterDistance = 300;
            /// <summary>Remembers the string last typed in the Find dialog.</summary>
            public string LastFindQuery = "";
            /// <summary>Remembers the last settings of the "Search Original" option in the Find dialog.</summary>
            public bool LastFindOrig = true;
            /// <summary>Remembers the last settings of the "Search Translation" option in the Find dialog.</summary>
            public bool LastFindTrans = true;
            /// <summary>Remembers the name of the last font used.</summary>
            public string FontName;
            /// <summary>Remembers the size of the last font used.</summary>
            public float FontSize;
        }

        /// <summary>Main constructor.</summary>
        /// <param name="translationType">The type containing the <see cref="TrString"/> and <see cref="TrStringNum"/> fields to be translated.</param>
        /// <param name="settings">Settings of the <see cref="TranslationWindow"/>.</param>
        /// <param name="icon">Application icon to use.</param>
        /// <param name="programTitle">Title of the program. Used in the title bar.</param>
        /// <param name="moduleName">Used for locating the translation file to be edited under the Translations directory.</param>
        /// <param name="language">The language to be edited.</param>
        public TranslationWindow(Type translationType, Settings settings, ImageSource icon, string programTitle, string moduleName, Language language)
            : base(settings)
        {
            InitializeComponent();

            _translationType = translationType;
            _moduleName = moduleName;
            _language = language;

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

            CommandBindings.Add(new CommandBinding(TranslationCommands.PrevTextBox, delegate { gotoTextBox(up: true); }));
            CommandBindings.Add(new CommandBinding(TranslationCommands.NextTextBox, delegate { gotoTextBox(up: false); }));
            CommandBindings.Add(new CommandBinding(TranslationCommands.PrevGroup, delegate { gotoGroup(up: true); }));
            CommandBindings.Add(new CommandBinding(TranslationCommands.NextGroup, delegate { gotoGroup(up: false); }));

            var original = (TranslationBase) Activator.CreateInstance(translationType);
            _translation = Lingo.LoadTranslation(translationType, moduleName, language);

            foreach (var group in TranslationDialogHelper.GetGroups(translationType, original, _translation))
                _groups.Add(group);

            ctGroups.Items.Clear();
            ctGroups.ItemsSource = _groups;
        }

        /// <summary>
        /// Fires every time the translation is updated on the disk (i.e. when the user clicks either "Save &amp; Close" or "Apply changes").
        /// </summary>
        public event Action<TranslationBase> TranslationChanged;

        /// <summary>Gets a value indicating whether any changes have been made by the user since the last save.</summary>
        public bool AnyChanges { get { return _anyChanges; } }

        /// <summary>TODO: comment</summary>
        public void SaveChanges(bool fireTranslationChanged)
        {
            if (AnyChanges)
            {
                Lingo.SaveTranslation(_translationType, _moduleName, _translation);
                if (fireTranslationChanged && TranslationChanged != null)
                    TranslationChanged(Lingo.LoadTranslation(_translationType, _moduleName, _language));
                _anyChanges = false;
            }
        }

        /// <summary>TODO: comment</summary>
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

        private void populateTrStringNumGrid(object sender, RoutedEventArgs e)
        {
            var grid = (Grid) sender;
            var info = (TrStringNumInfo) grid.DataContext;
            int nn = info.TranslationTr.IsNumber.Where(b => b).Count();
            int curRow = 0;

            string label2 = "Original:";
            if (info.TranslationTr.Old != null && !info.TranslationTr.Old.SequenceEqual(info.NewOriginal))
            {
                populateRows(grid, "Old Original:", info, info.TranslationTr.Old, info.OriginalNumSys, false, nn, ref curRow);
                label2 = "New Original:";
            }

            populateRows(grid, label2, info, info.NewOriginal, info.OriginalNumSys, false, nn, ref curRow);
            populateRows(grid, "Translation:", info, info.TranslationTr.Translations, info.TranslationNumSys, true, nn, ref curRow, info.NewOriginal);

            // Label (e.g. “Original”, “Translation”)
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
            for (int i = 0; i < nn; i++)
                // Number form descriptions (e.g. “sglr”, “plrl”) for each interpolated number
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
            // Original text or textboxes for translations
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            // OK button
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });

            for (int i = 0; i < curRow; i++)
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
        }

        private void populateRows(Grid grid, string label, TrStringNumInfo info, string[] display, NumberSystem ns, bool useTextBoxes, int nn, ref int curRow, string[] newOriginal = null)
        {
            int numRows = (int) Math.Pow(ns.NumStrings, nn);

            var textBlock = new TextBlock(new Run(label));
            grid.Children.Add(textBlock);
            Grid.SetColumn(textBlock, 0);
            Grid.SetRow(textBlock, curRow);
            Grid.SetRowSpan(textBlock, numRows);

            var textBoxes = useTextBoxes ? new List<TextBox>() : null;

            int column = 1;
            for (int i = 0; i < info.TranslationTr.IsNumber.Length; i++)
            {
                if (info.TranslationTr.IsNumber[i])
                {
                    var header = new TextBlock(new Run("{" + i + "}"));
                    Grid.SetColumn(header, column);
                    Grid.SetRow(header, curRow);
                    grid.Children.Add(header);
                    column++;
                }
            }
            curRow++;

            Button button = null;
            for (int row = 0; row < numRows; row++)
            {
                int col = 1;
                int r = row;
                for (int i = 0; i < info.TranslationTr.IsNumber.Length; i++)
                {
                    if (info.TranslationTr.IsNumber[i])
                    {
                        var numFormDescr = new TextBlock(new Run(ns.GetDescription(r % ns.NumStrings)));
                        Grid.SetColumn(numFormDescr, col);
                        Grid.SetRow(numFormDescr, curRow);
                        grid.Children.Add(numFormDescr);
                        col++;
                        r /= ns.NumStrings;
                    }
                }
                if (useTextBoxes)
                {
                    var textBox = new TextBox
                    {
                        Text = (display != null && row < display.Length) ? display[row] : "",
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        Width = double.NaN,
                        Height = double.NaN,
                        AcceptsReturn = true,
                        Tag = info
                    };
                    Grid.SetColumn(textBox, col);
                    Grid.SetRow(textBox, curRow);
                    grid.Children.Add(textBox);
                    if (row == 0)
                    {
                        button = new Button { Content = "OK", Tag = info };
                        Grid.SetColumn(button, col + 1);
                        Grid.SetRow(button, curRow);
                        Grid.SetRowSpan(button, numRows);
                        button.Click += delegate
                        {
                            var newTrans = textBoxes.Select(box => box.Text).ToArray();
                            if (info.State != TranslationInfoState.UpToDateAndSaved || !info.TranslationTr.Translations.SequenceEqual(newTrans))
                            {
                                info.TranslationTr.Translations = newTrans;
                                info.TranslationTr.Old = newOriginal;
                                _anyChanges = true;
                            }
                            fireCtrlDown();
                        };
                        // Do not add to the grid yet; must add it after all the textboxes so that its tab order is correct
                    }
                }
                else
                {
                    var originalText = new TextBlock(new Run((display != null && row < display.Length) ? display[row] : ""));
                    Grid.SetColumn(originalText, col);
                    Grid.SetColumnSpan(originalText, 2);
                    Grid.SetRow(originalText, curRow);
                    grid.Children.Add(originalText);
                }
                curRow++;
            }

            // Defer adding the button until the end so that its tab order is correct
            if (button != null)
                grid.Children.Add(button);
        }

        private void acceptTranslation(object sender, RoutedEventArgs e)
        {
            var info = (TrStringInfo) ((Button) sender).Tag;
            if (info.State != TranslationInfoState.UpToDateAndSaved)
            {
                info.TranslationTr.Old = info.NewOriginal;
                info.State = TranslationInfoState.Unsaved;
                _anyChanges = true;
            }
            fireCtrlDown();
        }

        private void fireCtrlDown()
        {
            throw new NotImplementedException();
        }

        private void ctGroups_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _uiElementCache = null;
            ctStrings.ItemsSource = null; // removes the example objects from XAML
            ctStrings.Items.Clear();
            ctStrings.ItemsSource = ((TranslationGroup) ctGroups.SelectedItem).Infos;
        }

        private object[] _uiElementCache;

        private static IEnumerable<DependencyObject> findVisualChildren(DependencyObject control, Predicate<DependencyObject> predicate)
        {
            if (control == null)
                yield break;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(control); i++)
            {
                var child = VisualTreeHelper.GetChild(control, i);
                if (child != null && predicate(child))
                    yield return child;
                foreach (var childOfChild in findVisualChildren(child, predicate))
                    yield return childOfChild;
            }
        }

        private void gotoGroup(bool up)
        {
            var curGroup = ctGroups.SelectedIndex;
            if (up)
            {
                if (curGroup == 0)
                    return;
                curGroup--;
            }
            else
            {
                if (curGroup == ctGroups.Items.Count - 1)
                    return;
                curGroup++;
            }
            ctGroups.SelectedIndex = curGroup;
            ctGroups.ScrollIntoView(ctGroups.SelectedItem);
        }

        private void gotoTextBox(bool up)
        {
            if (_uiElementCache == null)
                _uiElementCache = findVisualChildren(this, obj => obj is TextBox || obj is Button).ToArray<object>();

            var currentElement = FocusManager.GetFocusedElement(this);
            // TODO: Remove the next two lines once we think it never triggers
            if (currentElement != Keyboard.FocusedElement)
                System.Diagnostics.Debugger.Break();

            if (!(currentElement is TextBox || currentElement is Button))
                return;
            var index = Array.IndexOf(_uiElementCache, currentElement);

            if (up)
            {
                if (index == -1)
                    index = _uiElementCache.Length;
                do
                {
                    index--;
                    if (index == -1)
                        return;
                }
                while (index >= 0 && !(_uiElementCache[index] is TextBox));
            }
            else
            {
                do
                {
                    index++;
                    if (index == _uiElementCache.Length)
                        return;
                }
                while (index < _uiElementCache.Length && !(_uiElementCache[index] is TextBox));
            }
            var toFocus = (TextBox) _uiElementCache[index];
            toFocus.Focus();
            ((ContentPresenter) ctStrings.ItemContainerGenerator.ContainerFromItem(toFocus.Tag)).BringIntoView();
        }

        private void translationInfoTemplateLoad(object sender, RoutedEventArgs e)
        {
            var stackPanel = (StackPanel) sender;
            var source = (TrStringInfo) stackPanel.DataContext;
            stackPanel.IsKeyboardFocusWithinChanged += delegate { source.IsFocused = stackPanel.IsKeyboardFocusWithin; };
            //System.Diagnostics.Debugger.Break();
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
        public static RoutedCommand PrevTextBox = new RoutedCommand();
        public static RoutedCommand NextTextBox = new RoutedCommand();
        public static RoutedCommand PrevGroup = new RoutedCommand();
        public static RoutedCommand NextGroup = new RoutedCommand();
    }
}
