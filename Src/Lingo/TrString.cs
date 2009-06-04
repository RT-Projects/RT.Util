
namespace RT.Util.Lingo
{
    /// <summary>Represents a translatable string.</summary>
    public class TrString
    {
        /// <summary>Contains the current translation of this string, or for the original language, the current original text.</summary>
        public string Translation;

        /// <summary>Contains the original text this string was last translated from, or for the original language, the empty string.</summary>
        public string OldEnglish = "";

        /// <summary>Default constructor (required for XmlClassify).</summary>
        public TrString() { }
        /// <summary>Constructs a new translatable string given the specified translation.</summary>
        public TrString(string translation) { Translation = translation; }

        /// <summary>Implicit cast from string to TrString.</summary>
        public static implicit operator TrString(string translation)
        {
            return new TrString(translation);
        }

        /// <summary>Implicit cast from TrString to string.</summary>
        public static implicit operator string(TrString translatable)
        {
            return translatable.Translation;
        }

        /// <summary>Formats a string using <see cref="string.Format(string, object[])"/>.</summary>
        public string Fmt(params object[] args) { return string.Format(Translation, args); }

        /// <summary>Formats a string using <see cref="string.Format(string, object)"/>.</summary>
        public string Fmt(object arg0) { return string.Format(Translation, arg0); }

        /// <summary>Formats a string using <see cref="string.Format(string, object, object)"/>.</summary>
        public string Fmt(object arg0, object arg1) { return string.Format(Translation, arg0, arg1); }

        /// <summary>Formats a string using <see cref="string.Format(string, object, object, object)"/>.</summary>
        public string Fmt(object arg0, object arg1, object arg2) { return string.Format(Translation, arg0, arg1, arg2); }

        /// <summary>Returns the translation.</summary>
        /// <returns>The translation.</returns>
        public override string ToString() { return Translation; }
    }
}
