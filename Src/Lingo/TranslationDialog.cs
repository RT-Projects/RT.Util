using System;
using RT.Util.ExtensionMethods;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Windows;
using System.Collections.ObjectModel;
using System.Reflection;

namespace RT.Util.Lingo
{
    public interface ITranslationDialog
    {
        bool AnyChanges { get; }
        void SaveChanges(bool fireTranslationChanged);
        void CloseWithoutPrompts();
    }

    static class TranslationDialogHelper
    {
        public static IEnumerable<TranslationGroup> GetGroups(Type type, TranslationBase original, TranslationBase translation)
        {
            var dic = new Dictionary<object, TranslationGroup>();
            TranslationGroup ungrouped = null;
            getGroups(null, type, original, translation, original.Language.GetNumberSystem(), translation.Language.GetNumberSystem(), dic, ref ungrouped, new object[0], "");
            foreach (var kvp in dic)
                yield return kvp.Value;
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
                            Label = path + f.Name, Notes = notes,
                            NewOriginal = ((TrString) f.GetValue(original)).Translation,
                            TranslationTr = (TrString) f.GetValue(translation)
                        }
                        : (TranslationInfo) new TrStringNumInfo
                        {
                            Label = path + f.Name, Notes = notes,
                            NewOriginal = ((TrStringNum) f.GetValue(original)).Translations,
                            TranslationTr = (TrStringNum) f.GetValue(translation),
                            OriginalNumSys = originalNumSys, TranslationNumSys = translationNumSys
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

        private ObservableCollection<TranslationInfo> _panels = new ObservableCollection<TranslationInfo>();
        public ObservableCollection<TranslationInfo> Infos { get { return _panels; } }

        private void propertyChanged(string name) { PropertyChanged(this, new PropertyChangedEventArgs(name)); }
        public event PropertyChangedEventHandler PropertyChanged = delegate { };
    }

    abstract class TranslationInfo : INotifyPropertyChanged
    {
        public string Label { get { return _label; } set { _label = value; propertyChanged("Label"); } }
        private string _label;
        public string Notes { get { return _notes; } set { _notes = value; propertyChanged("Notes"); } }
        private string _notes;

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
        public string[] OldOriginal { get { return TranslationTr.Old; } }
        public string[] Translation { get { return TranslationTr.Translations; } set { TranslationTr.Translations = value.ToArray(); } }
    }
}
