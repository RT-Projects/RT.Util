using RT.Serialization;
using RT.Util.Consoles;
using RT.Util.ExtensionMethods;

namespace RT.Lingo;

/// <summary>Represents a translatable string.</summary>
public sealed class TrString
{
    /// <summary>Contains the current translation of this string, or for the original language, the current original text.</summary>
    public string Translation;

    /// <summary>Contains the original text this string was last translated from, or for the original language, null.</summary>
    public string Old = null;

    /// <summary>Default constructor (required for Classify).</summary>
    public TrString() { }
    /// <summary>Constructs a new translatable string given the specified translation.</summary>
    public TrString(string translation) { Translation = translation; }

    /// <summary>Implicit cast from string to TrString.</summary>
    public static implicit operator TrString(string translation) { return translation == null ? null : new TrString(translation); }

    /// <summary>Implicit cast from TrString to string.</summary>
    public static implicit operator string(TrString translatable) { return translatable == null ? null : translatable.Translation; }

    /// <summary>Formats a string using <see cref="string.Format(string, object[])"/>.</summary>
    public string Fmt(params object[] args) { try { return Translation.Fmt(args); } catch { return Translation; } }

    /// <summary>Formats a string using <see cref="ConsoleExtensions.FmtEnumerable(string,object[])"/>.</summary>
    public IEnumerable<object> FmtEnumerable(params object[] args) { return Translation.FmtEnumerable(args); }

    /// <summary>
    ///     Returns the translation.</summary>
    /// <returns>
    ///     The translation.</returns>
    public override string ToString() { return Translation; }

    /// <summary>
    ///     Returns the translation in the specified console color.</summary>
    /// <param name="foreground">
    ///     The foreground color in which to color the translation, or <c>null</c> to use the console’s default foreground
    ///     color.</param>
    /// <param name="background">
    ///     The background color in which to color the translation, or <c>null</c> to use the console’s default background
    ///     color.</param>
    /// <returns>
    ///     A potentially colourful string.</returns>
    public ConsoleColoredString Color(ConsoleColor? foreground, ConsoleColor? background = null) { return new ConsoleColoredString(Translation, foreground, background); }
}

/// <summary>
///     Represents a translatable string into which numbers are interpolated, requiring grammatical morphology according to a
///     language's <see cref="NumberSystem"/>.</summary>
public sealed class TrStringNum
{
    /// <summary>Specifies which of the interpolated objects are integers.</summary>
    public bool[] IsNumber;

    /// <summary>Contains the current translation of this string, or for the original language, the current original text.</summary>
    public string[] Translations;

    /// <summary>Contains the original text this string was last translated from. Null for the original language.</summary>
    public string[] Old = null;

    /// <summary>Default constructor (required for Classify).</summary>
    public TrStringNum() { Translations = new string[0]; IsNumber = new[] { true }; }

    /// <summary>
    ///     Constructs a new translatable string with the specified translations.</summary>
    /// <param name="translations">
    ///     Specifies the translations for this string. The number of elements is expected to be equal to the number of
    ///     strings as defined by your native language's NumberSystem, multiplied by the number of elements in <paramref
    ///     name="isNumber"/> that are true.</param>
    /// <param name="isNumber">
    ///     Specifies which of the interpolated variables are integers.</param>
    /// <example>
    ///     <para>
    ///         The following example code demonstrates how to instantiate a string that interpolates both a string (file
    ///         name) and an integer (number of bytes) correctly.</para>
    ///     <code>
    ///         TrStringNum MyString = new TrStringNum(
    ///             new[] { "The file {0} contains {1} byte.", "The file {0} contains {1} bytes." },
    ///             new[] { false, true }
    ///         );</code></example>
    public TrStringNum(string[] translations, bool[] isNumber) { Translations = translations; IsNumber = isNumber; }

    /// <summary>
    ///     Constructs a new translatable string with one interpolated integer and no other interpolated arguments, and the
    ///     specified translations.</summary>
    public TrStringNum(params string[] translations) { Translations = translations; IsNumber = new[] { true }; }

    /// <summary>
    ///     Selects the correct string and interpolates the specified arguments.</summary>
    /// <param name="tr">
    ///     Current translation. Its language’s number system will be used to interpolate the translation.</param>
    /// <param name="args">
    ///     Arguments to be interpolated into the translation.</param>
    public string Fmt(TranslationBase tr, params object[] args)
    {
        return Fmt(tr.Language.GetNumberSystem(), args);
    }

    /// <summary>
    ///     Selects the correct string and interpolates the specified arguments.</summary>
    /// <param name="lang">
    ///     Current translation’s language. Its number system will be used to interpolate the translation.</param>
    /// <param name="args">
    ///     Arguments to be interpolated into the translation.</param>
    public string Fmt(Language lang, params object[] args)
    {
        return Fmt(lang.GetNumberSystem(), args);
    }

    /// <summary>
    ///     Selects the correct string and interpolates the specified arguments.</summary>
    /// <param name="ns">
    ///     Number system to use to interpolate the translation.</param>
    /// <param name="args">
    ///     Arguments to be interpolated into the translation.</param>
    public string Fmt(NumberSystem ns, params object[] args)
    {
        try
        {
            int n = 0;
            int m = 1;
            for (int i = 0; i < IsNumber.Length; i++)
            {
                if (IsNumber[i])
                {
                    double numD = 0;
                    int numI;
                    bool isInteger;
                    if (args[i] is double || args[i] is float || args[i] is decimal)
                    {
                        numD = ExactConvert.ToDouble(args[i]);
                        numI = unchecked((int) numD);
                        isInteger = numD == (double) numI;
                    }
                    else if (args[i] is int || args[i] is uint || args[i] is long || args[i] is ulong || args[i] is short || args[i] is ushort || args[i] is byte || args[i] is sbyte)
                    {
                        numI = ExactConvert.ToInt(args[i]);
                        isInteger = true;
                    }
                    else
                        throw new ArgumentException("Argument #{0} was expected to be a number, but a {1} was given.".Fmt(i, args[i].GetType().FullName), "args");

                    if (isInteger)
                        n += ns.GetString(numI) * m;
                    else
                        n += ns.GetString(numD) * m;

                    m *= ns.NumStrings;
                }
            }
            return Translations[n].Fmt(args);
        }
        catch
        {
            if (Translations != null && Translations.Length > 0)
                return Translations[0];
            else
                return "(NO STRING)";
        }
    }
}
