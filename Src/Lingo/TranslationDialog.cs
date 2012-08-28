using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using RT.Util.ExtensionMethods;

namespace RT.Util.Lingo
{
    /// <summary>Contains methods and properties to be implemented by the different implementations of the Lingo translation GUI.</summary>
    internal interface ITranslationDialog
    {
        /// <summary>Determines whether any changes have been made to the translation by the user since the last save.</summary>
        bool AnyChanges { get; }
        /// <summary>Saves the current translation.</summary>
        /// <param name="fireTranslationChanged">True if the TranslationChanged event is to be fired; false if not.</param>
        void SaveChanges(bool fireTranslationChanged);
        /// <summary>Closes the dialog without prompting for saving or discarding unsaved changes.</summary>
        void CloseWithoutPrompts();
    }

    static class TranslationDialogHelper
    {
        public static IEnumerable<TranslationGroup> GetGroups(Type type, TranslationBase original, TranslationBase translation)
        {
            var dic = new Dictionary<object, TranslationGroup>();
            TranslationGroup ungrouped = null;
            getGroups(null, type, original, translation, original.Language.GetNumberSystem(), translation.Language.GetNumberSystem(), dic, ref ungrouped, new object[0], "");
            var enumTypes = dic.Keys.Select(k => k.GetType()).Distinct().OrderBy(t => t.Name).ToArray();
            var enumValues = enumTypes.SelectMany(t => Enum.GetValues(t).Cast<object>()).ToArray();
            foreach (var key in enumValues)
                if (dic.ContainsKey(key))
                    yield return dic[key];
            if (ungrouped != null)
                yield return ungrouped;
        }

        private static void getGroups(string fieldName, Type type, object original, object translation,
            NumberSystem originalNumSys, NumberSystem translationNumSys, Dictionary<object, TranslationGroup> dic,
            ref TranslationGroup ungrouped, IEnumerable<object> classGroups, string path)
        {
            if (!type.IsDefined<LingoStringClassAttribute>(true))
            {
                if (fieldName == null)
                    throw new ArgumentException(@"Type ""{0}"" must be marked with the [LingoStringClass] attribute.".Fmt(type.FullName), "type");
                else
                    throw new ArgumentException(@"Field ""{0}.{1}"" must either be marked with the [LingoIgnore] attribute, or be of type TrString, TrStringNumbers, or a type with the [LingoStringClass] attribute.".Fmt(type.FullName, fieldName), "type");
            }

            var thisClassGroups = type.GetCustomAttributes(true).OfType<LingoInGroupAttribute>().Select(attr => attr.Group);
            if (classGroups != null)
                thisClassGroups = thisClassGroups.Concat(classGroups);

            foreach (var f in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (f.FieldType == typeof(TrString) || f.FieldType == typeof(TrStringNum))
                {
                    string notes = f.GetCustomAttributes<LingoNotesAttribute>().Select(lna => lna.Notes).FirstOrDefault();
                    var trInfo = f.FieldType == typeof(TrString)
                        ? (TranslationInfo) new TrStringInfo
                        {
                            Label = path + f.Name,
                            Notes = notes,
                            NewOriginal = ((TrString) f.GetValue(original)).Translation,
                            TranslationTr = (TrString) f.GetValue(translation)
                        }
                        : (TranslationInfo) new TrStringNumInfo((TrStringNum) f.GetValue(original), (TrStringNum) f.GetValue(translation), originalNumSys, translationNumSys)
                        {
                            Label = path + f.Name,
                            Notes = notes
                        };

                    var groups = f.GetCustomAttributes<LingoInGroupAttribute>().Select(attr => attr.Group).Concat(thisClassGroups);
                    if (!groups.Any())
                    {
                        if (ungrouped == null)
                            ungrouped = new TranslationGroup { Label = "Ungrouped strings", Notes = "This group contains strings not found in any other group." };
                        ungrouped.Infos.Add(trInfo);
                    }
                    else
                    {
                        foreach (var group in groups)
                        {
                            TranslationGroup grp;
                            if (!dic.TryGetValue(group, out grp))
                            {
                                grp = createGroup(group);
                                dic[group] = grp;
                            }
                            grp.Infos.Add(trInfo);
                        }
                    }
                }
                else if (!f.IsDefined<LingoIgnoreAttribute>(true))
                    getGroups(f.Name, f.FieldType, f.GetValue(original), f.GetValue(translation), originalNumSys, translationNumSys, dic, ref ungrouped, thisClassGroups, path + f.Name + " / ");
            }
        }

