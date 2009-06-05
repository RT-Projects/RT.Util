using System;

namespace RT.Util.Lingo
{
    /// <summary>
    /// Use this attribute on a field for a translatable string to specify notes to the translator, detailing the purpose, context, or format of a translatable string.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = true)]
    public sealed class LingoNotesAttribute : Attribute
    {
        /// <summary>Constructor for a <see cref="LingoNotesAttribute"/> attribute.</summary>
        /// <param name="notes">Specifies notes to the translator, detailing the purpose, context, or format of a translatable string.</param>
        public LingoNotesAttribute(string notes) { _notes = notes; }

        /// <summary>Specifies notes to the translator, detailing the purpose, context, or format of a translatable string.</summary>
        public string Notes { get { return _notes; } }
        private string _notes;
    }

    /// <summary>Use this attribute on a class containing translatable strings. Otherwise that class will not appear in the translation interface.</summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class LingoGroupAttribute : Attribute
    {
        /// <summary>Constructor for a <see cref="LingoGroupAttribute"/> attribute.</summary>
        /// <param name="label">Provides a label for the tree node that represents this translation strings class.</param>
        /// <param name="description">Describes the strings contained in this translation strings class.</param>
        public LingoGroupAttribute(string label, string description) { _label = label; _description = description; }

        /// <summary>Provides a label for the tree node that represents this translation strings class.</summary>
        public string Label { get { return _label; } }
        /// <summary>Describes the strings contained in this translation strings class.</summary>
        public string Description { get { return _description; } }

        private string _label;
        private string _description;
    }

#if DEBUG
    /// <summary>
    /// Use this attribute on a type that contains translations for a form. <see cref="Lingo.TranslateControl"/> will automatically add missing fields to the class.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class LingoDebugAttribute : Attribute
    {
        /// <summary>
        /// Specifies the relative path from the compiled assembly to the source file of the translation type.
        /// </summary>
        public string RelativePath { get; set; }
    }
#endif

    /// <summary>Provides information about a language. Used on the values in the <see cref="Language"/> enum.</summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    public sealed class LanguageInfoAttribute : Attribute
    {
        private readonly string _languageCode;
        private readonly string _englishName;
        private readonly string _nativeName;
        private readonly NumberSystem _numberSystem;

        /// <summary>Constructor.</summary>
        /// <param name="languageCode">Specifies the ISO-639 language code of the language.</param>
        /// <param name="englishName">Specifies the English name of the language.</param>
        /// <param name="nativeName">Specifies the native name of the language.</param>
        /// <param name="numberSystem">Specifies the number system of the language.</param>
        public LanguageInfoAttribute(string languageCode, string englishName, string nativeName, Type numberSystem)
        {
            _languageCode = languageCode;
            _englishName = englishName;
            _nativeName = nativeName;
            if (numberSystem == typeof(NumberSystem))
                _numberSystem = null;
            else if (typeof(NumberSystem).IsAssignableFrom(numberSystem))
                _numberSystem = (NumberSystem) numberSystem.GetConstructor(Type.EmptyTypes).Invoke(null);
            else
                _numberSystem = null;
        }

        /// <summary>Specifies the ISO-639 language code of the language.</summary>
        public string LanguageCode { get { return _languageCode; } }
        /// <summary>Specifies the English name of the language.</summary>
        public string EnglishName { get { return _englishName; } }
        /// <summary>Specifies the native name of the language.</summary>
        public string NativeName { get { return _nativeName; } }
        /// <summary>Specifies the number system of the language.</summary>
        public NumberSystem NumberSystem { get { return _numberSystem; } }
    }
}
