using RT.Util;

namespace RT.Lingo;

/// <summary>Specifies notes to the translator, detailing the purpose, context, or format of a translatable string.</summary>
[AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false), RummageKeepUsersReflectionSafe]
public sealed class LingoNotesAttribute : Attribute
{
    private readonly string _notes;

    /// <summary>
    ///     Constructor for a <see cref="LingoNotesAttribute"/> attribute.</summary>
    /// <param name="notes">
    ///     Specifies notes to the translator, detailing the purpose, context, or format of a translatable string.</param>
    public LingoNotesAttribute(string notes) { _notes = notes; }

    /// <summary>
    ///     Gets the associated notes to the translator, detailing the purpose, context, or format of a translatable string.</summary>
    public string Notes { get { return _notes; } }
}

/// <summary>Specifies that a translatable string is in a particular group.</summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Class, Inherited = false, AllowMultiple = true), RummageKeepUsersReflectionSafe]
public sealed class LingoInGroupAttribute : Attribute
{
    private readonly object _group;
    /// <summary>
    ///     Constructor for a <see cref="LingoInGroupAttribute"/> attribute.</summary>
    /// <param name="group">
    ///     Specifies that a translatable string is in a particular group.</param>
    public LingoInGroupAttribute(object group)
    {
        if (!(group is Enum))
            throw new InvalidOperationException(@"Only enum types are allowed on a [LingoInGroup] attribute.");
        _group = group;
    }

    /// <summary>Gets the group a translatable string is in.</summary>
    public object Group { get { return _group; } }
}

/// <summary>Specifies that a class is a class containing translatable strings.</summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false), RummageKeepUsersReflectionSafe]
public sealed class LingoStringClassAttribute : Attribute { }

/// <summary>
///     Marks a field in a translation class as one that does not represent a translatable string or a set of such strings.</summary>
[AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
public sealed class LingoIgnoreAttribute : Attribute { }

/// <summary>
///     Specifies information about a group of translatable strings. Use this on a field in an enum type which enumerates the
///     available groups of strings.</summary>
[AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false), RummageKeepUsersReflectionSafe]
public sealed class LingoGroupAttribute : Attribute
{
    private readonly string _groupName;
    private readonly string _description;

    /// <summary>
    ///     Constructor.</summary>
    /// <param name="groupName">
    ///     Specifies a short name for the group, to be used in the listbox in the translation window.</param>
    /// <param name="description">
    ///     Specifies a long description for the group, to be displayed above the list of string codes while the group is
    ///     selected.</param>
    public LingoGroupAttribute(string groupName, string description)
    {
        _groupName = groupName;
        _description = description;
    }

    /// <summary>Specifies a short name for the group, to be used in the listbox in the translation window.</summary>
    public string GroupName { get { return _groupName; } }
    /// <summary>
    ///     Specifies a long description for the group, to be displayed above the list of string codes while the group is
    ///     selected.</summary>
    public string Description { get { return _description; } }
}

/// <summary>
///     Code that automatically generates Lingo-compatible class declarations can add this attribute to indicate fields of
///     string classes that were generated automatically. <c>Lingo.TranslationFileGenerator.TranslateControl</c> generates
///     this, for example.</summary>
[AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
public sealed class LingoAutoGeneratedAttribute : Attribute
{
    /// <summary>Constructor.</summary>
    public LingoAutoGeneratedAttribute() { }
}

/// <summary>Provides information about a language. Used on the values in the <see cref="Language"/> enum.</summary>
[AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
public sealed class LanguageInfoAttribute : Attribute
{
    private readonly string _languageCode;
    private readonly string _englishName;
    private readonly string _nativeName;
    private readonly NumberSystem _numberSystem;

    /// <summary>
    ///     Constructor.</summary>
    /// <param name="languageCode">
    ///     Specifies the ISO-639 language code of the language.</param>
    /// <param name="englishName">
    ///     Specifies the English name of the language.</param>
    /// <param name="nativeName">
    ///     Specifies the native name of the language.</param>
    /// <param name="numberSystem">
    ///     Specifies the number system of the language.</param>
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