        private static TranslationGroup createGroup(object groupEnum)
        {
            var type = groupEnum.GetType();
            if (!type.IsEnum)
                throw new ArgumentException(@"The type ""{0}"" is not an enum type.".Fmt(type.FullName));
            var field = type.GetField(groupEnum.ToString(), BindingFlags.Static | BindingFlags.Public);
            var attr = field.GetCustomAttributes<LingoGroupAttribute>().FirstOrDefault();
            if (attr == null)
                throw new ArgumentException(@"The enum value ""{0}.{1}"" does not have a LingoGroupAttribute.".Fmt(type.FullName, groupEnum));
            return new TranslationGroup { Label = attr.GroupName, Notes = attr.Description };
        }
    }

    sealed class TranslationGroup : INotifyPropertyChanged
    {
        public string Label { get { return _label; } set { _label = value; propertyChanged("Label"); } }
        private string _label;
        public string Notes { get { return _notes; } set { _notes = value; propertyChanged("Notes"); } }
        private string _notes;

        public bool OutOfDate { get { return _outOfDate; } set { _outOfDate = value; propertyChanged("OutOfDate"); propertyChanged("OutOfDateVisibility"); } }
        private bool _outOfDate;
        public Visibility OutOfDateVisibility { get { return _outOfDate ? Visibility.Visible : Visibility.Collapsed; } }

        private List<TranslationInfo> _infos = new List<TranslationInfo>();
        public List<TranslationInfo> Infos { get { return _infos; } }

        private void propertyChanged(string name) { PropertyChanged(this, new PropertyChangedEventArgs(name)); }
        public event PropertyChangedEventHandler PropertyChanged = delegate { };
    }

    abstract class TranslationInfo : INotifyPropertyChanged
    {
        public string Label { get { return _label; } set { _label = value; propertyChanged("Label"); } }
        private string _label;
        public string Notes { get { return _notes; } set { _notes = value; propertyChanged("Notes"); } }
        private string _notes;

        protected static Brush upToDateNormal = new SolidColorBrush(Color.FromRgb(0xcc, 0xcc, 0xcc));
        protected static Brush outOfDateNormal = new SolidColorBrush(Color.FromRgb(0xcc, 0xcc, 0xff));
        protected static Brush unsavedNormal = new SolidColorBrush(Color.FromRgb(0xff, 0xcc, 0xcc));
        protected static Brush upToDateFocus = new SolidColorBrush(Color.FromRgb(0xdd, 0xdd, 0xdd));
        protected static Brush outOfDateFocus = new SolidColorBrush(Color.FromRgb(0xdd, 0xdd, 0xff));
        protected static Brush unsavedFocus = new SolidColorBrush(Color.FromRgb(0xff, 0xdd, 0xdd));
        protected static Brush upToDateOldNormal = new SolidColorBrush(Color.FromRgb(0xbb, 0xbb, 0xbb));
        protected static Brush outOfDateOldNormal = new SolidColorBrush(Color.FromRgb(0xbb, 0xbb, 0xff));
        protected static Brush unsavedOldNormal = new SolidColorBrush(Color.FromRgb(0xff, 0xbb, 0xbb));
        protected static Brush upToDateOldFocus = new SolidColorBrush(Color.FromRgb(0xcc, 0xcc, 0xcc));
        protected static Brush outOfDateOldFocus = new SolidColorBrush(Color.FromRgb(0xcc, 0xcc, 0xff));
        protected static Brush unsavedOldFocus = new SolidColorBrush(Color.FromRgb(0xff, 0xcc, 0xcc));

        public virtual TranslationInfoState State { get { return _state; } set { _state = value; propertyChanged("State"); propertyChanged("Background"); propertyChanged("BackgroundForOldLabel"); } }
        private TranslationInfoState _state;
        public bool IsFocused { get { return _isFocused; } set { _isFocused = value; propertyChanged("IsFocused"); propertyChanged("Background"); propertyChanged("BackgroundForOldLabel"); } }
        private bool _isFocused;

        public Brush Background
        {
            get
            {
                switch (_state)
                {
                    case TranslationInfoState.UpToDateAndSaved:
                        return _isFocused ? upToDateFocus : upToDateNormal;
                    case TranslationInfoState.OutOfDate:
                        return _isFocused ? outOfDateFocus : outOfDateNormal;
                    case TranslationInfoState.Unsaved:
                        return _isFocused ? unsavedFocus : unsavedNormal;
                    default:
                        throw new InvalidOperationException(@"Invalid value of State: " + _state);
                }
            }
        }
        public Brush BackgroundForOldLabel
        {
            get
            {
                switch (_state)
                {
                    case TranslationInfoState.UpToDateAndSaved:
                        return _isFocused ? upToDateOldFocus : upToDateOldNormal;
                    case TranslationInfoState.OutOfDate:
                        return _isFocused ? outOfDateOldFocus : outOfDateOldNormal;
                    case TranslationInfoState.Unsaved:
                        return _isFocused ? unsavedOldFocus : unsavedOldNormal;
                    default:
                        throw new InvalidOperationException(@"Invalid value of State: " + _state);
                }
            }
        }

        protected void propertyChanged(string name) { PropertyChanged(this, new PropertyChangedEventArgs(name)); }
        public event PropertyChangedEventHandler PropertyChanged = delegate { };
    }

    sealed class TrStringInfo : TranslationInfo
    {
        public string NewOriginal { get; set; }
        public TrString TranslationTr;

        public bool HasOriginalChanged
        {
            get { return NewOriginal == TranslationTr.Old; }
            set
            {
                if (value == false)
                    throw new ArgumentException(@"You can't set HasOriginalChanged to false.");
                TranslationTr.Old = NewOriginal;
            }
        }
        public string OldOriginal { get { return TranslationTr.Old; } }
        public string Translation { get { return TranslationTr.Translation; } set { TranslationTr.Translation = value; } }

        public Visibility OldVisible { get { return TranslationTr.Old == NewOriginal ? Visibility.Collapsed : Visibility.Visible; } }
        public string NewLabel { get { return OldVisible == Visibility.Visible ? "New Original:" : "Original:"; } }

        public TrStringInfo()
        {
            NewOriginal = "New original.";
            TranslationTr = new TrString { Old = "Old original.", Translation = "Традуктирование." };
        }
    }

    sealed class TrStringNumInfo : TranslationInfo
    {
        public string[] NewOriginal { get; set; }
        public TrStringNum TranslationTr;
        public NumberSystem OriginalNumSys;
        public NumberSystem TranslationNumSys;

        public bool HasOriginalChanged
        {
            get { return NewOriginal.SequenceEqual(TranslationTr.Old); }
            set
            {
                if (value == false)
                    throw new ArgumentException(@"You can't set HasOriginalChanged to false.");
                TranslationTr.Old = NewOriginal.ToArray();
            }
        }

        /// <summary>For XAML. Do not call.</summary>
        public TrStringNumInfo()
            : this(new TrStringNum(new[] { "1 move", "{0} moves" }), new TrStringNum(new[] { "1 шаг", "{0} шага", "{0} шагов" }, new[] { true }),
            Language.EnglishUK.GetNumberSystem(), Language.Russian.GetNumberSystem()) { }

        public TrStringNumInfo(TrStringNum orig, TrStringNum trans, NumberSystem origNumberSystem, NumberSystem transNumberSystem)
        {
            NewOriginal = orig.Translations;
            TranslationTr = trans;
            OriginalNumSys = origNumberSystem;
            TranslationNumSys = transNumberSystem;

            int numberOfNumbers = trans.IsNumber.Where(b => b).Count();
        }
    }

    /// <summary>Describes the state of a single translation (a <see cref="TrString"/> or <see cref="TrStringNum"/> instance).</summary>
    public enum TranslationInfoState
    {
        /// <summary>The string is up to date and has been saved to the translation file.</summary>
        UpToDateAndSaved,
        /// <summary>The string is out of date, i.e. the original text has changed since the translation was written (or the user has explicitly marked it as out-of-date).</summary>
        OutOfDate,
        /// <summary>The user has made changes to the string which have as yet not been saved to the translation file.</summary>
        Unsaved
    }
}
